using VoidWireInteractive.Messaging.Contracts;

namespace VoidWireInteractive.Messaging.Samples.BasicExample
{
    /// <summary>
    /// Fired when a player gains a level.
    ///
    /// FYI, This was written as a sealed class intentionally simply for C# version compatibility in the samples. <br/><br/>In any real project, Id suggest a record class, which gives you gives more compact structure. Like the following record
    ///<code>
    ///   public record class PlayerLevelUp(string PlayerName, int OldLevel, int NewLevel, float ExperienceGained) : IMessage; 
    ///   </code>
    /// Both forms work identically with the message bus.
    /// </summary>
    public sealed class PlayerLevelUp : IMessage
    {
        public string PlayerName { get; }
        public int OldLevel { get; }
        public int NewLevel { get; }
        public float ExperienceGained { get; }

        public PlayerLevelUp(string playerName, int oldLevel, int newLevel, float experienceGained)
        {
            PlayerName = playerName;
            OldLevel = oldLevel;
            NewLevel = newLevel;
            ExperienceGained = experienceGained;
        }
    }

    /// <summary>
    /// Request message: ask the health system for a players current hp. Pair with <see cref="PlayerHealthResult"/> in a request/reply flow.
    /// </summary>
    public sealed class QueryPlayerHealth : IMessage
    {
        public string PlayerName { get; }

        public QueryPlayerHealth(string playerName)
        {
            PlayerName = playerName;
        }
    }

    /// <summary>
    /// Reply message: the health system response to a <see cref="QueryPlayerHealth"/> request.
    /// </summary>
    public sealed class PlayerHealthResult : IMessage
    {
        public string PlayerName { get; }
        public float CurrentHp { get; }
        public float MaxHp { get; }

        public PlayerHealthResult(string playerName, float currentHp, float maxHp)
        {
            PlayerName = playerName;
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }
    }
}
