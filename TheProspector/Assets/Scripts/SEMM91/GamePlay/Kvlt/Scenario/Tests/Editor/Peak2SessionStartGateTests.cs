using NUnit.Framework;

namespace SEMM91.GamePlay.Kvlt.Scenario.Tests.Editor
{
    public sealed class Peak2SessionStartGateTests
    {
        [Test]
        public void EmptyRosterWaitsForParticipants()
        {
            Assert.That(
                Evaluate(0, 0, 0, 0),
                Is.EqualTo(
                    Peak2SessionStartGateStatus
                        .WaitingForParticipants));
        }

        [Test]
        public void TwoPeersCannotStartFivePlayerScenario()
        {
            Assert.That(
                Evaluate(2, 2, 1, 1),
                Is.EqualTo(
                    Peak2SessionStartGateStatus
                        .WaitingForParticipants));
        }

        [Test]
        public void FiveConnectedPeersWaitForClassification()
        {
            Assert.That(
                Evaluate(5, 4, 1, 3),
                Is.EqualTo(
                    Peak2SessionStartGateStatus
                        .WaitingForClassification));
        }

        [Test]
        public void ClassifiedPeersWaitForEveryReadyAcknowledgement()
        {
            Assert.That(
                Evaluate(5, 4, 1, 4),
                Is.EqualTo(
                    Peak2SessionStartGateStatus
                        .WaitingForReadiness));
        }

        [Test]
        public void FiveHumansCannotStartCanonicalScenario()
        {
            Assert.That(
                Evaluate(5, 5, 5, 0),
                Is.EqualTo(
                    Peak2SessionStartGateStatus
                        .InvalidRoleComposition));
        }

        [Test]
        public void OneHumanAndFourReadyBotsCanStart()
        {
            Assert.That(
                Evaluate(5, 5, 1, 4),
                Is.EqualTo(
                    Peak2SessionStartGateStatus.Ready));
        }

        [Test]
        public void SixthPlayablePeerInvalidatesCanonicalRoster()
        {
            Assert.That(
                Evaluate(6, 6, 1, 5),
                Is.EqualTo(
                    Peak2SessionStartGateStatus
                        .InvalidParticipantCount));
        }

        [Test]
        public void SoloDevelopmentModeKeepsItsOnePeerContract()
        {
            Peak2SessionRosterCounts roster =
                new Peak2SessionRosterCounts(
                    connectedPlayers: 1,
                    readyPlayers: 0,
                    humanPlayers: 0,
                    botPlayers: 0);

            Assert.That(
                Peak2SessionStartGate.Evaluate(
                    requiredPlayers: 5,
                    roster,
                    allowSoloDevelopmentMode: true),
                Is.EqualTo(
                    Peak2SessionStartGateStatus.Ready));
        }

        [Test]
        public void ImpossibleCountsAreReportedAsInvalidRosterState()
        {
            Assert.That(
                Evaluate(2, 3, 1, 1),
                Is.EqualTo(
                    Peak2SessionStartGateStatus
                        .InvalidRosterState));
        }

        private static Peak2SessionStartGateStatus Evaluate(
            int connected,
            int ready,
            int humans,
            int bots)
        {
            return Peak2SessionStartGate.Evaluate(
                requiredPlayers: 5,
                new Peak2SessionRosterCounts(
                    connected,
                    ready,
                    humans,
                    bots),
                allowSoloDevelopmentMode: false);
        }
    }
}
