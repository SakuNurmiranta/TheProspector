using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class TrackNamingLexiconTests
    {
        [Test]
        public void DegreeLexicon_PreservesAllAuthoredColumns()
        {
            TrackNamingDegreeLexicon entry =
                new TrackNamingDegreeLexicon(
                    TagDegree.Dominant,
                    "Desecrating",
                    new[]
                    {
                        "desecrating",
                        "defiled"
                    },
                    new[]
                    {
                        "desecration",
                        "defilement"
                    },
                    new[]
                    {
                        "defiling",
                        "fouling"
                    },
                    new[]
                    {
                        "desecrated ground",
                        "the ruined taboo"
                    }
                );

            Assert.That(
                entry.Degree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                entry.DegreeLabel,
                Is.EqualTo("Desecrating")
            );

            Assert.That(
                entry.SurfaceModifiers[0],
                Is.EqualTo("desecrating")
            );

            Assert.That(
                entry.HeadNouns[0],
                Is.EqualTo("desecration")
            );

            Assert.That(
                entry.DominantActions[0],
                Is.EqualTo("defiling")
            );

            Assert.That(
                entry.SubmissivePhrases[0],
                Is.EqualTo("desecrated ground")
            );
        }

        [Test]
        public void ContextGroup_PreservesAuthoredLabelAndFragments()
        {
            TrackNamingContextFragmentGroup group =
                new TrackNamingContextFragmentGroup(
                    "Substances and instruments",
                    new[]
                    {
                        "dirt",
                        "spit",
                        "ash"
                    }
                );

            Assert.That(
                group.GroupLabel,
                Is.EqualTo(
                    "Substances and instruments"
                )
            );

            Assert.That(
                group.Fragments.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                group.Fragments[1],
                Is.EqualTo("spit")
            );
        }

        [Test]
        public void TagLexicon_ResolvesEveryDegreeByEnum()
        {
            TrackNamingTagLexicon lexicon =
                CreateCompleteProfaneLexicon();

            Assert.That(
                lexicon.Axis,
                Is.EqualTo(TagAxis.Symbolic)
            );

            Assert.That(
                lexicon.Pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                lexicon.GetDegreeEntry(
                    TagDegree.Neutral
                ).DegreeLabel,
                Is.EqualTo("Irreverent")
            );

            Assert.That(
                lexicon.GetDegreeEntry(
                    TagDegree.Weak
                ).DegreeLabel,
                Is.EqualTo("Defiant")
            );

            Assert.That(
                lexicon.GetDegreeEntry(
                    TagDegree.Dominant
                ).DegreeLabel,
                Is.EqualTo("Desecrating")
            );

            Assert.That(
                lexicon.GetDegreeEntry(
                    TagDegree.Transgressive
                ).DegreeLabel,
                Is.EqualTo("Sacrilegious")
            );
        }

        [Test]
        public void TagLexicon_MissingDegree_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new TrackNamingTagLexicon(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    new[]
                    {
                        CreateDegree(
                            TagDegree.Neutral,
                            "Irreverent"
                        ),
                        CreateDegree(
                            TagDegree.Weak,
                            "Defiant"
                        ),
                        CreateDegree(
                            TagDegree.Dominant,
                            "Desecrating"
                        )
                    },
                    new TrackNamingContextFragmentGroup[0]
                )
            );
        }

        [Test]
        public void TagLexicon_DuplicateDegree_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new TrackNamingTagLexicon(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    new[]
                    {
                        CreateDegree(
                            TagDegree.Neutral,
                            "Irreverent"
                        ),
                        CreateDegree(
                            TagDegree.Weak,
                            "Defiant"
                        ),
                        CreateDegree(
                            TagDegree.Dominant,
                            "Desecrating"
                        ),
                        CreateDegree(
                            TagDegree.Dominant,
                            "Duplicate"
                        )
                    },
                    new TrackNamingContextFragmentGroup[0]
                )
            );
        }

        private static TrackNamingTagLexicon
            CreateCompleteProfaneLexicon()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Symbolic,
                TagPole.Negative,
                new[]
                {
                    CreateDegree(
                        TagDegree.Neutral,
                        "Irreverent"
                    ),
                    CreateDegree(
                        TagDegree.Weak,
                        "Defiant"
                    ),
                    CreateDegree(
                        TagDegree.Dominant,
                        "Desecrating"
                    ),
                    CreateDegree(
                        TagDegree.Transgressive,
                        "Sacrilegious"
                    )
                },
                new[]
                {
                    new TrackNamingContextFragmentGroup(
                        "Consequences",
                        new[]
                        {
                            "left unclean",
                            "rendered profane"
                        }
                    )
                }
            );
        }

        private static TrackNamingDegreeLexicon
            CreateDegree(
                TagDegree degree,
                string label)
        {
            return new TrackNamingDegreeLexicon(
                degree,
                label,
                new[] { "modifier" },
                new[] { "noun" },
                new[] { "acting upon" },
                new[] { "the object" }
            );
        }
    }
}