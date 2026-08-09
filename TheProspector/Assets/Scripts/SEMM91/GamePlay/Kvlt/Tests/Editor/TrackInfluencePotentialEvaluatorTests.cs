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
    public class TrackInfluencePotentialEvaluatorTests
    {
        [Test]
        public void CapableDormantPair_HasPositiveInfluencePotential()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            NormativeCentre canon =
                Centre(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    1f
                );

            TrackInfluencePotentialEvaluation result =
                Evaluate(
                    track,
                    NormativeCentre.Neutral,
                    canon,
                    SacredSociety()
                );

            // Profane2 -> Sacred1
            //
            // B = 1
            // E_t = 3
            // T_max = 3
            //
            // Canon Surface = 2
            // Idea E = 1.5
            // R_canon = 2 * (1.5 / 3) = 1
            //
            // IP = 1 * (3 / 6) = 0.5

            Assert.That(
                result.MaximumTrve,
                Is.EqualTo(3f)
            );

            Assert.That(
                result.CanonSignedResonance,
                Is.EqualTo(1f)
            );

            Assert.That(
                result.InfluencePotential,
                Is.EqualTo(0.5f)
            );
        }

        [Test]
        public void MissingSocietyBacking_ProducesZeroPotential()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            NormativeCentre canon =
                Centre(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    1f
                );

            TrackInfluencePotentialEvaluation result =
                Evaluate(
                    track,
                    NormativeCentre.Neutral,
                    canon,
                    new SocietyNormativeProfile()
                );

            Assert.That(
                result.CanonResonance,
                Is.GreaterThan(0f)
            );

            Assert.That(
                result.MaximumTrve,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.InfluencePotential,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void InfluencePotential_UsesCanonCentreNotCurrentCentre()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            NormativeCentre current =
                Centre(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    -1f
                );

            NormativeCentre canon =
                Centre(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    1f
                );

            TrackInfluencePotentialEvaluation result =
                Evaluate(
                    track,
                    current,
                    canon,
                    SacredSociety()
                );

            Assert.That(
                result.CanonSignedResonance,
                Is.EqualTo(1f)
            );

            Assert.That(
                result.SignedInfluencePotential,
                Is.EqualTo(0.5f)
            );
        }

        [Test]
        public void CurrentAlignmentCannotCreateInfluencePotentialWhenCanonIsNeutral()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            NormativeCentre current =
                Centre(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    1f
                );

            TrackInfluencePotentialEvaluation result =
                Evaluate(
                    track,
                    current,
                    NormativeCentre.Neutral,
                    SacredSociety()
                );

            Assert.That(
                result.MaximumTrve,
                Is.EqualTo(3f)
            );

            Assert.That(
                result.CanonSignedResonance,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.InfluencePotential,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void MaximumTrve_IsMeanAcrossAllIdeasIncludingSolitaryZero()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "SINGLE",
                        0,
                        TagDegree.Transgressive
                    ),

                    CreatePairIdea(
                        "PAIR",
                        1,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            NormativeCentre canon =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            1f
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant,
                            1f
                        )
                    }
                );

            TrackInfluencePotentialEvaluation result =
                Evaluate(
                    track,
                    NormativeCentre.Neutral,
                    canon,
                    SacredSociety()
                );

            // solitary = 0 maximum TRVE
            // pair     = 3
            //
            // T_max = (0 + 3) / 2 = 1.5

            Assert.That(
                result.MaximumTrve,
                Is.EqualTo(1.5f)
            );

            Assert.That(
                result.MaximumTrveFraction,
                Is.EqualTo(0.25f)
            );

            Assert.That(
                result.IdeaEvaluations[0]
                    .MaximumValidTrveContribution,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.IdeaEvaluations[1]
                    .MaximumValidTrveContribution,
                Is.EqualTo(3f)
            );
        }

        [Test]
        public void SolitaryResonance_CanParticipateInTrackForecastWhenSiblingPairHasPotential()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreateSingleIdea(
                        "SINGLE",
                        0,
                        TagDegree.Transgressive
                    ),

                    CreatePairIdea(
                        "PAIR",
                        1,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            NormativeCentre canon =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            1f
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant,
                            1f
                        )
                    }
                );

            TrackInfluencePotentialEvaluation result =
                Evaluate(
                    track,
                    NormativeCentre.Neutral,
                    canon,
                    SacredSociety()
                );

            // SINGLE:
            // s = 3
            // e = 3
            // r = 3
            //
            // PAIR:
            // s = 2
            // e = 1.5
            // r = 1
            //
            // R_canon_signed = (3 + 1) / 2 = 2
            //
            // T_max = (0 + 3) / 2 = 1.5
            // tau_max = 0.25
            //
            // IP = 2 * 0.25 = 0.5

            Assert.That(
                result.CanonSignedResonance,
                Is.EqualTo(2f)
            );

            Assert.That(
                result.MaximumTrve,
                Is.EqualTo(1.5f)
            );

            Assert.That(
                result.InfluencePotential,
                Is.EqualTo(0.5f)
            );
        }

        [Test]
        public void NegativeCanonResonance_PreservesSignedForecastDirection()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack(
                    CreatePairIdea(
                        "PAIR",
                        0,
                        TagDegree.Dominant,
                        TagDegree.Weak
                    )
                );

            NormativeCentre canon =
                Centre(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    -1f
                );

            TrackInfluencePotentialEvaluation result =
                Evaluate(
                    track,
                    NormativeCentre.Neutral,
                    canon,
                    SacredSociety()
                );

            Assert.That(
                result.CanonSignedResonance,
                Is.EqualTo(-1f)
            );

            Assert.That(
                result.SignedInfluencePotential,
                Is.EqualTo(-0.5f)
            );

            Assert.That(
                result.InfluencePotential,
                Is.EqualTo(0.5f)
            );
        }

        [Test]
        public void EmptyTrack_HasZeroInfluencePotential()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack();

            TrackInfluencePotentialEvaluation result =
                Evaluate(
                    track,
                    NormativeCentre.Neutral,
                    NormativeCentre.Neutral,
                    new SocietyNormativeProfile()
                );

            Assert.That(
                result.CanonSignedResonance,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.MaximumTrve,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.InfluencePotential,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.IdeaEvaluations,
                Is.Empty
            );
        }

        private static
            TrackInfluencePotentialEvaluation
            Evaluate(
                DemoTapeTrackSnapshot track,
                NormativeCentre current,
                NormativeCentre canon,
                SocietyNormativeProfile society)
        {
            TrackEvaluationEnvironment environment =
                new TrackEvaluationEnvironment(
                    1,
                    current,
                    canon,
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

            return new TrackInfluencePotentialEvaluator()
                .Evaluate(
                    track,
                    environment,
                    capability
                );
        }

        private static SocietyNormativeProfile
            SacredSociety()
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

        private static NormativeCentre Centre(
            TagAxis axis,
            TagPole pole,
            TagDegree degree,
            float affinity)
        {
            return new NormativeCentre(
                new[]
                {
                    new NormativeAffinityEntry(
                        axis,
                        pole,
                        degree,
                        affinity
                    )
                }
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
                int index,
                TagDegree degree)
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
                        degree,
                        DemoTapeTagOccurrenceRole
                            .Solitary
                    )
                }
            );
        }

        private static DemoTapeIdeaSnapshot
            CreatePairIdea(
                string id,
                int index,
                TagDegree dominantDegree,
                TagDegree submissiveDegree)
        {
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
                        TagPole.Negative,
                        dominantDegree,
                        DemoTapeTagOccurrenceRole
                            .PairDominant
                    ),

                    new DemoTapeTagOccurrenceSnapshot(
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        submissiveDegree,
                        DemoTapeTagOccurrenceRole
                            .PairSubmissive
                    )
                }
            );
        }
    }
}