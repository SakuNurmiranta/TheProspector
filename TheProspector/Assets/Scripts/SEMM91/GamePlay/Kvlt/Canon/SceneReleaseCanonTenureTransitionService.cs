using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Resolves the SceneRelease consequence of one
    /// continuous Keeper-tenure transition.
    ///
    /// Same-tenure retention is a no-op.
    ///
    /// On an actual tenure change, every supplied
    /// CanonRetained release canonized under the
    /// ending tenure enters HistoricalCanon.
    ///
    /// Validation completes before any release is
    /// mutated.
    /// </summary>
    public sealed class
        SceneReleaseCanonTenureTransitionService
    {
        public IReadOnlyList<
                SceneReleaseCanonTenureTransitionApplication>
            Apply(
                IReadOnlyList<SceneRelease> releases,
                string endingKeeperTenureId,
                string nextKeeperTenureId,
                int globalTurn)
        {
            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            string endingTenure =
                RequireText(
                    endingKeeperTenureId,
                    nameof(endingKeeperTenureId)
                );

            string nextTenure =
                RequireText(
                    nextKeeperTenureId,
                    nameof(nextKeeperTenureId)
                );

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            /*
             * Same incumbent retained:
             *
             * the continuous tenure did not end.
             */
            if (string.Equals(
                    endingTenure,
                    nextTenure,
                    StringComparison.Ordinal))
            {
                return Array.Empty<
                    SceneReleaseCanonTenureTransitionApplication>();
            }

            HashSet<string> seenReleaseIds =
                new(
                    StringComparer.Ordinal
                );

            List<SceneRelease> targets =
                new();

            /*
             * PASS 1:
             * Validate complete input and identify
             * targets before any mutation.
             */
            foreach (
                SceneRelease release
                in releases)
            {
                if (release == null)
                {
                    throw new ArgumentException(
                        "Keeper tenure transition " +
                        "release collection cannot " +
                        "contain null.",
                        nameof(releases)
                    );
                }

                if (!seenReleaseIds.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Keeper tenure transition " +
                        "contains duplicate " +
                        "SceneRelease identity.",
                        nameof(releases)
                    );
                }

                if (release.LifecycleState !=
                    SceneReleaseLifecycleState
                        .CanonRetained)
                {
                    continue;
                }

                if (!string.Equals(
                        release
                            .CanonizedUnderKeeperTenureId,
                        endingTenure,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (!release.IsCanonized ||
                    release
                        .CanonizationFreezeState ==
                    null)
                {
                    throw new InvalidOperationException(
                        "CanonRetained SceneRelease has " +
                        "no canonical freeze state | " +
                        $"release={release.ReleaseId}"
                    );
                }

                if (globalTurn <
                    release.CanonizedTurn)
                {
                    throw new InvalidOperationException(
                        "Keeper tenure cannot end before " +
                        "the release canonized | " +
                        $"release={release.ReleaseId}"
                    );
                }

                targets.Add(
                    release
                );
            }

            targets.Sort(
                CompareReleaseIds
            );

            List<
                SceneReleaseCanonTenureTransitionApplication>
                applications =
                    new();

            /*
             * PASS 2:
             * Deterministic terminal mutation.
             */
            foreach (
                SceneRelease release
                in targets)
            {
                if (!release.TryEnterHistoricalCanon(
                        globalTurn))
                {
                    throw new InvalidOperationException(
                        "Validated CanonRetained release " +
                        "could not enter HistoricalCanon | " +
                        $"release={release.ReleaseId}"
                    );
                }

                applications.Add(
                    new
                        SceneReleaseCanonTenureTransitionApplication(
                            release.ReleaseId,
                            endingTenure,
                            nextTenure,
                            globalTurn
                        )
                );
            }

            return applications;
        }

        private static int CompareReleaseIds(
            SceneRelease left,
            SceneRelease right)
        {
            return string.CompareOrdinal(
                left.ReleaseId,
                right.ReleaseId
            );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Keeper tenure identity cannot " +
                    "be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}