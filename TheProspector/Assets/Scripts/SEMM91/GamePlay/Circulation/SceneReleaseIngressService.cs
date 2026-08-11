namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Applies an already-resolved ingress evaluation
    /// to the authoritative SceneRelease.
    ///
    /// It does not calculate Scene Standing or ingress
    /// policy.
    /// </summary>
    public sealed class SceneReleaseIngressService
    {
        public bool TryEstablish(
            SceneRelease release,
            SceneReleaseIngressEvaluation evaluation)
        {
            if (release == null ||
                evaluation == null)
            {
                return false;
            }

            if (release.ReleaseId !=
                evaluation.SceneReleaseId)
            {
                return false;
            }

            if (release.SourceOwnerEntityId !=
                evaluation.SourceOwnerEntityId)
            {
                return false;
            }

            if (release.HostedSceneNodeId !=
                evaluation.SceneId)
            {
                return false;
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                return false;
            }

            if (release.HasFieldPosition)
            {
                return false;
            }

            return release.TryEstablishFieldPosition(
                evaluation.AppliedInitialPosition,
                evaluation.PlacementTurn
            );
        }
    }
}