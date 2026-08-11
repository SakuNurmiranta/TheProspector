using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class
        SceneReleaseNexusBoundaryEvaluatorTests
    {
        private readonly
            SceneReleaseNexusBoundaryEvaluator evaluator =
                new();

        [Test]
        public void
            QualifyingBreakthroughBelowNexus_RemainsField()
        {
            SceneRelease release =
                MovedFieldRelease(
                    "A",
                    startPosition: 0.40f,
                    movement: 0.30f
                );

            SceneReleaseNexusBoundaryEvaluation result =
                evaluator.Evaluate(
                    release,
                    Breakthrough(
                        release,
                        startPosition: 0.40f,
                        qualifying: true
                    ),
                    nexusBoundary: 1f
                );

            Assert.That(
                release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.70f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.HasReachedNexusBoundary,
                Is.False
            );

            Assert.That(
                result.IsCanonCandidate,
                Is.False
            );

            Assert.That(
                result.Disposition,
                Is.EqualTo(
                    SceneReleaseNexusBoundaryDisposition
                        .RemainsField
                )
            );
        }

        [Test]
        public void
            QualifyingBreakthroughExactlyAtNexus_IsCanonCandidate()
        {
            SceneRelease release =
                MovedFieldRelease(
                    "A",
                    startPosition: 0.40f,
                    movement: 0.60f
                );

            SceneReleaseNexusBoundaryEvaluation result =
                evaluator.Evaluate(
                    release,
                    Breakthrough(
                        release,
                        0.40f,
                        qualifying: true
                    ),
                    nexusBoundary: 1f
                );

            Assert.That(
                result.FieldPosition,
                Is.EqualTo(1f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.SignedDistanceFromNexusBoundary,
                Is.EqualTo(0f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.HasReachedNexusBoundary,
                Is.True
            );

            Assert.That(
                result.IsCanonCandidate,
                Is.True
            );

            /*
             * Canon candidacy is not yet a lifecycle
             * transition. Assimilation and final Field
             * scoring still have to happen.
             */
            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );
        }

        [Test]
        public void
            QualifyingBreakthroughBeyondNexus_IsCanonCandidate()
        {
            SceneRelease release =
                MovedFieldRelease(
                    "A",
                    0.40f,
                    0.85f
                );

            SceneReleaseNexusBoundaryEvaluation result =
                evaluator.Evaluate(
                    release,
                    Breakthrough(
                        release,
                        0.40f,
                        qualifying: true
                    ),
                    nexusBoundary: 1f
                );

            Assert.That(
                result.FieldPosition,
                Is.EqualTo(1.25f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.SignedDistanceFromNexusBoundary,
                Is.EqualTo(0.25f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.IsCanonCandidate,
                Is.True
            );
        }

        [Test]
        public void
            ReachingNexusWithoutNovelRealizedClaim_DoesNotCanonizeOrthodoxy()
        {
            SceneRelease release =
                MovedFieldRelease(
                    "A",
                    0.40f,
                    0.80f
                );

            SceneReleaseNexusBoundaryEvaluation result =
                evaluator.Evaluate(
                    release,
                    Breakthrough(
                        release,
                        0.40f,
                        qualifying: false
                    ),
                    nexusBoundary: 1f
                );

            Assert.That(
                result.HasReachedNexusBoundary,
                Is.True
            );

            Assert.That(
                result.HasQualifyingBreakthrough,
                Is.False
            );

            Assert.That(
                result.IsCanonCandidate,
                Is.False
            );

            Assert.That(
                result.Disposition,
                Is.EqualTo(
                    SceneReleaseNexusBoundaryDisposition
                        .RemainsField
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );
        }

        [Test]
        public void
            NexusEvaluationRequiresSameTurnMovementSettlement()
        {
            SceneRelease release =
                new SceneRelease(
                    "Release A",
                    "DEMO_A",
                    "OWNER_A",
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
                    1.10f,
                    5
                ),
                Is.True
            );

            /*
             * There is no turn-6 SettlementMovement
             * transition.
             */
            Assert.Throws<
                InvalidOperationException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        Breakthrough(
                            release,
                            1.10f,
                            qualifying: true
                        ),
                        1f
                    )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );
        }

        [Test]
        public void
            BreakthroughMustUseSameFrozenStartPositionAsMovement()
        {
            SceneRelease release =
                MovedFieldRelease(
                    "A",
                    startPosition: 0.40f,
                    movement: 0.70f
                );

            /*
             * Movement actually began at 0.40, but
             * this breakthrough snapshot falsely says
             * it was evaluated from 0.30.
             */
            SceneReleaseCanonBreakthroughEvaluation stale =
                Breakthrough(
                    release,
                    startPosition: 0.30f,
                    qualifying: true
                );

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        stale,
                        1f
                    )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );
        }

        [Test]
        public void
            BreakthroughMustMatchExactSceneReleaseProvenance()
        {
            SceneRelease release =
                MovedFieldRelease(
                    "A",
                    0.40f,
                    0.70f
                );

            SceneReleaseCanonBreakthroughEvaluation
                wrongDemo =
                    Breakthrough(
                        sceneReleaseId:
                            release.ReleaseId,
                        sourceDemoTapeId:
                            "OTHER_DEMO",
                        sourceOwnerEntityId:
                            release.SourceOwnerEntityId,
                        sceneId:
                            release.HostedSceneNodeId,
                        startPosition:
                            0.40f,
                        qualifying:
                            true
                    );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        wrongDemo,
                        1f
                    )
            );
        }

        [Test]
        public void
            RejectedReleaseCannotBecomeCanonCandidate()
        {
            SceneRelease release =
                MovedFieldRelease(
                    "A",
                    startPosition: 0.40f,
                    movement: -0.60f
                );

            Assert.That(
                release.TryReject(
                    6,
                    outerBoundary: 0f
                ),
                Is.True
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Rejected
                )
            );

            Assert.Throws<
                ArgumentException>(
                () =>
                    evaluator.Evaluate(
                        release,
                        Breakthrough(
                            release,
                            0.40f,
                            qualifying: true
                        ),
                        nexusBoundary: 1f
                    )
            );
        }

        private static SceneRelease
            MovedFieldRelease(
                string suffix,
                float startPosition,
                float movement)
        {
            SceneRelease release =
                new SceneRelease(
                    "Release " + suffix,
                    "DEMO_" + suffix,
                    "OWNER_" + suffix,
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
                    startPosition,
                    5
                ),
                Is.True
            );

            Assert.That(
                release.TryApplyFieldMovement(
                    movement,
                    6,
                    out _
                ),
                Is.True
            );

            return release;
        }

        private static
            SceneReleaseCanonBreakthroughEvaluation
            Breakthrough(
                SceneRelease release,
                float startPosition,
                bool qualifying)
        {
            return Breakthrough(
                release.ReleaseId,
                release.SourceDemoTapeId,
                release.SourceOwnerEntityId,
                release.HostedSceneNodeId,
                startPosition,
                qualifying
            );
        }

        private static
            SceneReleaseCanonBreakthroughEvaluation
            Breakthrough(
                string sceneReleaseId,
                string sourceDemoTapeId,
                string sourceOwnerEntityId,
                string sceneId,
                float startPosition,
                bool qualifying)
        {
            SceneReleaseCanonBreakthroughClaim claim =
                qualifying
                    ? QualifyingClaim(
                        sceneReleaseId,
                        sourceDemoTapeId,
                        sourceOwnerEntityId,
                        sceneId
                    )
                    : NonQualifyingClaim(
                        sceneReleaseId,
                        sourceDemoTapeId,
                        sourceOwnerEntityId,
                        sceneId
                    );

            return new
                SceneReleaseCanonBreakthroughEvaluation(
                    sceneReleaseId,
                    sourceDemoTapeId,
                    sourceOwnerEntityId,
                    sceneId,
                    6,
                    startPosition,
                    new[]
                    {
                        claim
                    }
                );
        }

        private static
            SceneReleaseCanonBreakthroughClaim
            QualifyingClaim(
                string releaseId,
                string demoId,
                string ownerId,
                string sceneId)
        {
            /*
             * Recorded D2, active D2,
             * existing Canon D1:
             *
             * B = 1.
             */
            return new
                SceneReleaseCanonBreakthroughClaim(
                    releaseId,
                    demoId,
                    ownerId,
                    sceneId,
                    "TRACK",
                    "IDEA",
                    0,
                    6,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    TagDegree.Dominant,
                    true,
                    TagDegree.Weak,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak,
                    true
                );
        }

        private static
            SceneReleaseCanonBreakthroughClaim
            NonQualifyingClaim(
                string releaseId,
                string demoId,
                string ownerId,
                string sceneId)
        {
            /*
             * Recorded D2 but active only D1,
             * and Canon already recognizes D1.
             *
             * Potential novelty exists,
             * realized breakthrough does not.
             */
            return new
                SceneReleaseCanonBreakthroughClaim(
                    releaseId,
                    demoId,
                    ownerId,
                    sceneId,
                    "TRACK",
                    "IDEA",
                    0,
                    6,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    TagDegree.Weak,
                    true,
                    TagDegree.Weak,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak,
                    true
                );
        }
    }
}