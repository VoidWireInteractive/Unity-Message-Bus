namespace VoidWireInteractive.Messaging.Core
{
    /// <summary>
    /// Controls how a published message is delivered to its subscribers. <br/>
    /// - Broadcast: Every subscriber receives a copy of every message. Default AMQP/service bus style topic/fanout behavior.<br/>
    /// - Queue: Only one subscriber receives each message, rotating round robin across all Queue subscribers for that message type. Mirrors AMQP/service bus work queue / competing consumers behavior.<br/>
    /// - RequestReply: Designates this subscriber as the sole responder for a request type. Used in conjunction with MessageBus.Request<TRequest, TReply>. Only the first registered RequestReply
    /// handler is invoked per message and the subscription auto removes itself after one invocation.<br/>
    /// </summary>
    public enum RoutingMode
    {
        Broadcast,
        Queue,
        RequestReply
    }
}
