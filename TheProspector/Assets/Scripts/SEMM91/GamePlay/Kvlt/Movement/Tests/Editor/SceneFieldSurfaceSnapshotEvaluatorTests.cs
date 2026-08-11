using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class
        SceneFieldSurfaceSnapshotEvaluatorTests
    {
        private readonly
            SceneFieldSurfaceSnapshotEvaluator
                snapshotEvaluator =
                    new();

        [Test]
        public void
            ActiveTrveFieldReleases_FormCompleteSurfacePopulation()
        {
            Fixture low =
                CreateFixture(
                    "LOW",
                    TagDegree.Weak,
                    activeTrve: true,
                    field: true,
                    fieldPosition: 0.20f,
                    settledTurn: 6
                );

            Fixture high =
                CreateFixture(
                    "HIGH",
                    TagDegree.Transgressive,
                    activeTrve: true,
                    field: true,
                    fieldPosition: 0.60f,
                    settledTurn: 6
                );

            SceneFieldSurfaceSnapshot snapshot =
                snapshotEvaluator.Evaluate(
                    "KVLT",
                    6,
                    new[]
                    {
                        low.Release,
                        high.Release
                    },
                    new[]
                    {
                        low.Evaluation,
                        high.Evaluation
                    }
                );

            Assert.That(
                snapshot.PopulationCount,
                Is.EqualTo(2)
            );

            /*
             * Surface values are 1 and 3 because the
             * test Normative Centre gives affinity +1
             * to each recorded dominant degree.
             */
            Assert.That(
                snapshot.MeanSurface,
                Is.EqualTo(2f)
                    .Within(0.0001f)
            );

            Assert.That(
                snapshot.TryGetEntry(
                    low.Release.ReleaseId,
                    out SceneFieldSurfaceEntry
                        lowEntry
                ),
                Is.True
            );

            Assert.That(
                lowEntry.Surface,
                Is.EqualTo(1f)
                    .Within(0.0001f)
            );

            Assert.That(
                lowEntry.StartFieldPosition,
                Is.EqualTo(0.20f)
            );
        }

        [Test]
        public void
            FieldReleaseWithoutActiveTrve_DoesNotCompete()
        {
            Fixture inactive =
                CreateFixture(
                    "INACTIVE",
                    TagDegree.Dominant,
                    activeTrve: false,
                    field: true,
                    fieldPosition: 0.30f,
                    settledTurn: 6
                );

            SceneFieldSurfaceSnapshot snapshot =
                snapshotEvaluator.Evaluate(
                    "KVLT",
                    6,
                    new[]
                    {
                        inactive.Release
                    },
                    new[]
                    {
                        inactive.Evaluation
                    }
                );

            Assert.That(
                snapshot.PopulationCount,
                Is.EqualTo(0)
            );

            Assert.That(
                snapshot.MeanSurface,
                Is.Null
            );
        }

        [Test]
        public void
            FringeReleaseWithActiveTrve_DoesNotCompeteUntilFettered()
        {
            Fixture fringe =
                CreateFixture(
                    "FRINGE",
                    TagDegree.Dominant,
                    activeTrve: true,
                    field: false,
                    fieldPosition: 0f,
                    settledTurn: 6
                );

            SceneFieldSurfaceSnapshot snapshot =
                snapshotEvaluator.Evaluate(
                    "KVLT",
                    6,
                    new[]
                    {
                        fringe.Release
                    },
                    new[]
                    {
                        fringe.Evaluation
                    }
                );

            Assert.That(
                fringe.Evaluation.HasTrve,
                Is.True
            );

            Assert.That(
                fringe.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Fringe
                )
            );

            Assert.That(
                snapshot.PopulationCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void
            FieldReleaseWithoutIngressPosition_IsRejected()
        {
            Fixture field =
                CreateFixture(
                    "NO_POSITION",
                    TagDegree.Dominant,
                    activeTrve: true,
                    field: true,
                    fieldPosition: null,
                    settledTurn: 6
                );

            Assert.That(
                field.Release.HasFieldPosition,
                Is.False
            );

            Assert.Throws<
                System.InvalidOperationException>(
                () =>
                    snapshotEvaluator.Evaluate(
                        "KVLT",
                        6,
                        new[]
                        {
                            field.Release
                        },
                        new[]
                        {
                            field.Evaluation
                        }
                    )
            );
        }

        private static Fixture CreateFixture(
            string suffix,
            TagDegree dominantDegree,
            bool activeTrve,
            bool field,
            float? fieldPosition,
            int settledTurn)
        {
            DemoTape demo =
                Demo(
                    suffix,
                    dominantDegree
                );

            SceneRelease release =
                new SceneRelease(
                    "Release " + suffix,
                    demo.DemoTapeId,
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            if (activeTrve)
            {
                ApplyPerformanceActivation(
                    release,
                    dominantDegree,
                    settledTurn
                );
            }

            if (field)
            {
                Assert.That(
                    release.TryFetter(
                        settledTurn
                    ),
                    Is.True
                );

                if (fieldPosition.HasValue)
                {
                    Assert.That(
                        release.TryEstablishFieldPosition(
                            fieldPosition.Value,
                            settledTurn
                        ),
                        Is.True
                    );
                }
            }

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    new SceneReleaseLegitimacyEvaluator()
                        .Evaluate(
                            release,
                            demo,
                            Environment(
                                settledTurn,
                                dominantDegree
                            )
                        );

            return new Fixture(
                release,
                evaluation
            );
        }

        private static void
            ApplyPerformanceActivation(
                SceneRelease release,
                TagDegree recordedDominantDegree,
                int turn)
        {
            ActivationLegitimacyCandidate candidate =
                new ActivationLegitimacyCandidate(
                    ActivationAttemptRoute.Performance,
                    "HAPPENING",
                    "PERFORM",
                    "ACTOR",
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    "TRACK",
                    "IDEA",
                    0,
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    TagDegree.Weak,
                    recordedDominantDegree
                );

            AcceptedTransgressionRecord precedent =
                new AcceptedTransgressionRecord(
                    "AT_" + release.ReleaseId,
                    "KVLT",
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    0,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "TEST_SCENARIO"
                );

            ActivationLegitimacyAssessment assessment =
                new ActivationLegitimacyAssessment(
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
                        turn
                    ),
                Is.True
            );
        }

        private static DemoTape Demo(
            string suffix,
            TagDegree dominantDegree)
        {
            DemoTapeIdeaSnapshot pair =
                new DemoTapeIdeaSnapshot(
                    "IDEA",
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
                            dominantDegree,
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
                new DemoTapeTrackSnapshot(
                    "TRACK",
                    "Track",
                    1f,
                    1f,
                    new[]
                    {
                        pair
                    }
                );

            return new DemoTape(
                "DEMO_" + suffix,
                "Demo " + suffix,
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

        private static TrackEvaluationEnvironment
            Environment(
                int settledTurn,
                TagDegree dominantDegree)
        {
            NormativeCentre centre =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            dominantDegree,
                            1f
                        )
                    }
                );

            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            return new TrackEvaluationEnvironment(
                settledTurn,
                centre,
                centre,
                society
            );
        }

        private sealed class Fixture
        {
            public SceneRelease Release { get; }

            public SceneReleaseLegitimacyEvaluation
                Evaluation { get; }

            public Fixture(
                SceneRelease release,
                SceneReleaseLegitimacyEvaluation
                    evaluation)
            {
                Release =
                    release;

                Evaluation =
                    evaluation;
            }
        }
    }
}