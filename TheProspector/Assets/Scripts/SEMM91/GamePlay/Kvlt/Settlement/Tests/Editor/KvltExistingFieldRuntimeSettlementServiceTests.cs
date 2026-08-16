using NUnit.Framework;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Scenario;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltExistingFieldRuntimeSettlementServiceTests
    {
        private readonly
            KvltExistingFieldRuntimeSettlementService
            service =
                new();

        [Test]
        public void
            EmptyWorldBuildsCurrentTurnEnvironmentAndSettlesNothing()
        {
            SeededWorldState world =
                new(
                    new CollectiveRegistry()
                );

            KvltScenarioProfile scenario =
                Peak2KvltScenarioProfileFactory
                    .CreateDefault();

            KvltExistingFieldRuntimeSettlementResult
                result =
                    service.Settle(
                        world,
                        scenario,
                        "SCENE_NODE_KVLT",
                        settledTurn:
                            3
                    );

            Assert.That(
                result.SettledTurn,
                Is.EqualTo(3)
            );

            Assert.That(
                result.EvaluationEnvironment
                    .SettledTurn,
                Is.EqualTo(3)
            );

            /*
             * The adapter must consume the already
             * published Scene_t Normative Centre,
             * rather than constructing a new effective
             * centre for this pass.
             */
            Assert.That(
                result.EvaluationEnvironment
                    .CurrentNormativeCentre,
                Is.SameAs(
                    world.KvltNormativeCentre
                )
            );

            Assert.That(
                result.Movement
                    .MovementApplications,
                Is.Empty
            );

            Assert.That(
                result.Boundary
                    .BoundaryEvaluations,
                Is.Empty
            );

            Assert.That(
                result.Boundary
                    .RejectedCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void
            FieldReleaseWithoutAuthoritativeSourceOwnerFailsBeforeSettlement()
        {
            SeededWorldState world =
                new(
                    new CollectiveRegistry()
                );

            SceneRelease orphan =
                new(
                    "Orphan",
                    "MISSING_DEMO",
                    "MISSING_OWNER",
                    "SCENE_NODE_KVLT",
                    releasedTurn:
                        0,
                    sourceConveyance:
                        1f
                );

            Assert.That(
                orphan.TryFetter(0),
                Is.True
            );

            Assert.That(
                orphan.TryEstablishFieldPosition(
                    0.20f,
                    0
                ),
                Is.True
            );

            world.AddSceneRelease(
                orphan
            );

            Assert.Throws<
                System.InvalidOperationException>(
                () =>
                    service.Settle(
                        world,
                        Peak2KvltScenarioProfileFactory
                            .CreateDefault(),
                        "SCENE_NODE_KVLT",
                        settledTurn:
                            1
                    )
            );
        }
    }
}