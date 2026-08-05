using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class
        TrackNamingLexicalEntryTests
    {
        [Test]
        public void Constructor_ExplicitFamily_PreservesMetadata()
        {
            TrackNamingLexicalEntry entry =
                new TrackNamingLexicalEntry(
                    "  hateful  ",
                    "  Hate Family  "
                );

            Assert.That(
                entry.Text,
                Is.EqualTo("hateful")
            );

            Assert.That(
                entry.FamilyId,
                Is.EqualTo("hate-family")
            );

            Assert.That(
                entry.HasAuthoredFamilyId,
                Is.True
            );
        }

        [Test]
        public void Constructor_NoFamily_UsesExactTextFallback()
        {
            TrackNamingLexicalEntry first =
                new TrackNamingLexicalEntry(
                    "the altar"
                );

            TrackNamingLexicalEntry second =
                new TrackNamingLexicalEntry(
                    "  THE   ALTAR  "
                );

            Assert.That(
                first.FamilyId,
                Is.EqualTo("exact:the-altar")
            );

            Assert.That(
                second.FamilyId,
                Is.EqualTo(first.FamilyId)
            );

            Assert.That(
                first.HasAuthoredFamilyId,
                Is.False
            );
        }

        [Test]
        public void DegreeLexicon_StringConstructor_RemainsBackwardCompatible()
        {
            TrackNamingDegreeLexicon degree =
                new TrackNamingDegreeLexicon(
                    TagDegree.Dominant,
                    "Dominant",
                    new[] { "hateful" },
                    new[] { "hatred" },
                    new[] { "harming" },
                    new[] { "the rival" }
                );

            Assert.That(
                degree.SurfaceModifiers[0],
                Is.EqualTo("hateful")
            );

            Assert.That(
                degree.SurfaceModifierEntries[0]
                    .Text,
                Is.EqualTo("hateful")
            );

            Assert.That(
                degree.SurfaceModifierEntries[0]
                    .HasAuthoredFamilyId,
                Is.False
            );
        }

        [Test]
        public void DegreeLexicon_EntryConstructor_PreservesAuthoredFamily()
        {
            TrackNamingDegreeLexicon degree =
                new TrackNamingDegreeLexicon(
                    TagDegree.Dominant,
                    "Dominant",
                    new[]
                    {
                        new TrackNamingLexicalEntry(
                            "hateful",
                            "hate"
                        )
                    },
                    new[]
                    {
                        new TrackNamingLexicalEntry(
                            "hatred",
                            "hate"
                        )
                    },
                    new[]
                    {
                        new TrackNamingLexicalEntry(
                            "harming",
                            "harm"
                        )
                    },
                    new[]
                    {
                        new TrackNamingLexicalEntry(
                            "the rival",
                            "rival"
                        )
                    }
                );

            Assert.That(
                degree.SurfaceModifierEntries[0]
                    .FamilyId,
                Is.EqualTo("hate")
            );

            Assert.That(
                degree.HeadNounEntries[0]
                    .FamilyId,
                Is.EqualTo("hate")
            );

            Assert.That(
                degree.SurfaceModifiers[0],
                Is.EqualTo("hateful")
            );

            Assert.That(
                degree.HeadNouns[0],
                Is.EqualTo("hatred")
            );
        }

        [Test]
        public void SelectEntry_ReturnsMetadataWhileSelectReturnsVisibleText()
        {
            TrackNamingLexiconCatalog catalog =
                CreateCatalog();

            TrackNamingSemanticPosition position =
                new TrackNamingSemanticPosition(
                    "IDEA_TEST",
                    "ASPECT_TEST",
                    0,
                    TrackNamingSemanticRole.Solitary,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                );

            TrackNamingLexicalEntry entry =
                TrackNamingLexicalSelector.SelectEntry(
                    catalog,
                    position,
                    TrackNamingLexicalCategory.HeadNoun
                );

            string text =
                TrackNamingLexicalSelector.Select(
                    catalog,
                    position,
                    TrackNamingLexicalCategory.HeadNoun
                );

            Assert.That(
                entry.Text,
                Is.EqualTo("hatred")
            );

            Assert.That(
                entry.FamilyId,
                Is.EqualTo("hate")
            );

            Assert.That(
                entry.HasAuthoredFamilyId,
                Is.True
            );

            Assert.That(
                text,
                Is.EqualTo(entry.Text)
            );
        }

        private static TrackNamingLexiconCatalog
            CreateCatalog()
        {
            TrackNamingDegreeLexicon[] degrees =
            {
                CreateDegree(TagDegree.Neutral),
                CreateDegree(TagDegree.Weak),
                CreateDegree(TagDegree.Dominant),
                CreateDegree(TagDegree.Transgressive)
            };

            return new TrackNamingLexiconCatalog(
                new[]
                {
                    new TrackNamingTagLexicon(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        degrees,
                        new TrackNamingContextFragmentGroup[0]
                    )
                }
            );
        }

        private static TrackNamingDegreeLexicon
            CreateDegree(TagDegree degree)
        {
            return new TrackNamingDegreeLexicon(
                degree,
                degree.ToString(),
                new[]
                {
                    new TrackNamingLexicalEntry(
                        "hateful",
                        "hate"
                    )
                },
                new[]
                {
                    new TrackNamingLexicalEntry(
                        "hatred",
                        "hate"
                    )
                },
                new[]
                {
                    new TrackNamingLexicalEntry(
                        "harming",
                        "harm"
                    )
                },
                new[]
                {
                    new TrackNamingLexicalEntry(
                        "the rival",
                        "rival"
                    )
                }
            );
        }
    }
}