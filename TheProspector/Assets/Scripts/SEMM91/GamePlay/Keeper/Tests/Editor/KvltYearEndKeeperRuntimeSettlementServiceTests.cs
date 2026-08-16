using NUnit.Framework;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Score;

namespace SEMM91.GamePlay.Keeper.Tests.Editor
{
    public sealed class
        KvltYearEndKeeperRuntimeSettlementServiceTests
    {
        private readonly
            KvltYearEndKeeperRuntimeSettlementService
            service =
                new();

        [Test]
        public void
            YearInfluenceUsesGravityAndExcludesResonance()
        {
            ScoreLedger ledger =
                new();

            Assert.That(
                ledger.TryRecord(
                    Score(
                        "RESONANCE",
                        ScoreEventKind
                            .FirstFetterResonance,
                        20f,
                        0
                    )
                ),
                Is.True
            );

            Assert.That(
                ledger.TryRecord(
                    Score(
                        "GRAVITY",
                        ScoreEventKind
                            .FieldGravity,
                        3f,
                        3
                    )
                ),
                Is.True
            );

            var evaluations =
                service.EvaluateYearInfluence(
                    "KVLT",
                    settledTurn:
                        3,
                    turnsPerYear:
                        4,
                    beneficiaryEntityIds:
                    new[]
                    {
                        "PLAYER"
                    },
                    ledger:
                        ledger
                );

            Assert.That(
                evaluations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                evaluations[0].YearInfluence,
                Is.EqualTo(3f)
                    .Within(0.0001f)
            );

            Assert.That(
                evaluations[0]
                    .ContributingEvents.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            RetainedKeeperPreservesTenureIdentityAndAddsPull()
        {
            KeeperTenureState current =
                KeeperTenureState.Create(
                    keeperClientId:
                        1,
                    startedRound:
                        0,
                    pullGrant:
                        1f
                );

            KeeperTransitionResult transition =
                Transition(
                    KeeperTransitionReason
                        .YearEndRetained,
                    previous:
                        1,
                    next:
                        1,
                    influence:
                        2f,
                    pullGrant:
                        2f
                );

            KeeperTenureState next =
                service.CreateNextTenure(
                    transition,
                    current
                );

            Assert.That(
                next.KeeperTenureId,
                Is.EqualTo(
                    current.KeeperTenureId
                )
            );

            Assert.That(
                next.Pull,
                Is.EqualTo(3f)
                    .Within(0.0001f)
            );

            Assert.That(
                service.ApplyCanonTenureTransition(
                    System.Array.Empty<
                        SceneRelease>(),
                    current,
                    next,
                    settledTurn:
                        3
                ),
                Is.Empty
            );
        }

        [Test]
        public void
            ReplacementCreatesNewContinuousTenure()
        {
            KeeperTenureState current =
                KeeperTenureState.Create(
                    keeperClientId:
                        1,
                    startedRound:
                        0,
                    pullGrant:
                        8f
                );

            KeeperTransitionResult transition =
                Transition(
                    KeeperTransitionReason
                        .YearEndReplaced,
                    previous:
                        1,
                    next:
                        2,
                    influence:
                        4f,
                    pullGrant:
                        4f
                );

            KeeperTenureState next =
                service.CreateNextTenure(
                    transition,
                    current
                );

            Assert.That(
                next.KeeperClientId,
                Is.EqualTo(2ul)
            );

            Assert.That(
                next.KeeperTenureId,
                Is.Not.EqualTo(
                    current.KeeperTenureId
                )
            );

            Assert.That(
                next.StartedRound,
                Is.EqualTo(1)
            );

            Assert.That(
                next.Pull,
                Is.EqualTo(4f)
                    .Within(0.0001f)
            );
        }

        private static ScoreEvent Score(
            string id,
            ScoreEventKind kind,
            float amount,
            int turn)
        {
            return new ScoreEvent(
                id,
                "PLAYER",
                "PLAYER",
                "KVLT",
                "RELEASE",
                "DEMO",
                kind,
                amount,
                turn
            );
        }

        private static KeeperTransitionResult
            Transition(
                KeeperTransitionReason reason,
                ulong previous,
                ulong next,
                float influence,
                float pullGrant)
        {
            return new KeeperTransitionResult(
                resolvedRound:
                    1,
                reason:
                    reason,
                previousKeeperClientId:
                    previous,
                nextKeeperClientId:
                    next,
                previousSubjectReleaseId:
                    string.Empty,
                canonizedReleaseId:
                    string.Empty,
                incomingSubjectReleaseId:
                    string.Empty,
                winningSceneOutput:
                    influence,
                pullGrant:
                    pullGrant
            );
        }
    }
}