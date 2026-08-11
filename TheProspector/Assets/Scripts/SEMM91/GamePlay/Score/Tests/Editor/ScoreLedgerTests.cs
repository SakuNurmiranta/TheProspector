using NUnit.Framework;

namespace SEMM91.GamePlay.Score.Tests
{
    public class ScoreLedgerTests
    {
        [Test]
        public void Ledger_PreservesInsertionOrderAndExactHistory()
        {
            ScoreLedger ledger =
                new ScoreLedger();

            ScoreEvent first =
                Event(
                    "FIRST",
                    "PLAYER_A",
                    "RELEASE_A",
                    "DEMO_A",
                    ScoreEventKind.FirstFetterResonance,
                    1.5f,
                    3
                );

            ScoreEvent second =
                Event(
                    "SECOND",
                    "PLAYER_A",
                    "RELEASE_A",
                    "DEMO_A",
                    ScoreEventKind.FieldGravity,
                    0.5f,
                    4
                );

            Assert.That(
                ledger.TryRecord(first),
                Is.True
            );

            Assert.That(
                ledger.TryRecord(second),
                Is.True
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                ledger.History[0],
                Is.SameAs(first)
            );

            Assert.That(
                ledger.History[1],
                Is.SameAs(second)
            );
        }

        [Test]
        public void Ledger_RejectsDuplicateScoreEventIdentity()
        {
            ScoreLedger ledger =
                new ScoreLedger();

            Assert.That(
                ledger.TryRecord(
                    Event(
                        "SCORE",
                        "PLAYER_A",
                        "RELEASE_A",
                        "DEMO_A",
                        ScoreEventKind.FieldGravity,
                        1f,
                        4
                    )
                ),
                Is.True
            );

            Assert.That(
                ledger.TryRecord(
                    Event(
                        "SCORE",
                        "PLAYER_B",
                        "RELEASE_B",
                        "DEMO_B",
                        ScoreEventKind.FieldGravity,
                        2f,
                        5
                    )
                ),
                Is.False
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void LifetimeTotal_IsDerivedPerBeneficiary()
        {
            ScoreLedger ledger =
                new ScoreLedger();

            ledger.TryRecord(
                Event(
                    "A1",
                    "PLAYER_A",
                    "RELEASE_A",
                    "DEMO_A",
                    ScoreEventKind.FirstFetterResonance,
                    1.25f,
                    1
                )
            );

            ledger.TryRecord(
                Event(
                    "A2",
                    "PLAYER_A",
                    "RELEASE_A",
                    "DEMO_A",
                    ScoreEventKind.FieldGravity,
                    0.75f,
                    2
                )
            );

            ledger.TryRecord(
                Event(
                    "B1",
                    "PLAYER_B",
                    "RELEASE_B",
                    "DEMO_B",
                    ScoreEventKind.FieldGravity,
                    9f,
                    2
                )
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    "PLAYER_A"
                ),
                Is.EqualTo(2f)
                    .Within(0.0001f)
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    "PLAYER_B"
                ),
                Is.EqualTo(9f)
                    .Within(0.0001f)
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    "NO_SCORE"
                ),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void TurnRangeTotal_CanLaterRepresentYearInfluence()
        {
            ScoreLedger ledger =
                new ScoreLedger();

            ledger.TryRecord(
                Event(
                    "T1",
                    "PLAYER_A",
                    "RELEASE",
                    "DEMO",
                    ScoreEventKind.FieldGravity,
                    1f,
                    1
                )
            );

            ledger.TryRecord(
                Event(
                    "T2",
                    "PLAYER_A",
                    "RELEASE",
                    "DEMO",
                    ScoreEventKind.FieldGravity,
                    2f,
                    2
                )
            );

            ledger.TryRecord(
                Event(
                    "T5",
                    "PLAYER_A",
                    "RELEASE",
                    "DEMO",
                    ScoreEventKind.FieldGravity,
                    10f,
                    5
                )
            );

            Assert.That(
                ledger.GetTotalDuringTurns(
                    "PLAYER_A",
                    1,
                    4
                ),
                Is.EqualTo(3f)
            );

            Assert.That(
                ledger.GetTotalDuringTurns(
                    "PLAYER_A",
                    5,
                    8
                ),
                Is.EqualTo(10f)
            );
        }

        [Test]
        public void BeneficiaryHistory_IsFilteredWithoutLosingOrder()
        {
            ScoreLedger ledger =
                new ScoreLedger();

            ScoreEvent a1 =
                Event(
                    "A1",
                    "PLAYER_A",
                    "RELEASE_A",
                    "DEMO_A",
                    ScoreEventKind.FieldGravity,
                    1f,
                    1
                );

            ScoreEvent b =
                Event(
                    "B",
                    "PLAYER_B",
                    "RELEASE_B",
                    "DEMO_B",
                    ScoreEventKind.FieldGravity,
                    1f,
                    1
                );

            ScoreEvent a2 =
                Event(
                    "A2",
                    "PLAYER_A",
                    "RELEASE_C",
                    "DEMO_C",
                    ScoreEventKind.FieldGravity,
                    1f,
                    2
                );

            ledger.TryRecord(a1);
            ledger.TryRecord(b);
            ledger.TryRecord(a2);

            var playerA =
                ledger.GetForBeneficiary(
                    "PLAYER_A"
                );

            Assert.That(
                playerA.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                playerA[0],
                Is.SameAs(a1)
            );

            Assert.That(
                playerA[1],
                Is.SameAs(a2)
            );
        }

        [Test]
        public void ReleaseHistory_CanBeReconstructed()
        {
            ScoreLedger ledger =
                new ScoreLedger();

            ledger.TryRecord(
                Event(
                    "R1",
                    "PLAYER_A",
                    "RELEASE_A",
                    "DEMO",
                    ScoreEventKind
                        .FirstFetterResonance,
                    2f,
                    2
                )
            );

            ledger.TryRecord(
                Event(
                    "R2",
                    "PLAYER_A",
                    "RELEASE_A",
                    "DEMO",
                    ScoreEventKind.FieldGravity,
                    1f,
                    3
                )
            );

            ledger.TryRecord(
                Event(
                    "OTHER",
                    "PLAYER_A",
                    "RELEASE_B",
                    "DEMO",
                    ScoreEventKind.FieldGravity,
                    1f,
                    3
                )
            );

            var releaseHistory =
                ledger.GetForSceneRelease(
                    "RELEASE_A"
                );

            Assert.That(
                releaseHistory.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                releaseHistory[0].Kind,
                Is.EqualTo(
                    ScoreEventKind
                        .FirstFetterResonance
                )
            );

            Assert.That(
                releaseHistory[1].Kind,
                Is.EqualTo(
                    ScoreEventKind.FieldGravity
                )
            );
        }

        [Test]
        public void DemoTapeSceneHistory_CanSpanSeveralSceneReleases()
        {
            ScoreLedger ledger =
                new ScoreLedger();

            ledger.TryRecord(
                Event(
                    "ORIGINAL",
                    "PLAYER_A",
                    "RELEASE_A",
                    "DEMO_A",
                    ScoreEventKind
                        .FirstFetterResonance,
                    2f,
                    2
                )
            );

            ledger.TryRecord(
                Event(
                    "RERELEASE",
                    "PLAYER_A",
                    "RELEASE_A_2",
                    "DEMO_A",
                    ScoreEventKind.FieldGravity,
                    1f,
                    9
                )
            );

            ledger.TryRecord(
                new ScoreEvent(
                    "OTHER_SCENE",
                    "PLAYER_A",
                    "PLAYER_A",
                    "OTHER_SCENE",
                    "RELEASE_X",
                    "DEMO_A",
                    ScoreEventKind.FieldGravity,
                    1f,
                    9
                )
            );

            var sameDemoSameScene =
                ledger.GetForDemoTapeInScene(
                    "DEMO_A",
                    "KVLT"
                );

            Assert.That(
                sameDemoSameScene.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                sameDemoSameScene[0]
                    .SceneReleaseId,
                Is.EqualTo("RELEASE_A")
            );

            Assert.That(
                sameDemoSameScene[1]
                    .SceneReleaseId,
                Is.EqualTo("RELEASE_A_2")
            );
        }

        [Test]
        public void ZeroValueEvent_IsRetainedForProvenance()
        {
            ScoreLedger ledger =
                new ScoreLedger();

            ScoreEvent zeroGravity =
                Event(
                    "ZERO",
                    "PLAYER_A",
                    "RELEASE",
                    "DEMO",
                    ScoreEventKind.FieldGravity,
                    0f,
                    4
                );

            Assert.That(
                ledger.TryRecord(
                    zeroGravity
                ),
                Is.True
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    "PLAYER_A"
                ),
                Is.EqualTo(0f)
            );

            Assert.That(
                ledger.History[0],
                Is.SameAs(zeroGravity)
            );
        }

        private static ScoreEvent Event(
            string id,
            string beneficiary,
            string releaseId,
            string demoTapeId,
            ScoreEventKind kind,
            float amount,
            int turn)
        {
            return new ScoreEvent(
                id,
                beneficiary,
                beneficiary,
                "KVLT",
                releaseId,
                demoTapeId,
                kind,
                amount,
                turn
            );
        }
    }
}