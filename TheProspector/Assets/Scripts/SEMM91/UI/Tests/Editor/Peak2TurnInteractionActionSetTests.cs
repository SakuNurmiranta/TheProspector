using NUnit.Framework;
using SEMM91.GamePlay.Kvlt.TurnFlow;
using SEMM91.InputSystems;

namespace SEMM91.UI.Tests.Editor
{
    public sealed class Peak2TurnInteractionActionSetTests
    {
        [Test]
        public void IdleUsesTheNormalThreeDraftActions()
        {
            Peak2TurnInteractionActionSet result =
                Peak2TurnInteractionActionSet.Resolve(
                    KvltTurnResolutionRuntimePhase.Idle
                );

            Assert.That(result.OverridesTurnPlan, Is.False);
            Assert.That(
                result.PrimaryCommand,
                Is.EqualTo(PlayerCommand.DraftPrimaryAction));
            Assert.That(
                result.SecondaryCommand,
                Is.EqualTo(PlayerCommand.DraftSecondaryAction));
            Assert.That(result.HasTertiaryCommand, Is.True);
            Assert.That(
                result.TertiaryCommand,
                Is.EqualTo(PlayerCommand.DraftTertiaryAction));
        }

        [Test]
        public void HappeningWindowRoutesExistingButtonsToHails()
        {
            Peak2TurnInteractionActionSet result =
                Peak2TurnInteractionActionSet.Resolve(
                    KvltTurnResolutionRuntimePhase
                        .SharedHappeningWindowOpen
                );

            Assert.That(result.OverridesTurnPlan, Is.True);
            Assert.That(
                result.PrimaryCommand,
                Is.EqualTo(PlayerCommand.HailSatan));
            Assert.That(
                result.SecondaryCommand,
                Is.EqualTo(PlayerCommand.HailOdin));
            Assert.That(result.HasTertiaryCommand, Is.False);
            StringAssert.Contains(
                "HAPPENING WINDOW",
                result.WaitingLabel);
        }

        [Test]
        public void AllegianceCrisisRoutesExistingButtonsToVotes()
        {
            Peak2TurnInteractionActionSet result =
                Peak2TurnInteractionActionSet.Resolve(
                    KvltTurnResolutionRuntimePhase
                        .AwaitingAllegianceCrisisResolution
                );

            Assert.That(result.OverridesTurnPlan, Is.True);
            Assert.That(
                result.PrimaryCommand,
                Is.EqualTo(PlayerCommand.VoteSociety));
            Assert.That(
                result.SecondaryCommand,
                Is.EqualTo(PlayerCommand.VoteKvlt));
            Assert.That(result.HasTertiaryCommand, Is.False);
            StringAssert.Contains(
                "ALLEGIANCE CRISIS",
                result.WaitingLabel);
        }

        [Test]
        public void TransientSettlementPhaseDoesNotHijackTurnActions()
        {
            Peak2TurnInteractionActionSet result =
                Peak2TurnInteractionActionSet.Resolve(
                    KvltTurnResolutionRuntimePhase
                        .HappeningPrepared
                );

            Assert.That(result.OverridesTurnPlan, Is.False);
            Assert.That(result.HasTertiaryCommand, Is.True);
            Assert.That(result.WaitingLabel, Is.Empty);
        }
    }
}
