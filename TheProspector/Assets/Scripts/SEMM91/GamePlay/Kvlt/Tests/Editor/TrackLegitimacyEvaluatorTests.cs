using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class TrackLegitimacyEvaluatorTests
    {
        [Test]
        public void CompleteEvaluator_AssemblesAllTrackMetrics()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack();

            TrackEvaluationEnvironment environment =
                CreateEnvironment();

            TrackActivationEvaluationSnapshot activation =
                new TrackActivationEvaluationSnapshot(
                    "RELEASE",
                    "DEMO",
                    "TRACK",
                    new[]
                    {
                        new IdeaActivationEvaluationSnapshot(
                            "PAIR",
                            1
                        )
                    }
                );

            TrackLegitimacyEvaluation result =
                new TrackLegitimacyEvaluator()
                    .Evaluate(
                        track,
                        environment,
                        activation
                    );

            Assert.That(
                result.SourceReleaseId,
                Is.EqualTo("RELEASE")
            );

            Assert.That(
                result.SourceDemoTapeId,
                Is.EqualTo("DEMO")
            );

            Assert.That(
                result.SourceTrackId,
                Is.EqualTo("TRACK")
            );

            Assert.That(
                result.SettledTurn,
                Is.EqualTo(4)
            );

            // Pair = Profane2 -> Sacred1
            //
            // Current Surface:
            // 2 * +1 = 2
            //
            // E:
            // (2 + 1) / 2 = 1.5
            //
            // Society Sacred3:
            // B = 1
            //
            // Et = 3
            //
            // A = 1
            // F = 0.5
            // T = 1.5
            //
            // R:
            // 2 * (1.5 / 3) = 1
            //
            // tau:
            // 1.5 / 6 = 0.25
            //
            // G = 0.25
            //
            // E_s:
            // Profane2 violates Sacred norm
            // = 2
            //
            // IP:
            // Canon R = 1
            // Tmax = 3
            // tauMax = 0.5
            // IP = 0.5

            Assert.That(
                result.Surface,
                Is.EqualTo(2f)
            );

            Assert.That(
                result.Extremity,
                Is.EqualTo(1.5f)
            );

            Assert.That(
                result.Es,
                Is.EqualTo(2)
            );

            Assert.That(
                result.T,
                Is.EqualTo(1.5f)
            );

            Assert.That(
                result.HasTrve,
                Is.True
            );

            Assert.That(
                result.SignedResonance,
                Is.EqualTo(1f)
            );

            Assert.That(
                result.ResonanceMagnitude,
                Is.EqualTo(1f)
            );

            Assert.That(
                result.SignedGravity,
                Is.EqualTo(0.25f)
            );

            Assert.That(
                result.GravityMagnitude,
                Is.EqualTo(0.25f)
            );

            Assert.That(
                result.SignedInfluencePotential,
                Is.EqualTo(0.5f)
            );

            Assert.That(
                result.InfluencePotentialMagnitude,
                Is.EqualTo(0.5f)
            );
        }

        [Test]
        public void CompleteEvaluator_PreservesDetailedSubResults()
        {
            TrackLegitimacyEvaluation result =
                new TrackLegitimacyEvaluator()
                    .Evaluate(
                        CreateTrack(),
                        CreateEnvironment(),
                        new TrackActivationEvaluationSnapshot(
                            "RELEASE",
                            "DEMO",
                            "TRACK",
                            new[]
                            {
                                new IdeaActivationEvaluationSnapshot(
                                    "PAIR",
                                    1
                                )
                            }
                        )
                    );

            Assert.That(
                result.SurfaceExtremity
                    .IdeaEvaluations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.SocietyBacking
                    .IdeaEvaluations[0]
                    .SocietyBacking,
                Is.EqualTo(1f)
            );

            Assert.That(
                result.TrveCapability
                    .IdeaEvaluations[0]
                    .ContextualTrvePotential,
                Is.EqualTo(3f)
            );

            Assert.That(
                result.Trve
                    .IdeaEvaluations[0]
                    .LegitimateActiveDegree,
                Is.EqualTo(1)
            );

            Assert.That(
                result.SensationalExtremity
                    .OccurrenceEvaluations.Count,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void CompleteEvaluator_IsDeterministicForSameFrozenInputs()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack();

            TrackEvaluationEnvironment environment =
                CreateEnvironment();

            TrackActivationEvaluationSnapshot activation =
                new TrackActivationEvaluationSnapshot(
                    "RELEASE",
                    "DEMO",
                    "TRACK",
                    new[]
                    {
                        new IdeaActivationEvaluationSnapshot(
                            "PAIR",
                            1
                        )
                    }
                );

            TrackLegitimacyEvaluator evaluator =
                new TrackLegitimacyEvaluator();

            TrackLegitimacyEvaluation first =
                evaluator.Evaluate(
                    track,
                    environment,
                    activation
                );

            TrackLegitimacyEvaluation second =
                evaluator.Evaluate(
                    track,
                    environment,
                    activation
                );

            Assert.That(
                second.Surface,
                Is.EqualTo(first.Surface)
            );

            Assert.That(
                second.Extremity,
                Is.EqualTo(first.Extremity)
            );

            Assert.That(
                second.Es,
                Is.EqualTo(first.Es)
            );

            Assert.That(
                second.T,
                Is.EqualTo(first.T)
            );

            Assert.That(
                second.SignedResonance,
                Is.EqualTo(first.SignedResonance)
            );

            Assert.That(
                second.SignedGravity,
                Is.EqualTo(first.SignedGravity)
            );

            Assert.That(
                second.SignedInfluencePotential,
                Is.EqualTo(
                    first.SignedInfluencePotential
                )
            );
        }

        [Test]
        public void InfluencePotential_RemainsIndependentOfCurrentCentre()
        {
            DemoTapeTrackSnapshot track =
                CreateTrack();

            SocietyNormativeProfile society =
                SacredSociety();

            NormativeCentre current =
                Centre(
                    -1f
                );

            NormativeCentre canon =
                Centre(
                    1f
                );

            TrackEvaluationEnvironment environment =
                new TrackEvaluationEnvironment(
                    4,
                    current,
                    canon,
                    society
                );

            TrackLegitimacyEvaluation result =
                new TrackLegitimacyEvaluator()
                    .Evaluate(
                        track,
                        environment,
                        new TrackActivationEvaluationSnapshot(
                            "RELEASE",
                            "DEMO",
                            "TRACK",
                            new[]
                            {
                                new IdeaActivationEvaluationSnapshot(
                                    "PAIR",
                                    1
                                )
                            }
                        )
                    );

            Assert.That(
                result.SignedResonance,
                Is.EqualTo(-1f)
            );

            Assert.That(
                result.SignedGravity,
                Is.EqualTo(-0.25f)
            );

            Assert.That(
                result.SignedInfluencePotential,
                Is.EqualTo(0.5f)
            );
        }

        [Test]
        public void ActivationForDifferentTrack_IsRejected()
        {
            Assert.Throws<System.ArgumentException>(
                () =>
                    new TrackLegitimacyEvaluator()
                        .Evaluate(
                            CreateTrack(),
                            CreateEnvironment(),
                            new TrackActivationEvaluationSnapshot(
                                "RELEASE",
                                "DEMO",
                                "OTHER_TRACK",
                                System.Array.Empty<
                                    IdeaActivationEvaluationSnapshot>()
                            )
                        )
            );
        }

        private static DemoTapeTrackSnapshot
            CreateTrack()
        {
            return new DemoTapeTrackSnapshot(
                "TRACK",
                "Track",
                0.75f,
                0.75f,
                new[]
                {
                    new DemoTapeIdeaSnapshot(
                        "PAIR",
                        0,
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
                                TagDegree.Dominant,
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
                    )
                }
            );
        }

        private static TrackEvaluationEnvironment
            CreateEnvironment()
        {
            return new TrackEvaluationEnvironment(
                4,
                Centre(1f),
                Centre(1f),
                SacredSociety()
            );
        }

        private static NormativeCentre Centre(
            float affinity)
        {
            return new NormativeCentre(
                new[]
                {
                    new NormativeAffinityEntry(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        affinity
                    )
                }
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
    }
}