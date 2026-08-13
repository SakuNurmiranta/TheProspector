using NUnit.Framework;
using SEMM91.Core.Collectives;
using SEMM91.Core.Entities;
using SEMM91.GamePlay.Agency;
using SEMM91.GamePlay.World;
using UnityEngine;

namespace SEMM91.GamePlay.Collectives
    .Tests.Editor
{
    public class
        StartingPlayerPhysicalPlacementTests
    {
        [Test]
        public void
            AddedPlayerLeaderStartsAtTheHole()
        {
            StartingCollectiveBootstrapper
                collectiveBootstrapper =
                    new StartingCollectiveBootstrapper(
                        log: _ => { }
                    );

            StartingCollectiveBootstrapResult result =
                collectiveBootstrapper
                    .BootstrapSharedWorld();

            PlayerEntityBootstrapper
                playerBootstrapper =
                    new PlayerEntityBootstrapper(
                        log: _ => { }
                    );

            GameEntity player =
                playerBootstrapper
                    .CreatePlayerEntity(
                        clientId: 7,
                        displayName: "Player 7"
                    );

            try
            {
                Collective playerBand =
                    collectiveBootstrapper
                        .AddPlayerLeaderToWorld(
                            result.WorldState,
                            player,
                            playerId: 7
                        );

                Assert.That(
                    playerBand,
                    Is.Not.Null
                );

                Assert.That(
                    player.HasPhysicalLocation,
                    Is.True
                );

                Assert.That(
                    result.WorldState
                        .TryGetEntityPhysicalCoordinate(
                            result.TheHole,
                            out Vector2Int holeCoordinate
                        ),
                    Is.True
                );

                Assert.That(
                    result.WorldState
                        .TryGetEntityPhysicalCoordinate(
                            player,
                            out Vector2Int playerCoordinate
                        ),
                    Is.True
                );

                Assert.That(
                    playerCoordinate,
                    Is.EqualTo(holeCoordinate)
                );

                Assert.That(
                    playerCoordinate,
                    Is.EqualTo(
                        StartingCollectiveBootstrapper
                            .StartingPhysicalCoordinate
                    )
                );

                Assert.That(
                    player.PhysicalNodeId,
                    Is.EqualTo(
                        result.TheHole.PhysicalNodeId
                    )
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