using System;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Immutable provenance-bearing initial Field
    /// placement evaluation for one newly fettered
    /// SceneRelease.
    /// </summary>
    public sealed class
        SceneReleaseIngressEvaluation
    {
        public string SceneReleaseId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int PlacementTurn { get; }

        public bool HasBandScenePositionBasis
        {
            get;
        }

        public int? BandScenePositionBasisTurn
        {
            get;
        }

        public float? BandScenePositionBasis
        {
            get;
        }

        public float FreshReleasePosition
        {
            get;
        }

        public float InnerFieldEntryCeiling
        {
            get;
        }

        public float UncappedPosition { get; }

        public float AppliedInitialPosition
        {
            get;
        }

        public bool WasCapped { get; }

        public SceneReleaseIngressEvaluation(
            string sceneReleaseId,
            string sourceOwnerEntityId,
            string sceneId,
            int placementTurn,
            bool hasBandScenePositionBasis,
            int? bandScenePositionBasisTurn,
            float? bandScenePositionBasis,
            float freshReleasePosition,
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

            RequireFinite(
                freshReleasePosition,
                nameof(freshReleasePosition)
            );

            RequireFinite(
                innerFieldEntryCeiling,
                nameof(innerFieldEntryCeiling)
            );

            RequireFinite(
                uncappedPosition,
                nameof(uncappedPosition)
            );

            RequireFinite(
                appliedInitialPosition,
                nameof(appliedInitialPosition)
            );

            if (hasBandScenePositionBasis)
            {
                if (!bandScenePositionBasisTurn
                        .HasValue ||
                    !bandScenePositionBasis
                        .HasValue)
                {
                    throw new ArgumentException(
                        "Band-position-based ingress " +
                        "requires turn and position " +
                        "provenance."
                    );
                }

                if (bandScenePositionBasisTurn.Value <
                    0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(
                            bandScenePositionBasisTurn
                        )
                    );
                }

                RequireFinite(
                    bandScenePositionBasis.Value,
                    nameof(
                        bandScenePositionBasis
                    )
                );
            }
            else if (
                bandScenePositionBasisTurn.HasValue ||
                bandScenePositionBasis.HasValue)
            {
                throw new ArgumentException(
                    "Fresh-baseline ingress cannot " +
                    "retain Band Scene Position " +
                    "provenance."
                );
            }

            PlacementTurn =
                placementTurn;

            HasBandScenePositionBasis =
                hasBandScenePositionBasis;

            BandScenePositionBasisTurn =
                bandScenePositionBasisTurn;

            BandScenePositionBasis =
                bandScenePositionBasis;

            FreshReleasePosition =
                freshReleasePosition;

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
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Ingress provenance cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }

        private static void RequireFinite(
            float value,
            string parameterName)
        {
            if (float.IsNaN(value) ||
                float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }
    }
}