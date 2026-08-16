using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Score;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Authoritative composition of the Peak-2
    /// TurnGravityScoreSettlement phase.
    ///
    /// It composes existing Score primitives but owns
    /// the chronology-specific eligibility rules.
    ///
    /// Ordering:
    ///
    /// 1. HistoricalCanon frontier IG;
    /// 2. CanonRetained full frozen Gravity;
    /// 3. ordinary Field scoring.
    ///
    /// A release fettered during this completed turn
    /// receives First-Fetter Resonance but deliberately
    /// receives no same-turn recurring Field Gravity.
    ///
    /// Historical frontier eligibility is evaluated
    /// against historicalPayoutCanon. At year end the
    /// caller must supply the Canon snapshot from before
    /// this turn's Canonization merge so a frontier that
    /// is superseded later in the turn still receives
    /// its completed-turn payout.
    /// </summary>
    public sealed class
        KvltTurnScoreSettlementService
    {
        private readonly
            SceneReleaseScoreAwardService
            ordinaryScore =
                new();

        private readonly
            CanonRetainedScoreAwardService
            retainedScore =
                new();

        private readonly
            HistoricalCanonInstitutionalGravityScoreAwardService
            historicalScore =
                new();

        public KvltTurnScoreSettlementResult
            Settle(
                string sceneId,
                int globalTurn,
                string currentKeeperTenureId,
                CanonState historicalPayoutCanon,
                IReadOnlyList<SceneRelease> releases,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseLegitimacyEvaluation>
                    legitimacyByRelease,
                ScoreLedger ledger)
        {
            sceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            if (historicalPayoutCanon == null)
            {
                throw new ArgumentNullException(
                    nameof(historicalPayoutCanon)
                );
            }

            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            if (legitimacyByRelease == null)
            {
                throw new ArgumentNullException(
                    nameof(legitimacyByRelease)
                );
            }

            if (ledger == null)
            {
                throw new ArgumentNullException(
                    nameof(ledger)
                );
            }

            List<SceneRelease>
                sceneReleases =
                    GetOrderedSceneReleases(
                        releases,
                        sceneId
                    );

            int scoreStartIndex =
                ledger.Count;

            /*
             * PASS 1 — post-tenure historical frontier
             * Institutional Gravity.
             *
             * At year end historicalPayoutCanon is the
             * pre-Canonization snapshot. That preserves
             * the rule that a frontier which existed for
             * this completed turn earns this turn before
             * a later phase supersedes it.
             */
            foreach (
                SceneRelease release
                in sceneReleases)
            {
                if (release.LifecycleState !=
                    SceneReleaseLifecycleState
                        .HistoricalCanon)
                {
                    continue;
                }

                historicalScore
                    .AwardEligibleClaims(
                        release,
                        historicalPayoutCanon,
                        ledger,
                        globalTurn
                    );
            }

            /*
             * PASS 2 — continuous-tenure retained
             * release Gravity.
             *
             * This occurs after year-end Canonization,
             * so a newly canonized release is eligible
             * for its first frozen payout immediately.
             */
            foreach (
                SceneRelease release
                in sceneReleases)
            {
                if (release.LifecycleState !=
                    SceneReleaseLifecycleState
                        .CanonRetained)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                        currentKeeperTenureId))
                {
                    throw new InvalidOperationException(
                        "CanonRetained Score requires a " +
                        "current Keeper tenure."
                    );
                }

                retainedScore.TryAward(
                    release,
                    currentKeeperTenureId,
                    ledger,
                    globalTurn,
                    out _
                );
            }

            /*
             * PASS 3 — ordinary Field Score.
             *
             * Freshly fettered releases receive their
             * one-time Resonance here, but have not yet
             * ingressed into turn-t+1 Field physics and
             * therefore cannot receive recurring Field
             * Gravity for the completed turn.
             */
            foreach (
                SceneRelease release
                in sceneReleases)
            {
                if (release.LifecycleState !=
                    SceneReleaseLifecycleState.Field)
                {
                    continue;
                }

                SceneReleaseLegitimacyEvaluation
                    evaluation =
                        RequireCurrentEvaluation(
                            release,
                            legitimacyByRelease,
                            globalTurn
                        );

                if (HasFetterTransitionAtTurn(
                        release,
                        globalTurn))
                {
                    ordinaryScore
                        .TryAwardFirstFetterResonance(
                            release,
                            evaluation,
                            ledger,
                            globalTurn,
                            out _
                        );

                    /*
                     * Critical Peak-2 chronology rule:
                     * no same-turn recurring G.
                     *
                     * Even if First-Fetter Resonance was
                     * already paid for this DemoTape by a
                     * prior public release, a newly
                     * fettered rerelease still cannot
                     * collect Field Gravity until the
                     * following turn.
                     */
                    continue;
                }

                ordinaryScore
                    .TryAwardFieldGravity(
                        release,
                        evaluation,
                        ledger,
                        globalTurn,
                        out _
                    );
            }

            List<ScoreEvent>
                newScoreEvents =
                    new();

            for (int index = scoreStartIndex;
                 index < ledger.Count;
                 index++)
            {
                ScoreEvent scoreEvent =
                    ledger.History[index];

                /*
                 * Every service above was scoped to the
                 * requested scene. Keep the result
                 * defensive in case that changes later.
                 */
                if (scoreEvent.SceneId !=
                    sceneId)
                {
                    throw new InvalidOperationException(
                        "Turn Score settlement appended " +
                        "Score for another scene."
                    );
                }

                if (scoreEvent.GlobalTurn !=
                    globalTurn)
                {
                    throw new InvalidOperationException(
                        "Turn Score settlement appended " +
                        "Score for another turn."
                    );
                }

                newScoreEvents.Add(
                    scoreEvent
                );
            }

            return new
                KvltTurnScoreSettlementResult(
                    sceneId,
                    globalTurn,
                    newScoreEvents
                );
        }

        private static
            SceneReleaseLegitimacyEvaluation
            RequireCurrentEvaluation(
                SceneRelease release,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseLegitimacyEvaluation>
                    legitimacyByRelease,
                int globalTurn)
        {
            if (!legitimacyByRelease.TryGetValue(
                    release.ReleaseId,
                    out
                        SceneReleaseLegitimacyEvaluation
                        evaluation))
            {
                throw new InvalidOperationException(
                    "Turn Score has no current legitimacy " +
                    "evaluation for Field release | " +
                    $"release={release.ReleaseId}"
                );
            }

            if (evaluation == null)
            {
                throw new InvalidOperationException(
                    "Turn Score legitimacy lookup contains " +
                    "a null evaluation | " +
                    $"release={release.ReleaseId}"
                );
            }

            if (evaluation.SourceReleaseId !=
                    release.ReleaseId ||
                evaluation.SourceDemoTapeId !=
                    release.SourceDemoTapeId ||
                evaluation.SourceOwnerEntityId !=
                    release.SourceOwnerEntityId)
            {
                throw new InvalidOperationException(
                    "Turn Score legitimacy provenance does " +
                    "not match SceneRelease | " +
                    $"release={release.ReleaseId}"
                );
            }

            if (evaluation.SettledTurn !=
                globalTurn)
            {
                throw new InvalidOperationException(
                    "Turn Score legitimacy evaluation " +
                    "belongs to another turn | " +
                    $"release={release.ReleaseId} | " +
                    $"evaluationTurn=" +
                    $"{evaluation.SettledTurn} | " +
                    $"scoreTurn={globalTurn}"
                );
            }

            return evaluation;
        }

        private static bool
            HasFetterTransitionAtTurn(
                SceneRelease release,
                int globalTurn)
        {
            foreach (
                SceneReleaseLifecycleTransition transition
                in release.LifecycleTransitions)
            {
                if (transition.ToState ==
                        SceneReleaseLifecycleState.Field &&
                    transition.GlobalTurn ==
                        globalTurn)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<SceneRelease>
            GetOrderedSceneReleases(
                IReadOnlyList<SceneRelease> releases,
                string sceneId)
        {
            List<SceneRelease>
                result =
                    new();

            HashSet<string>
                releaseIds =
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
                        "Turn Score release population " +
                        "cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!releaseIds.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Turn Score release population " +
                        "contains duplicate identity.",
                        nameof(releases)
                    );
                }

                if (release.HostedSceneNodeId !=
                    sceneId)
                {
                    continue;
                }

                result.Add(
                    release
                );
            }

            result.Sort(
                (
                    left,
                    right
                ) =>
                    string.CompareOrdinal(
                        left.ReleaseId,
                        right.ReleaseId
                    )
            );

            return result;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Turn Score identity cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}