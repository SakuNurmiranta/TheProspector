using NUnit.Framework;
using SEMM91.Core.Entities;
using SEMM91.Core.Recordings;
using UnityEngine;

namespace SEMM91.GamePlay.Agency
    .Tests.Editor
{
    public class PlayerEntityBootstrapperTests
    {
        [Test]
        public void
            CreateStartingPlayerEntitySeedsHistoricalDemo()
        {
            PlayerEntityBootstrapper bootstrapper =
                new PlayerEntityBootstrapper(
                    log: _ => { }
                );

            GameEntity entity =
                bootstrapper
                    .CreateStartingPlayerEntity(
                        clientId: 7
                    );

            try
            {
                Assert.That(
                    entity,
                    Is.Not.Null
                );

                Assert.That(
                    entity.DemoTapes.Count,
                    Is.EqualTo(1)
                );

                DemoTape demo =
                    entity.DemoTapes[0];

                Assert.That(
                    demo.DemoTapeId,
                    Is.EqualTo(
                        "PRESESSION_DEMO_7"
                    )
                );

                Assert.That(
                    demo.DisplayName,
                    Is.EqualTo("Demo_1")
                );

                Assert.That(
                    demo.RecordedTurn,
                    Is.EqualTo(-1)
                );

                Assert.That(
                    demo.SceneState,
                    Is.EqualTo(
                        DemoTapeSceneState
                            .Unreleased
                    )
                );

                Assert.That(
                    demo.TrackSnapshots.Count,
                    Is.EqualTo(1)
                );

                Assert.That(
                    demo.AverageConveyance,
                    Is.EqualTo(0.60f)
                        .Within(0.0001f)
                );
            }
            finally
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