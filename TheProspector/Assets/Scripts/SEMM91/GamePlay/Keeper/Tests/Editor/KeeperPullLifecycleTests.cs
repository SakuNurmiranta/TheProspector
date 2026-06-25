using NUnit.Framework;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Keeper
    .Tests.Editor
{
    public class KeeperPullLifecycleTests
    {
        private KeeperTransitionResolver
            _transitionResolver;

        private KeeperLegacyResolver
            _legacyResolver;

        private SeededWorldState _world;

        [SetUp]
        public void SetUp()
        {
            _transitionResolver =
                new KeeperTransitionResolver();

            _legacyResolver =
                new KeeperLegacyResolver();

            _world =
                new SeededWorldState(null);
        }

        [Test]
        public void InitialAssignmentUsesWinningOutputAsPull()
        {
            KeeperTransitionResult transition =
                _transitionResolver
                    .ResolveInitialAssignment(
                        resolvedRound: 1,
                        new[]
                        {
                            new KeeperCandidate(
                                clientId: 1,
                                ownerEntityId:
                                    "Owner_A",
                                sceneOutput: 2.5f
                            )
                        }
                    );

            KeeperLegacyResolution resolution =
                _legacyResolver.Resolve(
                    transition,
                    currentTenure: null,
                    _world,
                    nextKeeperOwnerEntityId:
                        "Owner_A"
                );

            Assert.AreEqual(
                2.5f,
                transition.PullGrant,
                0.0001f
            );

            Assert.AreEqual(
                2.5f,
                resolution.NextTenure.Pull,
                0.0001f
            );
        }

        [Test]
        public void RetentionAddsCurrentOutputToPull()
        {
            KeeperTenureState currentTenure =
                KeeperTenureState.Create(
                    keeperClientId: 1,
                    startedRound: 1,
                    pullGrant: 1.0f
                );

            KeeperTransitionResult transition =
                _transitionResolver
                    .ResolveYearEnd(
                        resolvedRound: 2,
                        currentKeeperClientId: 1,
                        new[]
                        {
                            new KeeperCandidate(
                                clientId: 1,
                                ownerEntityId:
                                    "Owner_A",
                                sceneOutput: 1.5f
                            ),

                            new KeeperCandidate(
                                clientId: 2,
                                ownerEntityId:
                                    "Owner_B",
                                sceneOutput: 1.0f
                            )
                        }
                    );

            KeeperLegacyResolution resolution =
                _legacyResolver.Resolve(
                    transition,
                    currentTenure,
                    _world,
                    "Owner_A"
                );

            Assert.AreEqual(
                KeeperTransitionReason
                    .YearEndRetained,
                transition.Reason
            );

            Assert.AreEqual(
                1.5f,
                transition.PullGrant,
                0.0001f
            );

            Assert.AreEqual(
                2.5f,
                resolution.NextTenure.Pull,
                0.0001f
            );
        }

        [Test]
        public void ReplacementStartsWithIncomingOutput()
        {
            KeeperTenureState outgoingTenure =
                KeeperTenureState.Create(
                    keeperClientId: 1,
                    startedRound: 1,
                    pullGrant: 8.0f
                );

            KeeperTransitionResult transition =
                _transitionResolver
                    .ResolveYearEnd(
                        resolvedRound: 2,
                        currentKeeperClientId: 1,
                        new[]
                        {
                            new KeeperCandidate(
                                clientId: 1,
                                ownerEntityId:
                                    "Owner_A",
                                sceneOutput: 1.0f
                            ),

                            new KeeperCandidate(
                                clientId: 2,
                                ownerEntityId:
                                    "Owner_B",
                                sceneOutput: 3.0f
                            )
                        }
                    );

            KeeperLegacyResolution resolution =
                _legacyResolver.Resolve(
                    transition,
                    outgoingTenure,
                    _world,
                    "Owner_B"
                );

            Assert.AreEqual(
                KeeperTransitionReason
                    .YearEndReplaced,
                transition.Reason
            );

            Assert.AreEqual(
                3.0f,
                resolution.NextTenure.Pull,
                0.0001f
            );

            Assert.AreEqual(
                2ul,
                resolution.NextTenure
                    .KeeperClientId
            );
        }

        [Test]
        public void DisconnectPreservesPullWithoutGrant()
        {
            KeeperTenureState currentTenure =
                KeeperTenureState.Create(
                    keeperClientId: 1,
                    startedRound: 1,
                    pullGrant: 2.25f
                );

            KeeperTransitionResult transition =
                _transitionResolver
                    .ResolveDisconnectionFallback(
                        resolvedRound: 1,
                        disconnectedKeeperClientId: 1,
                        new[]
                        {
                            new KeeperCandidate(
                                clientId: 2,
                                ownerEntityId:
                                    "Owner_B",
                                sceneOutput: 9.0f
                            )
                        }
                    );

            KeeperLegacyResolution resolution =
                _legacyResolver.Resolve(
                    transition,
                    currentTenure,
                    _world,
                    "Owner_B"
                );

            Assert.AreEqual(
                0.0f,
                transition.PullGrant,
                0.0001f
            );

            Assert.AreEqual(
                2.25f,
                resolution.NextTenure.Pull,
                0.0001f
            );
        }

        [Test]
        public void CollapseLockPreservesPullWithoutGrant()
        {
            KeeperTenureState currentTenure =
                KeeperTenureState.Create(
                    keeperClientId: 1,
                    startedRound: 1,
                    pullGrant: 2.25f
                );

            KeeperTransitionResult transition =
                _transitionResolver
                    .ResolveSceneCollapseLock(
                        resolvedRound: 2,
                        currentKeeperClientId: 1,
                        currentKeeperSceneOutput:
                            9.0f
                    );

            KeeperLegacyResolution resolution =
                _legacyResolver.Resolve(
                    transition,
                    currentTenure,
                    _world,
                    "Owner_A"
                );

            Assert.AreEqual(
                0.0f,
                transition.PullGrant,
                0.0001f
            );

            Assert.AreSame(
                currentTenure,
                resolution.NextTenure
            );

            Assert.AreEqual(
                2.25f,
                resolution.NextTenure.Pull,
                0.0001f
            );
        }
    }
}