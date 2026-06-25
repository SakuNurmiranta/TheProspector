using NUnit.Framework;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Keeper
    .Tests.Editor
{
    public class
        KeeperInterventionResolverTests
    {
        private KeeperInterventionResolver
            _resolver;

        private SeededWorldState _world;
        private SceneRelease _release;

        [SetUp]
        public void SetUp()
        {
            _resolver =
                new KeeperInterventionResolver();

            _world =
                new SeededWorldState(
                    collectiveRegistry: null
                );

            _release =
                new SceneRelease(
                    displayName: "Test Release",
                    sourceDemoTapeId: "Demo_A",
                    sourceOwnerEntityId: "Owner_A",
                    hostedSceneNodeId: "KVLT",
                    releasedTurn: 1,
                    sourceConveyance: 0.75f
                );

            _world.AddSceneRelease(
                _release
            );
        }

        [Test]
        public void BoostSpendsPullAndStagesAdjustment()
        {
            KeeperTenureState tenure =
                CreateTenure(
                    pull: 1.0f
                );

            KeeperInterventionRequest request =
                CreateRequest(
                    KeeperInterventionType
                        .BoostVisibility,
                    pullSpend: 0.25f
                );

            bool success =
                _resolver.TryResolve(
                    request,
                    currentKeeperClientId: 1,
                    currentTurn: 4,
                    tenure,
                    _world,
                    out KeeperTenureState
                        nextTenure,
                    out KeeperInterventionResult
                        result
                );

            Assert.IsTrue(success);
            Assert.IsTrue(result.Succeeded);

            Assert.AreEqual(
                1.0f,
                tenure.Pull,
                0.0001f
            );

            Assert.AreEqual(
                0.75f,
                nextTenure.Pull,
                0.0001f
            );

            Assert.AreEqual(
                0.25f,
                result.PullSpent,
                0.0001f
            );

            Assert.AreEqual(
                0.25f,
                result.AppliedVisibilityDelta,
                0.0001f
            );

            Assert.AreEqual(
                0.25f,
                _release
                    .PendingVisibilityAdjustment,
                0.0001f
            );
        }

        [Test]
        public void SuppressionStagesNegativeAdjustment()
        {
            KeeperTenureState tenure =
                CreateTenure(
                    pull: 1.0f
                );

            KeeperInterventionRequest request =
                CreateRequest(
                    KeeperInterventionType
                        .SuppressVisibility,
                    pullSpend: 0.05f
                );

            bool success =
                _resolver.TryResolve(
                    request,
                    currentKeeperClientId: 1,
                    currentTurn: 4,
                    tenure,
                    _world,
                    out KeeperTenureState
                        nextTenure,
                    out KeeperInterventionResult
                        result
                );

            Assert.IsTrue(success);

            Assert.AreEqual(
                -0.05f,
                result.AppliedVisibilityDelta,
                0.0001f
            );

            Assert.AreEqual(
                -0.05f,
                _release
                    .PendingVisibilityAdjustment,
                0.0001f
            );

            Assert.AreEqual(
                0.95f,
                nextTenure.Pull,
                0.0001f
            );
        }

        [Test]
        public void WrongKeeperChangesNothing()
        {
            KeeperTenureState tenure =
                CreateTenure(
                    pull: 1.0f
                );

            KeeperInterventionRequest request =
                new KeeperInterventionRequest(
                    keeperClientId: 2,
                    releaseId:
                        _release.ReleaseId,
                    interventionType:
                        KeeperInterventionType
                            .BoostVisibility,
                    requestedTurn: 4,
                    pullSpend: 0.25f
                );

            bool success =
                _resolver.TryResolve(
                    request,
                    currentKeeperClientId: 1,
                    currentTurn: 4,
                    tenure,
                    _world,
                    out KeeperTenureState
                        nextTenure,
                    out KeeperInterventionResult
                        result
                );

            Assert.IsFalse(success);

            Assert.AreEqual(
                KeeperInterventionFailureReason
                    .NotCurrentKeeper,
                result.FailureReason
            );

            Assert.AreSame(
                tenure,
                nextTenure
            );

            Assert.IsFalse(
                _release
                    .HasPendingVisibilityAdjustment
            );
        }

        [Test]
        public void WrongTurnChangesNothing()
        {
            KeeperTenureState tenure =
                CreateTenure(
                    pull: 1.0f
                );

            KeeperInterventionRequest request =
                CreateRequest(
                    KeeperInterventionType
                        .BoostVisibility,
                    pullSpend: 0.25f,
                    requestedTurn: 3
                );

            bool success =
                _resolver.TryResolve(
                    request,
                    currentKeeperClientId: 1,
                    currentTurn: 4,
                    tenure,
                    _world,
                    out KeeperTenureState
                        nextTenure,
                    out KeeperInterventionResult
                        result
                );

            Assert.IsFalse(success);

            Assert.AreEqual(
                KeeperInterventionFailureReason
                    .InvalidTurn,
                result.FailureReason
            );

            Assert.AreSame(
                tenure,
                nextTenure
            );

            Assert.IsFalse(
                _release
                    .HasPendingVisibilityAdjustment
            );
        }

        [Test]
        public void MissingTenureChangesNothing()
        {
            KeeperInterventionRequest request =
                CreateRequest(
                    KeeperInterventionType
                        .BoostVisibility,
                    pullSpend: 0.25f
                );

            bool success =
                _resolver.TryResolve(
                    request,
                    currentKeeperClientId: 1,
                    currentTurn: 4,
                    currentTenure: null,
                    _world,
                    out KeeperTenureState
                        nextTenure,
                    out KeeperInterventionResult
                        result
                );

            Assert.IsFalse(success);

            Assert.AreEqual(
                KeeperInterventionFailureReason
                    .MissingTenure,
                result.FailureReason
            );

            Assert.IsNull(nextTenure);

            Assert.IsFalse(
                _release
                    .HasPendingVisibilityAdjustment
            );
        }

        [Test]
        public void MissingWorldChangesNothing()
        {
            KeeperTenureState tenure =
                CreateTenure(
                    pull: 1.0f
                );

            KeeperInterventionRequest request =
                CreateRequest(
                    KeeperInterventionType
                        .BoostVisibility,
                    pullSpend: 0.25f
                );

            bool success =
                _resolver.TryResolve(
                    request,
                    currentKeeperClientId: 1,
                    currentTurn: 4,
                    tenure,
                    world: null,
                    out KeeperTenureState
                        nextTenure,
                    out KeeperInterventionResult
                        result
                );

            Assert.IsFalse(success);

            Assert.AreEqual(
                KeeperInterventionFailureReason
                    .MissingWorldState,
                result.FailureReason
            );

            Assert.AreSame(
                tenure,
                nextTenure
            );
        }

        [Test]
        public void MissingReleaseChangesNothing()
        {
            KeeperTenureState tenure =
                CreateTenure(
                    pull: 1.0f
                );

            KeeperInterventionRequest request =
                new KeeperInterventionRequest(
                    keeperClientId: 1,
                    releaseId:
                        "Missing_Release",
                    interventionType:
                        KeeperInterventionType
                            .BoostVisibility,
                    requestedTurn: 4,
                    pullSpend: 0.25f
                );

            bool success =
                _resolver.TryResolve(
                    request,
                    currentKeeperClientId: 1,
                    currentTurn: 4,
                    tenure,
                    _world,
                    out KeeperTenureState
                        nextTenure,
                    out KeeperInterventionResult
                        result
                );

            Assert.IsFalse(success);

            Assert.AreEqual(
                KeeperInterventionFailureReason
                    .MissingRelease,
                result.FailureReason
            );

            Assert.AreSame(
                tenure,
                nextTenure
            );
        }

        [Test]
        public void InsufficientPullDoesNotStageAdjustment()
        {
            KeeperTenureState tenure =
                CreateTenure(
                    pull: 0.10f
                );

            KeeperInterventionRequest request =
                CreateRequest(
                    KeeperInterventionType
                        .BoostVisibility,
                    pullSpend: 0.25f
                );

            bool success =
                _resolver.TryResolve(
                    request,
                    currentKeeperClientId: 1,
                    currentTurn: 4,
                    tenure,
                    _world,
                    out KeeperTenureState
                        nextTenure,
                    out KeeperInterventionResult
                        result
                );

            Assert.IsFalse(success);

            Assert.AreEqual(
                KeeperInterventionFailureReason
                    .InsufficientPull,
                result.FailureReason
            );

            Assert.AreSame(
                tenure,
                nextTenure
            );

            Assert.IsFalse(
                _release
                    .HasPendingVisibilityAdjustment
            );
        }

        [Test]
        public void NoPossibleEffectDoesNotSpendPull()
        {
            bool prepared =
                _release
                    .TryStageVisibilityAdjustment(
                        requestedDelta: 1.0f,
                        out _
                    );

            Assert.IsTrue(prepared);

            float existingAdjustment =
                _release
                    .PendingVisibilityAdjustment;

            KeeperTenureState tenure =
                CreateTenure(
                    pull: 1.0f
                );

            KeeperInterventionRequest request =
                CreateRequest(
                    KeeperInterventionType
                        .BoostVisibility,
                    pullSpend: 0.25f
                );

            bool success =
                _resolver.TryResolve(
                    request,
                    currentKeeperClientId: 1,
                    currentTurn: 4,
                    tenure,
                    _world,
                    out KeeperTenureState
                        nextTenure,
                    out KeeperInterventionResult
                        result
                );

            Assert.IsFalse(success);

            Assert.AreEqual(
                KeeperInterventionFailureReason
                    .NoVisibilityEffect,
                result.FailureReason
            );

            Assert.AreSame(
                tenure,
                nextTenure
            );

            Assert.AreEqual(
                existingAdjustment,
                _release
                    .PendingVisibilityAdjustment,
                0.0001f
            );
        }

        private KeeperInterventionRequest
            CreateRequest(
                KeeperInterventionType type,
                float pullSpend,
                int requestedTurn = 4)
        {
            return new KeeperInterventionRequest(
                keeperClientId: 1,
                releaseId:
                    _release.ReleaseId,
                interventionType: type,
                requestedTurn,
                pullSpend
            );
        }

        private static KeeperTenureState
            CreateTenure(
                float pull)
        {
            return KeeperTenureState.Create(
                keeperClientId: 1,
                startedRound: 1,
                pullGrant: pull
            );
        }
    }
}