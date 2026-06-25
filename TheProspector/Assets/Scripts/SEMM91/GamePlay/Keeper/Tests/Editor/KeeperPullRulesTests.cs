using NUnit.Framework;

namespace SEMM91.GamePlay.Keeper
    .Tests.Editor
{
    public class KeeperPullRulesTests
    {
        [Test]
        public void InfluenceMapsDirectlyToPull()
        {
            Assert.AreEqual(
                1.75f,
                KeeperPullRules.FromInfluence(
                    1.75f
                ),
                0.0001f
            );
        }

        [TestCase(-1.0f)]
        [TestCase(0.0f)]
        public void NonPositiveInfluenceProducesNoPull(
            float influence)
        {
            Assert.AreEqual(
                0.0f,
                KeeperPullRules.FromInfluence(
                    influence
                )
            );
        }

        [Test]
        public void InvalidInfluenceProducesNoPull()
        {
            Assert.AreEqual(
                0.0f,
                KeeperPullRules.FromInfluence(
                    float.NaN
                )
            );

            Assert.AreEqual(
                0.0f,
                KeeperPullRules.FromInfluence(
                    float.PositiveInfinity
                )
            );
        }

        [Test]
        public void PullCanBeSpentImmutably()
        {
            KeeperTenureState original =
                KeeperTenureState.Create(
                    keeperClientId: 4,
                    startedRound: 2,
                    initialPull: 2.5f
                );

            bool success =
                original.TrySpendPull(
                    1.25f,
                    out KeeperTenureState result
                );

            Assert.IsTrue(success);

            Assert.AreEqual(
                2.5f,
                original.Pull,
                0.0001f
            );

            Assert.AreEqual(
                1.25f,
                result.Pull,
                0.0001f
            );

            Assert.AreNotSame(
                original,
                result
            );
        }

        [Test]
        public void InsufficientPullDoesNotChangeState()
        {
            KeeperTenureState original =
                KeeperTenureState.Create(
                    keeperClientId: 4,
                    startedRound: 2,
                    initialPull: 1.0f
                );

            bool success =
                original.TrySpendPull(
                    2.0f,
                    out KeeperTenureState result
                );

            Assert.IsFalse(success);
            Assert.AreSame(original, result);
        }

        [Test]
        public void AddingPullPreservesTenureIdentity()
        {
            KeeperTenureState original =
                new KeeperTenureState(
                    keeperClientId: 4,
                    startedRound: 2,
                    canonizationSubjectReleaseId:
                        "Release_A",
                    subjectTenureYears: 1,
                    pull: 0.5f
                );

            KeeperTenureState result =
                original.AddPull(1.25f);

            Assert.AreEqual(
                1.75f,
                result.Pull,
                0.0001f
            );

            Assert.AreEqual(
                original.KeeperClientId,
                result.KeeperClientId
            );

            Assert.AreEqual(
                original.StartedRound,
                result.StartedRound
            );

            Assert.AreEqual(
                original
                    .CanonizationSubjectReleaseId,
                result
                    .CanonizationSubjectReleaseId
            );

            Assert.AreEqual(
                original.SubjectTenureYears,
                result.SubjectTenureYears
            );
        }
    }
}