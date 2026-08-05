using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class
        TrackNamingGrammarRendererTests
    {
        [Test]
        public void TryRender_EmptyAnalysis_ReturnsFalse()
        {
            bool rendered =
                TrackNamingGrammarRenderer.TryRender(
                    Analyze(),
                    CreateCatalog(),
                    0,
                    out string title
                );

            Assert.That(rendered, Is.False);
            Assert.That(title, Is.EqualTo(string.Empty));
        }

        [Test]
        public void TryRender_OneAccumulative_UsesModifierAndNoun()
        {
            TrackNamingGrammarAnalysis analysis =
                Analyze(
                    CreatePosition(
                        "IDEA_RAW",
                        0,
                        TrackNamingSemanticRole.Solitary,
                        TagAxis.Expressive,
                        TagPole.Negative
                    )
                );

            bool rendered =
                TrackNamingGrammarRenderer.TryRender(
                    analysis,
                    CreateCatalog(),
                    0,
                    out string title
                );

            Assert.That(rendered, Is.True);

            Assert.That(
                title,
                Is.EqualTo("Primitive Instinct")
            );
        }

        [Test]
        public void TryRender_TwoAccumulative_UsesSecondModifierAndFirstNoun()
        {
            TrackNamingGrammarAnalysis analysis =
                Analyze(
                    CreatePosition(
                        "IDEA_MEANING",
                        0,
                        TrackNamingSemanticRole.Solitary,
                        TagAxis.Interpretive,
                        TagPole.Positive
                    ),
                    CreatePosition(
                        "IDEA_VOID",
                        1,
                        TrackNamingSemanticRole.Solitary,
                        TagAxis.Interpretive,
                        TagPole.Negative
                    )
                );

            bool rendered =
                TrackNamingGrammarRenderer.TryRender(
                    analysis,
                    CreateCatalog(),
                    0,
                    out string title
                );

            Assert.That(rendered, Is.True);

            Assert.That(
                title,
                Is.EqualTo("Bleak Purpose")
            );
        }

        [Test]
        public void TryRender_Pair_UsesDominantActionAndSubmissivePhrase()
        {
            TrackNamingGrammarAnalysis analysis =
                Analyze(
                    CreatePosition(
                        "IDEA_PAIR",
                        0,
                        TrackNamingSemanticRole
                            .PairDominant,
                        TagAxis.Interpretive,
                        TagPole.Positive
                    ),
                    CreatePosition(
                        "IDEA_PAIR",
                        0,
                        TrackNamingSemanticRole
                            .PairSubmissive,
                        TagAxis.Interpretive,
                        TagPole.Negative
                    )
                );

            bool rendered =
                TrackNamingGrammarRenderer.TryRender(
                    analysis,
                    CreateCatalog(),
                    0,
                    out string title
                );

            Assert.That(rendered, Is.True);

            Assert.That(
                title,
                Is.EqualTo("Naming the Abyss")
            );
        }

        [Test]
        public void TryRender_PairWithSurface_PrefixesSurfaceModifier()
        {
            TrackNamingGrammarAnalysis analysis =
                Analyze(
                    CreatePosition(
                        "IDEA_PAIR",
                        0,
                        TrackNamingSemanticRole
                            .PairDominant,
                        TagAxis.Interpretive,
                        TagPole.Positive
                    ),
                    CreatePosition(
                        "IDEA_PAIR",
                        0,
                        TrackNamingSemanticRole
                            .PairSubmissive,
                        TagAxis.Interpretive,
                        TagPole.Negative
                    ),
                    CreatePosition(
                        "IDEA_SURFACE",
                        1,
                        TrackNamingSemanticRole.Solitary,
                        TagAxis.Emotional,
                        TagPole.Positive
                    )
                );

            bool rendered =
                TrackNamingGrammarRenderer.TryRender(
                    analysis,
                    CreateCatalog(),
                    0,
                    out string title
                );

            Assert.That(rendered, Is.True);

            Assert.That(
                title,
                Is.EqualTo(
                    "Fervent Naming the Abyss"
                )
            );
        }

        [Test]
        public void TryRender_ThreeAccumulative_ReturnsFalseForNow()
        {
            TrackNamingGrammarAnalysis analysis =
                Analyze(
                    CreatePosition(
                        "IDEA_0",
                        0,
                        TrackNamingSemanticRole.Solitary,
                        TagAxis.Expressive,
                        TagPole.Negative
                    ),
                    CreatePosition(
                        "IDEA_1",
                        1,
                        TrackNamingSemanticRole.Solitary,
                        TagAxis.Interpretive,
                        TagPole.Positive
                    ),
                    CreatePosition(
                        "IDEA_2",
                        2,
                        TrackNamingSemanticRole.Solitary,
                        TagAxis.Emotional,
                        TagPole.Positive
                    )
                );

            bool rendered =
                TrackNamingGrammarRenderer.TryRender(
                    analysis,
                    CreateCatalog(),
                    0,
                    out string title
                );

            Assert.That(rendered, Is.False);
            Assert.That(title, Is.EqualTo(string.Empty));
        }

        [Test]
        public void TryRender_SameSeed_ReturnsSameTitle()
        {
            TrackNamingGrammarAnalysis analysis =
                Analyze(
                    CreatePosition(
                        "IDEA_PAIR",
                        0,
                        TrackNamingSemanticRole
                            .PairDominant,
                        TagAxis.Interpretive,
                        TagPole.Positive
                    ),
                    CreatePosition(
                        "IDEA_PAIR",
                        0,
                        TrackNamingSemanticRole
                            .PairSubmissive,
                        TagAxis.Interpretive,
                        TagPole.Negative
                    )
                );

            TrackNamingLexiconCatalog catalog =
                CreateCatalog();

            TrackNamingGrammarRenderer.TryRender(
                analysis,
                catalog,
                17,
                out string first
            );

            TrackNamingGrammarRenderer.TryRender(
                analysis,
                catalog,
                17,
                out string second
            );

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void TitleCase_LowercasesInteriorFunctionWordsAndHandlesHyphens()
        {
            string result =
                TrackNamingTitleCase.Apply(
                    "all-forgiving purpose " +
                    "within the lightless chamber"
                );

            Assert.That(
                result,
                Is.EqualTo(
                    "All-Forgiving Purpose " +
                    "within the Lightless Chamber"
                )
            );
        }

        private static TrackNamingGrammarAnalysis
            Analyze(
                params TrackNamingSemanticPosition[]
                    positions)
        {
            TrackNamingPositionSelection selection =
                new TrackNamingPositionSelection(
                    positions,
                    Array.Empty<
                        TrackNamingSemanticPosition>(),
                    positions.Length
                );

            return TrackNamingGrammarAnalyzer
                .Analyze(selection);
        }

        private static TrackNamingSemanticPosition
            CreatePosition(
                string ideaId,
                int ideaIndex,
                TrackNamingSemanticRole role,
                TagAxis axis,
                TagPole pole)
        {
            return new TrackNamingSemanticPosition(
                ideaId,
                "ASPECT_TEST",
                ideaIndex,
                role,
                axis,
                pole,
                TagDegree.Dominant
            );
        }

        private static TrackNamingLexiconCatalog
            CreateCatalog()
        {
            return new TrackNamingLexiconCatalog(
                new[]
                {
                    CreateTag(
                        TagAxis.Expressive,
                        TagPole.Negative,
                        "primitive",
                        "instinct",
                        "shattering",
                        "the broken form"
                    ),
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