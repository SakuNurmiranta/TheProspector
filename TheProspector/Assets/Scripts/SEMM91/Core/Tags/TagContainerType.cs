namespace SEMM91.Core.Tags
{
    public enum TagContainerType
    {
        Resonance, // changes based on long term surroundings (shared with the active collective)
        Mood, // changes based on recent events (simulacra on third usage); Also a mood tag can only be added to ones own aspect - you cannot make somebody else to sing the way you feel.
        Conviction, // always available but proselytizing
        Transient // For temporary tags, "work memory of tags"
    }
}
