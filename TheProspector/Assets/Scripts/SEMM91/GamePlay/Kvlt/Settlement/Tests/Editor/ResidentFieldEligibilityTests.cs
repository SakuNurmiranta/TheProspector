using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        ResidentFieldEligibilityTests
    {
        private const int Turn =
            8;

        [Test]
        public void
            NewlyFetteredReleaseDoesNotYetContributeStanding()
        {
            SceneRelease release =
                new(
                    "New Fetter",
                    "DEMO",
                    "OWNER",
                    "KVLT",
                    Turn - 1,
                    1f
                );

            Assert.That(
                release.TryFetter(
                    Turn
                ),
                Is.True
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                release.HasFieldPosition,
                Is.False
            );

            SceneStandingEvaluation standing =
                new SceneStandingCorpusProjector()
                    .Project(
                        "OWNER",
                        "KVLT",
                        Turn,
                        new[]
                        {
                            release
                        },
                        StandingPolicy()
                    );

            Assert.That(
                standing.HasStanding,
                Is.False
            );

            Assert.That(
                standing.Contributions,
                Is.Empty
            );
        }

        [Test]
        public void
            NewlyFetteredReleaseDoesNotYetContributePressure()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                new(
                    "New Fetter",
                    demo.DemoTapeId,
                    "OWNER",
                    "KVLT",
                    Turn - 1,
                    1f
                );

            Assert.That(
                release.TryFetter(
                    Turn
                ),
                Is.True
            );

            ScenePressureRebuildEvaluation pressure =
                new ScenePressureRebuilder()
                    .Rebuild(
                        "KVLT",
                        Turn,
                        new[]
                        {
                            release
                        },
                        new[]
                        {
                            demo
                        },
                        new CanonState(),
                        PressurePolicy()
                    );

            Assert.That(
                pressure.SourceContributionCount,
                Is.EqualTo(0)
            );

            Assert.That(
                pressure.Pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void
            ReleaseBeginsStandingAndPressureAfterIngressPositionExists()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                new(
                    "Resident",
                    demo.DemoTapeId,
                    "OWNER",
                    "KVLT",
                    Turn - 1,
                    1f
                );

            Assert.That(
                release.TryFetter(
                    Turn - 1
                ),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    0.40f,
                    Turn
                ),
                Is.True
            );

            SceneStandingEvaluation standing =
                new SceneStandingCorpusProjector()
                    .Project(
                        "OWNER",
                        "KVLT",
                        Turn,
                        new[]
                        {
                            release
                        },
                        StandingPolicy()
                    );

            Assert.That(
                standing.HasStanding,
                Is.True
            );

            Assert.That(
                standing.Contributions.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                standing.Standing,
                Is.EqualTo(0.40f)
                    .Within(0.0001f)
            );

            ScenePressureRebuildEvaluation pressure =
                new ScenePressureRebuilder()
                    .Rebuild(
                        "KVLT",
                        Turn,
                        new[]
                        {
                            release
                        },
                        new[]
                        {
                            demo
                        },
                        new CanonState(),
                        PressurePolicy()
                    );

            Assert.That(
                pressure.SourceContributionCount,
                Is.EqualTo(1)
            );

            Assert.That(
                pressure.Pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(1f)
            );
        }

        private static
            SceneStandingProjectionPolicy
            StandingPolicy()
        {
            return new SceneStandingProjectionPolicy(
                canonLegacyBaseWeight:
                    1f,
                canonLegacyBreakthroughDegreeWeight:
                    1f,
                rejectionScarBaseWeight:
                    1f,
                rejectionPeakPenetrationWeight:
                    1f,
                rejectionOutwardOvershootWeight:
                    1f
            );
        }

        private static
            ScenePressureRebuildPolicy
            PressurePolicy()
        {
            return new ScenePressureRebuildPolicy(
                neutralDirectionScale:
                    0.25f,
                counterCanonicalScale:
                    0.50f,
                maxAbsoluteEffectivePressure:
                    0.75f
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
                        new DemoTapeTagOccurrenceSnapshot(
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