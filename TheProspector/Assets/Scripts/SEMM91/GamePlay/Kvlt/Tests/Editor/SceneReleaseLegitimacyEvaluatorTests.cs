using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class SceneReleaseLegitimacyEvaluatorTests
    {
        [Test]
        public void MultiTrackRelease_AggregatesTrackMetricsCorrectly()
        {
            DemoTape demoTape =
                CreateDemoTape();

            SceneRelease release =
                CreateRelease(
                    demoTape.DemoTapeId
                );

            TrackEvaluationEnvironment environment =
                CreateEnvironment();

            TrackActivationEvaluationSnapshot pairActivation =
                new TrackActivationEvaluationSnapshot(
                    release.ReleaseId,
                    demoTape.DemoTapeId,
                    "TRACK_PAIR",
                    new[]
                    {
                        new IdeaActivationEvaluationSnapshot(
                            "IDEA_PAIR",
                            1
                        )
                    }
                );

            SceneReleaseLegitimacyEvaluation result =
                new SceneReleaseLegitimacyEvaluator()
                    .Evaluate(
                        release,
                        demoTape,
                        environment,
                        new[]
                        {
                            pairActivation
                        }
                    );

            Assert.That(
                result.TrackEvaluations.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                result.Surface,
                Is.EqualTo(2.5f)
            );

            Assert.That(
                result.Extremity,
                Is.EqualTo(2.25f)
            );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(3)
            );

            Assert.That(
                result.Trve,
                Is.EqualTo(0.75f)
            );

            Assert.That(
                result.Resonance,
                Is.EqualTo(2f)
            );

            Assert.That(
                result.Gravity,
                Is.EqualTo(0.125f)
            );

            Assert.That(
                result.InfluencePotential,
                Is.EqualTo(0.25f)
            );

            Assert.That(
                result.HasTrve,
                Is.True
            );
        }

        [Test]
        public void MissingTrackActivation_StillEvaluatesDormantSiblingTrack()
        {
            DemoTape demoTape =
                CreateDemoTape();

            SceneRelease release =
                CreateRelease(
                    demoTape.DemoTapeId
                );

            TrackActivationEvaluationSnapshot pairActivation =
                new TrackActivationEvaluationSnapshot(
                    release.ReleaseId,
                    demoTape.DemoTapeId,
                    "TRACK_PAIR",
                    new[]
                    {
                        new IdeaActivationEvaluationSnapshot(
                            "IDEA_PAIR",
                            1
                        )
                    }
                );

            SceneReleaseLegitimacyEvaluation result =
                new SceneReleaseLegitimacyEvaluator()
                    .Evaluate(
                        release,
                        demoTape,
                        CreateEnvironment(),
                        new[]
                        {
                            pairActivation
                        }
                    );

            Assert.That(
                result.TrackEvaluations.Count,
                Is.EqualTo(2)
            );

            TrackLegitimacyEvaluation solitaryTrack =
                result.TrackEvaluations[1];

            Assert.That(
                solitaryTrack.SourceTrackId,
                Is.EqualTo("TRACK_SOLITARY")
            );

            Assert.That(
                solitaryTrack.T,
                Is.EqualTo(0f)
            );

            Assert.That(
                solitaryTrack.GravityMagnitude,
                Is.EqualTo(0f)
            );

            Assert.That(
                solitaryTrack.ResonanceMagnitude,
                Is.EqualTo(3f)
            );
        }

        [Test]
        public void ReleaseUsesMaximumEs_ButMeansForOtherSummaries()
        {
            DemoTape demoTape =
                CreateDemoTape();

            SceneRelease release =
                CreateRelease(
                    demoTape.DemoTapeId
                );

            TrackActivationEvaluationSnapshot pairActivation =
                new TrackActivationEvaluationSnapshot(
                    release.ReleaseId,
                    demoTape.DemoTapeId,
                    "TRACK_PAIR",
                    new[]
                    {
                        new IdeaActivationEvaluationSnapshot(
                            "IDEA_PAIR",
                            1
                        )
                    }
                );

            SceneReleaseLegitimacyEvaluation result =
                new SceneReleaseLegitimacyEvaluator()
                    .Evaluate(
                        release,
                        demoTape,
                        CreateEnvironment(),
                        new[]
                        {
                            pairActivation
                        }
                    );

            // Track E:
            // pair     = 1.5
            // solitary = 3
            //
            // release E:
            // (1.5 + 3) / 2 = 2.25

            Assert.That(
                result.Extremity,
                Is.EqualTo(2.25f)
            );

            // Track E_s:
            // pair     = 2
            // solitary = 3
            //
            // release E_s:
            // max(2, 3) = 3

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(3)
            );
        }

        [Test]
        public void SceneReleaseAndDemoTapeIdentityMismatch_IsRejected()
        {
            DemoTape demoTape =
                CreateDemoTape(
                    demoTapeId: "DEMO_A"
                );

            SceneRelease release =
                CreateRelease(
                    sourceDemoTapeId: "DEMO_B"
                );

            Assert.Throws<ArgumentException>(
                () =>
                    new SceneReleaseLegitimacyEvaluator()
                        .Evaluate(
                            release,
                            demoTape,
                            CreateEnvironment(),
                            Array.Empty<
                                TrackActivationEvaluationSnapshot>()
                        )
            );
        }

        [Test]
        public void UnknownOrDuplicateTrackActivation_IsRejected()
        {
            DemoTape demoTape =
                CreateDemoTape();

            SceneRelease release =
                CreateRelease(
                    demoTape.DemoTapeId
                );

            TrackActivationEvaluationSnapshot unknown =
                new TrackActivationEvaluationSnapshot(
                    release.ReleaseId,
                    demoTape.DemoTapeId,
                    "NOT_ON_DEMO",
                    Array.Empty<
                        IdeaActivationEvaluationSnapshot>()
                );

            Assert.Throws<ArgumentException>(
                () =>
                    new SceneReleaseLegitimacyEvaluator()
                        .Evaluate(
                            release,
                            demoTape,
                            CreateEnvironment(),
                            new[]
                            {
                                unknown
                            }
                        )
            );

            TrackActivationEvaluationSnapshot first =
                new TrackActivationEvaluationSnapshot(
                    release.ReleaseId,
                    demoTape.DemoTapeId,
                    "TRACK_PAIR",
                    Array.Empty<
                        IdeaActivationEvaluationSnapshot>()
                );

            TrackActivationEvaluationSnapshot duplicate =
                new TrackActivationEvaluationSnapshot(
                    release.ReleaseId,
                    demoTape.DemoTapeId,
                    "TRACK_PAIR",
                    Array.Empty<
                        IdeaActivationEvaluationSnapshot>()
                );

            Assert.Throws<ArgumentException>(
                () =>
                    new SceneReleaseLegitimacyEvaluator()
                        .Evaluate(
                            release,
                            demoTape,
                            CreateEnvironment(),
                            new[]
                            {
                                first,
                                duplicate
                            }
                        )
            );
        }

        private static SceneRelease CreateRelease(
            string sourceDemoTapeId)
        {
            return new SceneRelease(
                "Test Release",
                sourceDemoTapeId,
                "OWNER",
                "KVLT",
                1,
                1f
            );
        }

        private static DemoTape CreateDemoTape(
            string demoTapeId = "DEMO")
        {
            DemoTapeTrackSnapshot pairTrack =
                CreatePairTrack();

            DemoTapeTrackSnapshot solitaryTrack =
                CreateSolitaryTrack();

            return new DemoTape(
                demoTapeId,
                "Test Demo",
                "SET",
                "Test Set",
                1,
                1,
                1f,
                new[]
                {
                    pairTrack,
                    solitaryTrack
                }
            );
        }

        private static DemoTapeTrackSnapshot
            CreatePairTrack()
        {
            DemoTapeIdeaSnapshot pairIdea =
                new DemoTapeIdeaSnapshot(
                    "IDEA_PAIR",
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
                );

            return new DemoTapeTrackSnapshot(
                "TRACK_PAIR",
                "Pair Track",
                0.75f,
                0.75f,
                new[]
                {
                    pairIdea
                }
            );
        }

        private static DemoTapeTrackSnapshot
            CreateSolitaryTrack()
        {
            DemoTapeIdeaSnapshot solitaryIdea =
                new DemoTapeIdeaSnapshot(
                    "IDEA_SOLITARY",
                    0,
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
                            DemoTapeTagOccurrenceRole
                                .Solitary
                        )
                    }
                );

            return new DemoTapeTrackSnapshot(
                "TRACK_SOLITARY",
                "Solitary Track",
                0.75f,
                0.75f,
                new[]
                {
                    solitaryIdea
                }
            );
        }

        private static TrackEvaluationEnvironment
            CreateEnvironment()
        {
            NormativeCentre centre =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant,
                            1f
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive,
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
                4,
                centre,
                centre,
                society
            );
        }
    }
}