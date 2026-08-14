using NUnit.Framework;

namespace SEMM91.GamePlay.Kvlt.TurnFlow.Tests.Editor
{
    public sealed class
        KvltTurnResolutionRuntimePhaseTests
    {
        [Test]
        public void
            SharedHappeningWindowPrecedesPreparation()
        {
            Assert.That(
                (int)KvltTurnResolutionRuntimePhase
                    .ExistingFieldSettled,
                Is.LessThan(
                    (int)KvltTurnResolutionRuntimePhase
                        .SharedHappeningWindowOpen
                )
            );

            Assert.That(
                (int)KvltTurnResolutionRuntimePhase
                    .SharedHappeningWindowOpen,
                Is.LessThan(
                    (int)KvltTurnResolutionRuntimePhase
                        .HappeningPrepared
                )
            );
        }

        [Test]
        public void
            HappeningVotingPauseExistsBetweenPreparationAndSettlement()
        {
            Assert.That(
                (int)KvltTurnResolutionRuntimePhase
                    .HappeningPrepared,
                Is.LessThan(
                    (int)KvltTurnResolutionRuntimePhase
                        .AwaitingAllegianceCrisisResolution
                )
            );

            Assert.That(
                (int)KvltTurnResolutionRuntimePhase
                    .AwaitingAllegianceCrisisResolution,
                Is.LessThan(
                    (int)KvltTurnResolutionRuntimePhase
                        .HappeningSettled
                )
            );
        }

        [Test]
        public void
            CanonCannotOccurBeforeHappeningSettlement()
        {
            Assert.That(
                (int)KvltTurnResolutionRuntimePhase
                    .HappeningSettled,
                Is.LessThan(
                    (int)KvltTurnResolutionRuntimePhase
                        .YearEndCanonSettled
                )
            );
        }
        
        [Test]
        public void
            CanonPrecedesScoreAndScorePrecedesSuccession()
        {
            Assert.That(
                (int)KvltTurnResolutionRuntimePhase
                    .YearEndCanonSettled,
                Is.LessThan(
                    (int)KvltTurnResolutionRuntimePhase
                        .TurnScoreSettled
                )
            );

            Assert.That(
                (int)KvltTurnResolutionRuntimePhase
                    .TurnScoreSettled,
                Is.LessThan(
                    (int)KvltTurnResolutionRuntimePhase
                        .YearEndSuccessionSettled
                )
            );
        }

        [Test]
        public void
            PublicationIsTerminalRuntimeCheckpoint()
        {
            Assert.That(
                (int)KvltTurnResolutionRuntimePhase
                    .Published,
                Is.GreaterThan(
                    (int)KvltTurnResolutionRuntimePhase
                        .NextSceneEnvironmentSettled
                )
            );
        }
    }
}