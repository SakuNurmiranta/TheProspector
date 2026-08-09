using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TagSensationalExtremityEvaluation
    {
        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public int OccurrenceIndex { get; }

        public DemoTapeTagOccurrenceRole SourceRole
        {
            get;
        }

        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree RecordedDegree { get; }

        public bool IsSocietyTransgressive { get; }

        public int CandidateMagnitude { get; }

        public TagSensationalExtremityEvaluation(
            string sourceIdeaId,
            int ideaIndex,
            int occurrenceIndex,
            DemoTapeTagOccurrenceRole sourceRole,
            TagAxis axis,
            TagPole pole,
            TagDegree recordedDegree,
            bool isSocietyTransgressive)
        {
            SourceIdeaId =
                sourceIdeaId;

            IdeaIndex =
                ideaIndex;

            OccurrenceIndex =
                occurrenceIndex;

            SourceRole =
                sourceRole;

            Axis =
                axis;

            Pole =
                pole;

            RecordedDegree =
                recordedDegree;

            IsSocietyTransgressive =
                isSocietyTransgressive;

            CandidateMagnitude =
                isSocietyTransgressive
                    ? (int)recordedDegree
                    : 0;
        }
    }
}