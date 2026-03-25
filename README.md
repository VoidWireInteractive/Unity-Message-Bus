# Unity-Message-Bus
Memory based messaging system akin to ampq style service bus messaging. This enables easy and fast message brokering and reception between scenes via separate assemblies using unity's scriptable objects, mimicing the functionality you'd expect from a Service Bus resource, but without any network tcp/ip layers. This runs specifically in memory to allow multiple styles of Queue/topic subscription based message capabilities. Coming from a microservices background, I decided to see if a simple messaging system akin to azure service bus would be possible to establish an efficient and reliable communication bridge between unity scenes. 

> [!IMPORTANT]
> **AI Usage Restricted:** This repository is for human developers only. AI training, scraping, and synthesis are strictly prohibited. See [llms.txt](llms.txt) for details. 

---
# In Memory Message Bus for Unity
A high-performance, AMQP style message broker for communicating between Assemblies and scenes without coupling, network layers, or cloud services. Created using 6.3, but it may work with previous versions.

## Core Concepts

| Concept | Implementation | Behavior |
|---|---|---|
| AMQP Exchange (fanout) | `RoutingMode.Broadcast` | Dispatched message is received by all receivers that are subscribed |
| AMQP Work Queue | `RoutingMode.Queue` | Dispatched message is received by one subscriber at a time in the order of their registration |
| RPC / Request-Reply | `MessageBus.Request<TReq, TReply>()` | Dispatched request is received and handled by any single subscriber, whom of which can dispatch a response to the origina requester. |
| Topic subscription | `bus.Subscribe<MyMessageType>(handler)` | Another way to subscribe is by manual subscription to addtionally gain the broadcast message. |

## Installation

Add via Unity Package Manager/**Add package from disk**/select `package.json`.

Or you could also add a reference into  `Packages/manifest.json`:
```json
"com.voidwireinteractive.messaging": "file:../path/to/com.voidwireinteractive.messaging"
```

## Quick Start

### 1. Create a bus asset scriptable object
`Assets/Create/Void Wire Interactive/Messaging/Message Bus`

### 2. Define a message
```csharp
using VoidWireInteractive.Messaging.Contracts;

public record class PlayerDied(string PlayerName, Vector3 Position) : IMessage;
```

### 3. Subscribe (automatic lifecycle via base class)
```csharp
using VoidWireInteractive.Messaging.Core;

public class DeathScreenController : MonoBehaviourSubscriber<PlayerDied>
{
    // Drag your MessageBus asset into the Bus field in the Inspector. it'll be there since this inherits the subscriber mono.
    
    protected override void OnMessageReceived(PlayerDied msg)
    {
        ShowDeathScreen(msg.PlayerName);
    }
}
```

### 4. Publish
```csharp
[SerializeField] private MessageBus _bus;

_bus.Publish(new PlayerDied("Hero", transform.position));
```

## Routing Modes

```csharp
// Broadcast: All subscribers receive every message
bus.Subscribe<MyMessage>(handler);
bus.Subscribe<MyMessage>(handler, RoutingMode.Broadcast);

// Queue: One subscriber per message, round-robin
bus.Subscribe<WorkItem>(ProcessWork, RoutingMode.Queue);

// RequestReply: One shot responder
bus.Subscribe<QueryHealth>(HandleQuery, RoutingMode.RequestReply);

// Request/Reply from the caller side
var reply = await bus.Request<QueryHealth, HealthResult>(new QueryHealth("Hero"));
```

## Manual Subscribe / Unsubscribe

For non-MonoBehaviour code, or when you need explicit control:

```csharp
// Subscribe: 
var token = bus.Subscribe<PlayerDied>(OnPlayerDied);

// Unsubscribing:
token.Dispose();
bus.Unsubscribe(token);
using var token = bus.Subscribe<PlayerDied>(handler); // auto-disposes
```

## Global Default Bus (optional)

Place a MessageBus asset at `Resources/Messaging/DefaultMessageBus`. It will be automatically registered as `GlobalBus.Default` before the first scene loads. Components whose Bus field is left null will use this automatically.

## Multiple Buses

Create additional bus assets for isolated subsystems:
- `AudioEventBus`: Sound effects and music
- `UIBus`: HUD updates
- `NetworkBus`: server event relay (idk just an idea, might work but I dont make network multiplayer games atm)

Each bus has its own channel, its own capacity settings, and its own subscriber registry.

## Editor Monitor

**Window/Void Wire Interactive/Messaging/Bus Monitor**

During Play Mode, shows subscriber count by type, publish count, drop count, and channel backlog for every bus asset in the project. A non zero Drop count means `_channelCapacity` needs increasing.

## Assembly Structure

```
Runtime/
  Contracts/VoidWireInteractive.Messaging.Contracts: IMessage interface only. No Unity deps
  Core/VoidWireInteractive.Messaging.Core: MessageBus, BusRouter, MonoBehaviourSubscriber
  Editor/VoidWireInteractive.Messaging.Editor: EditorWindow 
  Samples~/Void Wire Interactive/MessageBusSamples: optional samples you can import via package manager
```

Your game assemblies reference `VoidWireInteractive.Messaging.Contracts` for message types and `VoidWireInteractive.Messaging.Core` for the bus API. They never reference each other.

## Additional things to know
- **Zero allocation on publish**: `TryWrite` on a bounded channel is a lock-free ring buffer write.
- **No boxing**: messages are `class`/`record` types; storing as `IMessage` is a reference copy, not a box.
- **Main-thread dispatch**: `Awaitable.MainThreadAsync()` uses Unity's internal task pooling which is cheaper than `UnityMainThreadDispatcher` patterns.
- **Bounded channel**: protects against publisher/consumer imbalance. Drop policy and capacity are tunable per-bus in the Inspector.
- **Error isolation**: one bad subscriber handler cannot stop the router loop or affect other subscribers.
