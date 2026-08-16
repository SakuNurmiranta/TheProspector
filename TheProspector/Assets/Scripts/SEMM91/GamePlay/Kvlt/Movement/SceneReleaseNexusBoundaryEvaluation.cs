using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Immutable phase-aware interpretation of one
    /// current Field SceneRelease against the Nexus
    /// boundary.
    ///
    /// Reaching the Nexus is necessary but not
    /// sufficient for Canon candidacy. The release
    /// must also carry at least one qualifying realized
    /// Canon Breakthrough claim.
    ///
    /// The evaluation records whether the breakthrough
    /// came from the legacy pre-movement screening or
    /// the Peak-2 post-Happening year-end screening.
    /// </summary>
    public sealed class
        SceneReleaseNexusBoundaryEvaluation
    {
        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int SettledTurn { get; }

        public float NexusBoundary { get; }

        public float FieldPosition { get; }

        public float SignedDistanceFromNexusBoundary
        {
            get;
        }

        public bool HasReachedNexusBoundary { get; }

        public SceneReleaseCanonBreakthroughEvaluation
            BreakthroughEvaluation { get; }

        public IReadOnlyList<
                SceneReleaseCanonBreakthroughClaim>
            QualifyingBreakthroughClaims =>
            BreakthroughEvaluation.QualifyingClaims;

        public bool HasQualifyingBreakthrough =>
            BreakthroughEvaluation
                .HasQualifyingBreakthrough;

        public SceneReleaseCanonBreakthroughEvaluationPhase
            BreakthroughEvaluationPhase { get; }

        public bool RequiresSameTurnMovement =>
            BreakthroughEvaluationPhase ==
            SceneReleaseCanonBreakthroughEvaluationPhase
                .PreMovement;

        public bool IsPostHappeningScreening =>
            BreakthroughEvaluationPhase ==
            SceneReleaseCanonBreakthroughEvaluationPhase
                .PostHappening;

        public SceneReleaseNexusBoundaryDisposition
            Disposition { get; }

        public bool IsCanonCandidate =>
            Disposition ==
            SceneReleaseNexusBoundaryDisposition
                .CanonCandidate;

        public SceneReleaseNexusBoundaryEvaluation(
            SceneReleaseCanonBreakthroughEvaluation
                breakthroughEvaluation,
            float nexusBoundary,
            float fieldPosition,
            SceneReleaseNexusBoundaryDisposition
                disposition)
            : this(
                breakthroughEvaluation,
                nexusBoundary,
                fieldPosition,
                disposition,
                SceneReleaseCanonBreakthroughEvaluationPhase
                    .PreMovement
            )
        {
        }

        public SceneReleaseNexusBoundaryEvaluation(
            SceneReleaseCanonBreakthroughEvaluation
                breakthroughEvaluation,
            float nexusBoundary,
            float fieldPosition,
            SceneReleaseNexusBoundaryDisposition
                disposition,
            SceneReleaseCanonBreakthroughEvaluationPhase
                breakthroughEvaluationPhase)
        {
            BreakthroughEvaluation =
                breakthroughEvaluation ??
                throw new ArgumentNullException(
                    nameof(breakthroughEvaluation)
                );

            if (!IsFinite(nexusBoundary))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nexusBoundary)
                );
            }

            if (!IsFinite(fieldPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fieldPosition)
                );
            }

            if (!Enum.IsDefined(
                    typeof(
                        SceneReleaseNexusBoundaryDisposition
                    ),
                    disposition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(disposition)
                );
            }

            if (!Enum.IsDefined(
                    typeof(
                        SceneReleaseCanonBreakthroughEvaluationPhase
                    ),
                    breakthroughEvaluationPhase))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(breakthroughEvaluationPhase)
                );
            }

            bool hasReached =
                fieldPosition >=
                nexusBoundary;

            bool shouldBeCandidate =
                hasReached &&
                breakthroughEvaluation
                    .HasQualifyingBreakthrough;

            if (shouldBeCandidate !=
                (
                    disposition ==
                    SceneReleaseNexusBoundaryDisposition
                        .CanonCandidate
                ))
            {
                throw new ArgumentException(
                    "Nexus disposition does not match " +
                    "Field Position and qualifying " +
                    "Canon Breakthrough state.",
                    nameof(disposition)
                );
            }

            SceneReleaseId =
                breakthroughEvaluation
                    .SceneReleaseId;

            SourceDemoTapeId =
                breakthroughEvaluation
                    .SourceDemoTapeId;

            SourceOwnerEntityId =
                breakthroughEvaluation
                    .SourceOwnerEntityId;

            SceneId =
                breakthroughEvaluation.SceneId;

            SettledTurn =
                breakthroughEvaluation.SettledTurn;

            NexusBoundary =
                nexusBoundary;

            FieldPosition =
                fieldPosition;

            SignedDistanceFromNexusBoundary =
                fieldPosition -
                nexusBoundary;

            HasReachedNexusBoundary =
                hasReached;

            Disposition =
                disposition;

            BreakthroughEvaluationPhase =
                breakthroughEvaluationPhase;
        }

        private static bool IsFinite(
            float value)
        {
            return
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}