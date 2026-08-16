using System;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Applies Peak-2 Fringe lifecycle policy.
    ///
    /// This service does not calculate TRVE.
    /// The caller supplies the already evaluated
    /// current release-level HasActiveTRVE fact.
    /// </summary>
    public sealed class SceneReleaseFetteringService
    {
        public SceneReleaseFetteringResult Resolve(
            SceneRelease release,
            bool hasActiveTrve,
            int globalTurn)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            if (globalTurn <
                release.ReleasedTurn)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn),
                    globalTurn,
                    "Fettering settlement cannot " +
                    "precede the release turn."
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Fringe)
            {
                return
                    SceneReleaseFetteringResult
                        .NoChange;
            }

            /*
             * Fettering takes priority.
             *
             * If the current authoritative evaluation
             * says the release possesses active TRVE,
             * it has succeeded in entering the Field.
             */
            if (hasActiveTrve)
            {
                return release.TryFetter(
                        globalTurn
                    )
                    ? SceneReleaseFetteringResult
                        .Fettered
                    : SceneReleaseFetteringResult
                        .NoChange;
            }

            /*
             * The release turn itself is not the
             * failure deadline.
             *
             * The release receives the entire
             * following turn as its grace turn.
             */
            if (globalTurn ==
                release.ReleasedTurn)
            {
                return
                    SceneReleaseFetteringResult
                        .GraceOpen;
            }

            int graceTurn =
                release.ReleasedTurn + 1;

            /*
             * Peak 2 currently defines one explicit
             * grace turn: the entire turn immediately
             * following release.
             *
             * A release-specific Activation Attempt
             * during that turn protects the release
             * even when it failed to activate.
             */
            if (HasActivationAttemptDuringTurn(
                    release,
                    graceTurn
                ))
            {
                return
                    SceneReleaseFetteringResult
                        .GraceProtectedByAttempt;
            }

            /*
             * We have reached or passed the first
             * legal failure boundary with:
             *
             * - no active TRVE
             * - no activation attempt during the
             *   following grace turn.
             */
            return release.TryFailToFetter(
                    globalTurn
                )
                ? SceneReleaseFetteringResult
                    .FailedToFetter
                : SceneReleaseFetteringResult
                    .NoChange;
        }

        private static bool
            HasActivationAttemptDuringTurn(
                SceneRelease release,
                int globalTurn)
        {
            foreach (
                SceneReleaseActivationAttempt attempt
                in release.ActivationAttempts)
            {
                if (attempt.DeclaredTurn ==
                    globalTurn)
                {
                    return true;
                }
            }

            return false;
        }
    }
}