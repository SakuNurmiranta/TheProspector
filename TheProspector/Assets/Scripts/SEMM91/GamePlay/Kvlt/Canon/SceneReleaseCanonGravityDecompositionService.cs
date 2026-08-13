using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;

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

        /// <summary>
        /// Existing live Canon-settlement path.
        /// </summary>
        public SceneReleaseCanonGravityDecompositionState
            Apply(
                SceneRelease release,
                SceneReleaseCanonFreezeApplication
                    freezeApplication,
                CanonSimultaneousMergeEvaluation
                    canonMerge)
        {
            EnsureCanApply(
                release
            );

            SceneReleaseCanonGravityDecompositionState
                result =
                    evaluator.Evaluate(
                        release,
                        freezeApplication,
                        canonMerge
                    );

            Attach(
                release,
                result
            );

            return result;
        }

        /// <summary>
        /// Already-established Canon path.
        ///
        /// Used by authoritative Scenario initial state
        /// where no live merge or activation event is
        /// supposed to exist.
        /// </summary>
        public SceneReleaseCanonGravityDecompositionState
            Apply(
                SceneRelease release,
                SceneReleaseLegitimacyEvaluation
                    finalLegitimacy,
                IReadOnlyList<
                    SceneReleaseCanonFrontierClaim>
                    frontierClaims)
        {
            EnsureCanApply(
                release
            );

            SceneReleaseCanonGravityDecompositionState
                result =
                    evaluator.Evaluate(
                        release,
                        finalLegitimacy,
                        frontierClaims
                    );

            Attach(
                release,
                result
            );

            return result;
        }

        private static void EnsureCanApply(
            SceneRelease release)
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
        }

        private static void Attach(
            SceneRelease release,
            SceneReleaseCanonGravityDecompositionState
                result)
        {
            if (!release
                    .TryAttachCanonGravityDecomposition(
                        result))
            {
                throw new InvalidOperationException(
                    "Validated claim-level Gravity " +
                    "decomposition could not be attached."
                );
            }
        }
    }
}