using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class TrackTrveEvaluatorTests
    {
        [Test]
        public void SolitaryIdea_WithNoActivation_ContributesZero()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "SINGLE",
                        0
                    )
                );

            TrackTrveEvaluation result =
                Evaluate(
                    track,
                    SocietySacred(),
                    CreateActivation()
                );

            Assert.That(
                result.Trve,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.HasTrve,
                Is.False
            );

            Assert.That(
                result.IdeaEvaluations[0]
                    .ActiveTrveContribution,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void FullyActivatedCapablePair_ProducesFullContextualPotential()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            TrackTrveEvaluation result =
                Evaluate(
                    track,
                    SocietySacred(),
                    CreateActivation(
                        new IdeaActivationEvaluationSnapshot(
                            "PAIR",
                            2
                        )
                    )
                );

            IdeaTrveEvaluation idea =
                result.IdeaEvaluations[0];

            // D = 2
            // B = 1
            // E_t = 3
            // A = 2
            // F = 1
            // t = 3

            Assert.That(
                idea.ContextualTrvePotential,
                Is.EqualTo(3f)
            );

            Assert.That(
                idea.ActivationFraction,
                Is.EqualTo(1f)
            );

            Assert.That(
                idea.ActiveTrveContribution,
                Is.EqualTo(3f)
            );

            Assert.That(
                result.Trve,
                Is.EqualTo(3f)
            );

            Assert.That(
                result.HasTrve,
                Is.True
            );
        }

        [Test]
        public void PartialActivation_ScalesContextualPotential()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            TrackTrveEvaluation result =
                Evaluate(
                    track,
                    SocietySacred(),
                    CreateActivation(
                        new IdeaActivationEvaluationSnapshot(
                            "PAIR",
                            1
                        )
                    )
                );

            IdeaTrveEvaluation idea =
                result.IdeaEvaluations[0];

            // D = 2
            // B = 1
            // E_t = 3
            // A = 1
            // F = 0.5
            // t = 1.5

            Assert.That(
                idea.ActivationFraction,
                Is.EqualTo(0.5f)
            );

            Assert.That(
                idea.ActiveTrveContribution,
                Is.EqualTo(1.5f)
            );

            Assert.That(
                result.Trve,
                Is.EqualTo(1.5f)
            );
        }

        [Test]
        public void CapablePairWithoutActivation_ContributesZero()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            TrackTrveEvaluation result =
                Evaluate(
                    track,
                    SocietySacred(),
                    CreateActivation()
                );

            Assert.That(
                result.IdeaEvaluations[0]
                    .IsTrveCapable,
                Is.True
            );

            Assert.That(
                result.IdeaEvaluations[0]
                    .LegitimateActiveDegree,
                Is.EqualTo(0)
            );

            Assert.That(
                result.Trve,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.HasTrve,
                Is.False
            );
        }

        [Test]
        public void ExistingActivation_WithNoCurrentSocietyBacking_ProducesZeroTrve()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            TrackTrveEvaluation result =
                Evaluate(
                    track,
                    new SocietyNormativeProfile(),
                    CreateActivation(
                        new IdeaActivationEvaluationSnapshot(
                            "PAIR",
                            1
                        )
                    )
                );

            IdeaTrveEvaluation idea =
                result.IdeaEvaluations[0];

            Assert.That(
                idea.LegitimateActiveDegree,
                Is.EqualTo(1)
            );

            Assert.That(
                idea.ActivationFraction,
                Is.EqualTo(0.5f)
            );

            Assert.That(
                idea.IsTrveCapable,
                Is.False
            );

            Assert.That(
                idea.ActiveTrveContribution,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.HasTrve,
                Is.False
            );
        }

        [Test]
        public void TrackTrve_IsMeanAcrossAllIdeasIncludingZeroContributions()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "SINGLE",
                        0
                    ),

                    CreatePairIdea(
                        "PAIR",
                        1,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            TrackTrveEvaluation result =
                Evaluate(
                    track,
                    SocietySacred(),
                    CreateActivation(
                        new IdeaActivationEvaluationSnapshot(
                            "PAIR",
                            2
                        )
                    )
                );

            // SINGLE = 0
            // PAIR   = 3
            //
            // T = (0 + 3) / 2 = 1.5

            Assert.That(
                result.Trve,
                Is.EqualTo(1.5f)
            );

            Assert.That(
                result.IdeaEvaluations.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                result.HasTrve,
                Is.True
            );
        }

        [Test]
        public void OpposedDirectionalPairs_DoNotCancelAtTrveLevel()
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Weak
            );

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Weak
            );

            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PROFANE",
                        0,
                        TagPole.Negative,
                        TagDegree.Weak,
                        TagDegree.Weak
                    ),

                    CreatePairIdea(
                        "SACRED",
                        1,
                        TagPole.Positive,
                        TagDegree.Weak,
                        TagDegree.Weak
                    )
                );

            TrackTrveEvaluation result =
                Evaluate(
                    track,
                    society,
                    CreateActivation(
                        new IdeaActivationEvaluationSnapshot(
                            "PROFANE",
                            1
                        ),
                        new IdeaActivationEvaluationSnapshot(
                            "SACRED",
                            1
                        )
                    )
                );

            // Each pair:
            // D = 1, B = 1, E_t = 2
            // A = 1, F = 1
            // t = 2
            //
            // T = mean(2, 2) = 2
            //
            // They do NOT cancel to zero.

            Assert.That(
                result.Trve,
                Is.EqualTo(2f)
            );

            Assert.That(
                result.IdeaEvaluations[0]
                    .ActiveTrveContribution,
                Is.EqualTo(2f)
            );

            Assert.That(
                result.IdeaEvaluations[1]
                    .ActiveTrveContribution,
                Is.EqualTo(2f)
            );
        }

        [Test]
        public void ActivationAboveRecordedDominantDegree_IsRejected()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagPole.Negative,
                        TagDegree.Weak,
                        TagDegree.Weak
                    )
                );

            Assert.Throws<InvalidOperationException>(
                () =>
                    Evaluate(
                        track,
                        SocietySacred(),
                        CreateActivation(
                            new IdeaActivationEvaluationSnapshot(
                                "PAIR",
                                2
                            )
                        )
                    )
            );
        }

        [Test]
        public void NonZeroActivationOnSolitaryIdea_IsRejected()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "SINGLE",
                        0
                    )
                );

            Assert.Throws<InvalidOperationException>(
                () =>
                    Evaluate(
                        track,
                        SocietySacred(),
                        CreateActivation(
                            new IdeaActivationEvaluationSnapshot(
                                "SINGLE",
                                1
                            )
                        )
                    )
            );
        }

        [Test]
        public void ActivationForUnknownIdea_IsRejected()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagPole.Negative,
                        TagDegree.Weak,
                        TagDegree.Weak
                    )
                );

            Assert.Throws<ArgumentException>(
                () =>
                    Evaluate(
                        track,
                        SocietySacred(),
                        CreateActivation(
                            new IdeaActivationEvaluationSnapshot(
                                "NOT_ON_TRACK",
                                1
                            )
                        )
                    )
            );
        }

        [Test]
        public void ActivationForDifferentTrack_IsRejected()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagPole.Negative,
                        TagDegree.Weak,
                        TagDegree.Weak
                    )
                );

            TrackEvaluationEnvironment environment =
                CreateEnvironment(
                    SocietySacred()
                );

            TrackSocietyBackingEvaluation backing =
                new TrackSocietyBackingEvaluator()
                    .Evaluate(
                        track,
                        environment
                    );

            TrackTrveCapabilityEvaluation capability =
                new TrackTrveCapabilityEvaluator()
                    .Evaluate(
                        track,
                        backing
                    );

            TrackActivationEvaluationSnapshot activation =
                new TrackActivationEvaluationSnapshot(
                    "RELEASE",
                    "DEMO",
                    "SOME_OTHER_TRACK",
                    Array.Empty<
                        IdeaActivationEvaluationSnapshot>()
                );

            Assert.Throws<ArgumentException>(
                () =>
                    new TrackTrveEvaluator()
                        .Evaluate(
                            track,
                            capability,
                            activation
                        )
            );
        }

        private static TrackTrveEvaluation Evaluate(
            DemoTapeTrackSnapshot track,
            SocietyNormativeProfile society,
            TrackActivationEvaluationSnapshot activation)
        {
            TrackEvaluationEnvironment environment =
                CreateEnvironment(
                    society
                );

            TrackSocietyBackingEvaluation backing =
                new TrackSocietyBackingEvaluator()
                    .Evaluate(
                        track,
                        environment
                    );

            TrackTrveCapabilityEvaluation capability =
                new TrackTrveCapabilityEvaluator()
                    .Evaluate(
                        track,
                        backing
                    );

            return new TrackTrveEvaluator()
                .Evaluate(
                    track,
                    capability,
                    activation
                );
        }

        private static TrackEvaluationEnvironment
            CreateEnvironment(
                SocietyNormativeProfile society)
        {
            return new TrackEvaluationEnvironment(
                1,
                NormativeCentre.Neutral,
                NormativeCentre.Neutral,
                society
            );
        }

        private static SocietyNormativeProfile
            SocietySacred()
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            return society;
        }

        private static
            TrackActivationEvaluationSnapshot
            CreateActivation(
                params
                    IdeaActivationEvaluationSnapshot[]
                    activations)
        {
            return new TrackActivationEvaluationSnapshot(
                "RELEASE",
                "DEMO",
                "TRACK",
                activations
            );
        }

        private static DemoTapeTrackSnapshot
            CreateTrack(
                params DemoTapeIdeaSnapshot[] ideas)
        {
            return new DemoTapeTrackSnapshot(
                "TRACK",
                "Track",
                0.75f,
                0.75f,
                ideas
            );
        }

        private static DemoTapeIdeaSnapshot
            CreateSingleIdea(
                string id,
                int index)
        {
            return new DemoTapeIdeaSnapshot(
                id,
                index,
                "ASPECT",
                IdeaPayloadType.SingleTag,
                "AUTHOR",
                TagContainerType.Transient,
                0.75f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive,
                        DemoTapeTagOccurrenceRole.Solitary
                    )
                }
            );
        }

        private static DemoTapeIdeaSnapshot
            CreatePairIdea(
                string id,
                int index,
                TagPole dominantPole,
                TagDegree dominantDegree,
                TagDegree submissiveDegree)
        {
            TagPole submissivePole =
                dominantPole ==
                TagPole.Negative
                    ? TagPole.Positive
                    : TagPole.Negative;

            return new DemoTapeIdeaSnapshot(
                id,
                index,
                "ASPECT",
                IdeaPayloadType.TagPair,
                "AUTHOR",
                TagContainerType.Transient,
                0.75f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        TagAxis.Symbolic,
                        dominantPole,
                        dominantDegree,
                        DemoTapeTagOccurrenceRole
                            .PairDominant
                    ),

                    new DemoTapeTagOccurrenceSnapshot(
                        TagAxis.Symbolic,
                        submissivePole,
                        submissiveDegree,
                        DemoTapeTagOccurrenceRole
                            .PairSubmissive
                    )
                }
            );
        }
    }
}