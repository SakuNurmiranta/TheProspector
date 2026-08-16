using System;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Score
{
    /// <summary>
    /// Awards the frozen full-release Gravity of a
    /// CanonRetained SceneRelease while the exact
    /// continuous Keeper tenure under which it
    /// canonized remains current.
    ///
    /// This service performs no semantic evaluation.
    /// </summary>
    public sealed class
        CanonRetainedScoreAwardService
    {
        public bool TryAward(
            SceneRelease release,
            string currentKeeperTenureId,
            ScoreLedger ledger,
            int globalTurn,
            out ScoreEvent scoreEvent)
        {
            scoreEvent =
                null;

            if (release == null ||
                ledger == null ||
                string.IsNullOrWhiteSpace(
                    currentKeeperTenureId))
            {
                return false;
            }

            if (globalTurn < 0)
            {
                return false;
            }

            if (!release.IsCanonized ||
                release.CanonizationFreezeState == null)
            {
                return false;
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState
                    .CanonRetained)
            {
                return false;
            }

            /*
             * Entry 16's decomposition is part of the
             * completed canonization settlement.
             *
             * Do not begin institutional payout before
             * the frozen accounting basis exists.
             */
            if (!release.HasCanonGravityDecomposition ||
                release.CanonGravityDecompositionState ==
                    null)
            {
                return false;
            }

            if (globalTurn <
                release.CanonizedTurn)
            {
                return false;
            }

            string currentTenure =
                currentKeeperTenureId.Trim();

            if (!string.Equals(
                    currentTenure,
                    release
                        .CanonizedUnderKeeperTenureId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            /*
             * The new tenure payout supersedes an
             * ordinary Field-Gravity payout on the
             * canonization turn.
             *
             * If Field Gravity has somehow already
             * been recorded for this release/turn,
             * refuse the institutional payout rather
             * than double-score the same Gravity.
             */
            if (HasGravityAwardForReleaseTurn(
                    ledger,
                    release.ReleaseId,
                    globalTurn))
            {
                return false;
            }

            ScoreEvent proposed =
                new(
                    BuildEventId(
                        release,
                        currentTenure,
                        globalTurn
                    ),
                    release.SourceOwnerEntityId,
                    release.SourceOwnerEntityId,
                    release.HostedSceneNodeId,
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    ScoreEventKind
                        .CanonRetainedGravity,
                    release
                        .FrozenPostAssimilationGravity,
                    globalTurn
                );

            if (!ledger.TryRecord(
                    proposed))
            {
                return false;
            }

            scoreEvent =
                proposed;

            return true;
        }

        private static bool
            HasGravityAwardForReleaseTurn(
                ScoreLedger ledger,
                string sceneReleaseId,
                int globalTurn)
        {
            var events =
                ledger.GetForSceneRelease(
                    sceneReleaseId
                );

            foreach (
                ScoreEvent existing
                in events)
            {
                if (existing.GlobalTurn !=
                    globalTurn)
                {
                    continue;
                }

                switch (existing.Kind)
                {
                    case ScoreEventKind.FieldGravity:
                    case ScoreEventKind
                        .CanonRetainedGravity:

                        return true;
                }
            }

            return false;
        }

        private static string BuildEventId(
            SceneRelease release,
            string keeperTenureId,
            int globalTurn)
        {
            return
                "SCORE|CANON_RETAINED_GRAVITY|" +
                Encode(
                    keeperTenureId
                ) +
                "|" +
                Encode(
                    release.ReleaseId
                ) +
                "|" +
                globalTurn;
        }

        private static string Encode(
            string value)
        {
            return
                value.Length +
                ":" +
                value;
        }
    }
}