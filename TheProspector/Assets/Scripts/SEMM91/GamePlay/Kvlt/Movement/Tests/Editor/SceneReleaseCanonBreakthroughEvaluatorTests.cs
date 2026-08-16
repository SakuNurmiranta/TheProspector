using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class
        SceneReleaseCanonBreakthroughEvaluatorTests
    {
        private readonly
            SceneReleaseCanonBreakthroughEvaluator
                breakthroughEvaluator =
                    new();

        private readonly
            SceneReleaseActivationStateService
                activationService =
                    new();

        private readonly
            SceneReleaseLegitimacyEvaluator
                legitimacyEvaluator =
                    new();

        [Test]
        public void
            RecordedDegreeAboveCanonWithoutActivation_IsNotBreakthrough()
        {
            Fixture fixture =
                FixtureWithPair(
                    "A",
                    TagDegree.Transgressive,
                    applyActivation: null,
                    societySupportsPair: true
                );

            CanonState canon =
                Canon(
                    TagDegree.Weak
                );

            SceneReleaseCanonBreakthroughEvaluation
                result =
                    Evaluate(
                        fixture,
                        canon
                    );

            Assert.That(
                result.Claims.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.Claims[0]
                    .PotentialNoveltyDifferential,
                Is.EqualTo(2)
            );

            Assert.That(
                result.Claims[0]
                    .ActiveDominantDegree,
                Is.EqualTo(
                    TagDegree.Neutral
                )
            );

            Assert.That(
                result.Claims[0]
                    .RealizedBreakthroughDifferential,
                Is.EqualTo(0)
            );

            Assert.That(
                result.HasQualifyingBreakthrough,
                Is.False
            );
        }

        [Test]
        public void
            LegitimateActiveTrveAboveCanon_IsBreakthrough()
        {
            Fixture fixture =
                FixtureWithPair(
                    "A",
                    TagDegree.Transgressive,
                    applyActivation:
                        TagDegree.Dominant,
                    societySupportsPair: true
                );

            SceneReleaseCanonBreakthroughEvaluation
                result =
                    Evaluate(
                        fixture,
                        Canon(
                            TagDegree.Weak
                        )
                    );

            Assert.That(
                result.HasQualifyingBreakthrough,
                Is.True
            );

            Assert.That(
                result.QualifyingClaims.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.Claims[0]
                    .RecordedDominantDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                result.Claims[0]
                    .ActiveDominantDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                result.Claims[0]
                    .CanonicalDominantDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            Assert.That(
                result.Claims[0]
                    .RealizedBreakthroughDifferential,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            ActivationAtCanonicalCeiling_IsNotBreakthrough()
        {
            Fixture fixture =
                FixtureWithPair(
                    "A",
                    TagDegree.Transgressive,
                    applyActivation:
                        TagDegree.Dominant,
                    societySupportsPair: true
                );

            SceneReleaseCanonBreakthroughEvaluation
                result =
                    Evaluate(
                        fixture,
                        Canon(
                            TagDegree.Dominant
                        )
                    );

            Assert.That(
                result.Claims[0]
                    .PotentialNoveltyDifferential,
                Is.EqualTo(1)
            );

            Assert.That(
                result.Claims[0]
                    .RealizedBreakthroughDifferential,
                Is.EqualTo(0)
            );

            Assert.That(
                result.HasQualifyingBreakthrough,
                Is.False
            );
        }

        [Test]
        public void
            CanonAbsent_MakesAnyRealizedDegreeNovel()
        {
            Fixture fixture =
                FixtureWithPair(
                    "A",
                    TagDegree.Weak,
                    applyActivation:
                        TagDegree.Weak,
                    societySupportsPair: true
                );

            SceneReleaseCanonBreakthroughEvaluation
                result =
                    Evaluate(
                        fixture,
                        new CanonState()
                    );

            SceneReleaseCanonBreakthroughClaim claim =
                result.Claims[0];

            Assert.That(
                claim.HasCanonicalPrecedent,
                Is.False
            );

            Assert.That(
                claim.CanonicalDominantDegree,
                Is.EqualTo(
                    TagDegree.Neutral
                )
            );

            Assert.That(
                claim.RealizedBreakthroughDifferential,
                Is.EqualTo(1)
            );

            Assert.That(
                claim.IsQualifyingBreakthrough,
                Is.True
            );
        }

        [Test]
        public void
            ActivatedButCurrentlyNonTrvePair_DoesNotBreakThrough()
        {
            Fixture fixture =
                FixtureWithPair(
                    "A",
                    TagDegree.Transgressive,
                    applyActivation:
                        TagDegree.Transgressive,
                    societySupportsPair: false
                );

            Assert.That(
                fixture.Legitimacy.HasTrve,
                Is.False
            );

            SceneReleaseCanonBreakthroughEvaluation
                result =
                    Evaluate(
                        fixture,
                        Canon(
                            TagDegree.Weak
                        )
                    );

            Assert.That(
                result.Claims[0]
                    .ActiveDominantDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                result.Claims[0]
                    .IsCurrentlyTrve,
                Is.False
            );

            Assert.That(
                result.Claims[0]
                    .PotentialNoveltyDifferential,
                Is.EqualTo(2)
            );

            Assert.That(
                result.Claims[0]
                    .RealizedBreakthroughDifferential,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void
            PendingHigherDegree_DoesNotRaiseBreakthrough()
        {
            Fixture fixture =
                FixtureWithPair(
                    "A",
                    TagDegree.Transgressive,
                    applyActivation:
                        TagDegree.Weak,
                    societySupportsPair: true
                );

            /*
             * Canon already recognizes degree 1.
             *
             * Current A=1 therefore has no realized
             * breakthrough.
             */
            CanonState canon =
                Canon(
                    TagDegree.Weak
                );

            PendingActivationCandidate pending =
                new PendingActivationCandidate(
                    HailCandidate(
                        fixture.Release,
                        appliedDegree:
                            TagDegree.Transgressive,
                        recordedDegree:
                            TagDegree.Transgressive
                    ),
                    "CRISIS_PENDING",
                    6
                );

            Assert.That(
                activationService.StorePending(
                    fixture.Release,
                    pending,
                    out _
                ),
                Is.True
            );

            /*
             * Re-evaluate after Pending storage.
             * Pending itself must not alter A.
             */
            fixture.RefreshLegitimacy();

            SceneReleaseCanonBreakthroughEvaluation
                result =
                    Evaluate(
                        fixture,
                        canon
                    );

            Assert.That(
                fixture.Release.PendingActivations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.Claims[0]
                    .ActiveDominantDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            Assert.That(
                result.Claims[0]
                    .PotentialNoveltyDifferential,
                Is.EqualTo(2)
            );

            Assert.That(
                result.Claims[0]
                    .RealizedBreakthroughDifferential,
                Is.EqualTo(0)
            );

            Assert.That(
                result.HasQualifyingBreakthrough,
                Is.False
            );
        }

        [Test]
        public void
            FormalPairDirectionAndProvenanceArePreserved()
        {
            Fixture fixture =
                FixtureWithPair(
                    "A",
                    TagDegree.Dominant,
                    applyActivation:
                        TagDegree.Dominant,
                    societySupportsPair: true
                );

            SceneReleaseCanonBreakthroughEvaluation
                result =
                    Evaluate(
                        fixture,
                        new CanonState()
                    );

            SceneReleaseCanonBreakthroughClaim claim =
                result.Claims[0];

            Assert.That(
                claim.SceneReleaseId,
                Is.EqualTo(
                    fixture.Release.ReleaseId
                )
            );

            Assert.That(
                claim.SourceDemoTapeId,
                Is.EqualTo(
                    fixture.Demo.DemoTapeId
                )
            );

            Assert.That(
                claim.SourceTrackId,
                Is.EqualTo("TRACK_A")
            );

            Assert.That(
                claim.SourceIdeaId,
                Is.EqualTo("IDEA_A")
            );

            Assert.That(
                claim.IdeaIndex,
                Is.EqualTo(0)
            );

            Assert.That(
                claim.DominantAxis,
                Is.EqualTo(
                    TagAxis.Symbolic
                )
            );

            Assert.That(
                claim.DominantPole,
                Is.EqualTo(
                    TagPole.Negative
                )
            );

            Assert.That(
                claim.SubmissiveAxis,
                Is.EqualTo(
                    TagAxis.Symbolic
                )
            );

            Assert.That(
                claim.SubmissivePole,
                Is.EqualTo(
                    TagPole.Positive
                )
            );

            Assert.That(
                claim.RecordedSubmissiveDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );
        }

        private SceneReleaseCanonBreakthroughEvaluation
            Evaluate(
                Fixture fixture,
                CanonState canon)
        {
            return breakthroughEvaluator.Evaluate(
                fixture.Release,
                fixture.Demo,
                fixture.Legitimacy,
                canon
            );
        }

        private Fixture FixtureWithPair(
            string suffix,
            TagDegree recordedDegree,
            TagDegree? applyActivation,
            bool societySupportsPair)
        {
            DemoTape demo =
                Demo(
                    suffix,
                    recordedDegree
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

            Assert.That(
                release.TryFetter(5),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    0.40f,
                    5
                ),
                Is.True
            );

            if (applyActivation.HasValue)
            {
                ApplyLegitimateActivation(
                    release,
                    applyActivation.Value,
                    recordedDegree
                );
            }

            return new Fixture(
                release,
                demo,
                legitimacyEvaluator,
                Environment(
                    societySupportsPair
                )
            );
        }

        private void ApplyLegitimateActivation(
            SceneRelease release,
            TagDegree appliedDegree,
            TagDegree recordedDegree)
        {
            ActivationLegitimacyCandidate candidate =
                HailCandidate(
                    release,
                    appliedDegree,
                    recordedDegree
                );

            AcceptedTransgressionRecord precedent =
                new AcceptedTransgressionRecord(
                    "AT_" +
                    release.ReleaseId +
                    "_" +
                    appliedDegree,
                    "KVLT",
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    appliedDegree,
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
                activationService.ApplyCovered(
                    release,
                    assessment,
                    6
                ),
                Is.True
            );
        }

        private static
            ActivationLegitimacyCandidate
            HailCandidate(
                SceneRelease release,
                TagDegree appliedDegree,
                TagDegree recordedDegree)
        {
            return new ActivationLegitimacyCandidate(
                ActivationAttemptRoute.Hail,
                "HAPPENING",
                "HAIL_INTENT",
                "ACTOR",
                release.ReleaseId,
                release.SourceDemoTapeId,
                "TRACK_A",
                "IDEA_A",
                0,
                "CHURCH_ARSON",
                TagAxis.Symbolic,
                TagPole.Negative,
                appliedDegree,
                appliedDegree,
                recordedDegree,
                "BEHAVIOR",
                "HAIL",
                "ASPECT_SATAN"
            );
        }

        private static CanonState Canon(
            TagDegree degree)
        {
            CanonState canon =
                new CanonState();

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    degree,
                    CanonProvenanceKind.ScenarioSeed,
                    "CANON_SEED"
                ),
                Is.True
            );

            return canon;
        }

        private static DemoTape Demo(
            string suffix,
            TagDegree dominantDegree)
        {
            DemoTapeIdeaSnapshot pair =
                new DemoTapeIdeaSnapshot(
                    "IDEA_" + suffix,
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
                    "TRACK_" + suffix,
                    "Track " + suffix,
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
                bool societySupportsPair)
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            if (societySupportsPair)
            {
                society.SetNormativeDegree(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Transgressive
                );
            }

            return new TrackEvaluationEnvironment(
                6,
                NormativeCentre.Neutral,
                NormativeCentre.Neutral,
                society
            );
        }

        private sealed class Fixture
        {
            private readonly
                SceneReleaseLegitimacyEvaluator
                evaluator;

            private readonly
                TrackEvaluationEnvironment
                environment;

            public SceneRelease Release { get; }

            public DemoTape Demo { get; }

            public SceneReleaseLegitimacyEvaluation
                Legitimacy { get; private set; }

            public Fixture(
                SceneRelease release,
                DemoTape demo,
                SceneReleaseLegitimacyEvaluator evaluator,
                TrackEvaluationEnvironment environment)
            {
                Release =
                    release;

                Demo =
                    demo;

                this.evaluator =
                    evaluator;

                this.environment =
                    environment;

                RefreshLegitimacy();
            }

            public void RefreshLegitimacy()
            {
                Legitimacy =
                    evaluator.Evaluate(
                        Release,
                        Demo,
                        environment
                    );
            }
        }
    }
}