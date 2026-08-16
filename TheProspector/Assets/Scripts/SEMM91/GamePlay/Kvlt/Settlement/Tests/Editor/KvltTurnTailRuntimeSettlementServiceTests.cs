using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Scenario;
using SEMM91.GamePlay.Kvlt.Standing;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltTurnTailRuntimeSettlementServiceTests
    {
        private const string SceneId =
            "SCENE_NODE_KVLT";

        private const int Turn =
            0;

        private readonly
            KvltTurnTailRuntimeSettlementService
            service =
                new();

        private readonly KvltScenarioProfile scenario =
            Peak2KvltScenarioProfileFactory
                .CreateDefault();

        [Test]
        public void
            EmptyCorpusPublishesEveryTailPhaseForNextTurn()
        {
            SeededWorldState world =
                World();

            KvltSettledSceneStandingResult standing =
                service.SettleStanding(
                    world,
                    scenario,
                    SceneId,
                    Turn
                );

            KvltNextTurnIngressSettlementResult ingress =
                service.SettleIngress(
                    world,
                    scenario,
                    SceneId,
                    Turn
                );

            KvltNextSceneEnvironmentSettlementResult
                environment =
                    service.SettleNextEnvironment(
                        world,
                        scenario,
                        SceneId,
                        Turn,
                        ingress,
                        Array.Empty<
                            SceneReleaseActivationSource>()
                    );

            Assert.That(
                standing.OwnerCount,
                Is.EqualTo(0)
            );

            Assert.That(
                world.LastAppliedKvltStandingTurn,
                Is.EqualTo(Turn)
            );

            Assert.That(
                ingress.IngressedCount,
                Is.EqualTo(0)
            );

            Assert.That(
                environment.PublishedTurn,
                Is.EqualTo(Turn + 1)
            );

            Assert.That(
                world.KvltScenePressure.EntryCount,
                Is.EqualTo(0)
            );

            Assert.That(
                world.KvltNormativeCentre,
                Is.SameAs(
                    environment
                        .NormativeCentre
                        .Centre
                )
            );
        }

        [Test]
        public void
            StandingFreezesBeforeFreshFetterEntersNextTurnEnvironment()
        {
            SeededWorldState world =
                World();

            DemoTape demo =
                Demo();

            SceneRelease release =
                new(
                    "Fresh Release",
                    demo.DemoTapeId,
                    "OWNER",
                    SceneId,
                    Turn,
                    1f
                );

            Assert.That(
                release.TryFetter(Turn),
                Is.True
            );

            world.AddSceneRelease(
                release
            );

            KvltSettledSceneStandingResult standing =
                service.SettleStanding(
                    world,
                    scenario,
                    SceneId,
                    Turn
                );

            Assert.That(
                standing.TryGet(
                    "OWNER",
                    out SceneStandingEvaluation
                        ownerStanding
                ),
                Is.True
            );

            Assert.That(
                ownerStanding.HasStanding,
                Is.False
            );

            Assert.That(
                release.HasFieldPosition,
                Is.False
            );

            KvltNextTurnIngressSettlementResult ingress =
                service.SettleIngress(
                    world,
                    scenario,
                    SceneId,
                    Turn
                );

            Assert.That(
                ingress.IngressedCount,
                Is.EqualTo(1)
            );

            Assert.That(
                release.FieldPositionState
                    .EstablishedTurn,
                Is.EqualTo(Turn + 1)
            );

            KvltNextSceneEnvironmentSettlementResult
                environment =
                    service.SettleNextEnvironment(
                        world,
                        scenario,
                        SceneId,
                        Turn,
                        ingress,
                        new[]
                        {
                            new
                                SceneReleaseActivationSource(
                                    release,
                                    demo
                                )
                        }
                    );

            Assert.That(
                environment.Pressure.Pressure
                    .GetRawPressure(
                        TagAxis.Symbolic,
                        TagPole.Negative
                    ),
                Is.EqualTo(1f)
            );

            Assert.That(
                world.KvltScenePressure
                    .GetRawPressure(
                        TagAxis.Symbolic,
                        TagPole.Negative
                    ),
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void
            PublishedStandingCannotBeReplayedForSameTurn()
        {
            SeededWorldState world =
                World();

            service.SettleStanding(
                world,
                scenario,
                SceneId,
                Turn
            );

            Assert.Throws<InvalidOperationException>(
                () =>
                    service.SettleStanding(
                        world,
                        scenario,
                        SceneId,
                        Turn
                    )
            );
        }

        private static SeededWorldState World()
        {
            return new SeededWorldState(
                new CollectiveRegistry()
            );
        }

        private static DemoTape Demo()
        {
            DemoTapeIdeaSnapshot idea =
                new(
                    "IDEA",
                    0,
                    "ASPECT",
                    IdeaPayloadType.SingleTag,
                    "AUTHOR",
                    TagContainerType.Transient,
                    1f,
                    new[]
                    {
                        new
                            DemoTapeTagOccurrenceSnapshot(
                                TagAxis.Symbolic,
                                TagPole.Negative,
                                TagDegree.Weak,
                                DemoTapeTagOccurrenceRole
                                    .Solitary
                            )
                    }
                );

            DemoTapeTrackSnapshot track =
                new(
                    "TRACK",
                    "Track",
                    1f,
                    1f,
                    new[]
                    {
                        idea
                    }
                );

            return new DemoTape(
                "DEMO",
                "Demo",
                "SET",
                "Set",
                1,
                1,
                1f,
                new[]
                {
                    track
                }
            );
        }
    }
}