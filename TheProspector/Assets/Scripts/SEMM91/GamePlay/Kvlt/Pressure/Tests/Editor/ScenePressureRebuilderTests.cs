using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;

namespace SEMM91.GamePlay.Kvlt.Pressure.Tests.Editor
{
    public class ScenePressureRebuilderTests
    {
        private readonly ScenePressureRebuilder
            rebuilder =
                new();

        [Test]
        public void
            SolitaryAndFormalPairProduceRecordedRawPressure()
        {
            DemoTape tape =
                Demo(
                    "DEMO",
                    new[]
                    {
                        Single(
                            "IDEA_SINGLE",
                            0,
                            TagAxis.Emotional,
                            TagPole.Negative,
                            TagDegree.Dominant
                        ),

                        Pair(
                            "IDEA_PAIR",
                            1,
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant,
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            TagDegree.Weak
                        )
                    }
                );

            SceneRelease release =
                FieldRelease(
                    tape,
                    "RELEASE"
                );

            ScenePressureRebuildEvaluation result =
                rebuilder.Rebuild(
                    "KVLT",
                    6,
                    new[]
                    {
                        release
                    },
                    new[]
                    {
                        tape
                    },
                    new CanonState(),
                    Policy()
                );

            Assert.That(
                result.Pressure.GetRawPressure(
                    TagAxis.Emotional,
                    TagPole.Negative
                ),
                Is.EqualTo(2f)
            );

            Assert.That(
                result.Pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(2f)
            );

            Assert.That(
                result.Pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.EqualTo(-1f)
            );

            Assert.That(
                result.SourceContributionCount,
                Is.EqualTo(3)
            );
        }

        [Test]
        public void
            PressureUsesRecordedSemanticsWithoutActivation()
        {
            DemoTape tape =
                Demo(
                    "DEMO",
                    new[]
                    {
                        Pair(
                            "IDEA",
                            0,
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            TagDegree.Dominant
                        )
                    }
                );

            SceneRelease release =
                FieldRelease(
                    tape,
                    "RELEASE"
                );

            Assert.That(
                release.ActivationHistory,
                Is.Empty
            );

            ScenePressureRebuildEvaluation result =
                rebuilder.Rebuild(
                    "KVLT",
                    6,
                    new[]
                    {
                        release
                    },
                    new[]
                    {
                        tape
                    },
                    new CanonState(),
                    Policy()
                );

            Assert.That(
                result.Pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(3f)
            );

            Assert.That(
                result.Pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.EqualTo(-2f)
            );
        }

        [Test]
        public void
            OnlyCurrentFieldLifecycleContributes()
        {
            DemoTape fieldTape =
                Demo(
                    "FIELD_DEMO",
                    new[]
                    {
                        Single(
                            "FIELD_IDEA",
                            0,
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Weak
                        )
                    }
                );

            DemoTape fringeTape =
                Demo(
                    "FRINGE_DEMO",
                    new[]
                    {
                        Single(
                            "FRINGE_IDEA",
                            0,
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive
                        )
                    }
                );

            DemoTape failedTape =
                Demo(
                    "FAILED_DEMO",
                    new[]
                    {
                        Single(
                            "FAILED_IDEA",
                            0,
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive
                        )
                    }
                );

            DemoTape rejectedTape =
                Demo(
                    "REJECTED_DEMO",
                    new[]
                    {
                        Single(
                            "REJECTED_IDEA",
                            0,
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive
                        )
                    }
                );

            SceneRelease field =
                FieldRelease(
                    fieldTape,
                    "FIELD"
                );

            SceneRelease fringe =
                new(
                    "Fringe",
                    fringeTape.DemoTapeId,
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            SceneRelease failed =
                new(
                    "Failed",
                    failedTape.DemoTapeId,
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            Assert.That(
                failed.TryFailToFetter(6),
                Is.True
            );

            SceneRelease rejected =
                FieldRelease(
                    rejectedTape,
                    "REJECTED"
                );

            Assert.That(
                rejected.TryApplyFieldMovement(
                    -1f,
                    6,
                    out _
                ),
                Is.True
            );

            Assert.That(
                rejected.TryReject(
                    6,
                    0f
                ),
                Is.True
            );

            ScenePressureRebuildEvaluation result =
                rebuilder.Rebuild(
                    "KVLT",
                    6,
                    new[]
                    {
                        failed,
                        fringe,
                        rejected,
                        field
                    },
                    new[]
                    {
                        fieldTape,
                        fringeTape,
                        failedTape,
                        rejectedTape
                    },
                    new CanonState(),
                    Policy()
                );

            /*
             * Only the Field1 material survives into
             * Dynamic Scene Pressure.
             */
            Assert.That(
                result.Pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(1f)
            );

            Assert.That(
                result.SourceContributionCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            SeveralFieldReleasesAggregateIntoOneTagPressure()
        {
            DemoTape tapeA =
                Demo(
                    "A",
                    new[]
                    {
                        Single(
                            "IDEA_A",
                            0,
                            TagAxis.Physical,
                            TagPole.Negative,
                            TagDegree.Dominant
                        )
                    }
                );

            DemoTape tapeB =
                Demo(
                    "B",
                    new[]
                    {
                        Single(
                            "IDEA_B",
                            0,
                            TagAxis.Physical,
                            TagPole.Negative,
                            TagDegree.Weak
                        )
                    }
                );

            ScenePressureRebuildEvaluation result =
                rebuilder.Rebuild(
                    "KVLT",
                    6,
                    new[]
                    {
                        FieldRelease(
                            tapeB,
                            "B"
                        ),

                        FieldRelease(
                            tapeA,
                            "A"
                        )
                    },
                    new[]
                    {
                        tapeA,
                        tapeB
                    },
                    new CanonState(),
                    Policy()
                );

            Assert.That(
                result.Pressure.GetRawPressure(
                    TagAxis.Physical,
                    TagPole.Negative
                ),
                Is.EqualTo(3f)
            );
        }

        [Test]
        public void
            PositivePressureCannotStrengthenDirectCanon()
        {
            CanonState canon =
                Canon(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            ScenePressureRebuildEvaluation result =
                OneTagPressure(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive
                );

            Assert.That(
                result.Pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(3f)
            );

            Assert.That(
                result.Pressure.GetEffectivePressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void
            PressureTowardCanonicallyOpposedDirectionCanSoftenRejection()
        {
            /*
             * Sacred is canonical.
             * Profane therefore faces canonical
             * opposition.
             */
            CanonState canon =
                Canon(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                );

            ScenePressureRebuildEvaluation result =
                OneTagPressure(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive
                );

            /*
             * Policy counter scale = 0.5:
             * raw 3 → 1.5 → cap 0.75.
             */
            Assert.That(
                result.Pressure.GetEffectivePressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(0.75f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            NegativePressureAgainstDirectCanonActsAsCounterPressure()
        {
            DemoTape tape =
                Demo(
                    "DEMO",
                    new[]
                    {
                        Pair(
                            "IDEA",
                            0,
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Weak,
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            TagDegree.Dominant
                        )
                    }
                );

            /*
             * The submissive Sacred2 occurrence
             * contributes Sacred -2.
             *
             * Sacred itself is canonical, so this is
             * meaningful counter-pressure against the
             * direct canonical direction.
             */
            CanonState canon =
                Canon(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                );

            ScenePressureRebuildEvaluation result =
                rebuilder.Rebuild(
                    "KVLT",
                    6,
                    new[]
                    {
                        FieldRelease(
                            tape,
                            "RELEASE"
                        )
                    },
                    new[]
                    {
                        tape
                    },
                    canon,
                    Policy()
                );

            Assert.That(
                result.Pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.EqualTo(-2f)
            );

            Assert.That(
                result.Pressure.GetEffectivePressure(
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.EqualTo(-0.75f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            NeutralPressureUsesScenarioScaleAndCap()
        {
            ScenePressureRebuildEvaluation result =
                OneTagPressure(
                    new CanonState(),
                    TagAxis.Existential,
                    TagPole.Negative,
                    TagDegree.Transgressive
                );

            /*
             * Neutral scale = 0.25:
             * raw 3 → effective 0.75.
             */
            Assert.That(
                result.Pressure.GetEffectivePressure(
                    TagAxis.Existential,
                    TagPole.Negative
                ),
                Is.EqualTo(0.75f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            FieldReleaseWithoutSourceTapeIsRejected()
        {
            DemoTape tape =
                Demo(
                    "REAL_DEMO",
                    new[]
                    {
                        Single(
                            "IDEA",
                            0,
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Weak
                        )
                    }
                );

            SceneRelease release =
                FieldRelease(
                    tape,
                    "RELEASE"
                );

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    rebuilder.Rebuild(
                        "KVLT",
                        6,
                        new[]
                        {
                            release
                        },
                        Array.Empty<DemoTape>(),
                        new CanonState(),
                        Policy()
                    )
            );
        }

        private ScenePressureRebuildEvaluation
            OneTagPressure(
                CanonState canon,
                TagAxis axis,
                TagPole pole,
                TagDegree degree)
        {
            DemoTape tape =
                Demo(
                    "DEMO",
                    new[]
                    {
                        Single(
                            "IDEA",
                            0,
                            axis,
                            pole,
                            degree
                        )
                    }
                );

            return rebuilder.Rebuild(
                "KVLT",
                6,
                new[]
                {
                    FieldRelease(
                        tape,
                        "RELEASE"
                    )
                },
                new[]
                {
                    tape
                },
                canon,
                Policy()
            );
        }

        private static
            ScenePressureRebuildPolicy
            Policy()
        {
            return new ScenePressureRebuildPolicy(
                neutralDirectionScale: 0.25f,
                counterCanonicalScale: 0.50f,
                maxAbsoluteEffectivePressure: 0.75f
            );
        }

        private static SceneRelease
            FieldRelease(
                DemoTape tape,
                string suffix)
        {
            SceneRelease release =
                new(
                    "Release " + suffix,
                    tape.DemoTapeId,
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            Assert.That(
                release.TryFetter(5),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    0.50f,
                    5
                ),
                Is.True
            );

            return release;
        }

        private static DemoTape Demo(
            string id,
            DemoTapeIdeaSnapshot[] ideas)
        {
            DemoTapeTrackSnapshot track =
                new(
                    "TRACK_" + id,
                    "Track " + id,
                    1f,
                    1f,
                    ideas
                );

            return new DemoTape(
                id,
                "Demo " + id,
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

        private static DemoTapeIdeaSnapshot
            Single(
                string ideaId,
                int ideaIndex,
                TagAxis axis,
                TagPole pole,
                TagDegree degree)
        {
            return new DemoTapeIdeaSnapshot(
                ideaId,
                ideaIndex,
                "ASPECT_" + ideaId,
                IdeaPayloadType.SingleTag,
                "AUTHOR",
                TagContainerType.Transient,
                1f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        axis,
                        pole,
                        degree,
                        DemoTapeTagOccurrenceRole
                            .Solitary
                    )
                }
            );
        }

        private static DemoTapeIdeaSnapshot
            Pair(
                string ideaId,
                int ideaIndex,
                TagAxis dominantAxis,
                TagPole dominantPole,
                TagDegree dominantDegree,
                TagAxis submissiveAxis,
                TagPole submissivePole,
                TagDegree submissiveDegree)
        {
            return new DemoTapeIdeaSnapshot(
                ideaId,
                ideaIndex,
                "ASPECT_" + ideaId,
                IdeaPayloadType.TagPair,
                "AUTHOR",
                TagContainerType.Transient,
                1f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        dominantAxis,
                        dominantPole,
                        dominantDegree,
                        DemoTapeTagOccurrenceRole
                            .PairDominant
                    ),

                    new DemoTapeTagOccurrenceSnapshot(
                        submissiveAxis,
                        submissivePole,
                        submissiveDegree,
                        DemoTapeTagOccurrenceRole
                            .PairSubmissive
                    )
                }
            );
        }

        private static CanonState Canon(
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            CanonState canon =
                new();

            Assert.That(
                canon.TryRecordPrecedent(
                    axis,
                    pole,
                    degree,
                    CanonProvenanceKind
                        .ScenarioSeed,
                    "CANON"
                ),
                Is.True
            );

            return canon;
        }
    }
}