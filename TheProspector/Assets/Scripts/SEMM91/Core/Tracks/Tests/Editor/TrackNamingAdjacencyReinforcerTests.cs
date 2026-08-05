using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class
        TrackNamingAdjacencyReinforcerTests
    {
        [Test]
        public void ApplyAdjacencyMatches_AdjacentOverflow_AddsExactlyOneDegree()
        {
            TrackNamingSemanticPosition selected =
                CreatePosition(
                    "IDEA_SELECTED",
                    0,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            TrackNamingSemanticPosition overflow =
                CreatePosition(
                    "IDEA_OVERFLOW",
                    3,
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagDegree.Transgressive
                );

            TrackNamingPositionSelection result =
                TrackNamingOverflowReinforcer
                    .ApplyAdjacencyMatches(
                        CreateSelection(
                            new[] { selected },
                            new[] { overflow }
                        )
                    );

            Assert.That(
                result.SelectedOccurrences[0]
                    .MechanicalDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                result.SelectedOccurrences[0]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Dominant)
            );

            Assert.That(
                result.OverflowOccurrences,
                Is.Empty
            );
        }

        [Test]
        public void ApplyAdjacencyMatches_ReinforcementAtDegreeTwo_CapsAtThree()
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
                    .ApplyAdjacencyMatches(
                        CreateSelection(
                            new[] { selected },
                            new[] { overflow }
                        )
                    );

            Assert.That(
                result.SelectedOccurrences[0]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Transgressive)
            );
        }

        [Test]
        public void ApplyAdjacencyMatches_MultipleTargets_ReinforcesEarliestAdjacent()
        {
            // Raw is adjacent to both Profane and Malevolent.
            TrackNamingSemanticPosition profane =
                CreatePosition(
                    "IDEA_PROFANE",
                    0,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            TrackNamingSemanticPosition malevolent =
                CreatePosition(
                    "IDEA_MALEVOLENT",
                    1,
                    TagAxis.Physical,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            TrackNamingSemanticPosition rawOverflow =
                CreatePosition(
                    "IDEA_RAW",
                    3,
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagDegree.Dominant
                );

            TrackNamingPositionSelection result =
                TrackNamingOverflowReinforcer
                    .ApplyAdjacencyMatches(
                        CreateSelection(
                            new[]
                            {
                                profane,
                                malevolent
                            },
                            new[]
                            {
                                rawOverflow
                            }
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
        public void ApplyAdjacencyMatches_NonAdjacentOverflow_RemainsAvailable()
        {
            TrackNamingSemanticPosition sacred =
                CreatePosition(
                    "IDEA_SACRED",
                    0,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                );

            TrackNamingSemanticPosition rawOverflow =
                CreatePosition(
                    "IDEA_RAW",
                    3,
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            TrackNamingPositionSelection result =
                TrackNamingOverflowReinforcer
                    .ApplyAdjacencyMatches(
                        CreateSelection(
                            new[] { sacred },
                            new[] { rawOverflow }
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
                Is.EqualTo("IDEA_RAW")
            );
        }

        [Test]
        public void ApplyAdjacencyMatches_ExactMatch_IsNotTreatedAsAdjacency()
        {
            TrackNamingSemanticPosition selected =
                CreatePosition(
                    "IDEA_SELECTED",
                    0,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            TrackNamingSemanticPosition exactOverflow =
                CreatePosition(
                    "IDEA_OVERFLOW",
                    3,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            TrackNamingPositionSelection result =
                TrackNamingOverflowReinforcer
                    .ApplyAdjacencyMatches(
                        CreateSelection(
                            new[] { selected },
                            new[] { exactOverflow }
                        )
                    );

            Assert.That(
                result.SelectedOccurrences[0]
                    .NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                result.OverflowOccurrences.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void ApplyAdjacencyMatches_DoesNotMutateInputPosition()
        {
            TrackNamingSemanticPosition selected =
                CreatePosition(
                    "IDEA_SELECTED",
                    0,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            TrackNamingSemanticPosition overflow =
                CreatePosition(
                    "IDEA_OVERFLOW",
                    3,
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagDegree.Transgressive
                );

            TrackNamingPositionSelection input =
                CreateSelection(
                    new[] { selected },
                    new[] { overflow }
                );

            TrackNamingOverflowReinforcer
                .ApplyAdjacencyMatches(input);

            Assert.That(
                selected.MechanicalDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                selected.NamingEffectiveDegree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                input.OverflowOccurrences.Count,
                Is.EqualTo(1)
            );
        }

        private static TrackNamingPositionSelection
            CreateSelection(
                TrackNamingSemanticPosition[] selected,
                TrackNamingSemanticPosition[] overflow)
        {
            return new TrackNamingPositionSelection(
                selected,
                overflow,
                selected.Length
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