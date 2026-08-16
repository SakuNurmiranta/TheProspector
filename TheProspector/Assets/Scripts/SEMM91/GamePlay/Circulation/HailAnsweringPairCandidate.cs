using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class HailAnsweringPairCandidate
    {
        public string SourceTrackId { get; }

        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public string HostingAspectId { get; }

        public TagAxis DominantAxis { get; }

        public TagPole DominantPole { get; }

        public TagDegree
            RecordedDominantDegree { get; }

        public HailAnsweringPairCandidate(
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex,
            string hostingAspectId,
            TagAxis dominantAxis,
            TagPole dominantPole,
            TagDegree recordedDominantDegree)
        {
            SourceTrackId =
                RequireText(
                    sourceTrackId,
                    nameof(sourceTrackId)
                );

            SourceIdeaId =
                RequireText(
                    sourceIdeaId,
                    nameof(sourceIdeaId)
                );

            if (ideaIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ideaIndex)
                );
            }

            HostingAspectId =
                RequireText(
                    hostingAspectId,
                    nameof(hostingAspectId)
                );

            if (!Enum.IsDefined(
                    typeof(TagAxis),
                    dominantAxis))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dominantAxis)
                );
            }

            if (!Enum.IsDefined(
                    typeof(TagPole),
                    dominantPole))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dominantPole)
                );
            }

            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    recordedDominantDegree))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(recordedDominantDegree)
                );
            }

            if (recordedDominantDegree ==
                TagDegree.Neutral)
            {
                throw new ArgumentException(
                    "A Hail-answering pair requires " +
                    "dominant degree >= 1.",
                    nameof(recordedDominantDegree)
                );
            }

            IdeaIndex =
                ideaIndex;

            DominantAxis =
                dominantAxis;

            DominantPole =
                dominantPole;

            RecordedDominantDegree =
                recordedDominantDegree;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Hail candidate provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}