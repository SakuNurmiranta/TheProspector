using NUnit.Framework;
using SEMM91.Core.Entities;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Scenario;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Score;
using SEMM91.GamePlay.World;
using UnityEngine;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltPostHappeningRuntimeSettlementServiceTests
    {
        private const string SceneId =
            "SCENE_NODE_KVLT";

        private const string OwnerId =
            "OWNER";

        private const string TenureId =
            "TENURE_TEST";

        private const int WinterTurn =
            3;

        private readonly
            KvltPostHappeningRuntimeSettlementService
            service =
                new();

        private GameObject ownerObject;

        [TearDown]
        public void TearDown()
        {
            if (ownerObject != null)
            {
                Object.DestroyImmediate(
                    ownerObject
                );
            }
        }

        [Test]
        public void
            EmptyTurnScreensAndScoresWithoutChangingCanon()
        {
            SeededWorldState world =
                World();

            TrackEvaluationEnvironment environment =
                Environment(
                    world
                );

            int canonRecordCountBefore =
                world.KvltCanon.Records.Count;

            KvltPostHappeningCanonizationScreeningResult
                screening =
                    service.Screen(
                        world,
                        SceneId,
                        WinterTurn,
                        environment
                    );

            KvltTurnScoreSettlementResult score =
                service.SettleTurnScore(
                    world,
                    SceneId,
                    WinterTurn,
                    string.Empty,
                    screening
                );

            Assert.That(
                screening.LegitimacyEvaluations,
                Is.Empty
            );

            Assert.That(
                score.EventCount,
                Is.EqualTo(0)
            );

            Assert.That(
                world.KvltCanon.Records.Count,
                Is.EqualTo(
                    canonRecordCountBefore
                )
            );
        }

        [Test]
        public void
            WinterCanonPublishesBeforeRetainedGravityScore()
        {
            Fixture fixture =
                CreateCanonFixture();

            KvltPostHappeningCanonizationScreeningResult
                screening =
                    service.Screen(
                        fixture.World,
                        SceneId,
                        WinterTurn,
                        fixture.Environment
                    );

            Assert.That(
                screening
                    .BreakthroughsByRelease[
                        fixture.Release.ReleaseId
                    ]
                    .HasQualifyingBreakthrough,
                Is.True
            );

            KvltCanonizationSettlementResult canon =
                service.SettleYearEndCanon(
                    fixture.World,
                    Peak2KvltScenarioProfileFactory
                        .CreateDefault(),
                    SceneId,
                    WinterTurn,
                    TenureId,
                    fixture.Environment,
                    screening
                );

            Assert.That(
                canon.HasCanonization,
                Is.True
            );

            Assert.That(
                fixture.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                fixture.World.KvltCanon
                    .TryGetCanonicalDegree(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        out TagDegree degree
                    ),
                Is.True
            );

            Assert.That(
                degree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            KvltTurnScoreSettlementResult score =
                service.SettleTurnScore(
                    fixture.World,
                    SceneId,
                    WinterTurn,
                    TenureId,
                    screening
                );

            Assert.That(
                score.CountOf(
                    ScoreEventKind
                        .CanonRetainedGravity
                ),
                Is.EqualTo(1)
            );

            Assert.That(
                fixture.World.KvltScoreLedger.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            FieldReleaseWithoutAuthoritativeDemoFailsScreening()
        {
            SeededWorldState world =
                World();

            SceneRelease orphan =
                new(
                    "Orphan",
                    "MISSING_DEMO",
                    "MISSING_OWNER",
                    SceneId,
                    releasedTurn:
                        0,
                    sourceConveyance:
                        1f
                );

            Assert.That(
                orphan.TryFetter(0),
                Is.True
            );

            world.AddSceneRelease(
                orphan
            );

            Assert.Throws<
                System.InvalidOperationException>(
                () =>
                    service.Screen(
                        world,
                        SceneId,
                        WinterTurn,
                        Environment(
                            world
                        )
                    )
            );
        }

        private Fixture CreateCanonFixture()
        {
            SeededWorldState world =
                World();

            ownerObject =
                new GameObject(
                    "Post-Happening Runtime Owner"
                );

            GameEntity owner =
                ownerObject.AddComponent<
                    GameEntity
                >();

            owner.InitializeIdentity(
                OwnerId,
                "Owner",
                GameEntityType.Character
            );

            Assert.That(
                world.AddEntity(
                    owner
                ),
                Is.True
            );

            DemoTape demo =
                Demo();

            owner.AddDemoTape(
                demo
            );

            SceneRelease release =
                new(
                    "Release A",
                    demo.DemoTapeId,
                    OwnerId,
                    SceneId,
                    1,
                    1f
                );

            Assert.That(
                release.TryFetter(1),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    0.40f,
                    1
                ),
                Is.True
            );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.60f,
                    WinterTurn - 1,
                    out _
                ),
                Is.True
            );

            world.AddSceneRelease(
                release
            );

            Assert.That(
                world.KvltCanon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind
                        .ScenarioSeed,
                    "CANON_SEED"
                ),
                Is.True
            );

            world.SocietyNorms.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            ApplyWinterActivation(
                release,
                demo
            );

            return new Fixture(
                world,
                release,
                Environment(
                    world
                )
            );
        }

        private static void ApplyWinterActivation(
            SceneRelease release,
            DemoTape demo)
        {
            ActivationLegitimacyCandidate candidate =
                new(
                    ActivationAttemptRoute.Hail,
                    "HAPPENING_WINTER",
                    "INTENT_WINTER",
                    "ACTOR",
                    release.ReleaseId,
                    demo.DemoTapeId,
                    "TRACK_A",
                    "IDEA_A",
                    0,
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    TagDegree.Dominant,
                    TagDegree.Transgressive,
                    "BEHAVIOR_WINTER",
                    "HAIL_WINTER",
                    "ASPECT_SATAN"
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT_WINTER",
                    "KVLT",
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    WinterTurn,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "TEST"
                );

            ActivationLegitimacyAssessment assessment =
                new(
                    candidate,
                    ActivationLegitimacyDisposition
                        .Covered,
                    precedent
                );

            Assert.That(
                new SceneReleaseActivationStateService()
                    .ApplyCovered(
                        release,
                        assessment,
                        WinterTurn
                    ),
                Is.True
            );
        }

        private static SeededWorldState World()
        {
            return new SeededWorldState(
                new CollectiveRegistry()
            );
        }

        private static TrackEvaluationEnvironment
            Environment(
                SeededWorldState world)
        {
            return new TrackEvaluationEnvironment(
                WinterTurn,
                world.KvltNormativeCentre,
                world.KvltNormativeCentre,
                world.SocietyNorms
            );
        }

        private static DemoTape Demo()
        {
            DemoTapeIdeaSnapshot pair =
                new(
                    "IDEA_A",
                    0,
                    "ASPECT",
                    IdeaPayloadType.TagPair,
                    "AUTHOR",
                    TagContainerType.Transient,
                    1f,
                    new[]
                    {
                        new DemoTapeTagOccurrenceSnapshot(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            DemoTapeTagOccurrenceRole
                                .PairDominant
                        ),

                        new DemoTapeTagOccurrenceSnapshot(
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            TagDegree.Weak,
                            DemoTapeTagOccurrenceRole
                                .PairSubmissive
                        )
                    }
                );

            DemoTapeTrackSnapshot track =
                new(
                    "TRACK_A",
                    "Track A",
                    1f,
                    1f,
                    new[]
                    {
                        pair
                    }
                );

            return new DemoTape(
                "DEMO_A",
                "Demo A",
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

        private sealed class Fixture
        {
            public SeededWorldState World { get; }

            public SceneRelease Release { get; }

            public TrackEvaluationEnvironment
                Environment { get; }

            public Fixture(
                SeededWorldState world,
                SceneRelease release,
                TrackEvaluationEnvironment environment)
            {
                World =
                    world;

                Release =
                    release;

                Environment =
                    environment;
            }
        }
    }
}