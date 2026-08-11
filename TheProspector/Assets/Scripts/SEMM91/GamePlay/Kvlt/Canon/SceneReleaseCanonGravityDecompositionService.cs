using System;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Produces and permanently attaches the unique
    /// claim-level Gravity decomposition belonging to
    /// one frozen CanonRetained SceneRelease.
    /// </summary>
    public sealed class
        SceneReleaseCanonGravityDecompositionService
    {
        private readonly
            SceneReleaseCanonGravityDecompositionEvaluator
            evaluator =
                new();

        public SceneReleaseCanonGravityDecompositionState
            Apply(
                SceneRelease release,
                SceneReleaseCanonFreezeApplication
                    freezeApplication,
                CanonSimultaneousMergeEvaluation
                    canonMerge)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (release.HasCanonGravityDecomposition)
            {
                throw new InvalidOperationException(
                    "Canonized SceneRelease already has " +
                    "a frozen Gravity decomposition."
                );
            }

            SceneReleaseCanonGravityDecompositionState
                result =
                    evaluator.Evaluate(
                        release,
                        freezeApplication,
                        canonMerge
                    );

            if (!release
                    .TryAttachCanonGravityDecomposition(
                        result))
            {
                throw new InvalidOperationException(
                    "Validated claim-level Gravity " +
                    "decomposition could not be attached."
                );
            }

            return result;
        }
    }
}