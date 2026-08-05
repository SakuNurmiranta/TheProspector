using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class TrackNamingSeedTests
    {
        [Test]
        public void FromTrackId_SameIdentity_ReturnsSameSeed()
        {
            int first =
                TrackNamingSeed.FromTrackId(
                    "VHS_TRACK_PLAYER_0_A"
                );

            int second =
                TrackNamingSeed.FromTrackId(
                    "VHS_TRACK_PLAYER_0_A"
                );

            Assert.That(
                second,
                Is.EqualTo(first)
            );
        }

        [Test]
        public void FromTrackId_DifferentKnownIdentities_ReturnDifferentSeeds()
        {
            int first =
                TrackNamingSeed.FromTrackId(
                    "VHS_TRACK_PLAYER_0_A"
                );

            int second =
                TrackNamingSeed.FromTrackId(
                    "VHS_TRACK_PLAYER_0_B"
                );

            Assert.That(
                second,
                Is.Not.EqualTo(first)
            );
        }

        [Test]
        public void FromTrackId_ResultIsNonNegative()
        {
            int seed =
                TrackNamingSeed.FromTrackId(
                    "TRACK_TEST_Æ_1991"
                );

            Assert.That(
                seed,
                Is.GreaterThanOrEqualTo(0)
            );
        }

        [Test]
        public void FromTrackId_EmptyIdentity_Throws()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    TrackNamingSeed.FromTrackId(
                        "   "
                    )
            );
        }

        [Test]
        public void Generator_ImplicitSeedMatchesExplicitDerivedSeed()
        {
            Track track = CreatePairTrack();

            TrackNamingLexiconCatalog catalog =
                CreateCatalog();

            bool implicitGenerated =
                TrackNamingGenerator.TryGenerate(
                    track,
                    catalog,
                    out string implicitTitle
                );

            int explicitSeed =
                TrackNamingSeed.FromTrack(track);

            bool explicitGenerated =
                TrackNamingGenerator.TryGenerate(
                    track,
                    catalog,
                    explicitSeed,
                    out string explicitTitle
                );

            Assert.That(
                implicitGenerated,
                Is.EqualTo(explicitGenerated)
            );

            Assert.That(
                implicitTitle,
                Is.EqualTo(explicitTitle)
            );
        }

        private static Track CreatePairTrack()
        {
            Track track = new(
                "TRACK_STABLE_SEED_TEST",
                "Untitled Track",
                0.0f,
                0
            );

            TagPair pair = new(
                dominantTag: new TagInstance(
                    TagAxis.Interpretive,
                    TagPole.Positive,
                    TagDegree.Dominant
                ),
                submissiveTag: new TagInstance(
                    TagAxis.Interpretive,
                    TagPole.Negative,
                    TagDegree.Dominant
                )
            );

            track.AddIdea(
                new Idea(
                    ideaId: "IDEA_PAIR",
                    aspectId: "ASPECT_TEST",
                    tagPair: pair,
                    conveyance: 1.0f,
                    sourceEntityId: "ENTITY_TEST",
                    sourceContainerType:
                        TagContainerType.Transient
                )
            );

            return track;
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