using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class TrackNamingPositionSelectorTests
    {
        [Test]
        public void Select_EmptyProjection_ReturnsEmptySelection()
        {
            TrackNamingPositionSelection selection =
                TrackNamingPositionSelector.Select(
                    System.Array.Empty<
                        TrackNamingSemanticPosition>()
                );

            Assert.That(
                selection.SelectedOccurrences,
                Is.Empty
            );

            Assert.That(
                selection.OverflowOccurrences,
                Is.Empty
            );

            Assert.That(
                selection.SelectedPositionCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void Select_FourSolitaryOccurrences_SelectsFirstThree()
        {
            TrackNamingSemanticPosition[] projection =
            {
                CreateSolitary("IDEA_0", 0),
                CreateSolitary("IDEA_1", 1),
                CreateSolitary("IDEA_2", 2),
                CreateSolitary("IDEA_3", 3)
            };

            TrackNamingPositionSelection selection =
                TrackNamingPositionSelector.Select(
                    projection
                );

            Assert.That(
                selection.SelectedPositionCount,
                Is.EqualTo(3)
            );

            Assert.That(
                selection.SelectedOccurrences.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                selection.OverflowOccurrences.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                selection.SelectedOccurrences[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_0")
            );

            Assert.That(
                selection.SelectedOccurrences[1]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_1")
            );

            Assert.That(
                selection.SelectedOccurrences[2]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_2")
            );

            Assert.That(
                selection.OverflowOccurrences[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_3")
            );
        }

        [Test]
        public void Select_PairAndTwoSolitaryIdeas_SelectsThreeOccurrences()
        {
            TrackNamingSemanticPosition[] projection =
            {
                CreatePairDominant(
                    "IDEA_PAIR",
                    0
                ),
                CreatePairSubmissive(
                    "IDEA_PAIR",
                    0
                ),
                CreateSolitary(
                    "IDEA_SURFACE",
                    1
                ),
                CreateSolitary(
                    "IDEA_OVERFLOW",
                    2
                )
            };

            TrackNamingPositionSelection selection =
                TrackNamingPositionSelector.Select(
                    projection
                );

            Assert.That(
                selection.SelectedPositionCount,
                Is.EqualTo(3)
            );

            Assert.That(
                selection.SelectedOccurrences.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                selection.OverflowOccurrences.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                selection.SelectedOccurrences[0].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairDominant
                )
            );

            Assert.That(
                selection.SelectedOccurrences[1].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairSubmissive
                )
            );

            Assert.That(
                selection.SelectedOccurrences[2]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_SURFACE")
            );

            Assert.That(
                selection.OverflowOccurrences[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_OVERFLOW")
            );
        }

        [Test]
        public void Select_TwoPairs_SelectsPrimaryPairAndSecondaryDominant()
        {
            TrackNamingSemanticPosition[] projection =
            {
                CreatePairDominant(
                    "IDEA_PRIMARY_PAIR",
                    0
                ),
                CreatePairSubmissive(
                    "IDEA_PRIMARY_PAIR",
                    0
                ),
                CreatePairDominant(
                    "IDEA_SECONDARY_PAIR",
                    1
                ),
                CreatePairSubmissive(
                    "IDEA_SECONDARY_PAIR",
                    1
                )
            };

            TrackNamingPositionSelection selection =
                TrackNamingPositionSelector.Select(
                    projection
                );

            Assert.That(
                selection.SelectedOccurrences.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                selection.OverflowOccurrences.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                selection.SelectedOccurrences[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_PRIMARY_PAIR")
            );

            Assert.That(
                selection.SelectedOccurrences[0].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairDominant
                )
            );

            Assert.That(
                selection.SelectedOccurrences[1]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_PRIMARY_PAIR")
            );

            Assert.That(
                selection.SelectedOccurrences[1].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairSubmissive
                )
            );

            Assert.That(
                selection.SelectedOccurrences[2]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_SECONDARY_PAIR")
            );

            Assert.That(
                selection.SelectedOccurrences[2].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairDominant
                )
            );

            Assert.That(
                selection.OverflowOccurrences[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_SECONDARY_PAIR")
            );

            Assert.That(
                selection.OverflowOccurrences[0].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairSubmissive
                )
            );
        }

        [Test]
        public void Select_PairBeginningAtThirdOccurrence_SelectsOnlyDominantMember()
        {
            TrackNamingSemanticPosition[] projection =
            {
                CreateSolitary(
                    "IDEA_0",
                    0
                ),
                CreateSolitary(
                    "IDEA_1",
                    1
                ),
                CreatePairDominant(
                    "IDEA_PAIR",
                    2
                ),
                CreatePairSubmissive(
                    "IDEA_PAIR",
                    2
                )
            };

            TrackNamingPositionSelection selection =
                TrackNamingPositionSelector.Select(
                    projection
                );

            Assert.That(
                selection.SelectedOccurrences.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                selection.SelectedOccurrences[2].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairDominant
                )
            );

            Assert.That(
                selection.OverflowOccurrences.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                selection.OverflowOccurrences[0].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairSubmissive
                )
            );

            Assert.That(
                selection.SelectedOccurrences[2]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_PAIR")
            );

            Assert.That(
                selection.OverflowOccurrences[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_PAIR")
            );
        }

        private static TrackNamingSemanticPosition
            CreateSolitary(
                string ideaId,
                int ideaIndex)
        {
            return new TrackNamingSemanticPosition(
                ideaId,
                "ASPECT_TEST",
                ideaIndex,
                TrackNamingSemanticRole.Solitary,
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Dominant
            );
        }

        private static TrackNamingSemanticPosition
            CreatePairDominant(
                string ideaId,
                int ideaIndex)
        {
            return new TrackNamingSemanticPosition(
                ideaId,
                "ASPECT_TEST",
                ideaIndex,
                TrackNamingSemanticRole.PairDominant,
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Dominant
            );
        }

        private static TrackNamingSemanticPosition
            CreatePairSubmissive(
                string ideaId,
                int ideaIndex)
        {
            return new TrackNamingSemanticPosition(
                ideaId,
                "ASPECT_TEST",
                ideaIndex,
                TrackNamingSemanticRole.PairSubmissive,
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Weak
            );
        }
    }
}