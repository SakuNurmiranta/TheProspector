using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Circulation.Tests.Editor
{
    public sealed class
        SceneReleaseIngressEvaluatorTests
    {
        private readonly
            SceneReleaseIngressEvaluator
            evaluator =
                new();

        [Test]
        public void
            NoBandPositionUsesFreshReleasePosition()
        {
            SceneRelease release =
                FieldRelease();

            SceneReleaseIngressEvaluation result =
                evaluator.Evaluate(
                    release,
                    null,
                    freshReleasePosition:
                        0.20f,
                    innerFieldEntryCeiling:
                        0.70f,
                    placementTurn:
                        6
                );

            Assert.That(
                result.HasBandScenePositionBasis,
                Is.False
            );

            Assert.That(
                result.UncappedPosition,
                Is.EqualTo(0.20f)
            );

            Assert.That(
                result.AppliedInitialPosition,
                Is.EqualTo(0.20f)
            );
        }

        [Test]
        public void
            EmptyBandPositionUsesFreshReleasePosition()
        {
            SceneRelease release =
                FieldRelease();

            BandScenePositionEvaluation band =
                EmptyBandPosition();

            SceneReleaseIngressEvaluation result =
                evaluator.Evaluate(
                    release,
                    band,
                    0.20f,
                    0.70f,
                    6
                );

            Assert.That(
                result.HasBandScenePositionBasis,
                Is.False
            );

            Assert.That(
                result.AppliedInitialPosition,
                Is.EqualTo(0.20f)
            );
        }

        [Test]
        public void
            EstablishedBandPositionProvidesEntryPosition()
        {
            SceneRelease release =
                FieldRelease();

            SceneReleaseIngressEvaluation result =
                evaluator.Evaluate(
                    release,
                    BandPosition(
                        settledTurn:
                            5,
                        position:
                            0.45f
                    ),
                    0.20f,
                    0.70f,
                    6
                );

            Assert.That(
                result.HasBandScenePositionBasis,
                Is.True
            );

            Assert.That(
                result.BandScenePositionBasis,
                Is.EqualTo(0.45f)
            );

            Assert.That(
                result.UncappedPosition,
                Is.EqualTo(0.45f)
            );
        }

        [Test]
        public void
            BandPositionBelowFreshBaselineCannotPushReleaseOutward()
        {
            SceneRelease release =
                FieldRelease();

            SceneReleaseIngressEvaluation result =
                evaluator.Evaluate(
                    release,
                    BandPosition(
                        settledTurn:
                            5,
                        position:
                            0.10f
                    ),
                    0.20f,
                    0.70f,
                    6
                );

            Assert.That(
                result.BandScenePositionBasis,
                Is.EqualTo(0.10f)
            );

            Assert.That(
                result.UncappedPosition,
                Is.EqualTo(0.20f)
            );

            Assert.That(
                result.AppliedInitialPosition,
                Is.EqualTo(0.20f)
            );
        }

        [Test]
        public void
            BandPositionBeyondEntryCeilingIsCapped()
        {
            SceneRelease release =
                FieldRelease();

            SceneReleaseIngressEvaluation result =
                evaluator.Evaluate(
                    release,
                    BandPosition(
                        settledTurn:
                            5,
                        position:
                            0.95f
                    ),
                    0.20f,
                    0.70f,
                    6
                );

            Assert.That(
                result.UncappedPosition,
                Is.EqualTo(0.95f)
            );

            Assert.That(
                result.AppliedInitialPosition,
                Is.EqualTo(0.70f)
            );

            Assert.That(
                result.WasCapped,
                Is.True
            );
        }

        [Test]
        public void
            SameTurnOrFutureBandPositionCannotFeedPlacement()
        {
            SceneRelease release =
                FieldRelease();

            Assert.Throws<ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        BandPosition(
                            6,
                            0.50f
                        ),
                        0.20f,
                        0.70f,
                        6
                    )
            );

            Assert.Throws<ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        BandPosition(
                            7,
                            0.50f
                        ),
                        0.20f,
                        0.70f,
                        6
                    )
            );
        }

        [Test]
        public void
            BandPositionFromWrongOwnerOrSceneIsRejected()
        {
            SceneRelease release =
                FieldRelease();

            Assert.Throws<ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        new BandScenePositionEvaluation(
                            "OTHER_OWNER",
                            "KVLT_SCENE",
                            5,
                            new[]
                            {
                                "OLD"
                            },
                            0.50f
                        ),
                        0.20f,
                        0.70f,
                        6
                    )
            );

            Assert.Throws<ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        new BandScenePositionEvaluation(
                            "OWNER",
                            "OTHER_SCENE",
                            5,
                            new[]
                            {
                                "OLD"
                            },
                            0.50f
                        ),
                        0.20f,
                        0.70f,
                        6
                    )
            );
        }

        private static
            BandScenePositionEvaluation
            EmptyBandPosition()
        {
            return new BandScenePositionEvaluation(
                "OWNER",
                "KVLT_SCENE",
                5,
                Array.Empty<string>(),
                null
            );
        }

        private static
            BandScenePositionEvaluation
            BandPosition(
                int settledTurn,
                float position)
        {
            return new BandScenePositionEvaluation(
                "OWNER",
                "KVLT_SCENE",
                settledTurn,
                new[]
                {
                    "OLD_RELEASE"
                },
                position
            );
        }

        private static SceneRelease
            FieldRelease()
        {
            SceneRelease release =
                new(
                    "Test Release",
                    "DEMO",
                    "OWNER",
                    "KVLT_SCENE",
                    4,
                    1f
                );

            Assert.That(
                release.TryFetter(5),
                Is.True
            );

            return release;
        }
    }
}