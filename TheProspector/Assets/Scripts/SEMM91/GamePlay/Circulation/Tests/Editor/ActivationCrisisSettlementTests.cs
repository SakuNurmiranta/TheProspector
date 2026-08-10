using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation.Tests
{
    public class ActivationCrisisSettlementTests
    {
        private readonly
            ActivationCrisisSettlementService service =
                new();

        [Test]
        public void KvltVictory_RaisesAcceptedTransgressionAndLegitimizesCandidate()
        {
            ActivationLegitimacyCrisisGroup group =
                Group(
                    Uncovered(
                        "RELEASE_A",
                        "TRACK_A",
                        "IDEA_A"
                    )
                );

            AllegianceCrisis crisis =
                Resolve(
                    group.Question,
                    AllegianceChoice.Kvlt
                );

            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            ActivationCrisisSettlement settlement =
                service.Settle(
                    group,
                    crisis,
                    state
                );

            Assert.That(
                settlement.RaisedAcceptedPrecedent,
                Is.True
            );

            Assert.That(
                settlement.AcceptedPrecedent,
                Is.Not.Null
            );

            Assert.That(
                settlement.AcceptedPrecedent
                    .Key
                    .BehaviorTypeId,
                Is.EqualTo("CHURCH_ARSON")
            );

            Assert.That(
                settlement.AcceptedPrecedent
                    .AcceptedDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                settlement.AcceptedPrecedent
                    .SourceKind,
                Is.EqualTo(
                    AcceptedTransgressionSourceKind
                        .AllegianceCrisis
                )
            );

            Assert.That(
                settlement.AcceptedPrecedent.SourceId,
                Is.EqualTo(
                    group.Question.QuestionId
                )
            );

            Assert.That(
                settlement.LegitimizedCandidates.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                settlement.PendingCandidates,
                Is.Empty
            );

            Assert.That(
                state.IsCovered(
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive
                ),
                Is.True
            );
        }

        [Test]
        public void OneKvltVictoryCreatesOnePrecedentForSeveralAnsweringPairs()
        {
            ActivationLegitimacyCrisisGroup group =
                Group(
                    Uncovered(
                        "RELEASE_A",
                        "TRACK_A",
                        "IDEA_A"
                    ),
                    Uncovered(
                        "RELEASE_A",
                        "TRACK_B",
                        "IDEA_B"
                    ),
                    Uncovered(
                        "RELEASE_B",
                        "TRACK_C",
                        "IDEA_C"
                    )
                );

            AllegianceCrisis crisis =
                Resolve(
                    group.Question,
                    AllegianceChoice.Kvlt
                );

            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            ActivationCrisisSettlement settlement =
                service.Settle(
                    group,
                    crisis,
                    state
                );

            Assert.That(
                state.History.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                settlement.LegitimizedCandidates.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                settlement.LegitimizedCandidates[0]
                    .Candidate.SceneReleaseId,
                Is.EqualTo("RELEASE_A")
            );

            Assert.That(
                settlement.LegitimizedCandidates[1]
                    .Candidate.SourceTrackId,
                Is.EqualTo("TRACK_B")
            );

            Assert.That(
                settlement.LegitimizedCandidates[2]
                    .Candidate.SceneReleaseId,
                Is.EqualTo("RELEASE_B")
            );
        }

        [Test]
        public void SocietyVictory_CreatesPendingWithoutAcceptedPrecedent()
        {
            ActivationLegitimacyCrisisGroup group =
                Group(
                    Uncovered(
                        "RELEASE_A",
                        "TRACK",
                        "IDEA"
                    )
                );

            AllegianceCrisis crisis =
                Resolve(
                    group.Question,
                    AllegianceChoice.Society
                );

            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            ActivationCrisisSettlement settlement =
                service.Settle(
                    group,
                    crisis,
                    state
                );

            Assert.That(
                settlement.AcceptedPrecedent,
                Is.Null
            );

            Assert.That(
                settlement.RaisedAcceptedPrecedent,
                Is.False
            );

            Assert.That(
                settlement.LegitimizedCandidates,
                Is.Empty
            );

            Assert.That(
                settlement.PendingCandidates.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                state.History,
                Is.Empty
            );
        }

        [Test]
        public void PendingCandidate_PreservesExactReleasePairAndDegreeProvenance()
        {
            ActivationLegitimacyAssessment assessment =
                Uncovered(
                    "RELEASE_B",
                    "TRACK_7",
                    "IDEA_3"
                );

            ActivationLegitimacyCrisisGroup group =
                Group(assessment);

            AllegianceCrisis crisis =
                Resolve(
                    group.Question,
                    AllegianceChoice.Society
                );

            ActivationCrisisSettlement settlement =
                service.Settle(
                    group,
                    crisis,
                    new AcceptedTransgressionState(
                        "KVLT"
                    )
                );

            PendingActivationCandidate pending =
                settlement.PendingCandidates[0];

            Assert.That(
                pending.Candidate,
                Is.SameAs(
                    assessment.Candidate
                )
            );

            Assert.That(
                pending.Candidate.SceneReleaseId,
                Is.EqualTo("RELEASE_B")
            );

            Assert.That(
                pending.Candidate.SourceTrackId,
                Is.EqualTo("TRACK_7")
            );

            Assert.That(
                pending.Candidate.SourceIdeaId,
                Is.EqualTo("IDEA_3")
            );

            Assert.That(
                pending.Candidate.PraxisDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                pending.Candidate
                    .AppliedActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                pending.AllegianceCrisisQuestionId,
                Is.EqualTo(
                    group.Question.QuestionId
                )
            );
        }

        [Test]
        public void LegitimizedCandidate_PreservesExactPrecedentAndCrisisProvenance()
        {
            ActivationLegitimacyAssessment assessment =
                Uncovered(
                    "RELEASE",
                    "TRACK",
                    "IDEA"
                );

            ActivationLegitimacyCrisisGroup group =
                Group(assessment);

            AllegianceCrisis crisis =
                Resolve(
                    group.Question,
                    AllegianceChoice.Kvlt
                );

            ActivationCrisisSettlement settlement =
                service.Settle(
                    group,
                    crisis,
                    new AcceptedTransgressionState(
                        "KVLT"
                    )
                );

            LegitimizedActivationCandidate legitimate =
                settlement.LegitimizedCandidates[0];

            Assert.That(
                legitimate.Candidate,
                Is.SameAs(
                    assessment.Candidate
                )
            );

            Assert.That(
                legitimate.AllegianceCrisisQuestionId,
                Is.EqualTo(
                    group.Question.QuestionId
                )
            );

            Assert.That(
                legitimate.CoveringPrecedent,
                Is.SameAs(
                    settlement.AcceptedPrecedent
                )
            );

            Assert.That(
                legitimate.LegitimatedTurn,
                Is.EqualTo(
                    crisis.Resolution.ResolvedTurn
                )
            );
        }

        [Test]
        public void KvltVictory_ReusesAlreadyCoveringPrecedentWithoutDuplicateRatchet()
        {
            ActivationLegitimacyCrisisGroup group =
                Group(
                    Uncovered(
                        "RELEASE",
                        "TRACK",
                        "IDEA"
                    )
                );

            AllegianceCrisis crisis =
                Resolve(
                    group.Question,
                    AllegianceChoice.Kvlt
                );

            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            AcceptedTransgressionRecord existing =
                new AcceptedTransgressionRecord(
                    "AT_EXISTING",
                    "KVLT",
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    7,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "TEST_SCENARIO"
                );

            Assert.That(
                state.TryAccept(existing),
                Is.True
            );

            ActivationCrisisSettlement settlement =
                service.Settle(
                    group,
                    crisis,
                    state
                );

            Assert.That(
                settlement.RaisedAcceptedPrecedent,
                Is.False
            );

            Assert.That(
                settlement.AcceptedPrecedent,
                Is.SameAs(existing)
            );

            Assert.That(
                state.History.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                settlement.LegitimizedCandidates.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void UnresolvedCrisis_CannotProduceInstitutionalSettlement()
        {
            ActivationLegitimacyCrisisGroup group =
                Group(
                    Uncovered(
                        "RELEASE",
                        "TRACK",
                        "IDEA"
                    )
                );

            AllegianceCrisis unresolved =
                new AllegianceCrisis(
                    group.Question,
                    new[]
                    {
                        "KEEPER",
                        "PLAYER_B"
                    },
                    "KEEPER"
                );

            Assert.Throws<
                System.InvalidOperationException>(
                () =>
                    service.Settle(
                        group,
                        unresolved,
                        new AcceptedTransgressionState(
                            "KVLT"
                        )
                    )
            );
        }

        [Test]
        public void SettlementRejectsCrisisForDifferentQuestion()
        {
            ActivationLegitimacyCrisisGroup group =
                Group(
                    Uncovered(
                        "RELEASE",
                        "TRACK",
                        "IDEA"
                    )
                );

            AllegianceCrisisQuestion otherQuestion =
                new AllegianceCrisisQuestion(
                    "OTHER_HAPPENING",
                    "OTHER_INTENT",
                    "TRANSGRESSOR",
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    8,
                    "OTHER_BEHAVIOR"
                );

            AllegianceCrisis other =
                Resolve(
                    otherQuestion,
                    AllegianceChoice.Kvlt
                );

            Assert.Throws<System.ArgumentException>(
                () =>
                    service.Settle(
                        group,
                        other,
                        new AcceptedTransgressionState(
                            "KVLT"
                        )
                    )
            );
        }

        [Test]
        public void SocietyVictoryPreservesHailProvenanceInsidePendingCandidate()
        {
            ActivationLegitimacyAssessment assessment =
                Uncovered(
                    "RELEASE",
                    "TRACK",
                    "IDEA"
                );

            ActivationLegitimacyCrisisGroup group =
                Group(assessment);

            AllegianceCrisis crisis =
                Resolve(
                    group.Question,
                    AllegianceChoice.Society
                );

            PendingActivationCandidate pending =
                service.Settle(
                    group,
                    crisis,
                    new AcceptedTransgressionState(
                        "KVLT"
                    )
                ).PendingCandidates[0];

            Assert.That(
                pending.Candidate.BehaviorOccurrenceId,
                Is.EqualTo("BEHAVIOR")
            );

            Assert.That(
                pending.Candidate.HailOccurrenceId,
                Is.EqualTo("HAIL")
            );

            Assert.That(
                pending.Candidate.HailedAspectId,
                Is.EqualTo("ASPECT_SATAN")
            );
        }

        private static
            ActivationLegitimacyCrisisGroup
            Group(
                params
                ActivationLegitimacyAssessment[]
                assessments)
        {
            ActivationLegitimacyCandidate first =
                assessments[0].Candidate;

            AllegianceCrisisQuestion question =
                new AllegianceCrisisQuestion(
                    first.HappeningId,
                    first.SourceIntentId,
                    first.ActorEntityId,
                    first.BehaviorTypeId,
                    first.Axis,
                    first.Pole,
                    first.PraxisDegree,
                    8,
                    first.BehaviorOccurrenceId
                );

            return new ActivationLegitimacyCrisisGroup(
                question,
                assessments
            );
        }

        private static
            ActivationLegitimacyAssessment
            Uncovered(
                string releaseId,
                string trackId,
                string ideaId)
        {
            ActivationLegitimacyCandidate candidate =
                new ActivationLegitimacyCandidate(
                    ActivationAttemptRoute.Hail,
                    "HAPPENING",
                    "INTENT",
                    "TRANSGRESSOR",
                    releaseId,
                    "DEMO",
                    trackId,
                    ideaId,
                    0,
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    TagDegree.Dominant,
                    TagDegree.Dominant,
                    "BEHAVIOR",
                    "HAIL",
                    "ASPECT_SATAN"
                );

            return new ActivationLegitimacyAssessment(
                candidate,
                ActivationLegitimacyDisposition
                    .RequiresAllegianceCrisis,
                null
            );
        }

        private static AllegianceCrisis Resolve(
            AllegianceCrisisQuestion question,
            AllegianceChoice keeperChoice)
        {
            AllegianceCrisis crisis =
                new AllegianceCrisis(
                    question,
                    new[]
                    {
                        "KEEPER",
                        "PLAYER_B"
                    },
                    "KEEPER"
                );

            Assert.That(
                crisis.TryCastVote(
                    new AllegianceCrisisVote(
                        "KEEPER",
                        keeperChoice,
                        8
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryCastVote(
                    new AllegianceCrisisVote(
                        "PLAYER_B",
                        keeperChoice,
                        8
                    )
                ),
                Is.True
            );

            Assert.That(
                crisis.TryResolve(
                    8,
                    out _
                ),
                Is.True
            );

            return crisis;
        }
    }
}