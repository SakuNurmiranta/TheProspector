using NUnit.Framework;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Keeper
    .Tests.Editor
{
    public class KeeperLegacyResolverTests
    {
        private KeeperLegacyResolver _resolver;
        private SeededWorldState _world;

        [SetUp]
        public void SetUp()
        {
            _resolver =
                new KeeperLegacyResolver();

            _world =
                new SeededWorldState(null);
        }

        [Test]
        public void InitialAssignmentBeginsSubject()
        {
            SceneRelease release =
                AddRelease(
                    "Release A",
                    "Owner_A",
                    releasedTurn: 2
                );

            KeeperLegacyResolution resolution =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .InitialAssignment,
                        previousKeeper:
                            ulong.MaxValue,
                        nextKeeper: 1,
                        round: 1
                    ),
                    currentTenure: null,
                    _world,
                    nextKeeperOwnerEntityId:
                        "Owner_A"
                );

            Assert.AreEqual(
                SceneLegacyState
                    .CanonizationSubject,
                release.LegacyState
            );

            Assert.AreEqual(
                release.ReleaseId,
                resolution.TransitionResult
                    .IncomingSubjectReleaseId
            );

            Assert.AreEqual(
                release.ReleaseId,
                resolution.NextTenure
                    .CanonizationSubjectReleaseId
            );

            Assert.AreEqual(
                0,
                resolution.NextTenure
                    .SubjectTenureYears
            );
        }

        [Test]
        public void RetentionAdvancesExistingSubject()
        {
            SceneRelease release =
                AddRelease(
                    "Release A",
                    "Owner_A",
                    releasedTurn: 2
                );

            KeeperLegacyResolution initial =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .InitialAssignment,
                        ulong.MaxValue,
                        nextKeeper: 1,
                        round: 1
                    ),
                    currentTenure: null,
                    _world,
                    "Owner_A"
                );

            KeeperLegacyResolution retained =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .YearEndRetained,
                        previousKeeper: 1,
                        nextKeeper: 1,
                        round: 2
                    ),
                    initial.NextTenure,
                    _world,
                    "Owner_A"
                );

            Assert.AreEqual(
                1,
                release
                    .CanonizationSubjectYears
            );

            Assert.AreEqual(
                1,
                retained.NextTenure
                    .SubjectTenureYears
            );

            Assert.AreEqual(
                release.ReleaseId,
                retained.TransitionResult
                    .PreviousSubjectReleaseId
            );

            Assert.AreEqual(
                release.ReleaseId,
                retained.TransitionResult
                    .IncomingSubjectReleaseId
            );

            Assert.IsEmpty(
                retained.TransitionResult
                    .CanonizedReleaseId
            );
        }

        [Test]
        public void ReplacementCanonizesOutgoingAndBeginsIncoming()
        {
            SceneRelease outgoing =
                AddRelease(
                    "Outgoing",
                    "Owner_A",
                    releasedTurn: 1
                );

            SceneRelease incoming =
                AddRelease(
                    "Incoming",
                    "Owner_B",
                    releasedTurn: 3
                );

            KeeperLegacyResolution initial =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .InitialAssignment,
                        ulong.MaxValue,
                        nextKeeper: 1,
                        round: 1
                    ),
                    null,
                    _world,
                    "Owner_A"
                );

            KeeperLegacyResolution replaced =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .YearEndReplaced,
                        previousKeeper: 1,
                        nextKeeper: 2,
                        round: 2
                    ),
                    initial.NextTenure,
                    _world,
                    "Owner_B"
                );

            Assert.AreEqual(
                SceneLegacyState.Canonized,
                outgoing.LegacyState
            );

            Assert.AreEqual(
                SceneLegacyState
                    .CanonizationSubject,
                incoming.LegacyState
            );

            Assert.AreEqual(
                outgoing.ReleaseId,
                replaced.TransitionResult
                    .CanonizedReleaseId
            );

            Assert.AreEqual(
                incoming.ReleaseId,
                replaced.TransitionResult
                    .IncomingSubjectReleaseId
            );

            Assert.AreEqual(
                2ul,
                replaced.NextTenure
                    .KeeperClientId
            );
        }

        [Test]
        public void DisconnectTransfersSubjectWithoutCanonizing()
        {
            SceneRelease release =
                AddRelease(
                    "Release A",
                    "Owner_A",
                    releasedTurn: 2
                );

            KeeperLegacyResolution initial =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .InitialAssignment,
                        ulong.MaxValue,
                        nextKeeper: 1,
                        round: 1
                    ),
                    null,
                    _world,
                    "Owner_A"
                );

            KeeperLegacyResolution fallback =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .DisconnectionFallback,
                        previousKeeper: 1,
                        nextKeeper: 2,
                        round: 1
                    ),
                    initial.NextTenure,
                    _world,
                    "Owner_B"
                );

            Assert.AreEqual(
                SceneLegacyState
                    .CanonizationSubject,
                release.LegacyState
            );

            Assert.AreEqual(
                2ul,
                fallback.NextTenure
                    .KeeperClientId
            );

            Assert.AreEqual(
                release.ReleaseId,
                fallback.NextTenure
                    .CanonizationSubjectReleaseId
            );

            Assert.AreEqual(
                release.ReleaseId,
                fallback.TransitionResult
                    .IncomingSubjectReleaseId
            );

            Assert.IsEmpty(
                fallback.TransitionResult
                    .CanonizedReleaseId
            );
        }

        [Test]
        public void CollapseLockDoesNotAdvanceSubject()
        {
            SceneRelease release =
                AddRelease(
                    "Release A",
                    "Owner_A",
                    releasedTurn: 2
                );

            KeeperLegacyResolution initial =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .InitialAssignment,
                        ulong.MaxValue,
                        nextKeeper: 1,
                        round: 1
                    ),
                    null,
                    _world,
                    "Owner_A"
                );

            KeeperLegacyResolution locked =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .SceneCollapseLocked,
                        previousKeeper: 1,
                        nextKeeper: 1,
                        round: 2
                    ),
                    initial.NextTenure,
                    _world,
                    "Owner_A"
                );

            Assert.AreEqual(
                0,
                release
                    .CanonizationSubjectYears
            );

            Assert.AreSame(
                initial.NextTenure,
                locked.NextTenure
            );

            Assert.IsEmpty(
                locked.TransitionResult
                    .CanonizedReleaseId
            );
        }

        private SceneRelease AddRelease(
            string name,
            string owner,
            int releasedTurn)
        {
            SceneRelease release =
                new SceneRelease(
                    name,
                    sourceDemoTapeId:
                        $"{name}_Demo",
                    sourceOwnerEntityId:
                        owner,
                    hostedSceneNodeId:
                        "KVLT",
                    releasedTurn,
                    sourceConveyance: 1.0f
                );

            _world.AddSceneRelease(release);

            return release;
        }

        private static KeeperTransitionResult
            CreateTransition(
                KeeperTransitionReason reason,
                ulong previousKeeper,
                ulong nextKeeper,
                int round)
        {
            return new KeeperTransitionResult(
                resolvedRound: round,
                reason,
                previousKeeper,
                nextKeeper,
                previousSubjectReleaseId:
                    string.Empty,
                canonizedReleaseId:
                    string.Empty,
                incomingSubjectReleaseId:
                    string.Empty,
                winningSceneOutput: 1.0f,
                pullGrant: 0
            );
        }
    }
}