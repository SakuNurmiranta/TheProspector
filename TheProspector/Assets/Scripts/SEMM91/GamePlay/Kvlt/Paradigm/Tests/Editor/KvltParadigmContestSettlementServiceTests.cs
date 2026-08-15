using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Events;

namespace SEMM91.GamePlay.Kvlt.Paradigm.Tests.Editor
{
    public sealed class KvltParadigmContestSettlementServiceTests
    {
        private const string BehaviorTypeId =
            "PUBLIC_PARADIGM_DECLARATION";
        private const string KeeperEntityId = "KEEPER";

        [Test]
        public void EverySettledHailReinforcesDeclarerGrip()
        {
            const int turn = 10;
            BehaviorOccurrence behavior = CreateBehavior(
                "HAPPENING_GRIP",
                "BEHAVIOR_GRIP",
                turn);
            HailOccurrence[] hails = CreateHails(
                behavior,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId);
            KvltParadigmState state = new();

            Settle(turn, behavior, hails, KeeperEntityId, state);

            Assert.That(state.GripRecords.Count, Is.EqualTo(3));
            Assert.That(
                state.TryGetGrip(
                    "ACTOR_2",
                    Peak2KvltParadigmCatalog.OdinAspectId,
                    out KvltParadigmGripRecord losingGrip),
                Is.True);
            Assert.That(losingGrip.ReinforcementCount, Is.EqualTo(1));
            Assert.That(
                losingGrip.LastHailOccurrenceId,
                Is.EqualTo("HAPPENING_GRIP:HAIL:2"));
        }

        [Test]
        public void InclusiveThreeToTwoBoundaryCreatesBeef()
        {
            const int turn = 11;
            BehaviorOccurrence behavior = CreateBehavior(
                "HAPPENING_BOUNDARY_BEEF",
                "BEHAVIOR_BOUNDARY_BEEF",
                turn);
            HailOccurrence[] hails = CreateHails(
                behavior,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId);
            KvltParadigmState state = new();

            KvltParadigmContestResult result = Settle(
                turn,
                behavior,
                hails,
                KeeperEntityId,
                state);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsBeef, Is.True);
            Assert.That(result.StrongSideCount, Is.EqualTo(3));
            Assert.That(result.WeakSideCount, Is.EqualTo(2));
            Assert.That(result.NewPoserDeclarations, Is.Empty);
            Assert.That(state.BeefRecords.Count, Is.EqualTo(1));
        }

        [Test]
        public void ClearLossDeclaresPoserStartingNextTurn()
        {
            const int turn = 12;
            BehaviorOccurrence behavior = CreateBehavior(
                "HAPPENING_CLEAR_LOSS",
                "BEHAVIOR_CLEAR_LOSS",
                turn);
            HailOccurrence[] hails = CreateHails(
                behavior,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId);
            KvltParadigmState state = new();

            KvltParadigmContestResult result = Settle(
                turn,
                behavior,
                hails,
                KeeperEntityId,
                state);

            Assert.That(result.IsBeef, Is.False);
            Assert.That(
                result.WinningHailAspectId,
                Is.EqualTo(
                    Peak2KvltParadigmCatalog.SatanAspectId));
            Assert.That(
                result.LosingHailAspectId,
                Is.EqualTo(
                    Peak2KvltParadigmCatalog.OdinAspectId));
            Assert.That(
                result.NewPoserDeclarations.Count,
                Is.EqualTo(1));

            KvltPoserDeclaration poser =
                result.NewPoserDeclarations[0];

            Assert.That(poser.EntityId, Is.EqualTo("ACTOR_3"));
            Assert.That(poser.DurationTurns, Is.EqualTo(4));
            Assert.That(poser.StartsTurn, Is.EqualTo(turn + 1));
            Assert.That(poser.IsActiveAt(turn), Is.False);
            Assert.That(poser.IsActiveAt(turn + 1), Is.True);
        }

        [Test]
        public void PoserdomDurationScalesByNewLosingPoserCount()
        {
            const int turn = 13;
            BehaviorOccurrence behavior = CreateBehavior(
                "HAPPENING_SCALED_POSERDOM",
                "BEHAVIOR_SCALED_POSERDOM",
                turn);
            HailOccurrence[] hails = CreateHails(
                behavior,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId);
            KvltParadigmState state = new();

            KvltParadigmContestResult result = Settle(
                turn,
                behavior,
                hails,
                KeeperEntityId,
                state);

            Assert.That(result.IsBeef, Is.False);
            Assert.That(
                result.NewPoserDeclarations.Count,
                Is.EqualTo(2));
            Assert.That(
                result.NewPoserDeclarations[0].DurationTurns,
                Is.EqualTo(2));
            Assert.That(
                result.NewPoserDeclarations[1].DurationTurns,
                Is.EqualTo(2));
            Assert.That(
                state.PoserDeclarations.Count,
                Is.EqualTo(2));
        }

        [Test]
        public void ActiveKeeperIsImmuneButLosingGripPersists()
        {
            const int turn = 14;
            BehaviorOccurrence behavior = CreateBehavior(
                "HAPPENING_KEEPER_IMMUNITY",
                "BEHAVIOR_KEEPER_IMMUNITY",
                turn);
            HailOccurrence[] hails = CreateHails(
                behavior,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId);
            KvltParadigmState state = new();

            KvltParadigmContestResult result = Settle(
                turn,
                behavior,
                hails,
                "ACTOR_3",
                state);

            Assert.That(result.NewPoserDeclarations, Is.Empty);
            Assert.That(state.PoserDeclarations, Is.Empty);
            Assert.That(
                state.TryGetGrip(
                    "ACTOR_3",
                    Peak2KvltParadigmCatalog.OdinAspectId,
                    out KvltParadigmGripRecord keeperGrip),
                Is.True);
            Assert.That(keeperGrip.ReinforcementCount, Is.EqualTo(1));
        }

        [Test]
        public void ExistingActivePoserLossDoesNotRefreshDeclaration()
        {
            const int turn = 15;
            KvltParadigmState state = new();
            KvltPoserDeclaration existing =
                new KvltPoserDeclaration(
                    "POSER_EXISTING",
                    "ACTOR_3",
                    "HAPPENING_OLD",
                    "BEHAVIOR_OLD",
                    Peak2KvltParadigmCatalog.OdinAspectId,
                    Peak2KvltParadigmCatalog.SatanAspectId,
                    turn - 1,
                    4);

            Assert.That(state.TryDeclarePoser(existing), Is.True);

            BehaviorOccurrence behavior = CreateBehavior(
                "HAPPENING_NO_REFRESH",
                "BEHAVIOR_NO_REFRESH",
                turn);
            HailOccurrence[] hails = CreateHails(
                behavior,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.SatanAspectId,
                Peak2KvltParadigmCatalog.OdinAspectId);

            KvltParadigmContestResult result = Settle(
                turn,
                behavior,
                hails,
                KeeperEntityId,
                state);

            Assert.That(result.NewPoserDeclarations, Is.Empty);
            Assert.That(state.PoserDeclarations.Count, Is.EqualTo(1));
            Assert.That(
                state.PoserDeclarations[0].DeclarationId,
                Is.EqualTo(existing.DeclarationId));
            Assert.That(
                state.GetRemainingPoserdomTurns(
                    "ACTOR_3",
                    turn + 1),
                Is.EqualTo(3));
        }

        [Test]
        public void RepeatedBeefReinforcesOnePersistentRecord()
        {
            KvltParadigmState state = new();
            BehaviorOccurrence firstBehavior = CreateBehavior(
                "HAPPENING_BEEF_FIRST",
                "BEHAVIOR_BEEF_FIRST",
                16);
            BehaviorOccurrence secondBehavior = CreateBehavior(
                "HAPPENING_BEEF_SECOND",
                "BEHAVIOR_BEEF_SECOND",
                17);

            KvltParadigmContestResult first = Settle(
                16,
                firstBehavior,
                CreateHails(
                    firstBehavior,
                    Peak2KvltParadigmCatalog.SatanAspectId,
                    Peak2KvltParadigmCatalog.SatanAspectId,
                    Peak2KvltParadigmCatalog.SatanAspectId,
                    Peak2KvltParadigmCatalog.OdinAspectId,
                    Peak2KvltParadigmCatalog.OdinAspectId),
                KeeperEntityId,
                state);
            KvltParadigmContestResult second = Settle(
                17,
                secondBehavior,
                CreateHails(
                    secondBehavior,
                    Peak2KvltParadigmCatalog.SatanAspectId,
                    Peak2KvltParadigmCatalog.SatanAspectId,
                    Peak2KvltParadigmCatalog.OdinAspectId,
                    Peak2KvltParadigmCatalog.OdinAspectId),
                KeeperEntityId,
                state);

            Assert.That(first.IsBeef, Is.True);
            Assert.That(second.IsBeef, Is.True);
            Assert.That(state.BeefRecords.Count, Is.EqualTo(1));

            KvltParadigmBeefRecord beef = state.BeefRecords[0];

            Assert.That(beef.ReinforcementCount, Is.EqualTo(2));
            Assert.That(
                beef.FirstHappeningId,
                Is.EqualTo("HAPPENING_BEEF_FIRST"));
            Assert.That(
                beef.LastHappeningId,
                Is.EqualTo("HAPPENING_BEEF_SECOND"));
            Assert.That(beef.LastSettledTurn, Is.EqualTo(17));
        }

        private static KvltParadigmContestResult Settle(
            int settledTurn,
            BehaviorOccurrence behavior,
            HailOccurrence[] hails,
            string activeKeeperEntityId,
            KvltParadigmState state)
        {
            return new KvltParadigmContestSettlementService()
                .Settle(
                    behavior.HappeningId,
                    settledTurn,
                    behavior,
                    hails,
                    Peak2KvltParadigmCatalog.Oppositions[0],
                    activeKeeperEntityId,
                    state);
        }

        private static BehaviorOccurrence CreateBehavior(
            string happeningId,
            string behaviorOccurrenceId,
            int resolvedTurn)
        {
            return new BehaviorOccurrence(
                behaviorOccurrenceId,
                happeningId,
                $"{happeningId}:CONTEXT",
                new[] { "ACTOR_0" },
                BehaviorTypeId,
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Dominant,
                $"{happeningId}:INTENT",
                resolvedTurn);
        }

        private static HailOccurrence[] CreateHails(
            BehaviorOccurrence behavior,
            params string[] hailAspectIds)
        {
            HailOccurrence[] hails =
                new HailOccurrence[hailAspectIds.Length];

            for (int i = 0; i < hailAspectIds.Length; i++)
            {
                hails[i] = new HailOccurrence(
                    $"{behavior.HappeningId}:HAIL:{i}",
                    behavior.BehaviorOccurrenceId,
                    $"ACTOR_{i}",
                    hailAspectIds[i]);
            }

            return hails;
        }
    }
}
