using System;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Immutable provenance-bearing initial Field
    /// placement evaluation for one newly fettered
    /// SceneRelease.
    /// </summary>
    public sealed class SceneReleaseIngressEvaluation
    {
        public string SceneReleaseId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int PlacementTurn { get; }

        public bool HasStandingBasis { get; }

        public int? StandingBasisTurn { get; }

        public float? StandingBasisPosition { get; }

        public float NoStandingEntryPosition { get; }

        public float InnerFieldEntryCeiling { get; }

        public float UncappedPosition { get; }

        public float AppliedInitialPosition { get; }

        public bool WasCapped { get; }

        public SceneReleaseIngressEvaluation(
            string sceneReleaseId,
            string sourceOwnerEntityId,
            string sceneId,
            int placementTurn,
            bool hasStandingBasis,
            int? standingBasisTurn,
            float? standingBasisPosition,
            float noStandingEntryPosition,
            float innerFieldEntryCeiling,
            float uncappedPosition,
            float appliedInitialPosition)
        {
            SceneReleaseId =
                RequireText(
                    sceneReleaseId,
                    nameof(sceneReleaseId)
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

            if (placementTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(placementTurn)
                );
            }

            if (!IsFinite(noStandingEntryPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(noStandingEntryPosition)
                );
            }

            if (!IsFinite(innerFieldEntryCeiling))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(innerFieldEntryCeiling)
                );
            }

            if (!IsFinite(uncappedPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(uncappedPosition)
                );
            }

            if (!IsFinite(appliedInitialPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(appliedInitialPosition)
                );
            }

            if (hasStandingBasis)
            {
                if (!standingBasisTurn.HasValue ||
                    !standingBasisPosition.HasValue)
                {
                    throw new ArgumentException(
                        "Standing-based ingress requires " +
                        "standing turn and position provenance."
                    );
                }

                if (standingBasisTurn.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(standingBasisTurn)
                    );
                }

                if (!IsFinite(
                        standingBasisPosition.Value))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(standingBasisPosition)
                    );
                }
            }
            else
            {
                if (standingBasisTurn.HasValue ||
                    standingBasisPosition.HasValue)
                {
                    throw new ArgumentException(
                        "No-history ingress cannot retain " +
                        "standing provenance."
                    );
                }
            }

            PlacementTurn =
                placementTurn;

            HasStandingBasis =
                hasStandingBasis;

            StandingBasisTurn =
                standingBasisTurn;

            StandingBasisPosition =
                standingBasisPosition;

            NoStandingEntryPosition =
                noStandingEntryPosition;

            InnerFieldEntryCeiling =
                innerFieldEntryCeiling;

            UncappedPosition =
                uncappedPosition;

            AppliedInitialPosition =
                appliedInitialPosition;

            WasCapped =
                appliedInitialPosition <
                uncappedPosition;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Ingress provenance cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
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