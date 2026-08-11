using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class
        SceneReleaseActivationEvaluationProjectorTests
    {
        private readonly
            SceneReleaseActivationEvaluationProjector
                projector =
                    new();

        private readonly
            SceneReleaseActivationStateService
                activationService =
                    new();

        [Test]
        public void NoActivation_ProjectsNoIdeaActivation()
        {
            DemoTape demoTape =
                Demo(
                    PairTrack(
                        "TRACK",
                        "IDEA",
                        0,
                        TagDegree.Dominant
                    )
                );

            SceneRelease release =
                Release(
                    demoTape
                );

            var projected =
                projector.Project(
                    release,
                    demoTape
                );

            Assert.That(
                projected.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                projected[0].SourceTrackId,
                Is.EqualTo("TRACK")
            );

            Assert.That(
                projected[0].IdeaActivations,
                Is.Empty
            );

            Assert.That(
                projected[0]
                    .GetLegitimateActiveDegree(
                        "IDEA"
                    ),
                Is.EqualTo(0)
            );
        }

        [Test]
        public void LegitimateActivation_ProjectsCurrentDegree()
        {
            DemoTape demoTape =
                Demo(
                    PairTrack(
                        "TRACK",
                        "IDEA",
                        0,
                        TagDegree.Dominant
                    )
                );

            SceneRelease release =
                Release(
                    demoTape
                );

            Assert.That(
                activationService.ApplyCovered(
                    release,
                    Covered(
                        Candidate(
                            release,
                            "TRACK",
                            "IDEA",
                            0,
                            TagDegree.Weak,
                            TagDegree.Dominant
                        ),
                        Accepted(
                            "AT",
                            TagDegree.Transgressive,
                            0
                        )
                    ),
                    8
                ),
                Is.True
            );

            var projected =
                projector.Project(
                    release,
                    demoTape
                );

            Assert.That(
                projected[0]
                    .GetLegitimateActiveDegree(
                        "IDEA"
                    ),
                Is.EqualTo(1)
            );

            Assert.That(
                projected[0].IdeaActivations.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void PendingOnly_ProjectsZeroAndEvaluatorSeesNoTrve()
        {
            DemoTape demoTape =
                Demo(
                    PairTrack(
                        "TRACK",
                        "IDEA",
                        0,
                        TagDegree.Dominant
                    )
                );

            SceneRelease release =
                Release(
                    demoTape
                );

            PendingActivationCandidate pending =
                new PendingActivationCandidate(
                    Candidate(
                        release,
                        "TRACK",
                        "IDEA",
                        0,
                        TagDegree.Dominant,
                        TagDegree.Dominant
                    ),
                    "CRISIS",
                    8
                );

            Assert.That(
                activationService.StorePending(
                    release,
                    pending,
                    out _
                ),
                Is.True
            );

            var projected =
                projector.Project(
                    release,
                    demoTape
                );

            Assert.That(
                projected[0]
                    .GetLegitimateActiveDegree(
                        "IDEA"
                    ),
                Is.EqualTo(0)
            );

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    new SceneReleaseLegitimacyEvaluator()
                        .Evaluate(
                            release,
                            demoTape,
                            Environment()
                        );

            Assert.That(
                evaluation.HasTrve,
                Is.False
            );

            Assert.That(
                evaluation.Trve,
                Is.EqualTo(0f)
            );

            Assert.That(
                evaluation.Gravity,
                Is.EqualTo(0f)
            );

            Assert.That(
                release.PendingActivations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );
        }

        [Test]
        public void RedeemedPending_ProjectsRedeemedDegreeAndEvaluatorSeesTrve()
        {
            DemoTape demoTape =
                Demo(
                    PairTrack(
                        "TRACK",
                        "IDEA",
                        0,
                        TagDegree.Dominant
                    )
                );

            SceneRelease release =
                Release(
                    demoTape
                );

            Assert.That(
                activationService.StorePending(
                    release,
                    new PendingActivationCandidate(
                        Candidate(
                            release,
                            "TRACK",
                            "IDEA",
                            0,
                            TagDegree.Dominant,
                            TagDegree.Dominant
                        ),
                        "CRISIS",
                        8
                    ),
                    out SceneReleasePendingActivation
                        pending
                ),
                Is.True
            );

            AcceptedTransgressionState
                acceptedState =
                    new AcceptedTransgressionState(
                        "KVLT"
                    );

            AcceptedTransgressionRecord precedent =
                Accepted(
                    "AT_REDEMPTION",
                    TagDegree.Transgressive,
                    12
                );

            Assert.That(
                acceptedState.TryAccept(
                    precedent
                ),
                Is.True
            );

            Assert.That(
                activationService
                    .RedeemEligiblePending(
                        release,
                        acceptedState,
                        12
                    ),
                Is.EqualTo(1)
            );

            Assert.That(
                pending.IsRedeemed,
                Is.True
            );

            var projected =
                projector.Project(
                    release,
                    demoTape
                );

            Assert.That(
                projected[0]
                    .GetLegitimateActiveDegree(
                        "IDEA"
                    ),
                Is.EqualTo(2)
            );

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    new SceneReleaseLegitimacyEvaluator()
                        .Evaluate(
                            release,
                            demoTape,
                            Environment()
                        );

            Assert.That(
                evaluation.HasTrve,
                Is.True
            );

            Assert.That(
                evaluation.Trve,
                Is.GreaterThan(0f)
            );

            Assert.That(
                evaluation.Gravity,
                Is.GreaterThan(0f)
            );
        }

        [Test]
        public void SeveralTracksAndPairs_ProjectToExactRecordedIdeas()
        {
            DemoTape demoTape =
                Demo(
                    PairTrack(
                        "TRACK_A",
                        "IDEA_A",
                        0,
                        TagDegree.Dominant
                    ),

                    PairTrack(
                        "TRACK_B",
                        "IDEA_B",
                        0,
                        TagDegree.Transgressive
                    )
                );

            SceneRelease release =
                Release(
                    demoTape
                );

            AcceptedTransgressionRecord precedent =
                Accepted(
                    "AT",
                    TagDegree.Transgressive,
                    0
                );

            Assert.That(
                activationService.ApplyCovered(
                    release,
                    Covered(
                        Candidate(
                            release,
                            "TRACK_A",
                            "IDEA_A",
                            0,
                            TagDegree.Weak,
                            TagDegree.Dominant
                        ),
                        precedent
                    ),
                    8
                ),
                Is.True
            );

            Assert.That(
                activationService.ApplyCovered(
                    release,
                    Covered(
                        Candidate(
                            release,
                            "TRACK_B",
                            "IDEA_B",
                            0,
                            TagDegree.Dominant,
                            TagDegree.Transgressive
                        ),
                        precedent
                    ),
                    8
                ),
                Is.True
            );

            var projected =
                projector.Project(
                    release,
                    demoTape
                );

            Assert.That(
                projected.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                projected[0].SourceTrackId,
                Is.EqualTo("TRACK_A")
            );

            Assert.That(
                projected[0]
                    .GetLegitimateActiveDegree(
                        "IDEA_A"
                    ),
                Is.EqualTo(1)
            );

            Assert.That(
                projected[0]
                    .GetLegitimateActiveDegree(
                        "IDEA_B"
                    ),
                Is.EqualTo(0)
            );

            Assert.That(
                projected[1].SourceTrackId,
                Is.EqualTo("TRACK_B")
            );

            Assert.That(
                projected[1]
                    .GetLegitimateActiveDegree(
                        "IDEA_B"
                    ),
                Is.EqualTo(2)
            );

            Assert.That(
                projected[1]
                    .GetLegitimateActiveDegree(
                        "IDEA_A"
                    ),
                Is.EqualTo(0)
            );
        }

        [Test]
        public void RecordedDominantMismatch_IsRejected()
        {
            DemoTape demoTape =
                Demo(
                    PairTrack(
                        "TRACK",
                        "IDEA",
                        0,

                        // Immutable recording says D = 2.
                        TagDegree.Dominant
                    )
                );

            SceneRelease release =
                Release(
                    demoTape
                );

            // Deliberately corrupt/stale runtime state:
            // candidate says the exact same pair was
            // recorded with D = 3.
            ActivationLegitimacyCandidate candidate =
                Candidate(
                    release,
                    "TRACK",
                    "IDEA",
                    0,
                    TagDegree.Weak,
                    TagDegree.Transgressive
                );

            Assert.That(
                activationService.ApplyCovered(
                    release,
                    Covered(
                        candidate,
                        Accepted(
                            "AT",
                            TagDegree.Transgressive,
                            0
                        )
                    ),
                    8
                ),
                Is.True
            );

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    projector.Project(
                        release,
                        demoTape
                    )
            );
        }

        private static SceneRelease Release(
            DemoTape demoTape)
        {
            return new SceneRelease(
                "Test Release",
                demoTape.DemoTapeId,
                "OWNER",
                "KVLT",
                4,
                1f
            );
        }

        private static DemoTape Demo(
            params DemoTapeTrackSnapshot[] tracks)
        {
            return new DemoTape(
                "DEMO",
                "Test Demo",
                "SET",
                "Test Set",
                1,
                1,
                1f,
                tracks
            );
        }

        private static DemoTapeTrackSnapshot
            PairTrack(
                string trackId,
                string ideaId,
                int ideaIndex,
                TagDegree dominantDegree)
        {
            DemoTapeIdeaSnapshot idea =
                new DemoTapeIdeaSnapshot(
                    ideaId,
                    ideaIndex,
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

            return new DemoTapeTrackSnapshot(
                trackId,
                trackId,
                1f,
                1f,
                new[]
                {
                    idea
                }
            );
        }

        private static
            ActivationLegitimacyCandidate
            Candidate(
                SceneRelease release,
                string trackId,
                string ideaId,
                int ideaIndex,
                TagDegree appliedDegree,
                TagDegree recordedDominantDegree)
        {
            return new ActivationLegitimacyCandidate(
                ActivationAttemptRoute.Hail,
                "HAPPENING",
                "INTENT",
                "PLAYER_A",
                release.ReleaseId,
                release.SourceDemoTapeId,
                trackId,
                ideaId,
                ideaIndex,
                "CHURCH_ARSON",
                TagAxis.Symbolic,
                TagPole.Negative,

                // Praxis severity remains independent
                // from the recording's activation cap.
                TagDegree.Transgressive,

                appliedDegree,
                recordedDominantDegree,
                "BEHAVIOR",
                "HAIL",
                "ASPECT_SATAN"
            );
        }

        private static
            ActivationLegitimacyAssessment
            Covered(
                ActivationLegitimacyCandidate candidate,
                AcceptedTransgressionRecord precedent)
        {
            return new ActivationLegitimacyAssessment(
                candidate,
                ActivationLegitimacyDisposition
                    .Covered,
                precedent
            );
        }

        private static
            AcceptedTransgressionRecord
            Accepted(
                string id,
                TagDegree degree,
                int turn)
        {
            return new AcceptedTransgressionRecord(
                id,
                "KVLT",
                "CHURCH_ARSON",
                TagAxis.Symbolic,
                TagPole.Negative,
                degree,
                turn,
                AcceptedTransgressionSourceKind
                    .ScenarioSeed,
                "TEST_SCENARIO"
            );
        }

        private static TrackEvaluationEnvironment
            Environment()
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
                12,
                centre,
                centre,
                society
            );
        }
    }
}