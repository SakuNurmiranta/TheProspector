using System;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Pure initial Field-position policy.
    ///
    /// Final Peak-2 rule:
    ///
    /// max(
    ///     FreshReleasePosition,
    ///     BandScenePosition
    /// )
    ///
    /// followed by the Inner-Field Entry Ceiling.
    /// </summary>
    public sealed class SceneReleaseIngressEvaluator
    {
        public SceneReleaseIngressEvaluation Evaluate(
            SceneRelease release,
            BandScenePositionEvaluation
                bandScenePosition,
            float freshReleasePosition,
            float innerFieldEntryCeiling,
            int placementTurn)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new ArgumentException(
                    "Only a fettered Field release " +
                    "can receive initial Field placement.",
                    nameof(release)
                );
            }

            if (placementTurn <
                release.ReleasedTurn)
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

            if (freshReleasePosition >
                innerFieldEntryCeiling)
            {
                throw new ArgumentException(
                    "Fresh Release Position cannot be " +
                    "inward of the Inner-Field Entry " +
                    "Ceiling.",
                    nameof(freshReleasePosition)
                );
            }

            bool hasBandBasis =
                false;

            int? bandBasisTurn =
                null;

            float? bandBasisPosition =
                null;

            float uncappedPosition =
                freshReleasePosition;

            if (bandScenePosition != null)
            {
                if (bandScenePosition
                        .SourceOwnerEntityId !=
                    release.SourceOwnerEntityId)
                {
                    throw new ArgumentException(
                        "Band Scene Position belongs to " +
                        "a different release owner.",
                        nameof(bandScenePosition)
                    );
                }

                if (bandScenePosition.SceneId !=
                    release.HostedSceneNodeId)
                {
                    throw new ArgumentException(
                        "Band Scene Position belongs to " +
                        "a different scene.",
                        nameof(bandScenePosition)
                    );
                }

                if (bandScenePosition.SettledTurn >=
                    placementTurn)
                {
                    throw new ArgumentException(
                        "Initial Field placement requires " +
                        "Band Scene Position from the " +
                        "completed prior turn.",
                        nameof(bandScenePosition)
                    );
                }

                if (bandScenePosition
                    .HasScenePosition)
                {
                    if (!bandScenePosition
                        .ScenePosition.HasValue)
                    {
                        throw new
                            InvalidOperationException(
                                "Band Scene Position " +
                                "claims a position but " +
                                "contains no value."
                            );
                    }

                    hasBandBasis =
                        true;

                    bandBasisTurn =
                        bandScenePosition.SettledTurn;

                    bandBasisPosition =
                        bandScenePosition
                            .ScenePosition.Value;

                    uncappedPosition =
                        Math.Max(
                            freshReleasePosition,
                            bandScenePosition
                                .ScenePosition.Value
                        );
                }
            }

            float appliedPosition =
                Math.Min(
                    uncappedPosition,
                    innerFieldEntryCeiling
                );

            return new SceneReleaseIngressEvaluation(
                release.ReleaseId,
                release.SourceOwnerEntityId,
                release.HostedSceneNodeId,
                placementTurn,
                hasBandBasis,
                bandBasisTurn,
                bandBasisPosition,
                freshReleasePosition,
                innerFieldEntryCeiling,
                uncappedPosition,
                appliedPosition
            );
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