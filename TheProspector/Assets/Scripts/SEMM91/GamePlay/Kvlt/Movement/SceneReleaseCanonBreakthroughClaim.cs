using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Frozen Canon-novelty interpretation of one
    /// formal recorded Tag-pair.
    ///
    /// Potential novelty describes what the recording
    /// claims above Canon.
    ///
    /// Realized breakthrough describes only what has
    /// become legitimate current active TRVE above
    /// Canon.
    /// </summary>
    public sealed class
        SceneReleaseCanonBreakthroughClaim
    {
        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public string SourceTrackId { get; }

        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public int SettledTurn { get; }

        public TagAxis DominantAxis { get; }

        public TagPole DominantPole { get; }

        public TagDegree RecordedDominantDegree { get; }

        public TagDegree ActiveDominantDegree { get; }

        public bool HasCanonicalPrecedent { get; }

        public TagDegree CanonicalDominantDegree { get; }

        public TagAxis SubmissiveAxis { get; }

        public TagPole SubmissivePole { get; }

        public TagDegree RecordedSubmissiveDegree { get; }

        public bool IsCurrentlyTrve { get; }

        public int PotentialNoveltyDifferential { get; }

        public int RealizedBreakthroughDifferential { get; }

        public bool HasPotentialNovelty =>
            PotentialNoveltyDifferential > 0;

        public bool IsQualifyingBreakthrough =>
            RealizedBreakthroughDifferential > 0;

        public SceneReleaseCanonBreakthroughClaim(
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex,
            int settledTurn,
            TagAxis dominantAxis,
            TagPole dominantPole,
            TagDegree recordedDominantDegree,
            TagDegree activeDominantDegree,
            bool hasCanonicalPrecedent,
            TagDegree canonicalDominantDegree,
            TagAxis submissiveAxis,
            TagPole submissivePole,
            TagDegree recordedSubmissiveDegree,
            bool isCurrentlyTrve)
        {
            SceneReleaseId =
                RequireText(
                    sceneReleaseId,
                    nameof(sceneReleaseId)
                );

            SourceDemoTapeId =
                RequireText(
                    sourceDemoTapeId,
                    nameof(sourceDemoTapeId)
                );

            SourceOwnerEntityId =
                RequireText(
                    sourceOwnerEntityId,
                    nameof(sourceOwnerEntityId)
                );

            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

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

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            ValidateDegree(
                recordedDominantDegree,
                nameof(recordedDominantDegree)
            );

            ValidateDegree(
                activeDominantDegree,
                nameof(activeDominantDegree)
            );

            ValidateDegree(
                canonicalDominantDegree,
                nameof(canonicalDominantDegree)
            );

            ValidateDegree(
                recordedSubmissiveDegree,
                nameof(recordedSubmissiveDegree)
            );

            if (recordedDominantDegree ==
                TagDegree.Neutral)
            {
                throw new ArgumentException(
                    "Formal breakthrough claim requires " +
                    "recorded dominant degree >= 1.",
                    nameof(recordedDominantDegree)
                );
            }

            if ((int)activeDominantDegree >
                (int)recordedDominantDegree)
            {
                throw new ArgumentException(
                    "Active dominant degree cannot exceed " +
                    "recorded dominant degree.",
                    nameof(activeDominantDegree)
                );
            }

            if (isCurrentlyTrve &&
                activeDominantDegree ==
                    TagDegree.Neutral)
            {
                throw new ArgumentException(
                    "Current active TRVE claim cannot " +
                    "have zero activation.",
                    nameof(activeDominantDegree)
                );
            }

            IdeaIndex =
                ideaIndex;

            SettledTurn =
                settledTurn;

            DominantAxis =
                dominantAxis;

            DominantPole =
                dominantPole;

            RecordedDominantDegree =
                recordedDominantDegree;

            ActiveDominantDegree =
                activeDominantDegree;

            HasCanonicalPrecedent =
                hasCanonicalPrecedent;

            CanonicalDominantDegree =
                canonicalDominantDegree;

            SubmissiveAxis =
                submissiveAxis;

            SubmissivePole =
                submissivePole;

            RecordedSubmissiveDegree =
                recordedSubmissiveDegree;

            IsCurrentlyTrve =
                isCurrentlyTrve;

            PotentialNoveltyDifferential =
                Math.Max(
                    0,
                    (int)recordedDominantDegree -
                    (int)canonicalDominantDegree
                );

            /*
             * Activation alone is insufficient.
             *
             * The claim must still be current active
             * TRVE under the frozen scene evaluation.
             */
            RealizedBreakthroughDifferential =
                isCurrentlyTrve
                    ? Math.Max(
                        0,
                        (int)activeDominantDegree -
                        (int)canonicalDominantDegree
                    )
                    : 0;
        }

        private static void ValidateDegree(
            TagDegree degree,
            string parameterName)
        {
            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    degree))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Canon breakthrough provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}