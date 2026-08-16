using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class ReleaseNamingGeneratorTests
    {
        [Test]
        public void TryGenerate_UsesTrvePreferredProjectionWhenItCanRender()
        {
            RehearsalSet set =
                CreateSet();

            Track surfaceTrack =
                CreateTrack("TRACK_SURFACE");

            surfaceTrack.AddIdea(
                CreateSingleIdea(
                    "IDEA_MORBID",
                    TagAxis.Existential,
                    TagPole.Negative,
                    TagDegree.Dominant
                )
            );

            surfaceTrack.AddIdea(
                CreateSingleIdea(
                    "IDEA_COLD",
                    TagAxis.Emotional,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            surfaceTrack.AddIdea(
                CreateSingleIdea(
                    "IDEA_RAW",
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            Track trveTrack =
                CreateTrack("TRACK_TRVE");

            trveTrack.AddIdea(
                CreatePairIdea(
                    "IDEA_PAIR",
                    TagAxis.Symbolic,
                    TagDegree.Dominant,
                    TagDegree.Weak
                )
            );

            set.AddTrack(surfaceTrack);
            set.AddTrack(trveTrack);

            TrackNamingLexiconCatalog catalog =
                TrackNamingLexiconCatalogData
                    .CreateDefault();

            const int seed = 12345;

            TrackNamingPositionSelection primary =
                ReleaseNamingProjectionPipeline
                    .ResolveTrvePreferred(set);

            TrackNamingGrammarAnalysis primaryAnalysis =
                TrackNamingGrammarAnalyzer.Analyze(
                    primary
                );

            bool primaryRendered =
                TrackNamingGrammarRenderer.TryRender(
                    primaryAnalysis,
                    catalog,
                    seed,
                    out string expectedPrimaryTitle
                );

            Assert.That(
                primaryRendered,
                Is.True
            );

            /*
             * The ordinary projection starts with three
             * solitary positions. Existing Peak 1 grammar
             * deliberately cannot render ThreeAccumulative,
             * so this proves that success came from the
             * TRVE-preferred pass.
             */
            TrackNamingPositionSelection regular =
                ReleaseNamingProjectionPipeline
                    .ResolveRegularFallback(set);

            TrackNamingGrammarAnalysis regularAnalysis =
                TrackNamingGrammarAnalyzer.Analyze(
                    regular
                );

            bool regularRendered =
                TrackNamingGrammarRenderer.TryRender(
                    regularAnalysis,
                    catalog,
                    seed,
                    out _
                );

            Assert.That(
                regularRendered,
                Is.False
            );

            bool generated =
                ReleaseNamingGenerator.TryGenerate(
                    set,
                    catalog,
                    seed,
                    out string title
                );

            Assert.That(
                generated,
                Is.True
            );

            Assert.That(
                title,
                Is.EqualTo(expectedPrimaryTitle)
            );
        }

        [Test]
        public void TryGenerate_FallsBackToRegularProjectionWhenTrveAttemptFails()
        {
            RehearsalSet set =
                CreateSet();

            Track track =
                CreateTrack("TRACK_SURFACE");

            track.AddIdea(
                CreateSingleIdea(
                    "IDEA_MORBID",
                    TagAxis.Existential,
                    TagPole.Negative,
                    TagDegree.Dominant
                )
            );

            set.AddTrack(track);

            TrackNamingLexiconCatalog catalog =
                TrackNamingLexiconCatalogData
                    .CreateDefault();

            const int seed = 54321;

            TrackNamingPositionSelection primary =
                ReleaseNamingProjectionPipeline
                    .ResolveTrvePreferred(set);

            TrackNamingGrammarAnalysis primaryAnalysis =
                TrackNamingGrammarAnalyzer.Analyze(
                    primary
                );

            bool primaryRendered =
                TrackNamingGrammarRenderer.TryRender(
                    primaryAnalysis,
                    catalog,
                    seed,
                    out _
                );

            Assert.That(
                primaryRendered,
                Is.False
            );

            TrackNamingPositionSelection regular =
                ReleaseNamingProjectionPipeline
                    .ResolveRegularFallback(set);

            TrackNamingGrammarAnalysis regularAnalysis =
                TrackNamingGrammarAnalyzer.Analyze(
                    regular
                );

            bool regularRendered =
                TrackNamingGrammarRenderer.TryRender(
                    regularAnalysis,
                    catalog,
                    seed,
                    out string expectedFallbackTitle
                );

            Assert.That(
                regularRendered,
                Is.True
            );

            bool generated =
                ReleaseNamingGenerator.TryGenerate(
                    set,
                    catalog,
                    seed,
                    out string title
                );

            Assert.That(
                generated,
                Is.True
            );

            Assert.That(
                title,
                Is.EqualTo(expectedFallbackTitle)
            );
        }

        [Test]
        public void TryGenerate_EmptySet_ReturnsFalse()
        {
            RehearsalSet set =
                CreateSet();

            bool generated =
                ReleaseNamingGenerator.TryGenerate(
                    set,
                    out string title
                );

            Assert.That(
                generated,
                Is.False
            );

            Assert.That(
                title,
                Is.Empty
            );
        }

        private static RehearsalSet CreateSet()
        {
            return new RehearsalSet(
                "SET_RELEASE_NAME_TEST",
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