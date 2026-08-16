using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class Camp1LegitimacyAcceptanceTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void PersistedMultiTrackDemo_ProducesExpectedCompleteEvaluation()
        {
            DemoTape demoTape =
                CreateDemoTape();

            SceneRelease release =
                CreateRelease(
                    demoTape.DemoTapeId
                );

            TrackEvaluationEnvironment environment =
                CreateEnvironment();

            SceneReleaseLegitimacyEvaluation result =
                new SceneReleaseLegitimacyEvaluator()
                    .Evaluate(
                        release,
                        demoTape,
                        environment,
                        CreateActivations(
                            release,
                            demoTape
                        )
                    );

            Assert.That(
                result.SourceReleaseId,
                Is.EqualTo(release.ReleaseId)
            );

            Assert.That(
                result.SourceDemoTapeId,
                Is.EqualTo(demoTape.DemoTapeId)
            );

            Assert.That(
                result.SourceOwnerEntityId,
                Is.EqualTo("OWNER")
            );

            Assert.That(
                result.SettledTurn,
                Is.EqualTo(7)
            );

            Assert.That(
                result.TrackEvaluations.Count,
                Is.EqualTo(3)
            );

            /*
             * TRACK A
             *
             * Profane2 -> Sacred1
             *
             * Current Profane2 affinity = +0.5
             *
             * Surface = 2 * .5 = 1
             *
             * E = (2 + 1) / 2 = 1.5
             *
             * Society Sacred3:
             * B = min(1, 3) = 1
             *
             * Et = 2 + 1 = 3
             *
             * A = 1
             * F = 1 / 2 = .5
             *
             * T = 3 * .5 = 1.5
             *
             * R_signed
             * = 1 * (1.5 / 3)
             * = .5
             *
             * tau = 1.5 / 6 = .25
             *
             * G = .5 * .25 = .125
             *
             * Canon Profane2 affinity = +1
             *
             * Canon R = 2 * .5 = 1
             *
             * Tmax = 3
             * tauMax = .5
             *
             * IP = .5
             */

            TrackLegitimacyEvaluation trackA =
                result.TrackEvaluations[0];

            Assert.That(
                trackA.SourceTrackId,
                Is.EqualTo("TRACK_PROFANE_PAIR")
            );

            Assert.That(
                trackA.Surface,
                Is.EqualTo(1f).Within(Tolerance)
            );

            Assert.That(
                trackA.Extremity,
                Is.EqualTo(1.5f).Within(Tolerance)
            );

            Assert.That(
                trackA.Es,
                Is.EqualTo(2)
            );

            Assert.That(
                trackA.T,
                Is.EqualTo(1.5f).Within(Tolerance)
            );

            Assert.That(
                trackA.SignedResonance,
                Is.EqualTo(0.5f).Within(Tolerance)
            );

            Assert.That(
                trackA.SignedGravity,
                Is.EqualTo(0.125f).Within(Tolerance)
            );

            Assert.That(
                trackA.SignedInfluencePotential,
                Is.EqualTo(0.5f).Within(Tolerance)
            );

            Assert.That(
                trackA.HasTrve,
                Is.True
            );

            /*
             * TRACK B
             *
             * solitary Profane3
             *
             * Current affinity = +.25
             *
             * Surface = 3 * .25 = .75
             * E = 3
             * Es = 3
             *
             * solitary -> T = 0
             *
             * R = .75
             * G = 0
             *
             * Canon recognizes Profane3 fully,
             * but solitary material has no Tmax.
             *
             * IP = 0
             */

            TrackLegitimacyEvaluation trackB =
                result.TrackEvaluations[1];

            Assert.That(
                trackB.SourceTrackId,
                Is.EqualTo("TRACK_SOLITARY")
            );

            Assert.That(
                trackB.Surface,
                Is.EqualTo(0.75f).Within(Tolerance)
            );

            Assert.That(
                trackB.Extremity,
                Is.EqualTo(3f).Within(Tolerance)
            );

            Assert.That(
                trackB.Es,
                Is.EqualTo(3)
            );

            Assert.That(
                trackB.T,
                Is.EqualTo(0f).Within(Tolerance)
            );

            Assert.That(
                trackB.ResonanceMagnitude,
                Is.EqualTo(0.75f).Within(Tolerance)
            );

            Assert.That(
                trackB.GravityMagnitude,
                Is.EqualTo(0f).Within(Tolerance)
            );

            Assert.That(
                trackB.InfluencePotentialMagnitude,
                Is.EqualTo(0f).Within(Tolerance)
            );

            Assert.That(
                trackB.HasTrve,
                Is.False
            );

            /*
             * TRACK C
             *
             * Raw3 -> Honed2
             *
             * Current Raw3 affinity = -.5
             *
             * Surface = -1.5
             *
             * E = (3 + 2) / 2 = 2.5
             *
             * Society Honed2:
             * B = 2
             *
             * Et = 3 + 2 = 5
             *
             * A = 3
             * F = 1
             *
             * T = 5
             *
             * R_signed
             * = -1.5 * (2.5 / 3)
             * = -1.25
             *
             * tau = 5 / 6
             *
             * G_signed
             * = -1.25 * (5 / 6)
             * = -1.041666...
             *
             * Canon contains Profane2.
             * Raw is adjacent to Profane.
             *
             * Canon Raw affinity = +.5
             *
             * Canon Surface = 1.5
             * Canon R = +1.25
             *
             * Tmax = 5
             *
             * IP_signed
             * = 1.25 * (5 / 6)
             * = +1.041666...
             */

            TrackLegitimacyEvaluation trackC =
                result.TrackEvaluations[2];

            Assert.That(
                trackC.SourceTrackId,
                Is.EqualTo("TRACK_RAW_PAIR")
            );

            Assert.That(
                trackC.Surface,
                Is.EqualTo(-1.5f).Within(Tolerance)
            );

            Assert.That(
                trackC.Extremity,
                Is.EqualTo(2.5f).Within(Tolerance)
            );

            Assert.That(
                trackC.Es,
                Is.EqualTo(3)
            );

            Assert.That(
                trackC.T,
                Is.EqualTo(5f).Within(Tolerance)
            );

            Assert.That(
                trackC.SignedResonance,
                Is.EqualTo(-1.25f).Within(Tolerance)
            );

            Assert.That(
                trackC.SignedGravity,
                Is.EqualTo(-1.0416667f)
                    .Within(Tolerance)
            );

            Assert.That(
                trackC.SignedInfluencePotential,
                Is.EqualTo(1.0416667f)
                    .Within(Tolerance)
            );

            Assert.That(
                trackC.HasTrve,
                Is.True
            );

            // Current scene and underlying Canon
            // point in opposite directions here.

            Assert.That(
                trackC.SignedGravity,
                Is.LessThan(0f)
            );

            Assert.That(
                trackC.SignedInfluencePotential,
                Is.GreaterThan(0f)
            );

            /*
             * RELEASE SUMMARY
             *
             * Surface:
             * (1 + .75 - 1.5) / 3
             * = .083333...
             *
             * E:
             * (1.5 + 3 + 2.5) / 3
             * = 2.333333...
             *
             * Es:
             * max(2, 3, 3)
             * = 3
             *
             * T:
             * (1.5 + 0 + 5) / 3
             * = 2.166666...
             *
             * R magnitude:
             * (.5 + .75 + 1.25) / 3
             * = .833333...
             *
             * G magnitude:
             * (.125 + 0 + 1.0416667) / 3
             * = .388888...
             *
             * IP magnitude:
             * (.5 + 0 + 1.0416667) / 3
             * = .513888...
             */

            Assert.That(
                result.Surface,
                Is.EqualTo(0.08333333f)
                    .Within(Tolerance)
            );

            Assert.That(
                result.Extremity,
                Is.EqualTo(2.3333333f)
                    .Within(Tolerance)
            );

            Assert.That(
                result.SensationalExtremity,
                Is.EqualTo(3)
            );

            Assert.That(
                result.Trve,
                Is.EqualTo(2.1666667f)
                    .Within(Tolerance)
            );

            Assert.That(
                result.Resonance,
                Is.EqualTo(0.8333333f)
                    .Within(Tolerance)
            );

            Assert.That(
                result.Gravity,
                Is.EqualTo(0.3888889f)
                    .Within(Tolerance)
            );

            Assert.That(
                result.InfluencePotential,
                Is.EqualTo(0.5138889f)
                    .Within(Tolerance)
            );

            Assert.That(
                result.HasTrve,
                Is.True
            );
        }

        [Test]
        public void IdenticalFrozenInputs_ProduceIdenticalResultsAndProvenance()
        {
            DemoTape demoTape =
                CreateDemoTape();

            SceneRelease release =
                CreateRelease(
                    demoTape.DemoTapeId
                );

            TrackEvaluationEnvironment environment =
                CreateEnvironment();

            TrackActivationEvaluationSnapshot[]
                activations =
                    CreateActivations(
                        release,
                        demoTape
                    );

            SceneReleaseLegitimacyEvaluator evaluator =
                new SceneReleaseLegitimacyEvaluator();

            SceneReleaseLegitimacyEvaluation first =
                evaluator.Evaluate(
                    release,
                    demoTape,
                    environment,
                    activations
                );

            SceneReleaseLegitimacyEvaluation second =
                evaluator.Evaluate(
                    release,
                    demoTape,
                    environment,
                    activations
                );

            Assert.That(
                second.SourceReleaseId,
                Is.EqualTo(first.SourceReleaseId)
            );

            Assert.That(
                second.SourceDemoTapeId,
                Is.EqualTo(first.SourceDemoTapeId)
            );

            Assert.That(
                second.SourceOwnerEntityId,
                Is.EqualTo(first.SourceOwnerEntityId)
            );

            Assert.That(
                second.SettledTurn,
                Is.EqualTo(first.SettledTurn)
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
                second.SensationalExtremity,
                Is.EqualTo(first.SensationalExtremity)
            );

            Assert.That(
                second.Trve,
                Is.EqualTo(first.Trve)
            );

            Assert.That(
                second.Resonance,
                Is.EqualTo(first.Resonance)
            );

            Assert.That(
                second.Gravity,
                Is.EqualTo(first.Gravity)
            );

            Assert.That(
                second.InfluencePotential,
                Is.EqualTo(first.InfluencePotential)
            );

            Assert.That(
                second.HasTrve,
                Is.EqualTo(first.HasTrve)
            );

            Assert.That(
                second.TrackEvaluations.Count,
                Is.EqualTo(
                    first.TrackEvaluations.Count
                )
            );

            for (int index = 0;
                 index < first.TrackEvaluations.Count;
                 index++)
            {
                TrackLegitimacyEvaluation firstTrack =
                    first.TrackEvaluations[index];

                TrackLegitimacyEvaluation secondTrack =
                    second.TrackEvaluations[index];

                Assert.That(
                    secondTrack.SourceTrackId,
                    Is.EqualTo(
                        firstTrack.SourceTrackId
                    )
                );

                Assert.That(
                    secondTrack.Surface,
                    Is.EqualTo(firstTrack.Surface)
                );

                Assert.That(
                    secondTrack.Extremity,
                    Is.EqualTo(firstTrack.Extremity)
                );

                Assert.That(
                    secondTrack.Es,
                    Is.EqualTo(firstTrack.Es)
                );

                Assert.That(
                    secondTrack.T,
                    Is.EqualTo(firstTrack.T)
                );

                Assert.That(
                    secondTrack.SignedResonance,
                    Is.EqualTo(
                        firstTrack.SignedResonance
                    )
                );

                Assert.That(
                    secondTrack.SignedGravity,
                    Is.EqualTo(
                        firstTrack.SignedGravity
                    )
                );

                Assert.That(
                    secondTrack.SignedInfluencePotential,
                    Is.EqualTo(
                        firstTrack
                            .SignedInfluencePotential
                    )
                );
            }

            // Explicit deep provenance check.

            Assert.That(
                first.TrackEvaluations[0]
                    .Trve
                    .IdeaEvaluations[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_PROFANE_PAIR")
            );

            Assert.That(
                first.TrackEvaluations[0]
                    .Trve
                    .IdeaEvaluations[0]
                    .LegitimateActiveDegree,
                Is.EqualTo(1)
            );

            Assert.That(
                first.TrackEvaluations[2]
                    .Trve
                    .IdeaEvaluations[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_RAW_PAIR")
            );

            Assert.That(
                first.TrackEvaluations[2]
                    .Trve
                    .IdeaEvaluations[0]
                    .LegitimateActiveDegree,
                Is.EqualTo(3)
            );
        }

        [Test]
        public void CompleteEvaluation_DoesNotMutatePersistedDemoTapeSemantics()
        {
            DemoTape demoTape =
                CreateDemoTape();

            SceneRelease release =
                CreateRelease(
                    demoTape.DemoTapeId
                );

            DemoTapeIdeaSnapshot profanePair =
                demoTape.TrackSnapshots[0]
                    .IdeaSnapshots[0];

            DemoTapeIdeaSnapshot solitary =
                demoTape.TrackSnapshots[1]
                    .IdeaSnapshots[0];

            DemoTapeIdeaSnapshot rawPair =
                demoTape.TrackSnapshots[2]
                    .IdeaSnapshots[0];

            new SceneReleaseLegitimacyEvaluator()
                .Evaluate(
                    release,
                    demoTape,
                    CreateEnvironment(),
                    CreateActivations(
                        release,
                        demoTape
                    )
                );

            // Track and Idea identity remain unchanged.

            Assert.That(
                demoTape.TrackSnapshots[0]
                    .SourceTrackId,
                Is.EqualTo("TRACK_PROFANE_PAIR")
            );

            Assert.That(
                profanePair.SourceIdeaId,
                Is.EqualTo("IDEA_PROFANE_PAIR")
            );

            Assert.That(
                solitary.SourceIdeaId,
                Is.EqualTo("IDEA_SOLITARY")
            );

            Assert.That(
                rawPair.SourceIdeaId,
                Is.EqualTo("IDEA_RAW_PAIR")
            );

            // Profane2 -> Sacred1 remains exactly recorded.

            Assert.That(
                profanePair.TagOccurrences[0].Axis,
                Is.EqualTo(TagAxis.Symbolic)
            );

            Assert.That(
                profanePair.TagOccurrences[0].Pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                profanePair.TagOccurrences[0].Degree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                profanePair.TagOccurrences[0].Role,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole
                        .PairDominant
                )
            );

            Assert.That(
                profanePair.TagOccurrences[1].Pole,
                Is.EqualTo(TagPole.Positive)
            );

            Assert.That(
                profanePair.TagOccurrences[1].Degree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                profanePair.TagOccurrences[1].Role,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole
                        .PairSubmissive
                )
            );

            // Solitary Profane3 remains solitary.

            Assert.That(
                solitary.PayloadType,
                Is.EqualTo(
                    IdeaPayloadType.SingleTag
                )
            );

            Assert.That(
                solitary.TagOccurrences[0].Degree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                solitary.TagOccurrences[0].Role,
                Is.EqualTo(
                    DemoTapeTagOccurrenceRole
                        .Solitary
                )
            );

            // Raw3 -> Honed2 also remains unchanged.

            Assert.That(
                rawPair.TagOccurrences[0].Axis,
                Is.EqualTo(TagAxis.Expressive)
            );

            Assert.That(
                rawPair.TagOccurrences[0].Degree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                rawPair.TagOccurrences[1].Degree,
                Is.EqualTo(TagDegree.Dominant)
            );
        }

        private static DemoTape CreateDemoTape()
        {
            return new DemoTape(
                "DEMO_ACCEPTANCE",
                "Camp 1 Acceptance Demo",
                "SET_ACCEPTANCE",
                "Acceptance Set",
                1,
                1,
                1f,
                new[]
                {
                    CreateProfanePairTrack(),
                    CreateSolitaryTrack(),
                    CreateRawPairTrack()
                }
            );
        }

        private static SceneRelease CreateRelease(
            string demoTapeId)
        {
            return new SceneRelease(
                "Camp 1 Acceptance Release",
                demoTapeId,
                "OWNER",
                "KVLT",
                1,
                1f
            );
        }

        private static
            TrackEvaluationEnvironment
            CreateEnvironment()
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Dominant,
                CanonProvenanceKind.ScenarioSeed,
                "CANON_PROFANE_2"
            );

            NormativeCentre canonCentre =
                new CanonicalNormativeCentreDeriver()
                    .Derive(canon);

            /*
             * Current Centre represents the already-settled
             * scene treatment for this turn.
             *
             * It intentionally differs from Canon:
             *
             * Profane2 = +.5
             * Profane3 = +.25
             * Raw3     = -.5
             *
             * Canon-only treatment will instead give:
             *
             * Profane2 = +1
             * Profane3 = +1
             * Raw3     = +.5 through adjacency to Profane
             */

            NormativeCentre currentCentre =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant,
                            0.5f
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            0.25f
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Expressive,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            -0.5f
                        )
                    }
                );

            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            // Sacred backs Profane -> Sacred
            // and makes Profane sensational.

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            // Honed backs Raw -> Honed
            // and makes Raw sensational.

            society.SetNormativeDegree(
                TagAxis.Expressive,
                TagPole.Positive,
                TagDegree.Dominant
            );

            return new TrackEvaluationEnvironment(
                7,
                currentCentre,
                canonCentre,
                society
            );
        }

        private static
            TrackActivationEvaluationSnapshot[]
            CreateActivations(
                SceneRelease release,
                DemoTape demoTape)
        {
            return new[]
            {
                new TrackActivationEvaluationSnapshot(
                    release.ReleaseId,
                    demoTape.DemoTapeId,
                    "TRACK_PROFANE_PAIR",
                    new[]
                    {
                        new IdeaActivationEvaluationSnapshot(
                            "IDEA_PROFANE_PAIR",
                            1
                        )
                    }
                ),

                new TrackActivationEvaluationSnapshot(
                    release.ReleaseId,
                    demoTape.DemoTapeId,
                    "TRACK_RAW_PAIR",
                    new[]
                    {
                        new IdeaActivationEvaluationSnapshot(
                            "IDEA_RAW_PAIR",
                            3
                        )
                    }
                )

                // TRACK_SOLITARY deliberately omitted.
                // Missing activation = dormant A=0.
            };
        }

        private static
            DemoTapeTrackSnapshot
            CreateProfanePairTrack()
        {
            return new DemoTapeTrackSnapshot(
                "TRACK_PROFANE_PAIR",
                "Profane Pair",
                0.75f,
                0.75f,
                new[]
                {
                    new DemoTapeIdeaSnapshot(
                        "IDEA_PROFANE_PAIR",
                        0,
                        "ASPECT_PROFANE",
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

        private static
            DemoTapeTrackSnapshot
            CreateSolitaryTrack()
        {
            return new DemoTapeTrackSnapshot(
                "TRACK_SOLITARY",
                "Solitary Profane",
                0.75f,
                0.75f,
                new[]
                {
                    new DemoTapeIdeaSnapshot(
                        "IDEA_SOLITARY",
                        0,
                        "ASPECT_SOLITARY",
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
                    )
                }
            );
        }

        private static
            DemoTapeTrackSnapshot
            CreateRawPairTrack()
        {
            return new DemoTapeTrackSnapshot(
                "TRACK_RAW_PAIR",
                "Raw Pair",
                0.75f,
                0.75f,
                new[]
                {
                    new DemoTapeIdeaSnapshot(
                        "IDEA_RAW_PAIR",
                        0,
                        "ASPECT_RAW",
                        IdeaPayloadType.TagPair,
                        "AUTHOR",
                        TagContainerType.Transient,
                        0.75f,
                        new[]
                        {
                            new DemoTapeTagOccurrenceSnapshot(
                                TagAxis.Expressive,
                                TagPole.Negative,
                                TagDegree.Transgressive,
                                DemoTapeTagOccurrenceRole
                                    .PairDominant
                            ),

                            new DemoTapeTagOccurrenceSnapshot(
                                TagAxis.Expressive,
                                TagPole.Positive,
                                TagDegree.Dominant,
                                DemoTapeTagOccurrenceRole
                                    .PairSubmissive
                            )
                        }
                    )
                }
            );
        }
    }
}