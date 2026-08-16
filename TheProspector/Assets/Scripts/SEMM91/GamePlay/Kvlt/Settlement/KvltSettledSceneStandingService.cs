using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Projects the fully resolved turn-t public
    /// release corpus into frozen Scene Standing.
    ///
    /// This is deliberately executed before
    /// next-turn ingress:
    ///
    /// Standing_t
    ///     → ingress_(t+1)
    ///
    /// The existing SceneStandingCorpusProjector
    /// owns all lifecycle-specific weighting math.
    /// This service owns only deterministic owner
    /// population and ordering.
    /// </summary>
    public sealed class
        KvltSettledSceneStandingService
    {
        private readonly
            SceneStandingCorpusProjector
            projector =
                new();

        public KvltSettledSceneStandingResult
            Settle(
                string sceneId,
                int settledTurn,
                IReadOnlyList<SceneRelease> releases,
                SceneStandingProjectionPolicy policy)
        {
            sceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            if (policy == null)
            {
                throw new ArgumentNullException(
                    nameof(policy)
                );
            }

            HashSet<string> seenReleaseIds =
                new(
                    StringComparer.Ordinal
                );

            HashSet<string> ownerSet =
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
                        "Settled Standing release " +
                        "population cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!seenReleaseIds.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Settled Standing release " +
                        "population contains duplicate " +
                        "SceneRelease identity.",
                        nameof(releases)
                    );
                }

                if (!string.Equals(
                        release.HostedSceneNodeId,
                        sceneId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                /*
                 * Preserve the old Camp-3 owner
                 * population rule:
                 *
                 * an owner receives an explicit
                 * evaluation even when their current
                 * corpus contributes no Standing.
                 *
                 * This is particularly important for
                 * a newly fettered release waiting for
                 * first ingress.
                 */
                ownerSet.Add(
                    release.SourceOwnerEntityId
                );
            }

            string[] owners =
                new string[
                    ownerSet.Count
                ];

            ownerSet.CopyTo(
                owners
            );

            Array.Sort(
                owners,
                StringComparer.Ordinal
            );

            List<SceneStandingEvaluation>
                evaluations =
                    new();

            foreach (
                string owner
                in owners)
            {
                evaluations.Add(
                    projector.Project(
                        owner,
                        sceneId,
                        settledTurn,
                        releases,
                        policy
                    )
                );
            }

            return new
                KvltSettledSceneStandingResult(
                    sceneId,
                    settledTurn,
                    evaluations
                );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Settled Standing identity cannot " +
                    "be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}