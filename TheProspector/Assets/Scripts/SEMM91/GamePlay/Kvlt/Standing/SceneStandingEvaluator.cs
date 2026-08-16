using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Standing
{
    /// <summary>
    /// Produces one immutable settled Scene Standing
    /// evaluation from the complete remembered corpus
    /// supplied by the settlement layer.
    /// </summary>
    public sealed class SceneStandingEvaluator
    {
        public SceneStandingEvaluation Evaluate(
            string sourceOwnerEntityId,
            string sceneId,
            int settledTurn,
            IReadOnlyList<
                    SceneStandingContribution>
                contributions)
        {
            if (contributions == null)
            {
                throw new ArgumentNullException(
                    nameof(contributions)
                );
            }

            return new SceneStandingEvaluation(
                sourceOwnerEntityId,
                sceneId,
                settledTurn,
                contributions
            );
        }
    }
}