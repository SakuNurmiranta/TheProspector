using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    /// <summary>
    /// Complete authoritative result of materializing
    /// one Scenario-seeded canonical institution.
    ///
    /// The underlying Canon/Society starting state is
    /// assumed to have already been established.
    /// </summary>
    public sealed class
        KvltStartingCanonInstitutionBootstrapResult
    {
        public SceneRelease Release { get; }

        public SceneReleaseLegitimacyEvaluation
            FinalLegitimacy { get; }

        public SceneReleaseCanonizationFreezeState
            FreezeState { get; }

        public IReadOnlyList<
                SceneReleaseCanonFrontierClaim>
            FrontierClaims { get; }

        public
            SceneReleaseCanonGravityDecompositionState
            GravityDecomposition { get; }

        public
            KvltStartingCanonInstitutionBootstrapResult(
                SceneRelease release,
                SceneReleaseLegitimacyEvaluation
                    finalLegitimacy,
                SceneReleaseCanonizationFreezeState
                    freezeState,
                IReadOnlyList<
                    SceneReleaseCanonFrontierClaim>
                    frontierClaims,
                SceneReleaseCanonGravityDecompositionState
                    gravityDecomposition)
        {
            Release =
                release ??
                throw new ArgumentNullException(
                    nameof(release)
                );

            FinalLegitimacy =
                finalLegitimacy ??
                throw new ArgumentNullException(
                    nameof(finalLegitimacy)
                );

            FreezeState =
                freezeState ??
                throw new ArgumentNullException(
                    nameof(freezeState)
                );

            FrontierClaims =
                frontierClaims ??
                throw new ArgumentNullException(
                    nameof(frontierClaims)
                );

            GravityDecomposition =
                gravityDecomposition ??
                throw new ArgumentNullException(
                    nameof(gravityDecomposition)
                );
        }
    }
}