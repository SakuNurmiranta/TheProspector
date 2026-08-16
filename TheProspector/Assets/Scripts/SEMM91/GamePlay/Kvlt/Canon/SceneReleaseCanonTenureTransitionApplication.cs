using System;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Immutable proof that one CanonRetained release
    /// left active Scene Space because its continuous
    /// canonizing Keeper tenure ended.
    /// </summary>
    public sealed class
        SceneReleaseCanonTenureTransitionApplication
    {
        public string SceneReleaseId { get; }

        public string EndingKeeperTenureId { get; }

        public string NextKeeperTenureId { get; }

        public int GlobalTurn { get; }

        public SceneReleaseCanonTenureTransitionApplication(
            string sceneReleaseId,
            string endingKeeperTenureId,
            string nextKeeperTenureId,
            int globalTurn)
        {
            SceneReleaseId =
                RequireText(
                    sceneReleaseId,
                    nameof(sceneReleaseId)
                );

            EndingKeeperTenureId =
                RequireText(
                    endingKeeperTenureId,
                    nameof(endingKeeperTenureId)
                );

            NextKeeperTenureId =
                RequireText(
                    nextKeeperTenureId,
                    nameof(nextKeeperTenureId)
                );

            if (EndingKeeperTenureId ==
                NextKeeperTenureId)
            {
                throw new ArgumentException(
                    "Historical Canon transition " +
                    "requires an actual Keeper-tenure " +
                    "change."
                );
            }

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            GlobalTurn =
                globalTurn;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Keeper tenure transition " +
                    "provenance cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}