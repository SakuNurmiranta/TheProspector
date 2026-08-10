using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions.History;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation.Tests
{
    public class
        ActivationLegitimacyClassificationTests
    {
        private readonly
            ActivationAttemptDiscoveryService discovery =
                new();

        private readonly
            ActivationLegitimacyClassificationService
                classifier =
                    new();

        [Test]
        public void BlockedPerformance_RemainsAttemptButCreatesNoLegitimacyQuestion()
        {
            Happening happening =
                CreateHappening();

            SceneReleaseActivationSource source =
                Source(
                    Pair(
                        "IDEA",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            RecordPerform(
                happening,
                "PERFORM",
                source.Release.ReleaseId
            );

            Block(
                happening,
                "PERFORM"
            );

            SceneReleaseActivationAttempt attempt =
                discovery.Discover(
                    happening,
                    "PERFORM",
                    new[] { source }
                )[0];

            Assert.That(
                classifier.Classify(
                    happening,
                    attempt,
                    source,
                    EmptyState()
                ),
                Is.Empty
            );
        }

        [Test]
        public void SuccessfulPerformance_UsesPublicPerformancePraxisAndExactPrecedent()
        {
            Happening happening =
                CreateHappening();

            SceneReleaseActivationSource source =
                Source(
                    Pair(
                        "IDEA",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive
                    )
                );

            RecordPerform(
                happening,
                "PERFORM",
                source.Release.ReleaseId
            );

            Succeed(
                happening,
                "PERFORM"
            );

            AcceptedTransgressionState state =
                EmptyState();

            AcceptedTransgressionRecord precedent =
                Accept(
                    state,
                    "AT_PERFORMANCE",
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            SceneReleaseActivationAttempt attempt =
                discovery.Discover(
                    happening,
                    "PERFORM",
                    new[] { source }
                )[0];

            var assessments =
                classifier.Classify(
                    happening,
                    attempt,
                    source,
                    state
                );

            Assert.That(
                assessments.Count,
                Is.EqualTo(1)
            );

            ActivationLegitimacyAssessment result =
                assessments[0];

            Assert.That(
                result.Disposition,
                Is.EqualTo(
                    ActivationLegitimacyDisposition
                        .Covered
                )
            );

            Assert.That(
                result.CoveringPrecedent,
                Is.SameAs(precedent)
            );

            Assert.That(
                result.Candidate.BehaviorTypeId,
                Is.EqualTo(
                    "PUBLIC_PERFORMANCE"
                )
            );

            Assert.That(
                result.Candidate.PraxisDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                result.Candidate
                    .AppliedActivationDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                result.Candidate
                    .RecordedDominantDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );
        }

        [Test]
        public void PerformanceHasNoAutomaticDegreeOneLegitimacy()
        {
            Happening happening =
                CreateHappening();

            SceneReleaseActivationSource source =
                Source(
                    Pair(
                        "IDEA",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            RecordPerform(
                happening,
                "PERFORM",
                source.Release.ReleaseId
            );

            Succeed(
                happening,
                "PERFORM"
            );

            SceneReleaseActivationAttempt attempt =
                discovery.Discover(
                    happening,
                    "PERFORM",
                    new[] { source }
                )[0];

            var assessments =
                classifier.Classify(
                    happening,
                    attempt,
                    source,
                    EmptyState()
                );

            Assert.That(
                assessments.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                assessments[0].Disposition,
                Is.EqualTo(
                    ActivationLegitimacyDisposition
                        .RequiresAllegianceCrisis
                )
            );
        }

        [Test]
        public void PerformancePairsWithDifferentDirections_AreClassifiedIndependently()
        {
            Happening happening =
                CreateHappening();

            SceneReleaseActivationSource source =
                Source(
                    Pair(
                        "PROFANE",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    ),
                    Pair(
                        "SACRED",
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        TagDegree.Dominant
                    )
                );

            RecordPerform(
                happening,
                "PERFORM",
                source.Release.ReleaseId
            );

            Succeed(
                happening,
                "PERFORM"
            );

            AcceptedTransgressionState state =
                EmptyState();

            Accept(
                state,
                "AT_NEGATIVE",
                "PUBLIC_PERFORMANCE",
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Weak
            );

            SceneReleaseActivationAttempt attempt =
                discovery.Discover(
                    happening,
                    "PERFORM",
                    new[] { source }
                )[0];

            var assessments =
                classifier.Classify(
                    happening,
                    attempt,
                    source,
                    state
                );

            Assert.That(
                assessments.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                assessments[0].Disposition,
                Is.EqualTo(
                    ActivationLegitimacyDisposition
                        .Covered
                )
            );

            Assert.That(
                assessments[1].Disposition,
                Is.EqualTo(
                    ActivationLegitimacyDisposition
                        .RequiresAllegianceCrisis
                )
            );
        }

        [Test]
        public void SuccessfulHail_RequiresFactualBehaviorAndHailBeforeClassification()
        {
            Happening happening =
                CreateHappening();

            SceneReleaseActivationSource source =
                Source(
                    Pair(
                        "IDEA",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            RecordHail(
                happening,
                "HAIL",
                TagDegree.Transgressive,
                "ASPECT_SATAN"
            );

            Succeed(
                happening,
                "HAIL"
            );

            SceneReleaseActivationAttempt attempt =
                discovery.Discover(
                    happening,
                    "HAIL",
                    new[] { source }
                )[0];

            Assert.That(
                classifier.Classify(
                    happening,
                    attempt,
                    source,
                    EmptyState()
                ),
                Is.Empty
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "HAIL",
                        "BEHAVIOR",
                        "HAIL_OCCURRENCE"
                    ),
                Is.True
            );

            Assert.That(
                classifier.Classify(
                    happening,
                    attempt,
                    source,
                    EmptyState()
                ).Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void HailLegitimacy_IsIndependentOfHailedAspect()
        {
            Happening happening =
                CreateHappening();

            SceneReleaseActivationSource source =
                Source(
                    Pair(
                        "IDEA",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive
                    )
                );

            RecordHail(
                happening,
                "HAIL",
                TagDegree.Dominant,
                "ASPECT_ODIN"
            );

            Succeed(
                happening,
                "HAIL"
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "HAIL",
                        "BEHAVIOR",
                        "HAIL_OCCURRENCE"
                    ),
                Is.True
            );

            AcceptedTransgressionState state =
                EmptyState();

            AcceptedTransgressionRecord precedent =
                Accept(
                    state,
                    "AT",
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                );

            SceneReleaseActivationAttempt attempt =
                discovery.Discover(
                    happening,
                    "HAIL",
                    new[] { source }
                )[0];

            ActivationLegitimacyAssessment result =
                classifier.Classify(
                    happening,
                    attempt,
                    source,
                    state
                )[0];

            Assert.That(
                result.IsCovered,
                Is.True
            );

            Assert.That(
                result.CoveringPrecedent,
                Is.SameAs(precedent)
            );

            Assert.That(
                result.Candidate.HailedAspectId,
                Is.EqualTo("ASPECT_ODIN")
            );
        }

        [Test]
        public void HailChecksFullBehaviorSeverityBeforeRecordedDegreeCap()
        {
            Happening happening =
                CreateHappening();

            SceneReleaseActivationSource source =
                Source(
                    Pair(
                        "IDEA",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            RecordHail(
                happening,
                "HAIL",
                TagDegree.Transgressive,
                "ASPECT_SATAN"
            );

            Succeed(
                happening,
                "HAIL"
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "HAIL",
                        "BEHAVIOR",
                        "HAIL_OCCURRENCE"
                    ),
                Is.True
            );

            AcceptedTransgressionState state =
                EmptyState();

            // KVLT accepts only degree-2 church arson.
            Accept(
                state,
                "AT_TWO",
                "CHURCH_ARSON",
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Dominant
            );

            SceneReleaseActivationAttempt attempt =
                discovery.Discover(
                    happening,
                    "HAIL",
                    new[] { source }
                )[0];

            ActivationLegitimacyAssessment result =
                classifier.Classify(
                    happening,
                    attempt,
                    source,
                    state
                )[0];

            Assert.That(
                result.Candidate.PraxisDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                result.Candidate
                    .AppliedActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                result.Disposition,
                Is.EqualTo(
                    ActivationLegitimacyDisposition
                        .RequiresAllegianceCrisis
                )
            );
        }

        [Test]
        public void HailCanProduceSeveralExactPairAssessments()
        {
            Happening happening =
                CreateHappening();

            SceneReleaseActivationSource source =
                Source(
                    Pair(
                        "IDEA_A",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak
                    ),
                    Pair(
                        "IDEA_B",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive
                    )
                );

            RecordHail(
                happening,
                "HAIL",
                TagDegree.Dominant,
                "ASPECT_REVOLUTION"
            );

            Succeed(
                happening,
                "HAIL"
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "HAIL",
                        "BEHAVIOR",
                        "HAIL_OCCURRENCE"
                    ),
                Is.True
            );

            SceneReleaseActivationAttempt attempt =
                discovery.Discover(
                    happening,
                    "HAIL",
                    new[] { source }
                )[0];

            var assessments =
                classifier.Classify(
                    happening,
                    attempt,
                    source,
                    EmptyState()
                );

            Assert.That(
                assessments.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                assessments[0].Candidate.SourceIdeaId,
                Is.EqualTo("IDEA_A")
            );

            Assert.That(
                assessments[0].Candidate
                    .AppliedActivationDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                assessments[1].Candidate.SourceIdeaId,
                Is.EqualTo("IDEA_B")
            );

            Assert.That(
                assessments[1].Candidate
                    .AppliedActivationDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                assessments[0].Candidate
                    .BehaviorOccurrenceId,
                Is.EqualTo("BEHAVIOR")
            );

            Assert.That(
                assessments[0].Candidate
                    .HailOccurrenceId,
                Is.EqualTo("HAIL_OCCURRENCE")
            );
        }

        [Test]
        public void BlockedHail_HasAttemptButNoLegitimacyAssessment()
        {
            Happening happening =
                CreateHappening();

            SceneReleaseActivationSource source =
                Source(
                    Pair(
                        "IDEA",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            RecordHail(
                happening,
                "HAIL",
                TagDegree.Transgressive,
                "ASPECT_SATAN"
            );

            Block(
                happening,
                "HAIL"
            );

            SceneReleaseActivationAttempt attempt =
                discovery.Discover(
                    happening,
                    "HAIL",
                    new[] { source }
                )[0];

            Assert.That(
                classifier.Classify(
                    happening,
                    attempt,
                    source,
                    EmptyState()
                ),
                Is.Empty
            );

            Assert.That(
                happening.BehaviorOccurrences,
                Is.Empty
            );

            Assert.That(
                happening.HailOccurrences,
                Is.Empty
            );
        }

        [Test]
        public void CoveringAssessmentPreservesExactAcceptedTransgressionProvenance()
        {
            Happening happening =
                CreateHappening();

            SceneReleaseActivationSource source =
                Source(
                    Pair(
                        "IDEA",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            RecordHail(
                happening,
                "HAIL",
                TagDegree.Weak,
                "ASPECT_SATAN"
            );

            Succeed(
                happening,
                "HAIL"
            );

            Assert.That(
                happening
                    .TryMaterializeSuccessfulEnactBehavior(
                        "HAIL",
                        "BEHAVIOR",
                        "HAIL_OCCURRENCE"
                    ),
                Is.True
            );

            AcceptedTransgressionState state =
                EmptyState();

            AcceptedTransgressionRecord precedent =
                new AcceptedTransgressionRecord(
                    "AT_CRISIS_17",
                    "KVLT",
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    6,
                    AcceptedTransgressionSourceKind
                        .AllegianceCrisis,
                    "CRISIS_17"
                );

            Assert.That(
                state.TryAccept(precedent),
                Is.True
            );

            SceneReleaseActivationAttempt attempt =
                discovery.Discover(
                    happening,
                    "HAIL",
                    new[] { source }
                )[0];

            ActivationLegitimacyAssessment result =
                classifier.Classify(
                    happening,
                    attempt,
                    source,
                    state
                )[0];

            Assert.That(
                result.CoveringPrecedent
                    .AcceptedTransgressionId,
                Is.EqualTo("AT_CRISIS_17")
            );

            Assert.That(
                result.CoveringPrecedent.SourceId,
                Is.EqualTo("CRISIS_17")
            );
        }

        private static Happening CreateHappening()
        {
            Happening happening =
                new Happening(
                    "HAPPENING",
                    "COLLECTIVE_KVLT",
                    "KEEPER",
                    "CAUSE",
                    "CRUX",
                    "NODE_HOLE",
                    7,
                    new CharacterActionKey(
                        "KEEPER",
                        7,
                        1
                    )
                );

            Assert.That(
                happening.TryAddContext(
                    new HappeningContext(
                        "CTX",
                        "KVLT night",
                        HappeningContextAnchorKind
                            .Circumstance,
                        "KVLT_NIGHT",
                        "KEEPER",
                        7
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.TryAddParticipant("KEEPER"),
                Is.True
            );

            Assert.That(
                happening.TryAddParticipant("PLAYER_A"),
                Is.True
            );

            Assert.That(
                happening.TryBeginResolving(7),
                Is.True
            );

            return happening;
        }

        private static void RecordPerform(
            Happening happening,
            string intentId,
            string releaseId)
        {
            Assert.That(
                happening.TryRecordParticipantIntent(
                    new HappeningPerformIntent(
                        intentId,
                        "HAPPENING",
                        "CTX",
                        "PLAYER_A",
                        7,
                        releaseId
                    )
                ),
                Is.True
            );
        }

        private static void RecordHail(
            Happening happening,
            string intentId,
            TagDegree degree,
            string aspectId)
        {
            Assert.That(
                happening.TryRecordParticipantIntent(
                    new HappeningEnactBehaviorIntent(
                        intentId,
                        "HAPPENING",
                        "CTX",
                        "PLAYER_A",
                        7,
                        "CHURCH_ARSON",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        degree,
                        aspectId
                    )
                ),
                Is.True
            );
        }

        private static void Succeed(
            Happening happening,
            string intentId)
        {
            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        intentId,
                        HappeningIntentOutcome.Succeeded,
                        8
                    )
                ),
                Is.True
            );
        }

        private static void Block(
            Happening happening,
            string targetIntentId)
        {
            string opposeId =
                $"OPPOSE_{targetIntentId}";

            Assert.That(
                happening.TryRecordParticipantIntent(
                    new HappeningOpposeIntent(
                        opposeId,
                        "HAPPENING",
                        "CTX",
                        "KEEPER",
                        7,
                        targetIntentId
                    )
                ),
                Is.True
            );

            Assert.That(
                happening.TryRecordIntentResolution(
                    new HappeningIntentResolution(
                        targetIntentId,
                        HappeningIntentOutcome.Blocked,
                        8,
                        opposeId
                    )
                ),
                Is.True
            );
        }

        private static
            AcceptedTransgressionState
            EmptyState()
        {
            return new AcceptedTransgressionState(
                "KVLT"
            );
        }

        private static
            AcceptedTransgressionRecord
            Accept(
                AcceptedTransgressionState state,
                string id,
                string behaviorTypeId,
                TagAxis axis,
                TagPole pole,
                TagDegree degree)
        {
            AcceptedTransgressionRecord record =
                new AcceptedTransgressionRecord(
                    id,
                    "KVLT",
                    behaviorTypeId,
                    axis,
                    pole,
                    degree,
                    0,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "TEST"
                );

            Assert.That(
                state.TryAccept(record),
                Is.True
            );

            return record;
        }

        private static
            SceneReleaseActivationSource
            Source(
                params DemoTapeIdeaSnapshot[] ideas)
        {
            DemoTapeTrackSnapshot track =
                new DemoTapeTrackSnapshot(
                    "TRACK",
                    "TRACK",
                    1f,
                    1f,
                    ideas
                );

            DemoTape demo =
                new DemoTape(
                    "DEMO",
                    "DEMO",
                    "SET",
                    "SET",
                    1,
                    1,
                    1f,
                    new[] { track }
                );

            SceneRelease release =
                new SceneRelease(
                    "RELEASE",
                    demo.DemoTapeId,
                    "OWNER",
                    "KVLT_SCENE",
                    5,
                    1f
                );

            return new SceneReleaseActivationSource(
                release,
                demo
            );
        }

        private static DemoTapeIdeaSnapshot Pair(
            string ideaId,
            TagAxis axis,
            TagPole dominantPole,
            TagDegree dominantDegree)
        {
            TagPole submissivePole =
                dominantPole ==
                TagPole.Negative
                    ? TagPole.Positive
                    : TagPole.Negative;

            int ideaIndex =
                ideaId == "IDEA_B"
                    ? 1
                    : 0;

            return new DemoTapeIdeaSnapshot(
                ideaId,
                ideaIndex,
                $"ASPECT_{ideaId}",
                IdeaPayloadType.TagPair,
                "SOURCE_ENTITY",
                TagContainerType.Transient,
                1f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        axis,
                        dominantPole,
                        dominantDegree,
                        DemoTapeTagOccurrenceRole
                            .PairDominant
                    ),
                    new DemoTapeTagOccurrenceSnapshot(
                        axis,
                        submissivePole,
                        TagDegree.Weak,
                        DemoTapeTagOccurrenceRole
                            .PairSubmissive
                    )
                }
            );
        }
    }
}