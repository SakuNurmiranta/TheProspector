using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Core.Collectives;
using SEMM91.Core.Entities;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.World;
using UnityEngine;

namespace SEMM91.GamePlay.Keeper
    .Tests.Editor
{
    public class KeeperRoleResolverTests
    {
        private KeeperRoleResolver _resolver;
        private SeededWorldState _world;

        private GameEntity _leader0;
        private GameEntity _leader1;

        [SetUp]
        public void SetUp()
        {
            StartingCollectiveBootstrapper
                bootstrapper =
                    new StartingCollectiveBootstrapper(
                        _ => { }
                    );

            StartingCollectiveBootstrapResult result =
                bootstrapper.BootstrapSharedWorld();

            _world =
                result.WorldState;

            _leader0 =
                CreatePlayerLeader(
                    bootstrapper,
                    clientId: 0
                );

            _leader1 =
                CreatePlayerLeader(
                    bootstrapper,
                    clientId: 1
                );

            _resolver =
                new KeeperRoleResolver();
        }

        [TearDown]
        public void TearDown()
        {
            List<GameObject> objects =
                new List<GameObject>();

            foreach (GameEntity entity
                     in _world.Entities)
            {
                if (entity != null &&
                    !objects.Contains(
                        entity.gameObject
                    ))
                {
                    objects.Add(
                        entity.gameObject
                    );
                }
            }

            foreach (GameObject gameObject
                     in objects)
            {
                Object.DestroyImmediate(
                    gameObject
                );
            }
        }

        [Test]
        public void InitialAssignmentEntersKeeperRole()
        {
            KeeperRoleResolution resolution =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .InitialAssignment,
                        previousKeeper:
                            ulong.MaxValue,
                        nextKeeper: 0
                    ),
                    _world
                );

            Assert.IsTrue(
                resolution.Succeeded,
                resolution.FailureReason
            );

            AssertKeeperRole(
                clientId: 0,
                _leader0
            );

            AssertRegularRole(
                clientId: 1,
                _leader1
            );

            Assert.AreEqual(
                _leader0.EntityId,
                _world.FindKvlt()
                    .LeaderEntityId
            );
        }

        [Test]
        public void RetentionDoesNotExitAndReenter()
        {
            _resolver.Resolve(
                CreateTransition(
                    KeeperTransitionReason
                        .InitialAssignment,
                    ulong.MaxValue,
                    nextKeeper: 0
                ),
                _world
            );

            KeeperRoleResolution retained =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .YearEndRetained,
                        previousKeeper: 0,
                        nextKeeper: 0
                    ),
                    _world
                );

            Assert.IsTrue(
                retained.Succeeded,
                retained.FailureReason
            );

            Assert.IsFalse(
                retained.OutgoingRoleReleased
            );

            AssertKeeperRole(
                clientId: 0,
                _leader0
            );
        }

        [Test]
        public void ReplacementRestoresOutgoingBandAndDormantsIncomingBand()
        {
            _resolver.Resolve(
                CreateTransition(
                    KeeperTransitionReason
                        .InitialAssignment,
                    ulong.MaxValue,
                    nextKeeper: 0
                ),
                _world
            );

            KeeperRoleResolution replaced =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .YearEndReplaced,
                        previousKeeper: 0,
                        nextKeeper: 1
                    ),
                    _world
                );

            Assert.IsTrue(
                replaced.Succeeded,
                replaced.FailureReason
            );

            Assert.IsTrue(
                replaced.OutgoingRoleReleased
            );

            Assert.IsTrue(
                replaced.IncomingRoleApplied
            );

            AssertRegularRole(
                clientId: 0,
                _leader0
            );

            AssertKeeperRole(
                clientId: 1,
                _leader1
            );

            Assert.AreEqual(
                _leader1.EntityId,
                _world.FindKvlt()
                    .LeaderEntityId
            );
        }

        [Test]
        public void DisconnectFallbackTransfersInstitutionalRole()
        {
            _resolver.Resolve(
                CreateTransition(
                    KeeperTransitionReason
                        .InitialAssignment,
                    ulong.MaxValue,
                    nextKeeper: 0
                ),
                _world
            );

            KeeperRoleResolution fallback =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .DisconnectionFallback,
                        previousKeeper: 0,
                        nextKeeper: 1
                    ),
                    _world
                );

            Assert.IsTrue(
                fallback.Succeeded,
                fallback.FailureReason
            );

            AssertRegularRole(
                clientId: 0,
                _leader0
            );

            AssertKeeperRole(
                clientId: 1,
                _leader1
            );
        }

        [Test]
        public void UnassignedFallbackReleasesOutgoingRole()
        {
            _resolver.Resolve(
                CreateTransition(
                    KeeperTransitionReason
                        .InitialAssignment,
                    ulong.MaxValue,
                    nextKeeper: 0
                ),
                _world
            );

            KeeperRoleResolution fallback =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .DisconnectionFallback,
                        previousKeeper: 0,
                        nextKeeper:
                            ulong.MaxValue
                    ),
                    _world
                );

            Assert.IsTrue(
                fallback.Succeeded,
                fallback.FailureReason
            );

            AssertRegularRole(
                clientId: 0,
                _leader0
            );

            Assert.IsTrue(
                string.IsNullOrWhiteSpace(
                    _world.FindKvlt()
                        .LeaderEntityId
                )
            );
        }

        [Test]
        public void CollapseLockPreservesCurrentRole()
        {
            _resolver.Resolve(
                CreateTransition(
                    KeeperTransitionReason
                        .InitialAssignment,
                    ulong.MaxValue,
                    nextKeeper: 0
                ),
                _world
            );

            KeeperRoleResolution locked =
                _resolver.Resolve(
                    CreateTransition(
                        KeeperTransitionReason
                            .SceneCollapseLocked,
                        previousKeeper: 0,
                        nextKeeper: 0
                    ),
                    _world
                );

            Assert.IsTrue(
                locked.Succeeded,
                locked.FailureReason
            );

            Assert.IsFalse(
                locked.OutgoingRoleReleased
            );

            Assert.IsFalse(
                locked.IncomingRoleApplied
            );

            AssertKeeperRole(
                clientId: 0,
                _leader0
            );
        }

        private GameEntity CreatePlayerLeader(
            StartingCollectiveBootstrapper
                bootstrapper,
            ulong clientId)
        {
            GameObject gameObject =
                new GameObject(
                    $"Player_{clientId}_Leader"
                );

            GameEntity leader =
                gameObject
                    .AddComponent<GameEntity>();

            leader.InitializeIdentity(
                $"ENTITY_PLAYER_{clientId}",
                $"Player {clientId}",
                GameEntityType.Character
            );

            bootstrapper.AddPlayerLeaderToWorld(
                _world,
                leader,
                clientId
            );

            return leader;
        }

        private void AssertKeeperRole(
            ulong clientId,
            GameEntity leader)
        {
            Collective band =
                FindPlayerBand(clientId);

            Collective kvlt =
                _world.FindKvlt();

            Assert.IsFalse(
                band.IsActive
            );

            Assert.IsTrue(
                leader.HasActiveCollectiveMembership(
                    StartingCollectiveBootstrapper
                        .KvltId
                )
            );

            Assert.AreEqual(
                CollectiveMembershipMode.Passive,
                FindMembershipMode(
                    band,
                    leader.EntityId
                )
            );

            Assert.AreEqual(
                CollectiveMembershipMode.Active,
                FindMembershipMode(
                    kvlt,
                    leader.EntityId
                )
            );
        }

        private void AssertRegularRole(
            ulong clientId,
            GameEntity leader)
        {
            Collective band =
                FindPlayerBand(clientId);

            Collective kvlt =
                _world.FindKvlt();

            Assert.IsTrue(
                band.IsActive
            );

            Assert.IsTrue(
                leader.HasActiveCollectiveMembership(
                    band.CollectiveId
                )
            );

            Assert.AreEqual(
                CollectiveMembershipMode.Active,
                FindMembershipMode(
                    band,
                    leader.EntityId
                )
            );

            Assert.AreEqual(
                CollectiveMembershipMode.Passive,
                FindMembershipMode(
                    kvlt,
                    leader.EntityId
                )
            );
        }

        private Collective FindPlayerBand(
            ulong clientId)
        {
            return _world.FindCollective(
                StartingCollectiveBootstrapper
                    .PlayerBandIdPrefix +
                clientId
            );
        }

        private static CollectiveMembershipMode
            FindMembershipMode(
                Collective collective,
                string entityId)
        {
            foreach (
                CollectiveMemberRecord membership
                in collective.EntityMemberships)
            {
                if (membership.MemberId ==
                    entityId)
                {
                    return membership
                        .MembershipMode;
                }
            }

            Assert.Fail(
                $"Missing membership for " +
                $"{entityId} in " +
                $"{collective.CollectiveId}."
            );

            return default;
        }

        private static KeeperTransitionResult
            CreateTransition(
                KeeperTransitionReason reason,
                ulong previousKeeper,
                ulong nextKeeper)
        {
            return new KeeperTransitionResult(
                resolvedRound: 1,
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