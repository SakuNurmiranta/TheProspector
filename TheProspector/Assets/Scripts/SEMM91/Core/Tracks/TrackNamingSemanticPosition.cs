using SEMM91.Core.Tags;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Immutable naming-side interpretation of one concrete
    /// Tag occurrence inside one Idea.
    ///
    /// This is not authoritative track data. It retains the
    /// mechanical degree while allowing naming-only degree
    /// reinforcement through copied instances.
    /// </summary>
    public sealed class TrackNamingSemanticPosition
    {
        public string SourceIdeaId { get; }
        public string AspectId { get; }

        public int IdeaIndex { get; }

        public TrackNamingSemanticRole Role { get; }

        public TagAxis Axis { get; }
        public TagPole Pole { get; }

        public TagDegree MechanicalDegree { get; }

        public TagDegree NamingEffectiveDegree { get; }

        public bool IsPaired =>
            Role != TrackNamingSemanticRole.Solitary;

        public TrackNamingSemanticPosition(
            string sourceIdeaId,
            string aspectId,
            int ideaIndex,
            TrackNamingSemanticRole role,
            TagAxis axis,
            TagPole pole,
            TagDegree mechanicalDegree)
            : this(
                sourceIdeaId,
                aspectId,
                ideaIndex,
                role,
                axis,
                pole,
                mechanicalDegree,
                mechanicalDegree
            )
        {
        }

        private TrackNamingSemanticPosition(
            string sourceIdeaId,
            string aspectId,
            int ideaIndex,
            TrackNamingSemanticRole role,
            TagAxis axis,
            TagPole pole,
            TagDegree mechanicalDegree,
            TagDegree namingEffectiveDegree)
        {
            SourceIdeaId = sourceIdeaId;
            AspectId = aspectId;
            IdeaIndex = ideaIndex;
            Role = role;
            Axis = axis;
            Pole = pole;
            MechanicalDegree = mechanicalDegree;
            NamingEffectiveDegree =
                namingEffectiveDegree;
        }

        public TrackNamingSemanticPosition
            WithNamingEffectiveDegree(
                TagDegree namingEffectiveDegree)
        {
            return new TrackNamingSemanticPosition(
                SourceIdeaId,
                AspectId,
                IdeaIndex,
                Role,
                Axis,
                Pole,
                MechanicalDegree,
                namingEffectiveDegree
            );
        }
    }
}