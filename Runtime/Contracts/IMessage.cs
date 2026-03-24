namespace VoidWireInteractive.Messaging.Contracts
{
    /// <summary>
    /// Marker interface that all message types must implement. Use <code> public record class MyMessage(MyBlah blah) : IMessage</code> for immutable messages.
    /// Using a class or record class (not struct!) avoids boxing when stored in the channel.
    /// </summary>
    public interface IMessage { }
}
