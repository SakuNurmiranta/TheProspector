using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions.History;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Paradigm.Tests.Editor
{
    public sealed class KvltParadigmRuntimeMountTests
    {
        [Test]
        public void WorldOwnsOneAuthoritativeParadigmState()
        {
            SeededWorldState world =
                new SeededWorldState(new CollectiveRegistry());

            Assert.That(world.KvltParadigmState, Is.Not.Null);
            Assert.That(
                world.KvltParadigmState.GripRecords,
                Is.Empty);
            Assert.That(
                world.KvltParadigmState.BeefRecords,
                Is.Empty);
            Assert.That(
                world.KvltParadigmState.PoserDeclarations,
                Is.Empty);
        }

        [Test]
        public void SeveralHailsMaterializeAgainstOneBehaviorOccurrence()
        {
            Happening happening = CreateResolvingHappening(
                4,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId);

            Assert.That(
                happening.BehaviorOccurrences.Count,
                Is.EqualTo(1));
            Assert.That(
                happening.HailOccurrences.Count,
                Is.EqualTo(3));

            string behaviorId = happening
                .BehaviorOccurrences[0]
                .BehaviorOccurrenceId;

            foreach (HailOccurrence hail
                     in happening.HailOccurrences)
            {
                Assert.That(
                    hail.BehaviorOccurrenceId,
                    Is.EqualTo(behaviorId));
            }
        }

        [Test]
        public void InclusiveThreeToTwoSplitCreatesPersistentBeef()
        {
            Happening happening = CreateResolvingHappening(
                7,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId);
            KvltParadigmState state = new();

            IReadOnlyList<KvltParadigmContestResult> results =
                new KvltParadigmContestSettlementService()
                    .SettleAll(
                        7,
                        new[] { happening },
                        Peak2KvltParadigmCatalog.Oppositions,
                        "KEEPER",
                        state);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].IsBeef, Is.True);
            Assert.That(
                results[0].NewPoserDeclarations,
                Is.Empty);
            Assert.That(
                state.BeefRecords.Count,
                Is.EqualTo(1));
            Assert.That(
                state.GripRecords.Count,
                Is.EqualTo(5));
        }

        [Test]
        public void FiveToTwoClearLossDeclaresTwoTurnPosers()
        {
            Happening happening = CreateResolvingHappening(
                9,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId);
            KvltParadigmState state = new();

            KvltParadigmContestResult result =
                new KvltParadigmContestSettlementService()
                    .SettleAll(
                        9,
                        new[] { happening },
                        Peak2KvltParadigmCatalog.Oppositions,
                        "KEEPER",
                        state)[0];

            Assert.That(result.IsBeef, Is.False);
            Assert.That(
                result.WinningHailAspectId,
                Is.EqualTo(
                    Peak2KvltParadigmCatalog.SatanAspectId));
            Assert.That(
                result.NewPoserDeclarations.Count,
                Is.EqualTo(2));
            Assert.That(
                result.NewPoserDeclarations[0].DurationTurns,
                Is.EqualTo(2));
            Assert.That(
                result.NewPoserDeclarations[0].ActiveFromTurn,
                Is.EqualTo(10));
            Assert.That(
                state.GripRecords.Count,
                Is.EqualTo(7));
        }

        [Test]
        public void ActivePoserLossDoesNotRefreshDeclaration()
        {
            KvltParadigmState state = new();
            state.TryDeclarePoser(
                new KvltPoserDeclaration(
                    "POSER:OLD",
                    "ACTOR_3",
                    "OLD_HAPPENING",
                    "OLD_BEHAVIOR",
                    Peak2KvltParadigmCatalog.OdinAspectId,
                    Peak2KvltParadigmCatalog.SatanAspectId,
                    4,
                    4));

            Happening happening = CreateResolvingHappening(
                5,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId);

            KvltParadigmContestResult result =
                new KvltParadigmContestSettlementService()
                    .SettleAll(
                        5,
                        new[] { happening },
                        Peak2KvltParadigmCatalog.Oppositions,
                        "KEEPER",
                        state)[0];

            Assert.That(
                result.NewPoserDeclarations,
                Is.Empty);
            Assert.That(
                state.PoserDeclarations.Count,
                Is.EqualTo(1));
            Assert.That(
                state.GetRemainingPoserdomTurns(
                    "ACTOR_3",
                    6),
                Is.EqualTo(3));
        }

        [Test]
        public void ActiveKeeperLossReinforcesGripWithoutPoserdom()
        {
            Happening happening = CreateResolvingHappening(
                12,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId);
            KvltParadigmState state = new();

            KvltParadigmContestResult result =
                new KvltParadigmContestSettlementService()
                    .SettleAll(
                        12,
                        new[] { happening },
                        Peak2KvltParadigmCatalog.Oppositions,
                        "ACTOR_3",
                        state)[0];

            Assert.That(
                result.NewPoserDeclarations,
                Is.Empty);
            Assert.That(
                state.PoserDeclarations,
                Is.Empty);
            Assert.That(
                state.GripRecords.Count,
                Is.EqualTo(4));
        }

        private static Happening CreateResolvingHappening(
            int turn,
            params string[] hailAspectIds)
        {
            Happening happening = new Happening(
                $"HAPPENING_{turn}",
                "KVLT",
                "ACTOR_0",
                "Cause",
                "Crux",
                "KVLT_NODE",
                turn,
                new CharacterActionKey(
                    "ACTOR_0",
                    turn,
                    0));

            happening.TryAddContext(
                new HappeningContext(
                    $"CONTEXT_{turn}",
                    "Contest one factual praxis episode",
                    HappeningContextAnchorKind.None,
                    string.Empty,
                    "ACTOR_0",
                    turn));

            for (int i = 0; i < hailAspectIds.Length; i++)
                happening.TryAddParticipant($"ACTOR_{i}");

            HappeningEnactBehaviorIntent primary =
                new HappeningEnactBehaviorIntent(
                    $"INTENT_{turn}_0",
                    happening.HappeningId,
                    happening.Contexts[0].ContextId,
                    "ACTOR_0",
                    turn,
                    "PUBLIC_PARADIGM_DECLARATION",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    hailAspectIds[0]);

            Assert.That(
                happening.TryRecordParticipantIntent(primary),
                Is.True);

            for (int i = 1; i < hailAspectIds.Length; i++)
            {
                Assert.That(
                    happening.TryRecordParticipantIntent(
                        new HappeningHailBehaviorIntent(
                            $"INTENT_{turn}_{i}",
                            happening.HappeningId,
                            happening.Contexts[0].ContextId,
                            $"ACTOR_{i}",
                            turn,
                            primary.IntentId,
                            hailAspectIds[i])),
                    Is.True);
            }

            Assert.That(
                happening.TryBeginResolving(turn),
                Is.True);

            new HappeningIntentSettlementService()
                .Settle(happening, turn);

            Assert.That(
                happening.TryMaterializeSuccessfulEnactBehavior(
                    primary.IntentId,
                    $"BEHAVIOR_{turn}",
                    $"HAIL_{turn}_0"),
                Is.True);

            for (int i = 1; i < hailAspectIds.Length; i++)
            {
                Assert.That(
                    happening.TryMaterializeSuccessfulHailBehavior(
                        $"INTENT_{turn}_{i}",
                        $"HAIL_{turn}_{i}"),
                    Is.True);
            }

            return happening;
        }
    }
}
