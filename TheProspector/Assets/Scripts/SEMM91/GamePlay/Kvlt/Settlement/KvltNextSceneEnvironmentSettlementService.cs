using System;
using System.Collections.Generic;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Builds the semantic environment published for
    /// turn t+1.
    ///
    /// Ordering contract:
    ///
    /// Standing_t
    ///     → ingress_(t+1)
    ///     → Pressure_(t+1)
    ///     → NormativeCentre_(t+1)
    ///
    /// Canon supplied here is already the final Canon
    /// after any turn-t year-end canonization.
    /// </summary>
    public sealed class
        KvltNextSceneEnvironmentSettlementService
    {
        private readonly
            ScenePressureRebuilder
            pressureRebuilder =
                new();

        private readonly
            CanonicalNormativeCentreDeriver
            normativeDeriver =
                new();

        public
            KvltNextSceneEnvironmentSettlementResult
            Settle(
                string sceneId,
                CanonState currentCanon,
                IReadOnlyList<SceneRelease> releases,
                IReadOnlyList<DemoTape> demoTapes,
                KvltNextTurnIngressSettlementResult
                    ingress,
                ScenePressureRebuildPolicy
                    pressurePolicy,
                NormativePressureBlendPolicy
                    normativePolicy)
        {
            sceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (currentCanon == null)
            {
                throw new ArgumentNullException(
                    nameof(currentCanon)
                );
            }

            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            if (demoTapes == null)
            {
                throw new ArgumentNullException(
                    nameof(demoTapes)
                );
            }

            if (ingress == null)
            {
                throw new ArgumentNullException(
                    nameof(ingress)
                );
            }

            if (pressurePolicy == null)
            {
                throw new ArgumentNullException(
                    nameof(pressurePolicy)
                );
            }

            if (normativePolicy == null)
            {
                throw new ArgumentNullException(
                    nameof(normativePolicy)
                );
            }

            if (ingress.SceneId !=
                sceneId)
            {
                throw new ArgumentException(
                    "Next scene environment ingress " +
                    "belongs to another scene.",
                    nameof(ingress)
                );
            }

            /*
             * This is deliberately stronger than
             * relying on ScenePressureRebuilder's
             * resident-Field filter.
             *
             * By this phase, every Field release must
             * already have completed its initial
             * placement. A Field-without-position here
             * means the chronology called environment
             * derivation before ingress.
             */
            RequireIngressComplete(
                sceneId,
                releases
            );

            int publishedTurn =
                ingress.PlacementTurn;

            ScenePressureRebuildEvaluation
                pressure =
                    pressureRebuilder.Rebuild(
                        sceneId,
                        publishedTurn,
                        releases,
                        demoTapes,
                        currentCanon,
                        pressurePolicy
                    );

            NormativeCentre
                canonNormativeCentre =
                    normativeDeriver.Derive(
                        currentCanon
                    );

            NormativeCentreDerivationEvaluation
                normativeCentre =
                    normativeDeriver
                        .DeriveWithPressure(
                            currentCanon,
                            pressure,
                            normativePolicy
                        );

            return new
                KvltNextSceneEnvironmentSettlementResult(
                    sceneId,
                    ingress.CompletedTurn,
                    publishedTurn,
                    pressure,
                    canonNormativeCentre,
                    normativeCentre
                );
        }

        private static void RequireIngressComplete(
            string sceneId,
            IReadOnlyList<SceneRelease> releases)
        {
            HashSet<string> releaseIds =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                SceneRelease release
                in releases)
            {
                if (release == null)
                {
                    throw new ArgumentException(
                        "Next scene environment release " +
                        "population cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!releaseIds.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Next scene environment release " +
                        "population contains duplicate " +
                        "identity.",
                        nameof(releases)
                    );
                }

                if (release.HostedSceneNodeId !=
                    sceneId)
                {
                    continue;
                }

                if (release.LifecycleState !=
                    SceneReleaseLifecycleState.Field)
                {
                    continue;
                }

                if (!release.HasFieldPosition ||
                    release.FieldPositionState == null)
                {
                    throw new InvalidOperationException(
                        "Next scene environment cannot " +
                        "be derived before Field ingress " +
                        "is complete | " +
                        $"release={release.ReleaseId}"
                    );
                }
            }
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Next scene environment identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}