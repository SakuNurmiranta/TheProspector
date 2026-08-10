using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation.Tests
{
    public class
        ActivationLegitimacyCrisisGroupingTests
    {
        private readonly
            ActivationLegitimacyCrisisGroupingService
                grouping =
                    new();

        [Test]
        public void SeveralPairsFromSamePraxisBecomeOneInstitutionalQuestion()
        {
            ActivationLegitimacyAssessment first =
                Uncovered(
                    "RELEASE_A",
                    "TRACK_A",
                    "IDEA_A",
                    TagPole.Negative
                );

            ActivationLegitimacyAssessment second =
                Uncovered(
                    "RELEASE_A",
                    "TRACK_B",
                    "IDEA_B",
                    TagPole.Negative
                );

            ActivationLegitimacyAssessment third =
                Uncovered(
                    "RELEASE_B",
                    "TRACK_C",
                    "IDEA_C",
                    TagPole.Negative
                );

            var groups =
                grouping.Group(
                    new[]
                    {
                        first,
                        second,
                        third
                    },
                    8
                );

            Assert.That(
                groups.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                groups[0].Assessments.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                groups[0].Question.BehaviorTypeId,
                Is.EqualTo("CHURCH_ARSON")
            );

            Assert.That(
                groups[0].Question.PraxisDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );
        }

        [Test]
        public void DifferentPraxisDirectionsRemainDifferentQuestions()
        {
            var groups =
                grouping.Group(
                    new[]
                    {
                        Uncovered(
                            "RELEASE",
                            "TRACK_A",
                            "IDEA_A",
                            TagPole.Negative
                        ),
                        Uncovered(
                            "RELEASE",
                            "TRACK_B",
                            "IDEA_B",
                            TagPole.Positive
                        )
                    },
                    8
                );

            Assert.That(
                groups.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                groups[0].Question.Pole,
                Is.Not.EqualTo(
                    groups[1].Question.Pole
                )
            );
        }

        [Test]
        public void CoveredAssessmentsDoNotCreateCrisisQuestions()
        {
            ActivationLegitimacyCandidate candidate =
                Candidate(
                    "RELEASE",
                    "TRACK",
                    "IDEA",
                    TagPole.Negative
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT",
                    "KVLT",
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    1,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "SCENARIO"
                );

            ActivationLegitimacyAssessment covered =
                new(
                    candidate,
                    ActivationLegitimacyDisposition
                        .Covered,
                    precedent
                );

            Assert.That(
                grouping.Group(
                    new[] { covered },
                    8
                ),
                Is.Empty
            );
        }

        [Test]
        public void QuestionIdentityDependsOnPraxisOccurrenceNotAnsweringRelease()
        {
            var first =
                grouping.Group(
                    new[]
                    {
                        Uncovered(
                            "RELEASE_A",
                            "TRACK_A",
                            "IDEA_A",
                            TagPole.Negative
                        )
                    },
                    8
                )[0];

            var second =
                grouping.Group(
                    new[]
                    {
                        Uncovered(
                            "RELEASE_B",
                            "TRACK_Z",
                            "IDEA_Z",
                            TagPole.Negative
                        )
                    },
                    8
                )[0];

            Assert.That(
                first.Question.QuestionId,
                Is.EqualTo(
                    second.Question.QuestionId
                )
            );
        }

        private static
            ActivationLegitimacyAssessment
            Uncovered(
                string releaseId,
                string trackId,
                string ideaId,
                TagPole pole)
        {
            return new ActivationLegitimacyAssessment(
                Candidate(
                    releaseId,
                    trackId,
                    ideaId,
                    pole
                ),
                ActivationLegitimacyDisposition
                    .RequiresAllegianceCrisis,
                null
            );
        }

        private static
            ActivationLegitimacyCandidate
            Candidate(
                string releaseId,
                string trackId,
                string ideaId,
                TagPole pole)
        {
            return new ActivationLegitimacyCandidate(
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
                pole,
                TagDegree.Transgressive,
                TagDegree.Dominant,
                TagDegree.Dominant,
                "BEHAVIOR",
                "HAIL",
                "ASPECT_SATAN"
            );
        }
    }
}