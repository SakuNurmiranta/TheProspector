using System;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    public sealed class
        KvltStartingCanonReleaseBootstrapResult
    {
        public SceneRelease Release { get; }

        public SceneReleaseLegitimacyEvaluation
            FinalLegitimacy { get; }

        public SceneReleaseCanonizationFreezeState
            FreezeState { get; }

        public KvltStartingCanonReleaseBootstrapResult(
            SceneRelease release,
            SceneReleaseLegitimacyEvaluation
                finalLegitimacy,
            SceneReleaseCanonizationFreezeState
                freezeState)
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
        }
    }
}