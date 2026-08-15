using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Events;

namespace SEMM91.GamePlay.Kvlt.Paradigm
    .Tests.Editor
{
    public sealed class
        KvltParadigmContestSettlementServiceTests
    {
        private const string FirstAspect =
            "ASPECT_ODIN";

        private const string SecondAspect =
            "ASPECT_SATAN";

        private readonly
            KvltParadigmContestSettlementService
            service =
                new();

        private readonly
            KvltParadigmOpposition
            opposition =
                new(
                    FirstAspect,
                    SecondAspect
                );

        [Test]
        public void
            TwoToThreeCreatesBeefWithoutPoserdom()
        {
            KvltParadigmState state =
                new();

            IReadOnlyList<
                    KvltParadigmContestResult>
                results =
                    Settle(
                        firstCount: 3,
                        secondCount: 2,
                        state
                    );

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].IsBeef, Is.True);
            Assert.That(
                results[0].NewPoserDeclarations.Count,
                Is.EqualTo(0)
            );

            Assert.That(
                state.BeefRecords.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                state.GripRecords.Count,
                Is.EqualTo(5)
            );
        }

        [Test]
        public void
            OneClearLoserReceivesFourPlayableTurns()
        {
            KvltParadigmState state =
                new();

            IReadOnlyList<
                    KvltParadigmContestResult>
                results =
                    Settle(
                        firstCount: 3,
                        secondCount: 1,
                        state,
                        settledTurn: 3
                    );

            KvltPoserDeclaration declaration =
                results[0]
                    .NewPoserDeclarations[0];

            Assert.That(
                declaration.EntityId,
                Is.EqualTo("B_0")
            );

            Assert.That(
                declaration.StartsTurn,
                Is.EqualTo(4)
            );

            Assert.That(
                declaration.DurationTurns,
                Is.EqualTo(4)
            );

            Assert.That(
                declaration.ExpiresAtTurnExclusive,
                Is.EqualTo(8)
            );

            Assert.That(
                declaration.IsActiveAt(4),
                Is.True
            );

            Assert.That(
                declaration.IsActiveAt(7),
                Is.True
            );

            Assert.That(
                declaration.IsActiveAt(8),
                Is.False
            );
        }

        [Test]
        public void
            TwoClearLosersEachReceiveTwoTurns()
        {
            KvltParadigmState state =
                new();

            IReadOnlyList<
                    KvltParadigmContestResult>
                results =
                    Settle(
                        firstCount: 5,
                        secondCount: 2,
                        state
                    );

            Assert.That(
                results[0]
                    .NewPoserDeclarations.Count,
                Is.EqualTo(2)
            );

            foreach (
                KvltPoserDeclaration declaration
                in results[0]
                    .NewPoserDeclarations)
            {
                Assert.That(
                    declaration.DurationTurns,
                    Is.EqualTo(2)
                );
            }
        }

        [Test]
        public void
            KeeperAndActivePoserDoNotInflateDuration()
        {
            KvltParadigmState state =
                new();

            Assert.That(
                state.TryDeclarePoser(
                    new KvltPoserDeclaration(
                        "POSER_PREEXISTING",
                        "B_1",
                        "HAPPENING_OLD",
                        "BEHAVIOR_OLD",
                        SecondAspect,
                        FirstAspect,
                        declaredDuringTurn: 1,
                        durationTurns: 4
                    )
                ),
                Is.True
            );

            IReadOnlyList<
                    KvltParadigmContestResult>
                results =
                    Settle(
                        firstCount: 7,
                        secondCount: 3,
                        state,
                        keeperEntityId: "B_0",
                        settledTurn: 3
                    );

            Assert.That(
                results[0]
                    .NewPoserDeclarations.Count,
                Is.EqualTo(1)
            );

            KvltPoserDeclaration declaration =
                results[0]
                    .NewPoserDeclarations[0];

            Assert.That(
                declaration.EntityId,
                Is.EqualTo("B_2")
            );

            Assert.That(
                declaration.DurationTurns,
                Is.EqualTo(4)
            );
        }

        [Test]
        public void
            ActivePoserdomIsNotRefreshed()
        {
            KvltParadigmState state =
                new();

            KvltPoserDeclaration existing =
                new(
                    "POSER_PREEXISTING",
                    "B_0",
                    "HAPPENING_OLD",
                    "BEHAVIOR_OLD",
                    SecondAspect,
                    FirstAspect,
                    declaredDuringTurn: 1,
                    durationTurns: 4
                );

            Assert.That(
                state.TryDeclarePoser(existing),
                Is.True
            );

            IReadOnlyList<
                    KvltParadigmContestResult>
                results =
                    Settle(
                        firstCount: 3,
                        secondCount: 1,
                        state,
                        settledTurn: 3
                    );

            Assert.That(
                results[0]
                    .NewPoserDeclarations.Count,
                Is.EqualTo(0)
            );

            Assert.That(
                state.PoserDeclarations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                state.PoserDeclarations[0]
                    .ExpiresAtTurnExclusive,
                Is.EqualTo(
                    existing
                        .ExpiresAtTurnExclusive
                )
            );
        }

        [Test]
        public void
            LosingHailStillReinforcesParadigmGrip()
        {
            KvltParadigmState state =
                new();

            Settle(
                firstCount: 3,
                secondCount: 1,
                state
            );

            Assert.That(
                state.TryGetGrip(
                    "B_0",
                    SecondAspect,
                    out KvltParadigmGripRecord grip
                ),
                Is.True
            );

            Assert.That(
                grip.ReinforcementCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            RepeatedBeefReinforcesPersistentRecord()
        {
            KvltParadigmState state =
                new();

            Settle(
                firstCount: 3,
                secondCount: 2,
                state,
                happeningId: "HAPPENING_1",
                behaviorOccurrenceId:
                    "BEHAVIOR_1",
                settledTurn: 3
            );

            Settle(
                firstCount: 3,
                secondCount: 2,
                state,
                happeningId: "HAPPENING_2",
                behaviorOccurrenceId:
                    "BEHAVIOR_2",
                settledTurn: 4
            );

            Assert.That(
                state.BeefRecords.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                state.BeefRecords[0]
                    .ReinforcementCount,
                Is.EqualTo(2)
            );

            Assert.That(
                state.BeefRecords[0]
                    .LastHappeningId,
                Is.EqualTo("HAPPENING_2")
            );
        }

        private IReadOnlyList<
                KvltParadigmContestResult>
            Settle(
                int firstCount,
                int secondCount,
                KvltParadigmState state,
                string keeperEntityId =
                    "KEEPER",
                int settledTurn = 3,
                string happeningId =
                    "HAPPENING",
                string behaviorOccurrenceId =
                    "BEHAVIOR")
        {
            BehaviorOccurrence behavior =
                new(
                    behaviorOccurrenceId,
                    happeningId,
                    "CONTEXT",
                    new[]
                    {
                        "ACTOR"
                    },
                    "CHURCH_FIRE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    "INTENT",
                    settledTurn
                );

            List<HailOccurrence> hails =
                new();

            for (int i = 0;
                 i < firstCount;
                 i++)
            {
                hails.Add(
                    new HailOccurrence(
                        $"HAIL_A_{happeningId}_{i}",
                        behaviorOccurrenceId,
                        $"A_{i}",
                        FirstAspect
                    )
                );
            }

            for (int i = 0;
                 i < secondCount;
                 i++)
            {
                hails.Add(
                    new HailOccurrence(
                        $"HAIL_B_{happeningId}_{i}",
                        behaviorOccurrenceId,
                        $"B_{i}",
                        SecondAspect
                    )
                );
            }

            return service.Settle(
                happeningId,
                new[]
                {
                    behavior
                },
                hails,
                opposition,
                keeperEntityId,
                state,
                settledTurn
            );
        }
    }
}