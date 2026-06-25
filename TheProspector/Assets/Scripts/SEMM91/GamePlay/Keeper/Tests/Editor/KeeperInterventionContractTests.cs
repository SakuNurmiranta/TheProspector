using NUnit.Framework;

namespace SEMM91.GamePlay.Keeper
    .Tests.Editor
{
    public class KeeperInterventionContractTests
    {
        [Test]
        public void ValidBoostRequestPassesValidation()
        {
            KeeperInterventionRequest request =
                CreateRequest(
                    KeeperInterventionType
                        .BoostVisibility
                );

            Assert.AreEqual(
                KeeperInterventionFailureReason.None,
                KeeperPullRules
                    .ValidateInterventionRequest(
                        request
                    )
            );
        }

        [Test]
        public void ValidSuppressRequestPassesValidation()
        {
            KeeperInterventionRequest request =
                CreateRequest(
                    KeeperInterventionType
                        .SuppressVisibility
                );

            Assert.AreEqual(
                KeeperInterventionFailureReason.None,
                KeeperPullRules
                    .ValidateInterventionRequest(
                        request
                    )
            );
        }

        [Test]
        public void EmptyReleaseIsRejected()
        {
            KeeperInterventionRequest request =
                new KeeperInterventionRequest(
                    keeperClientId: 1,
                    releaseId: string.Empty,
                    KeeperInterventionType
                        .BoostVisibility,
                    requestedTurn: 2,
                    pullSpend: 1.0f
                );

            Assert.AreEqual(
                KeeperInterventionFailureReason
                    .InvalidRelease,
                KeeperPullRules
                    .ValidateInterventionRequest(
                        request
                    )
            );
        }

        [Test]
        public void ZeroPullSpendIsRejected()
        {
            KeeperInterventionRequest request =
                new KeeperInterventionRequest(
                    keeperClientId: 1,
                    releaseId: "Release_A",
                    KeeperInterventionType
                        .BoostVisibility,
                    requestedTurn: 2,
                    pullSpend: 0.0f
                );

            Assert.AreEqual(
                KeeperInterventionFailureReason
                    .InvalidPullSpend,
                KeeperPullRules
                    .ValidateInterventionRequest(
                        request
                    )
            );
        }

        [Test]
        public void NoneInterventionIsRejected()
        {
            KeeperInterventionRequest request =
                CreateRequest(
                    KeeperInterventionType.None
                );

            Assert.AreEqual(
                KeeperInterventionFailureReason
                    .UnsupportedIntervention,
                KeeperPullRules
                    .ValidateInterventionRequest(
                        request
                    )
            );
        }

        private static KeeperInterventionRequest
            CreateRequest(
                KeeperInterventionType type)
        {
            return new KeeperInterventionRequest(
                keeperClientId: 1,
                releaseId: "Release_A",
                interventionType: type,
                requestedTurn: 2,
                pullSpend: 1.0f
            );
        }
    }
}