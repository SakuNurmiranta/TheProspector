using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class
        TrackNamingLexicalSelectorTests
    {
        [Test]
        public void Select_SameInput_ReturnsSamePhrase()
        {
            TrackNamingLexiconCatalog catalog =
                CreateCatalog();

            TrackNamingSemanticPosition position =
                CreatePosition(
                    TagDegree.Dominant
                );

            string first =
                TrackNamingLexicalSelector.Select(
                    catalog,
                    position,
                    TrackNamingLexicalCategory.HeadNoun
                );

            string second =
                TrackNamingLexicalSelector.Select(
                    catalog,
                    position,
                    TrackNamingLexicalCategory.HeadNoun
                );

            Assert.That(
                second,
                Is.EqualTo(first)
            );
        }

        [Test]
        public void Select_UsesNamingEffectiveDegree()
        {
            TrackNamingLexiconCatalog catalog =
                CreateCatalog();

            TrackNamingSemanticPosition mechanicalWeak =
                CreatePosition(
                    TagDegree.Weak
                );

            TrackNamingSemanticPosition reinforced =
                mechanicalWeak.WithNamingEffectiveDegree(
                    TagDegree.Transgressive
                );

            string selected =
                TrackNamingLexicalSelector.Select(
                    catalog,
                    reinforced,
                    TrackNamingLexicalCategory.HeadNoun
                );

            Assert.That(
                selected.StartsWith(
                    "transgressive-noun-"
                ),
                Is.True
            );

            Assert.That(
                reinforced.MechanicalDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                reinforced.NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Transgressive)
            );
        }

        [Test]
        public void Select_Category_UsesCorrectAuthoredColumn()
        {
            TrackNamingLexiconCatalog catalog =
                CreateCatalog();

            TrackNamingSemanticPosition position =
                CreatePosition(
                    TagDegree.Dominant
                );

            string modifier =
                TrackNamingLexicalSelector.Select(
                    catalog,
                    position,
                    TrackNamingLexicalCategory
                        .SurfaceModifier
                );

            string noun =
                TrackNamingLexicalSelector.Select(
                    catalog,
                    position,
                    TrackNamingLexicalCategory
                        .HeadNoun
                );

            string action =
                TrackNamingLexicalSelector.Select(
                    catalog,
                    position,
                    TrackNamingLexicalCategory
                        .DominantAction
                );

            string submissive =
                TrackNamingLexicalSelector.Select(
                    catalog,
                    position,
                    TrackNamingLexicalCategory
                        .SubmissivePhrase
                );

            Assert.That(
                modifier.StartsWith(
                    "dominant-modifier-"
                ),
                Is.True
            );

            Assert.That(
                noun.StartsWith(
                    "dominant-noun-"
                ),
                Is.True
            );

            Assert.That(
                action.StartsWith(
                    "dominant-action-"
                ),
                Is.True
            );

            Assert.That(
                submissive.StartsWith(
                    "dominant-submissive-"
                ),
                Is.True
            );
        }

        [Test]
        public void Select_DifferentVariationIndex_SelectsDifferentAlternative()
        {
            TrackNamingLexiconCatalog catalog =
                CreateCatalog();

            TrackNamingSemanticPosition position =
                CreatePosition(
                    TagDegree.Dominant
                );

            string variationZero =
                TrackNamingLexicalSelector.Select(
                    catalog,
                    position,
                    TrackNamingLexicalCategory.HeadNoun,
                    0
                );

            string variationOne =
                TrackNamingLexicalSelector.Select(
                    catalog,
                    position,
                    TrackNamingLexicalCategory.HeadNoun,
                    1
                );

            Assert.That(
                variationOne,
                Is.Not.EqualTo(variationZero)
            );
        }

        [Test]
        public void Select_NegativeVariationIndex_Throws()
        {
            TrackNamingLexiconCatalog catalog =
                CreateCatalog();

            TrackNamingSemanticPosition position =
                CreatePosition(
                    TagDegree.Dominant
                );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    TrackNamingLexicalSelector.Select(
                        catalog,
                        position,
                        TrackNamingLexicalCategory.HeadNoun,
                        -1
                    )
            );
        }

        private static TrackNamingSemanticPosition
            CreatePosition(
                TagDegree mechanicalDegree)
        {
            return new TrackNamingSemanticPosition(
                "IDEA_TEST",
                "ASPECT_TEST",
                1,
                TrackNamingSemanticRole.Solitary,
                TagAxis.Symbolic,
                TagPole.Negative,
                mechanicalDegree
            );
        }

        private static TrackNamingLexiconCatalog
            CreateCatalog()
        {
            TrackNamingDegreeLexicon[] degrees =
            {
                CreateDegree(
                    TagDegree.Neutral,
                    "neutral"
                ),
                CreateDegree(
                    TagDegree.Weak,
                    "weak"
                ),
                CreateDegree(
                    TagDegree.Dominant,
                    "dominant"
                ),
                CreateDegree(
                    TagDegree.Transgressive,
                    "transgressive"
                )
            };

            TrackNamingTagLexicon profane =
                new TrackNamingTagLexicon(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    degrees,
                    new TrackNamingContextFragmentGroup[0]
                );

            return new TrackNamingLexiconCatalog(
                new[]
                {
                    profane
                }
            );
        }

        private static TrackNamingDegreeLexicon
            CreateDegree(
                TagDegree degree,
                string prefix)
        {
            return new TrackNamingDegreeLexicon(
                degree,
                prefix,
                CreatePhrases(
                    prefix + "-modifier-"
                ),
                CreatePhrases(
                    prefix + "-noun-"
                ),
                CreatePhrases(
                    prefix + "-action-"
                ),
                CreatePhrases(
                    prefix + "-submissive-"
                )
            );
        }

        private static string[] CreatePhrases(
            string prefix)
        {
            return new[]
            {
                prefix + "0",
                prefix + "1",
                prefix + "2",
                prefix + "3"
            };
        }
    }
}