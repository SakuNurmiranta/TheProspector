using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;

namespace SEMM91.GamePlay.Score
{
    /// <summary>
    /// Pays post-tenure Institutional Gravity from
    /// still-current canonical frontier claims.
    ///
    /// Eligibility is evaluated against the supplied
    /// frozen Canon state.
    ///
    /// This service deliberately does not mutate Canon
    /// and does not decide settlement ordering.
    /// </summary>
    public sealed class
        HistoricalCanonInstitutionalGravityScoreAwardService
    {
        public IReadOnlyList<
                HistoricalCanonInstitutionalGravityAward>
            AwardEligibleClaims(
                SceneRelease release,
                CanonState frozenCanon,
                ScoreLedger ledger,
                int globalTurn)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (frozenCanon == null)
            {
                throw new ArgumentNullException(
                    nameof(frozenCanon)
                );
            }

            if (ledger == null)
            {
                throw new ArgumentNullException(
                    nameof(ledger)
                );
            }

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState
                    .HistoricalCanon)
            {
                return Array.Empty<
                    HistoricalCanonInstitutionalGravityAward>();
            }

            if (!release.IsCanonized ||
                release.CanonizationFreezeState == null ||
                !release.HasCanonGravityDecomposition ||
                release.CanonGravityDecompositionState ==
                    null)
            {
                throw new InvalidOperationException(
                    "HistoricalCanon release is missing " +
                    "frozen canonical accounting state."
                );
            }

            if (globalTurn <
                release.CanonizedTurn)
            {
                return Array.Empty<
                    HistoricalCanonInstitutionalGravityAward>();
            }

            /*
             * Important phase-separation rule.
             *
             * If the full CanonRetained release Gravity
             * already paid on this same turn before the
             * tenure transition, do not also start
             * claim-level historical payout.
             */
            if (HasFullReleaseTenureAward(
                    release,
                    ledger,
                    globalTurn))
            {
                return Array.Empty<
                    HistoricalCanonInstitutionalGravityAward>();
            }

            List<
                HistoricalCanonInstitutionalGravityAward>
                awards =
                    new();

            foreach (
                SceneReleaseCanonFrontierGravityContribution
                    frontier
                in release
                    .CanonGravityDecompositionState
                    .FrontierContributions)
            {
                SceneReleaseCanonFrontierClaim claim =
                    frontier.FrontierClaim;

                bool hasCurrentCanon =
                    frozenCanon.TryGetCanonicalDegree(
                        claim.Axis,
                        claim.Pole,
                        out TagDegree currentDegree
                    );

                /*
                 * Exact equality is intentional.
                 *
                 * Lower/absent Canon:
                 * this frozen state does not yet
                 * recognize the claim.
                 *
                 * Equal Canon:
                 * claim remains current.
                 *
                 * Higher Canon:
                 * claim has been superseded.
                 */
                if (!hasCurrentCanon ||
                    currentDegree !=
                    claim.CanonicalDegreeEstablished)
                {
                    continue;
                }

                ScoreEvent proposed =
                    new(
                        BuildEventId(
                            frontier,
                            globalTurn
                        ),
                        claim.CanonicalActivatorPlayerId,
                        claim.SourceOwnerEntityId,
                        release.HostedSceneNodeId,
                        release.ReleaseId,
                        release.SourceDemoTapeId,
                        ScoreEventKind
                            .InstitutionalGravity,
                        frontier
                            .ClaimReleaseGravityContribution,
                        globalTurn
                    );

                /*
                 * Duplicate event identity means this
                 * exact claim has already paid this
                 * turn. Skip rather than double-score.
                 */
                if (!ledger.TryRecord(
                        proposed))
                {
                    continue;
                }

                awards.Add(
                    new
                        HistoricalCanonInstitutionalGravityAward(
                            frontier,
                            proposed
                        )
                );
            }

            return awards;
        }

        private static bool
            HasFullReleaseTenureAward(
                SceneRelease release,
                ScoreLedger ledger,
                int globalTurn)
        {
            IReadOnlyList<ScoreEvent> events =
                ledger.GetForSceneRelease(
                    release.ReleaseId
                );

            foreach (
                ScoreEvent scoreEvent
                in events)
            {
                if (scoreEvent.GlobalTurn ==
                        globalTurn &&
                    scoreEvent.Kind ==
                        ScoreEventKind
                            .CanonRetainedGravity)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildEventId(
            SceneReleaseCanonFrontierGravityContribution
                frontier,
            int globalTurn)
        {
            SceneReleaseCanonFrontierClaim claim =
                frontier.FrontierClaim;

            return
                "SCORE|INSTITUTIONAL_GRAVITY|" +
                Encode(claim.SceneReleaseId) +
                "|" +
                Encode(claim.SourceTrackId) +
                "|" +
                Encode(claim.SourceIdeaId) +
                "|" +
                claim.IdeaIndex +
                "|" +
                (int)claim.Axis +
                "|" +
                (int)claim.Pole +
                "|" +
                (int)claim
                    .CanonicalDegreeEstablished +
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