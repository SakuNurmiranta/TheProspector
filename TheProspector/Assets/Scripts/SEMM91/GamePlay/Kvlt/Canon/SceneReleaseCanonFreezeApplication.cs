using System;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Proof that NewCanon Assimilation was followed by
    /// production legitimacy recalculation and permanent
    /// canonical activation/Gravity freeze.
    /// </summary>
    public sealed class
        SceneReleaseCanonFreezeApplication
    {
        public
            SceneReleaseCanonAssimilationApplication
            AssimilationApplication { get; }

        public SceneReleaseLegitimacyEvaluation
            FinalLegitimacy { get; }

        public SceneReleaseCanonizationFreezeState
            FreezeState { get; }

        public SceneReleaseCanonFreezeApplication(
            SceneReleaseCanonAssimilationApplication
                assimilationApplication,
            SceneReleaseLegitimacyEvaluation
                finalLegitimacy,
            SceneReleaseCanonizationFreezeState
                freezeState)
        {
            AssimilationApplication =
                assimilationApplication ??
                throw new ArgumentNullException(
                    nameof(assimilationApplication)
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

            SceneReleaseCanonAssimilationEvaluation
                assimilation =
                    assimilationApplication.Evaluation;

            if (finalLegitimacy.SourceReleaseId !=
                    assimilation.SceneReleaseId ||
                finalLegitimacy.SourceDemoTapeId !=
                    assimilation.SourceDemoTapeId ||
                finalLegitimacy.SourceOwnerEntityId !=
                    assimilation.SourceOwnerEntityId ||
                finalLegitimacy.SettledTurn !=
                    assimilation.SettledTurn)
            {
                throw new ArgumentException(
                    "Final legitimacy provenance does " +
                    "not match Canon Assimilation.",
                    nameof(finalLegitimacy)
                );
            }

            if (freezeState.SceneReleaseId !=
                    assimilation.SceneReleaseId ||
                freezeState.SourceDemoTapeId !=
                    assimilation.SourceDemoTapeId ||
                freezeState.SourceOwnerEntityId !=
                    assimilation.SourceOwnerEntityId ||
                freezeState.SceneId !=
                    assimilation.SceneId ||
                freezeState.CanonizedTurn !=
                    assimilation.SettledTurn ||
                freezeState
                    .CanonizedUnderKeeperTenureId !=
                    assimilation.KeeperTenureId)
            {
                throw new ArgumentException(
                    "Canonization freeze provenance " +
                    "does not match Canon Assimilation.",
                    nameof(freezeState)
                );
            }

            if (Math.Abs(
                    freezeState
                        .FrozenPostAssimilationGravity -
                    finalLegitimacy.Gravity) >
                0.0001f)
            {
                throw new ArgumentException(
                    "Frozen post-assimilation Gravity " +
                    "does not match final production " +
                    "legitimacy evaluation.",
                    nameof(freezeState)
                );
            }
        }
    }
}