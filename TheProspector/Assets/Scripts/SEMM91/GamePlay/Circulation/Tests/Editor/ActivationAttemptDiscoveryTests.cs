using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions.History;
using SEMM91.GamePlay.Events;

namespace SEMM91.GamePlay.Circulation.Tests
{
    public class ActivationAttemptDiscoveryTests
    {
        private readonly
            ActivationAttemptDiscoveryService service =
                new();

        [Test]
        public void UnknownIntent_DiscoversNothing()
        {
            Happening happening =
                CreateResolvingHappening();

            Assert.That(
                service.Discover(
                    happening,
                    "UNKNOWN",
                    Array.Empty<
                        SceneReleaseActivationSource>()
                ),
                Is.Empty
            );
        }

        [Test]
        public void OpposeAndSupportIntents_AreNotActivationAttempts()
        {
            Happening happening =
                CreateResolvingHappening();

            SceneReleaseActivationSource source =
                CreateSource(
                    "DEMO",
                    5
                );

            HappeningPerformIntent primary =
                new HappeningPerformIntent(
                    "PRIMARY",
                    "HAPPENING",
                    "CTX",
                    "PLAYER_A",
                    7,
                    source.Release.ReleaseId
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    primary
                ),
                Is.True
            );

            HappeningOpposeIntent oppose =
                new HappeningOpposeIntent(
                    "OPPOSE",
                    "HAPPENING",
                    "CTX",
                    "KEEPER",
                    7,
                    "PRIMARY"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    oppose
                ),
                Is.True
            );

            HappeningSupportIntent support =
                new HappeningSupportIntent(
                    "SUPPORT",
                    "HAPPENING",
                    "CTX",
                    "KEEPER",
                    7,
                    "PRIMARY"
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    support
                ),
                Is.True
            );

            Assert.That(
                service.Discover(
                    happening,
                    "OPPOSE",
                    new[] { source }
                ),
                Is.Empty
            );

            Assert.That(
                service.Discover(
                    happening,
                    "SUPPORT",
                    new[] { source }
                ),
                Is.Empty
            );
        }

        [Test]
        public void PerformanceIntent_DiscoversOnlyExplicitTargetWithoutPairInspection()
        {
            Happening happening =
                CreateResolvingHappening();

            SceneReleaseActivationSource target =
                CreateSource(
                    "DEMO_TARGET",
                    5
                );

            SceneReleaseActivationSource other =
                CreateSource(
                    "DEMO_OTHER",
                    5,
                    Track(
                        "TRACK_OTHER",
                        Pair(
                            "IDEA_OTHER",
                            0,
                            "ASPECT_GUITAR",
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive
                        )
                    )
                );

            RecordPerform(
                happening,
                "PERFORM",
                target.Release.ReleaseId
            );

            var attempts =
                service.Discover(
                    happening,
                    "PERFORM",
                    new[]
                    {
                        target,
                        other
                    }
                );

            Assert.That(
                attempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                attempts[0].Route,
                Is.EqualTo(
                    ActivationAttemptRoute.Performance
                )
            );

            Assert.That(
                attempts[0].SceneReleaseId,
                Is.EqualTo(
                    target.Release.ReleaseId
                )
            );

            Assert.That(
                attempts[0]
                    .AnsweringPairCandidates,
                Is.Empty
            );
        }

        [Test]
        public void BlockedPerformance_StillDiscoversReleaseAttempt()
        {
            Happening happening =
                CreateResolvingHappening();

            SceneReleaseActivationSource source =
                CreateSource(
                    "DEMO",
                    5
                );

            RecordPerform(
                happening,
                "PERFORM",
                source.Release.ReleaseId
            );

            RecordOppositionAndBlock(
                happening,
                "PERFORM"
            );

            Assert.That(
                service.Discover(
                    happening,
                    "PERFORM",
                    new[] { source }
                ).Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void HailIntent_DiscoversMatchingPairAndPreservesExactProvenance()
        {
            Happening happening =
                CreateResolvingHappening();

            SceneReleaseActivationSource source =
                CreateSource(
                    "DEMO",
                    5,
                    Track(
                        "TRACK",
                        Pair(
                            "IDEA",
                            0,
                            "ASPECT_GUITAR",
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant
                        )
                    )
                );

            RecordHail(
                happening,
                "HAIL_INTENT",
                TagAxis.Symbolic,
                TagPole.Negative,
                "ASPECT_SATAN"
            );

            var attempts =
                service.Discover(
                    happening,
                    "HAIL_INTENT",
                    new[] { source }
                );

            Assert.That(
                attempts.Count,
                Is.EqualTo(1)
            );

            SceneReleaseActivationAttempt attempt =
                attempts[0];

            Assert.That(
                attempt.Route,
                Is.EqualTo(
                    ActivationAttemptRoute.Hail
                )
            );

            Assert.That(
                attempt.HailedAspectId,
                Is.EqualTo("ASPECT_SATAN")
            );

            Assert.That(
                attempt.AnsweringPairCandidates.Count,
                Is.EqualTo(1)
            );

            HailAnsweringPairCandidate pair =
                attempt.AnsweringPairCandidates[0];

            Assert.That(
                pair.SourceTrackId,
                Is.EqualTo("TRACK")
            );

            Assert.That(
                pair.SourceIdeaId,
                Is.EqualTo("IDEA")
            );

            Assert.That(
                pair.IdeaIndex,
                Is.EqualTo(0)
            );

            // Recorded Hosting Aspect is deliberately
            // different from enacted Hailed Aspect.

            Assert.That(
                pair.HostingAspectId,
                Is.EqualTo("ASPECT_GUITAR")
            );

            Assert.That(
                attempt.HailedAspectId,
                Is.Not.EqualTo(
                    pair.HostingAspectId
                )
            );

            Assert.That(
                pair.DominantAxis,
                Is.EqualTo(TagAxis.Symbolic)
            );

            Assert.That(
                pair.DominantPole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                pair.RecordedDominantDegree,
                Is.EqualTo(TagDegree.Dominant)
            );
        }

        [Test]
        public void HailIntent_CanDiscoverSeveralPairsAndSeveralReleases()
        {
            Happening happening =
                CreateResolvingHappening();

            SceneReleaseActivationSource first =
                CreateSource(
                    "DEMO_A",
                    5,
                    Track(
                        "TRACK_A",
                        Pair(
                            "IDEA_A1",
                            0,
                            "ASPECT_A",
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Weak
                        ),
                        Pair(
                            "IDEA_A2",
                            1,
                            "ASPECT_B",
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive
                        )
                    )
                );

            SceneReleaseActivationSource second =
                CreateSource(
                    "DEMO_B",
                    5,
                    Track(
                        "TRACK_B",
                        Pair(
                            "IDEA_B",
                            0,
                            "ASPECT_C",
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant
                        )
                    )
                );

            RecordHail(
                happening,
                "HAIL",
                TagAxis.Symbolic,
                TagPole.Negative,
                "ASPECT_ODIN"
            );

            var attempts =
                service.Discover(
                    happening,
                    "HAIL",
                    new[]
                    {
                        first,
                        second
                    }
                );

            Assert.That(
                attempts.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                attempts[0]
                    .AnsweringPairCandidates.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                attempts[1]
                    .AnsweringPairCandidates.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void HailDiscovery_RejectsSolitaryWrongDirectionAndZeroDominant()
        {
            Happening happening =
                CreateResolvingHappening();

            SceneReleaseActivationSource source =
                CreateSource(
                    "DEMO",
                    5,
                    Track(
                        "TRACK",
                        Single(
                            "SOLITARY",
                            0,
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive
                        ),
                        Pair(
                            "WRONG_AXIS",
                            1,
                            "ASPECT_A",
                            TagAxis.Physical,
                            TagPole.Negative,
                            TagDegree.Transgressive
                        ),
                        Pair(
                            "WRONG_POLE",
                            2,
                            "ASPECT_B",
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            TagDegree.Transgressive
                        ),
                        Pair(
                            "ZERO_DOMINANT",
                            3,
                            "ASPECT_C",
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Neutral
                        ),
                        Pair(
                            "VALID",
                            4,
                            "ASPECT_D",
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Weak
                        )
                    )
                );

            RecordHail(
                happening,
                "HAIL",
                TagAxis.Symbolic,
                TagPole.Negative,
                "ASPECT_REVOLUTION"
            );

            var attempts =
                service.Discover(
                    happening,
                    "HAIL",
                    new[] { source }
                );

            Assert.That(
                attempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                attempts[0]
                    .AnsweringPairCandidates.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                attempts[0]
                    .AnsweringPairCandidates[0]
                    .SourceIdeaId,
                Is.EqualTo("VALID")
            );
        }

        [Test]
        public void BehaviorIntentWithoutHail_DiscoversNoReleaseAttempt()
        {
            Happening happening =
                CreateResolvingHappening();

            SceneReleaseActivationSource source =
                CreateSource(
                    "DEMO",
                    5,
                    Track(
                        "TRACK",
                        Pair(
                            "IDEA",
                            0,
                            "ASPECT_GUITAR",
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant
                        )
                    )
                );

            HappeningEnactBehaviorIntent intent =
                new HappeningEnactBehaviorIntent(
                    "BEHAVIOR",
                    "HAPPENING",
                    "CTX",
                    "PLAYER_A",
                    7,
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    null
                );

            Assert.That(
                happening.TryRecordParticipantIntent(
                    intent
                ),
                Is.True
            );

            Assert.That(
                service.Discover(
                    happening,
                    "BEHAVIOR",
                    new[] { source }
                ),
                Is.Empty
            );
        }

        [Test]
        public void BlockedHail_StillProtectsStructurallyCompatibleRelease()
        {
            Happening happening =
                CreateResolvingHappening();

            SceneReleaseActivationSource source =
                CreateSource(
                    "DEMO",
                    5,
                    Track(
                        "TRACK",
                        Pair(
                            "IDEA",
                            0,
                            "ASPECT_GUITAR",
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant
                        )
                    )
                );

            RecordHail(
                happening,
                "HAIL",
                TagAxis.Symbolic,
                TagPole.Negative,
                "ASPECT_SATAN"
            );

            RecordOppositionAndBlock(
                happening,
                "HAIL"
            );

            Assert.That(
                service.Discover(
                    happening,
                    "HAIL",
                    new[] { source }
                ).Count,
                Is.EqualTo(1)
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
        public void FailedToFetterRelease_DoesNotReceiveNewAttempt()
        {
            Happening happening =
                CreateResolvingHappening();

            SceneReleaseActivationSource source =
                CreateSource(
                    "DEMO",
                    5
                );

            Assert.That(
                source.Release
                    .TryFailToFetter(6),
                Is.True
            );

            RecordPerform(
                happening,
                "PERFORM",
                source.Release.ReleaseId
            );

            Assert.That(
                service.Discover(
                    happening,
                    "PERFORM",
                    new[] { source }
                ),
                Is.Empty
            );
        }

        [Test]
        public void ReleaseMustAlreadyExistWhenIntentIsDeclared()
        {
            Happening happening =
                CreateResolvingHappening();

            SceneReleaseActivationSource future =
                CreateSource(
                    "FUTURE_DEMO",
                    8
                );

            RecordPerform(
                happening,
                "PERFORM",
                future.Release.ReleaseId
            );

            Assert.That(
                service.Discover(
                    happening,
                    "PERFORM",
                    new[] { future }
                ),
                Is.Empty
            );
        }

        [Test]
        public void SceneReleaseAttemptHistory_PreservesAndDeduplicatesDiscovery()
        {
            Happening happening =
                CreateResolvingHappening();

            SceneReleaseActivationSource first =
                CreateSource(
                    "DEMO_A",
                    5
                );

            SceneReleaseActivationSource second =
                CreateSource(
                    "DEMO_B",
                    5
                );

            RecordPerform(
                happening,
                "PERFORM",
                first.Release.ReleaseId
            );

            SceneReleaseActivationAttempt attempt =
                service.Discover(
                    happening,
                    "PERFORM",
                    new[] { first }
                )[0];

            Assert.That(
                first.Release
                    .TryRecordActivationAttempt(
                        attempt
                    ),
                Is.True
            );

            Assert.That(
                first.Release
                    .TryRecordActivationAttempt(
                        attempt
                    ),
                Is.False
            );

            Assert.That(
                second.Release
                    .TryRecordActivationAttempt(
                        attempt
                    ),
                Is.False
            );

            Assert.That(
                first.Release.ActivationAttempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                first.Release.ActivationAttempts[0],
                Is.SameAs(attempt)
            );
        }

        private static Happening
            CreateResolvingHappening()
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
                        "Active KVLT situation",
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
                happening.TryAddParticipant(
                    "KEEPER"
                ),
                Is.True
            );

            Assert.That(
                happening.TryAddParticipant(
                    "PLAYER_A"
                ),
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
            TagAxis axis,
            TagPole pole,
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
                        "BEHAVIOR",
                        axis,
                        pole,
                        TagDegree.Transgressive,
                        aspectId
                    )
                ),
                Is.True
            );
        }

        private static void
            RecordOppositionAndBlock(
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
                        7,
                        opposeId
                    )
                ),
                Is.True
            );
        }

        private static
            SceneReleaseActivationSource
            CreateSource(
                string demoId,
                int releasedTurn,
                params DemoTapeTrackSnapshot[]
                    tracks)
        {
            DemoTape demo =
                new DemoTape(
                    demoId,
                    demoId,
                    "SET",
                    "SET",
                    1,
                    1,
                    1f,
                    tracks
                );

            SceneRelease release =
                new SceneRelease(
                    demoId,
                    demo.DemoTapeId,
                    "OWNER",
                    "KVLT_SCENE",
                    releasedTurn,
                    1f
                );

            return new SceneReleaseActivationSource(
                release,
                demo
            );
        }

        private static DemoTapeTrackSnapshot
            Track(
                string trackId,
                params DemoTapeIdeaSnapshot[]
                    ideas)
        {
            return new DemoTapeTrackSnapshot(
                trackId,
                trackId,
                1f,
                1f,
                ideas
            );
        }

        private static DemoTapeIdeaSnapshot
            Pair(
                string ideaId,
                int ideaIndex,
                string hostingAspectId,
                TagAxis axis,
                TagPole dominantPole,
                TagDegree dominantDegree)
        {
            TagPole submissivePole =
                dominantPole ==
                TagPole.Negative
                    ? TagPole.Positive
                    : TagPole.Negative;

            return new DemoTapeIdeaSnapshot(
                ideaId,
                ideaIndex,
                hostingAspectId,
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

        private static DemoTapeIdeaSnapshot
            Single(
                string ideaId,
                int ideaIndex,
                TagAxis axis,
                TagPole pole,
                TagDegree degree)
        {
            return new DemoTapeIdeaSnapshot(
                ideaId,
                ideaIndex,
                "ASPECT_SINGLE",
                IdeaPayloadType.SingleTag,
                "SOURCE_ENTITY",
                TagContainerType.Transient,
                1f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        axis,
                        pole,
                        degree,
                        DemoTapeTagOccurrenceRole
                            .Solitary
                    )
                }
            );
        }
    }
}