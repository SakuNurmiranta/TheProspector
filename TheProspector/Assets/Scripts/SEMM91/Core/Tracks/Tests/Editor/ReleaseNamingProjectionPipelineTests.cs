using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class
        ReleaseNamingProjectionPipelineTests
    {
        [Test]
        public void ResolveTrvePreferred_UsesEligiblePairsAcrossTracks()
        {
            RehearsalSet set =
                CreateSet();

            Track firstTrack =
                CreateTrack("TRACK_A");

            firstTrack.AddIdea(
                CreateSingleIdea(
                    "IDEA_SOLITARY",
                    TagAxis.Existential,
                    TagPole.Negative,
                    TagDegree.Transgressive
                )
            );

            firstTrack.AddIdea(
                CreatePairIdea(
                    "IDEA_PAIR_A",
                    TagAxis.Symbolic,
                    TagDegree.Dominant,
                    TagDegree.Weak
                )
            );

            Track secondTrack =
                CreateTrack("TRACK_B");

            secondTrack.AddIdea(
                CreatePairIdea(
                    "IDEA_PAIR_B",
                    TagAxis.Expressive,
                    TagDegree.Dominant,
                    TagDegree.Weak
                )
            );

            set.AddTrack(firstTrack);
            set.AddTrack(secondTrack);

            TrackNamingPositionSelection selection =
                ReleaseNamingProjectionPipeline
                    .ResolveTrvePreferred(set);

            Assert.That(
                selection.SelectedOccurrences.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                selection.SelectedOccurrences[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_PAIR_A")
            );

            Assert.That(
                selection.SelectedOccurrences[0].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole
                        .PairDominant
                )
            );

            Assert.That(
                selection.SelectedOccurrences[1]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_PAIR_A")
            );

            Assert.That(
                selection.SelectedOccurrences[1].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole
                        .PairSubmissive
                )
            );

            Assert.That(
                selection.SelectedOccurrences[2]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_PAIR_B")
            );

            Assert.That(
                selection.SelectedOccurrences[2].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole
                        .PairDominant
                )
            );

            Assert.That(
                selection.OverflowOccurrences,
                Is.Empty
            );

            Assert.That(
                selection.SelectedOccurrences[1]
                    .MechanicalDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                selection.SelectedOccurrences[1]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Dominant)
            );
        }

        [Test]
        public void ResolveTrvePreferred_IgnoresPairBelowExistingTrveMinimum()
        {
            RehearsalSet set =
                CreateSet();

            Track track =
                CreateTrack("TRACK_A");

            track.AddIdea(
                CreatePairIdea(
                    "IDEA_WEAK_PAIR",
                    TagAxis.Symbolic,
                    TagDegree.Neutral,
                    TagDegree.Weak
                )
            );

            set.AddTrack(track);

            TrackNamingPositionSelection selection =
                ReleaseNamingProjectionPipeline
                    .ResolveTrvePreferred(set);

            Assert.That(
                selection.SelectedOccurrences,
                Is.Empty
            );

            Assert.That(
                selection.OverflowOccurrences,
                Is.Empty
            );
        }

        [Test]
        public void ResolveRegularFallback_IncludesSolitaryTags()
        {
            RehearsalSet set =
                CreateSet();

            Track firstTrack =
                CreateTrack("TRACK_A");

            firstTrack.AddIdea(
                CreateSingleIdea(
                    "IDEA_MORBID",
                    TagAxis.Existential,
                    TagPole.Negative,
                    TagDegree.Dominant
                )
            );

            Track secondTrack =
                CreateTrack("TRACK_B");

            secondTrack.AddIdea(
                CreateSingleIdea(
                    "IDEA_COLD",
                    TagAxis.Emotional,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            set.AddTrack(firstTrack);
            set.AddTrack(secondTrack);

            TrackNamingPositionSelection selection =
                ReleaseNamingProjectionPipeline
                    .ResolveRegularFallback(set);

            Assert.That(
                selection.SelectedOccurrences.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                selection.SelectedOccurrences[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_MORBID")
            );

            Assert.That(
                selection.SelectedOccurrences[1]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_COLD")
            );

            Assert.That(
                selection.SelectedOccurrences[0].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.Solitary
                )
            );

            Assert.That(
                selection.SelectedOccurrences[1].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.Solitary
                )
            );
        }

        private static RehearsalSet CreateSet()
        {
            return new RehearsalSet(
                "SET_TEST",
                "Untitled Set",
                0
            );
        }

        private static Track CreateTrack(
            string trackId)
        {
            return new Track(
                trackId,
                "Untitled Track",
                0.0f,
                0
            );
        }

        private static Idea CreateSingleIdea(
            string ideaId,
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            return new Idea(
                ideaId: ideaId,
                aspectId: "ASPECT_TEST",
                tagInstance: new TagInstance(
                    axis,
                    pole,
                    degree
                ),
                conveyance: 1.0f,
                sourceEntityId: "ENTITY_TEST",
                sourceContainerType:
                    TagContainerType.Transient
            );
        }

        private static Idea CreatePairIdea(
            string ideaId,
            TagAxis axis,
            TagDegree dominantDegree,
            TagDegree submissiveDegree)
        {
            TagPair pair =
                new(
                    new TagInstance(
                        axis,
                        TagPole.Negative,
                        dominantDegree
                    ),
                    new TagInstance(
                        axis,
                        TagPole.Positive,
                        submissiveDegree
                    )
                );

            return new Idea(
                ideaId: ideaId,
                aspectId: "ASPECT_TEST",
                tagPair: pair,
                conveyance: 1.0f,
                sourceEntityId: "ENTITY_TEST",
                sourceContainerType:
                    TagContainerType.Transient
            );
        }
    }
}