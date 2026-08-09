using NUnit.Framework;
using SEMM91.Core.Collectives;
using SEMM91.Core.Entities;
using SEMM91.GamePlay.Agency;
using UnityEngine;

namespace SEMM91.GamePlay.Collectives.Tests.Editor
{
    public class SocietyMembershipBootstrapTests
    {
        [Test]
        public void AddPlayerLeaderToWorld_AddsPassiveSocietyMembershipWithoutReplacingActiveBand()
        {
            StartingCollectiveBootstrapper worldBootstrapper =
                new StartingCollectiveBootstrapper(
                    log: _ => { }
                );

            StartingCollectiveBootstrapResult result =
                worldBootstrapper.BootstrapSharedWorld();

            PlayerEntityBootstrapper playerBootstrapper =
                new PlayerEntityBootstrapper(
                    log: _ => { }
                );

            GameEntity player =
                playerBootstrapper
                    .CreateStartingPlayerEntity(7);

            Collective band =
                worldBootstrapper
                    .AddPlayerLeaderToWorld(
                        result.WorldState,
                        player,
                        7
                    );

            try
            {
                Assert.That(
                    band,
                    Is.Not.Null
                );

                Assert.That(
                    result.Society.HasEntityMember(
                        player.EntityId
                    ),
                    Is.True
                );

                Assert.That(
                    player.HasCollectiveMembership(
                        StartingCollectiveBootstrapper
                            .SocietyId
                    ),
                    Is.True
                );

                Assert.That(
                    player.HasActiveCollectiveMembership(
                        StartingCollectiveBootstrapper
                            .SocietyId
                    ),
                    Is.False
                );

                Assert.That(
                    player.HasCollectiveMembership(
                        StartingCollectiveBootstrapper
                            .KvltId
                    ),
                    Is.True
                );

                Assert.That(
                    player.HasActiveCollectiveMembership(
                        StartingCollectiveBootstrapper
                            .KvltId
                    ),
                    Is.False
                );

                Assert.That(
                    player.HasActiveCollectiveMembership(
                        band.CollectiveId
                    ),
                    Is.True
                );

                CollectiveMemberRecord
                    societyMembership = null;

                foreach (
                    CollectiveMemberRecord membership
                    in result.Society.EntityMemberships)
                {
                    if (membership.MemberId ==
                        player.EntityId)
                    {
                        societyMembership =
                            membership;

                        break;
                    }
                }

                Assert.That(
                    societyMembership,
                    Is.Not.Null
                );

                Assert.That(
                    societyMembership.MembershipMode,
                    Is.EqualTo(
                        CollectiveMembershipMode.Passive
                    )
                );

                Assert.That(
                    societyMembership.IsActive,
                    Is.True
                );
            }
            finally
            {
                foreach (GameEntity entity
                         in result.WorldState.Entities)
                {
                    if (entity != null)
                    {
                        Object.DestroyImmediate(
                            entity.gameObject
                        );
                    }
                }
            }
        }
    }
}