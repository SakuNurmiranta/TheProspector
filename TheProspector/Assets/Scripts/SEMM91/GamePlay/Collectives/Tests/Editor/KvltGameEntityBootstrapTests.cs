using NUnit.Framework;
using SEMM91.Core.Entities;
using UnityEngine;

namespace SEMM91.GamePlay.Collectives.Tests.Editor
{
    public class KvltGameEntityBootstrapTests
    {
        [Test]
        public void BootstrapSharedWorld_CreatesDistinctPersistentKvltGameEntity()
        {
            StartingCollectiveBootstrapper bootstrapper =
                new StartingCollectiveBootstrapper(
                    log: _ => { }
                );

            StartingCollectiveBootstrapResult result =
                bootstrapper.BootstrapSharedWorld();

            try
            {
                Assert.That(
                    result.KvltEntity,
                    Is.Not.Null
                );

                Assert.That(
                    result.KvltEntity.EntityId,
                    Is.EqualTo(
                        StartingCollectiveBootstrapper.KvltEntityId
                    )
                );

                Assert.That(
                    result.KvltEntity.EntityType,
                    Is.EqualTo(GameEntityType.Scene)
                );

                Assert.That(
                    result.WorldState.FindKvltEntity(),
                    Is.SameAs(result.KvltEntity)
                );

                Assert.That(
                    result.Kvlt,
                    Is.Not.Null
                );

                Assert.That(
                    result.Kvlt.CollectiveId,
                    Is.EqualTo(
                        StartingCollectiveBootstrapper.KvltId
                    )
                );

                Assert.That(
                    result.Kvlt.IsActive,
                    Is.True
                );

                Assert.That(
                    result.KvltEntity.EntityId,
                    Is.Not.EqualTo(
                        result.Kvlt.CollectiveId
                    )
                );

                Assert.That(
                    result.KvltEntity.EntityId,
                    Is.Not.EqualTo(
                        result.TheHole.EntityId
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