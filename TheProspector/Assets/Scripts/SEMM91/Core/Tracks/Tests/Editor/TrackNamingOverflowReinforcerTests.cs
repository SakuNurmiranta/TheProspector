using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class
        TrackNamingOverflowReinforcerTests
    {
        [Test]
        public void ApplyExactMatches_MatchingOverflow_AddsDegree()
        {
            TrackNamingSemanticPosition selected =
                CreatePosition(
                    "IDEA_SELECTED",
                    0,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                );

            TrackNamingSemanticPosition overflow =
                CreatePosition(
                    "IDEA_OVERFLOW",
                    3,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            TrackNamingPositionSelection input =
                CreateSelection(
                    new[] { selected },
                    new[] { overflow },
                    1
                );

            TrackNamingPositionSelection result =
                TrackNamingOverflowReinforcer
                    .ApplyExactMatches(input);

            Assert.That(
                result.SelectedOccurrences.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.SelectedOccurrences[0]
                    .MechanicalDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                result.SelectedOccurrences[0]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Transgressive)
            );

            Assert.That(
                result.OverflowOccurrences,
                Is.Empty
            );
        }

        [Test]
        public void ApplyExactMatches_ReinforcementAboveThree_IsCapped()
        {
            TrackNamingSemanticPosition selected =
                CreatePosition(
                    "IDEA_SELECTED",
                    0,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                );

            TrackNamingSemanticPosition overflow =
                CreatePosition(
                    "IDEA_OVERFLOW",
                    3,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                );

            TrackNamingPositionSelection result =
                TrackNamingOverflowReinforcer
                    .ApplyExactMatches(
                        CreateSelection(
                            new[] { selected },
                            new[] { overflow },
                            1
                        )
                    );

            Assert.That(
                result.SelectedOccurrences[0]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Transgressive)
            );

            Assert.That(
                result.SelectedOccurrences[0]
                    .MechanicalDegree,
                Is.EqualTo(TagDegree.Dominant)
            );
        }

        [Test]
        public void ApplyExactMatches_NonMatchingOverflow_RemainsAvailable()
        {
            TrackNamingSemanticPosition selected =
                CreatePosition(
                    "IDEA_SELECTED",
                    0,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                );

            TrackNamingSemanticPosition overflow =
                CreatePosition(
                    "IDEA_OVERFLOW",
                    3,
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            TrackNamingPositionSelection result =
                TrackNamingOverflowReinforcer
                    .ApplyExactMatches(
                        CreateSelection(
                            new[] { selected },
                            new[] { overflow },
                            1
                        )
                    );

            Assert.That(
                result.SelectedOccurrences[0]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                result.OverflowOccurrences.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.OverflowOccurrences[0]
                    .SourceIdeaId,
                Is.EqualTo("IDEA_OVERFLOW")
            );
        }

        [Test]
        public void ApplyExactMatches_DuplicateSelectedTags_ReinforcesEarliest()
        {
            TrackNamingSemanticPosition first =
                CreatePosition(
                    "IDEA_FIRST",
                    0,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                );

            TrackNamingSemanticPosition second =
                CreatePosition(
                    "IDEA_SECOND",
                    1,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                );

            TrackNamingSemanticPosition overflow =
                CreatePosition(
                    "IDEA_OVERFLOW",
                    3,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                );

            TrackNamingPositionSelection result =
                TrackNamingOverflowReinforcer
                    .ApplyExactMatches(
                        CreateSelection(
                            new[] { first, second },
                            new[] { overflow },
                            2
                        )
                    );

            Assert.That(
                result.SelectedOccurrences[0]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                result.SelectedOccurrences[1]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Weak)
            );
        }

        [Test]
        public void ApplyExactMatches_DoesNotMutateInputPositions()
        {
            TrackNamingSemanticPosition selected =
                CreatePosition(
                    "IDEA_SELECTED",
                    0,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                );

            TrackNamingSemanticPosition overflow =
                CreatePosition(
                    "IDEA_OVERFLOW",
                    3,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            TrackNamingPositionSelection input =
                CreateSelection(
                    new[] { selected },
                    new[] { overflow },
                    1
                );

            TrackNamingOverflowReinforcer
                .ApplyExactMatches(input);

            Assert.That(
                selected.MechanicalDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                selected.NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                input.OverflowOccurrences.Count,
                Is.EqualTo(1)
            );
        }

        private static TrackNamingPositionSelection
            CreateSelection(
                TrackNamingSemanticPosition[] selected,
                TrackNamingSemanticPosition[] overflow,
                int selectedPositionCount)
        {
            return new TrackNamingPositionSelection(
                selected,
                overflow,
                selectedPositionCount
            );
        }

        private static TrackNamingSemanticPosition
            CreatePosition(
                string ideaId,
                int ideaIndex,
                TagAxis axis,
                TagPole pole,
                TagDegree degree)
        {
            return new TrackNamingSemanticPosition(
                ideaId,
                "ASPECT_TEST",
                ideaIndex,
                TrackNamingSemanticRole.Solitary,
                axis,
                pole,
                degree
            );
        }
    }
}