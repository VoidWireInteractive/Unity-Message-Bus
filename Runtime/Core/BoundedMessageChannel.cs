using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using VoidWireInteractive.Messaging.Contracts;

namespace VoidWireInteractive.Messaging.Core
{
    /// <summary>
    /// Controls behaviour when a message is published to a full channel. Replaces System.Threading.Channels.BoundedChannelFullMode, which is not reliably available in all Unity configurations.<br/>
    /// - DropWrite: Silently discard the incoming message. The publisher never blocks. a drop counter in GetStats() will reveal if this is happening.<br/>
    /// - DropOldes: Discard the oldest buffered message to make room for the new one. this should be preferred for telemetry or ui events where recency matters more than completeness
    /// </summary>
    public enum ChannelFullMode
    {
        DropWrite,
        DropOldest
    }

    /// <summary>
    /// A thread safe, bounded, single reader message queue built on ConcurrentQueue and SemaphoreSlim. Provides the same contract as System.Threading.Channels.Channel without requiring that assembly since Unity does not ship.
    ///<br/><br/>
    /// - TryWrite is lock free for the common path via ConcurrentQueue.Enqueue. ** non full path **<br/>
    /// - WaitToReadAsync returns a Task that completes as soon as at least one message is available, using SemaphoreSlim as the signal.<br/>
    /// - Complete() wakes the waiting reader so the router loop can exit cleanly.<br/>
    /// - Count is an Interlocked counter kept in sync with the queue. Its accurate enough for capacity enforcement and telemetry<br/>
    /// </summary>
    internal sealed class BoundedMessageChannel
    {
        private readonly ConcurrentQueue<IMessage> _queue = new();
        private readonly SemaphoreSlim _gate;
        private readonly int _capacity;
        private readonly ChannelFullMode _fullMode;

        private int  _count;      // syncd via interlocked
        private bool _completed;  // set by Complete metthod. no lock needed as its written once

        internal int  Count       => _queue.Count;
        internal bool IsCompleted => _completed && _queue.IsEmpty;

        internal BoundedMessageChannel(int capacity, ChannelFullMode fullMode)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be > 0.");
            _capacity = capacity;
            _fullMode = fullMode;
            _gate     = new SemaphoreSlim(0, int.MaxValue);
        }

        /// <summary>
        /// Attempt to enqueue a message. Returns true on success, false if the message was dropped. channel full and FullMode==DropWrite, or channel completed. non blocker 
        /// </summary>
        internal bool TryWrite(IMessage message)
        {
            if (_completed) return false;

            // Enforce capacity.
            int current = Interlocked.CompareExchange(ref _count, 0, 0);
            if (current >= _capacity)
            {
                if (_fullMode == ChannelFullMode.DropWrite)
                    return false;

                // for the DropOldest selection, dequeues one message to make room.
                if (_queue.TryDequeue(out _))
                    Interlocked.Decrement(ref _count);
                // Do NOT release the gate here. we removed one slot but havent added yet.
            }

            _queue.Enqueue(message);
            Interlocked.Increment(ref _count);
            _gate.Release(); // signal the reader that a message is available
            return true;
        }

        /// <summary>
        /// Try to dequeue a message without waiting. Returns false if the queue is empty.
        /// </summary>
        internal bool TryRead(out IMessage message)
        {
            if (_queue.TryDequeue(out message))
            {
                Interlocked.Decrement(ref _count);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Asynchronously wait until at least one message is available or the channel is completed/cancelled. Throws OperationCanceledException if the token fires. The caller should check IsCompleted and drain remaining messages after this returns.
        /// </summary>
        internal async Task WaitToReadAsync(CancellationToken ct)
        {
            // Fast path. message already waiting.
            if (!_queue.IsEmpty) return;
            if (_completed)      return;

            // Block until trywrite releases the gate or Complete() wakes it.
            await _gate.WaitAsync(ct); // throw if cancelld
        }

        /// <summary>
        /// Signal that no more messages will be written. Wakes the reader so the router loop can drain remaining messages and exit cleanly.
        /// </summary>
        internal void Complete()
        {
            _completed = true;
            _gate.Release(); // unblock WaitToReadAsync so the router can observe IsCompleted
        }
    }
}
