using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class TrackNamingLexiconCatalogTests
    {
        [Test]
        public void CreateDefault_ContainsAllFourteenTagLexicons()
        {
            TrackNamingLexiconCatalog catalog =
                TrackNamingLexiconCatalogData.CreateDefault();

            Assert.That(catalog.Count, Is.EqualTo(14));
        }

        [Test]
        public void CreateDefault_ResolvesEveryAxisAndPole()
        {
            TrackNamingLexiconCatalog catalog =
                TrackNamingLexiconCatalogData.CreateDefault();

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Emotional,
                    TagPole.Negative
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Emotional,
                    TagPole.Positive
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Expressive,
                    TagPole.Negative
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Expressive,
                    TagPole.Positive
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Temporal,
                    TagPole.Negative
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Temporal,
                    TagPole.Positive
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Physical,
                    TagPole.Negative
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Physical,
                    TagPole.Positive
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Existential,
                    TagPole.Negative
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Existential,
                    TagPole.Positive
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Interpretive,
                    TagPole.Negative
                ),
                Is.Not.Null
            );

            Assert.That(
                catalog.GetRequired(
                    TagAxis.Interpretive,
                    TagPole.Positive
                ),
                Is.Not.Null
            );
        }

        [Test]
        public void CreateDefault_EveryTagContainsFourCompleteDegreeEntries()
        {
            TrackNamingLexiconCatalog catalog =
                TrackNamingLexiconCatalogData.CreateDefault();

            TagAxis[] axes =
            {
                TagAxis.Symbolic,
                TagAxis.Emotional,
                TagAxis.Expressive,
                TagAxis.Temporal,
                TagAxis.Physical,
                TagAxis.Existential,
                TagAxis.Interpretive
            };

            TagPole[] poles =
            {
                TagPole.Negative,
                TagPole.Positive
            };

            foreach (TagAxis axis in axes)
            {
                foreach (TagPole pole in poles)
                {
                    TrackNamingTagLexicon lexicon =
                        catalog.GetRequired(axis, pole);

                    Assert.That(
                        lexicon.DegreeEntries.Count,
                        Is.EqualTo(4)
                    );

                    foreach (
                        TrackNamingDegreeLexicon degree
                        in lexicon.DegreeEntries)
                    {
                        Assert.That(
                            degree.SurfaceModifiers.Count,
                            Is.EqualTo(8)
                        );

                        Assert.That(
                            degree.HeadNouns.Count,
                            Is.EqualTo(8)
                        );

                        Assert.That(
                            degree.DominantActions.Count,
                            Is.EqualTo(8)
                        );

                        Assert.That(
                            degree.SubmissivePhrases.Count,
                            Is.EqualTo(8)
                        );
                    }
                }
            }
        }

        [Test]
        public void CreateDefault_PreservesCanonicalProfaneAndMeaningEntries()
        {
            TrackNamingLexiconCatalog catalog =
                TrackNamingLexiconCatalogData.CreateDefault();

            TrackNamingTagLexicon profane =
                catalog.GetRequired(
                    TagAxis.Symbolic,
                    TagPole.Negative
                );

            Assert.That(
                profane.GetDegreeEntry(
                    TagDegree.Transgressive
                ).DegreeLabel,
                Is.EqualTo("Sacrilegious")
            );

            Assert.That(
                profane.GetDegreeEntry(
                    TagDegree.Transgressive
                ).HeadNouns[0],
                Is.EqualTo("sacrilege")
            );

            TrackNamingTagLexicon meaning =
                catalog.GetRequired(
                    TagAxis.Interpretive,
                    TagPole.Positive
                );

            Assert.That(
                meaning.GetDegreeEntry(
                    TagDegree.Transgressive
                ).DegreeLabel,
                Is.EqualTo("Transcendent")
            );

            Assert.That(
                meaning.GetDegreeEntry(
                    TagDegree.Transgressive
                ).DominantActions[2],
                Is.EqualTo("naming the world")
            );
        }

        [Test]
        public void Constructor_DuplicateTagIdentity_Throws()
        {
            TrackNamingTagLexicon duplicate =
                CreateMinimalLexicon(
                    TagAxis.Symbolic,
                    TagPole.Negative
                );

            Assert.Throws<ArgumentException>(
                () => new TrackNamingLexiconCatalog(
                    new[]
                    {
                        duplicate,
                        duplicate
                    }
                )
            );
        }

        private static TrackNamingTagLexicon
            CreateMinimalLexicon(
                TagAxis axis,
                TagPole pole)
        {
            return new TrackNamingTagLexicon(
                axis,
                pole,
                new[]
                {
                    CreateDegree(
                        TagDegree.Neutral,
                        "Degree 0"
                    ),
                    CreateDegree(
                        TagDegree.Weak,
                        "Degree 1"
                    ),
                    CreateDegree(
                        TagDegree.Dominant,
                        "Degree 2"
                    ),
                    CreateDegree(
                        TagDegree.Transgressive,
                        "Degree 3"
                    )
                },
                new TrackNamingContextFragmentGroup[0]
            );
        }

        private static TrackNamingDegreeLexicon CreateDegree(
            TagDegree degree,
            string label)
        {
            return new TrackNamingDegreeLexicon(
                degree,
                label,
                new[] { "modifier" },
                new[] { "noun" },
                new[] { "action" },
                new[] { "object" }
            );
        }
    }
}
