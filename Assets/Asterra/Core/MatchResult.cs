namespace Asterra.Core
{
    public enum MatchEndReason : byte
    {
        None = 0,
        KeepDestroyed = 1,
        TerritoryHeld = 2,
        ObjectivesComplete = 3,
        ObjectiveFailed = 4,
    }

    public readonly struct MatchResult
    {
        public readonly bool IsOver;
        public readonly PlayerId Winner;
        public readonly MatchEndReason Reason;

        public MatchResult(bool isOver, PlayerId winner, MatchEndReason reason)
        {
            IsOver = isOver;
            Winner = winner;
            Reason = reason;
        }

        public static MatchResult None => new MatchResult(false, default, MatchEndReason.None);
    }
}
