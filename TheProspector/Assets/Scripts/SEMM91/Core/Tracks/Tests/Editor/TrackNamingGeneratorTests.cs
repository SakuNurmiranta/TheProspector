using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class TrackNamingGeneratorTests
    {
        [Test]
        public void TryGenerate_EmptyTrack_ReturnsFalse()
        {
            Track track = CreateTrack();

            bool generated =
                TrackNamingGenerator.TryGenerate(
                    track,
                    CreateCatalog(),
                    0,
                    out string title
                );

            Assert.That(generated, Is.False);
            Assert.That(title, Is.EqualTo(string.Empty));
        }

        [Test]
        public void TryGenerate_FormalPair_ProducesPairTitle()
        {
            Track track = CreateTrack();

            track.AddIdea(
                CreatePairIdea(
                    "IDEA_PAIR",
                    TagAxis.Interpretive,
                    TagPole.Positive,
                    TagAxis.Interpretive,
                    TagPole.Negative
                )
            );

            bool generated =
                TrackNamingGenerator.TryGenerate(
                    track,
                    CreateCatalog(),
                    0,
                    out string title
                );

            Assert.That(generated, Is.True);

            Assert.That(
                title,
                Is.EqualTo("Naming the Abyss")
            );
        }

        [Test]
        public void TryGenerate_PairWithSurface_ProducesSurfacePairTitle()
        {
            Track track = CreateTrack();

            track.AddIdea(
                CreatePairIdea(
                    "IDEA_PAIR",
                    TagAxis.Interpretive,
                    TagPole.Positive,
                    TagAxis.Interpretive,
                    TagPole.Negative
                )
            );

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_SURFACE",
                    TagAxis.Emotional,
                    TagPole.Positive
                )
            );

            bool generated =
                TrackNamingGenerator.TryGenerate(
                    track,
                    CreateCatalog(),
                    0,
                    out string title
                );

            Assert.That(generated, Is.True);

            Assert.That(
                title,
                Is.EqualTo(
                    "Fervent Naming the Abyss"
                )
            );
        }

        [Test]
        public void TryGenerate_SameCompositionAndSeed_ReturnsSameTitle()
        {
            Track track = CreateTrack();

            track.AddIdea(
                CreatePairIdea(
                    "IDEA_PAIR",
                    TagAxis.Interpretive,
                    TagPole.Positive,
                    TagAxis.Interpretive,
                    TagPole.Negative
                )
            );

            TrackNamingLexiconCatalog catalog =
                CreateCatalog();

            TrackNamingGenerator.TryGenerate(
                track,
                catalog,
                17,
                out string first
            );

            TrackNamingGenerator.TryGenerate(
                track,
                catalog,
                17,
                out string second
            );

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void TryGenerate_DoesNotMutateTrackTitle()
        {
            Track track = CreateTrack();

            track.AddIdea(
                CreatePairIdea(
                    "IDEA_PAIR",
                    TagAxis.Interpretive,
                    TagPole.Positive,
                    TagAxis.Interpretive,
                    TagPole.Negative
                )
            );

            bool generated =
                TrackNamingGenerator.TryGenerate(
                    track,
                    CreateCatalog(),
                    0,
                    out string generatedTitle
                );

            Assert.That(generated, Is.True);

            Assert.That(
                generatedTitle,
                Is.EqualTo("Naming the Abyss")
            );

            Assert.That(
                track.DisplayName,
                Is.EqualTo("Untitled Track")
            );
        }

        [Test]
        public void TryGenerate_ThreeSolitaryPositions_ReturnsFalseForNow()
        {
            Track track = CreateTrack();

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_0",
                    TagAxis.Interpretive,
                    TagPole.Positive
                )
            );

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_1",
                    TagAxis.Interpretive,
                    TagPole.Negative
                )
            );

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_2",
                    TagAxis.Emotional,
                    TagPole.Positive
                )
            );

            bool generated =
                TrackNamingGenerator.TryGenerate(
                    track,
                    CreateCatalog(),
                    0,
                    out string title
                );

            Assert.That(generated, Is.False);
            Assert.That(title, Is.EqualTo(string.Empty));
        }

        private static Track CreateTrack()
        {
            return new Track(
                "TRACK_TEST",
                "Untitled Track",
                0.0f,
                0
            );
        }

        private static Idea CreateSingleIdea(
            string ideaId,
            TagAxis axis,
            TagPole pole)
        {
            return new Idea(
                ideaId: ideaId,
                aspectId: "ASPECT_TEST",
                tagInstance: new TagInstance(
                    axis,
                    pole,
                    TagDegree.Dominant
                ),
                conveyance: 1.0f,
                sourceEntityId: "ENTITY_TEST",
                sourceContainerType:
                    TagContainerType.Transient
            );
        }

        private static Idea CreatePairIdea(
            string ideaId,
            TagAxis dominantAxis,
            TagPole dominantPole,
            TagAxis submissiveAxis,
            TagPole submissivePole)
        {
            TagPair pair = new(
                dominantTag: new TagInstance(
                    dominantAxis,
                    dominantPole,
                    TagDegree.Dominant
                ),
                submissiveTag: new TagInstance(
                    submissiveAxis,
                    submissivePole,
                    TagDegree.Dominant
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

        private static TrackNamingLexiconCatalog
            CreateCatalog()
        {
            return new TrackNamingLexiconCatalog(
                new[]
                {
                    CreateTag(
                        TagAxis.Interpretive,
                        TagPole.Positive,
                        "purposeful",
                        "purpose",
                        "naming",
                        "the chosen path"
                    ),
                    CreateTag(
                        TagAxis.Interpretive,
                        TagPole.Negative,
                        "bleak",
                        "abyss",
                        "erasing",
                        "the abyss"
                    ),
                    CreateTag(
                        TagAxis.Emotional,
                        TagPole.Positive,
                        "fervent",
                        "communion",
                        "embracing",
                        "the beloved"
                    )
                }
            );
        }

        private static TrackNamingTagLexicon CreateTag(
            TagAxis axis,
            TagPole pole,
            string modifier,
            string noun,
            string action,
            string submissivePhrase)
        {
            return new TrackNamingTagLexicon(
                axis,
                pole,
                new[]
                {
                    CreateDegree(
                        TagDegree.Neutral,
                        modifier,
                        noun,
                        action,
                        submissivePhrase
                    ),
                    CreateDegree(
                        TagDegree.Weak,
                        modifier,
                        noun,
                        action,
                        submissivePhrase
                    ),
                    CreateDegree(
                        TagDegree.Dominant,
                        modifier,
                        noun,
                        action,
                        submissivePhrase
                    ),
                    CreateDegree(
                        TagDegree.Transgressive,
                        modifier,
                        noun,
                        action,
                        submissivePhrase
                    )
                },
                new TrackNamingContextFragmentGroup[0]
            );
        }

        private static TrackNamingDegreeLexicon
            CreateDegree(
                TagDegree degree,
                string modifier,
                string noun,
                string action,
                string submissivePhrase)
        {
            return new TrackNamingDegreeLexicon(
                degree,
                degree.ToString(),
                new[] { modifier },
                new[] { noun },
                new[] { action },
                new[] { submissivePhrase }
            );
        }
    }
}