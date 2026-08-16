using System;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Exact successful post-movement Nexus candidate
    /// participating in one simultaneous Canon merge.
    /// </summary>
    public sealed class
        SceneReleaseCanonMergeCandidate
    {
        public SceneRelease Release { get; }

        public SceneReleaseNexusBoundaryEvaluation
            NexusEvaluation { get; }

        public string SceneReleaseId =>
            Release.ReleaseId;

        public int SettledTurn =>
            NexusEvaluation.SettledTurn;

        public SceneReleaseCanonMergeCandidate(
            SceneRelease release,
            SceneReleaseNexusBoundaryEvaluation
                nexusEvaluation)
        {
            Release =
                release ??
                throw new ArgumentNullException(
                    nameof(release)
                );

            NexusEvaluation =
                nexusEvaluation ??
                throw new ArgumentNullException(
                    nameof(nexusEvaluation)
                );

            if (!nexusEvaluation.IsCanonCandidate)
            {
                throw new ArgumentException(
                    "Canon merge requires a successful " +
                    "Nexus candidate.",
                    nameof(nexusEvaluation)
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new ArgumentException(
                    "Canon merge candidate must still " +
                    "be a Field release.",
                    nameof(release)
                );
            }

            if (nexusEvaluation.SceneReleaseId !=
                    release.ReleaseId ||
                nexusEvaluation.SourceDemoTapeId !=
                    release.SourceDemoTapeId ||
                nexusEvaluation.SourceOwnerEntityId !=
                    release.SourceOwnerEntityId ||
                nexusEvaluation.SceneId !=
                    release.HostedSceneNodeId)
            {
                throw new ArgumentException(
                    "Nexus candidate provenance does " +
                    "not match SceneRelease.",
                    nameof(nexusEvaluation)
                );
            }
        }
    }
}