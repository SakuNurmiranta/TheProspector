using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class
        TrackNamingGrammarAnalyzerTests
    {
        [Test]
        public void Analyze_EmptySelection_ReturnsEmpty()
        {
            TrackNamingGrammarAnalysis result =
                Analyze();

            Assert.That(
                result.Shape,
                Is.EqualTo(
                    TrackNamingGrammarShape.Empty
                )
            );

            Assert.That(
                result.AccumulativePositions,
                Is.Empty
            );

            Assert.That(
                result.HasCompletePair,
                Is.False
            );
        }

        [Test]
        public void Analyze_OneSolitary_ReturnsOneAccumulative()
        {
            TrackNamingGrammarAnalysis result =
                Analyze(
                    CreateSolitary(
                        "IDEA_PROFANE",
                        0,
                        TagPole.Negative
                    )
                );

            Assert.That(
                result.Shape,
                Is.EqualTo(
                    TrackNamingGrammarShape
                        .OneAccumulative
                )
            );

            Assert.That(
                result.AccumulativePositions.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void Analyze_OpposingSolitaryTags_DoNotBecomePair()
        {
            TrackNamingGrammarAnalysis result =
                Analyze(
                    CreateSolitary(
                        "IDEA_PROFANE",
                        0,
                        TagPole.Negative
                    ),
                    CreateSolitary(
                        "IDEA_SACRED",
                        1,
                        TagPole.Positive
                    )
                );

            Assert.That(
                result.Shape,
                Is.EqualTo(
                    TrackNamingGrammarShape
                        .TwoAccumulative
                )
            );

            Assert.That(
                result.HasCompletePair,
                Is.False
            );

            Assert.That(
                result.AccumulativePositions.Count,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void Analyze_CompleteFormalPair_PreservesDirection()
        {
            TrackNamingGrammarAnalysis result =
                Analyze(
                    CreatePairDominant(
                        "IDEA_PAIR",
                        0
                    ),
                    CreatePairSubmissive(
                        "IDEA_PAIR",
                        0
                    )
                );

            Assert.That(
                result.Shape,
                Is.EqualTo(
                    TrackNamingGrammarShape.Pair
                )
            );

            Assert.That(
                result.HasCompletePair,
                Is.True
            );

            Assert.That(
                result.PairDominant.Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairDominant
                )
            );

            Assert.That(
                result.PairSubmissive.Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairSubmissive
                )
            );
        }

        [Test]
        public void Analyze_PairThenSolitary_ReturnsPairWithSurface()
        {
            TrackNamingGrammarAnalysis result =
                Analyze(
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
                        1,
                        TagPole.Negative
                    )
                );

            Assert.That(
                result.Shape,
                Is.EqualTo(
                    TrackNamingGrammarShape
                        .PairWithSurface
                )
            );

            Assert.That(
                result.SurfacePosition.SourceIdeaId,
                Is.EqualTo("IDEA_SURFACE")
            );
        }

        [Test]
        public void Analyze_SolitaryThenPair_StillUsesPairAsCore()
        {
            TrackNamingGrammarAnalysis result =
                Analyze(
                    CreateSolitary(
                        "IDEA_SURFACE",
                        0,
                        TagPole.Negative
                    ),
                    CreatePairDominant(
                        "IDEA_PAIR",
                        1
                    ),
                    CreatePairSubmissive(
                        "IDEA_PAIR",
                        1
                    )
                );

            Assert.That(
                result.Shape,
                Is.EqualTo(
                    TrackNamingGrammarShape
                        .PairWithSurface
                )
            );

            Assert.That(
                result.PairDominant.SourceIdeaId,
                Is.EqualTo("IDEA_PAIR")
            );

            Assert.That(
                result.SurfacePosition.SourceIdeaId,
                Is.EqualTo("IDEA_SURFACE")
            );
        }

        [Test]
        public void Analyze_TwoPairsProjection_UsesSecondaryDominantAsSurface()
        {
            TrackNamingGrammarAnalysis result =
                Analyze(
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
                    )
                );

            Assert.That(
                result.Shape,
                Is.EqualTo(
                    TrackNamingGrammarShape
                        .PairWithSurface
                )
            );

            Assert.That(
                result.PairDominant.SourceIdeaId,
                Is.EqualTo("IDEA_PRIMARY_PAIR")
            );

            Assert.That(
                result.SurfacePosition.SourceIdeaId,
                Is.EqualTo("IDEA_SECONDARY_PAIR")
            );

            Assert.That(
                result.SurfacePosition.Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairDominant
                )
            );
        }

        [Test]
        public void Analyze_PairDominantAtFinalBoundary_RemainsAccumulative()
        {
            TrackNamingGrammarAnalysis result =
                Analyze(
                    CreateSolitary(
                        "IDEA_0",
                        0,
                        TagPole.Negative
                    ),
                    CreateSolitary(
                        "IDEA_1",
                        1,
                        TagPole.Positive
                    ),
                    CreatePairDominant(
                        "IDEA_TRUNCATED_PAIR",
                        2
                    )
                );

            Assert.That(
                result.Shape,
                Is.EqualTo(
                    TrackNamingGrammarShape
                        .ThreeAccumulative
                )
            );

            Assert.That(
                result.HasCompletePair,
                Is.False
            );

            Assert.That(
                result.AccumulativePositions.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                result.AccumulativePositions[2].Role,
                Is.EqualTo(
                    TrackNamingSemanticRole.PairDominant
                )
            );
        }

        private static TrackNamingGrammarAnalysis
            Analyze(
                params TrackNamingSemanticPosition[]
                    selected)
        {
            TrackNamingPositionSelection selection =
                new TrackNamingPositionSelection(
                    selected,
                    Array.Empty<
                        TrackNamingSemanticPosition>(),
                    selected.Length
                );

            return TrackNamingGrammarAnalyzer
                .Analyze(selection);
        }

        private static TrackNamingSemanticPosition
            CreateSolitary(
                string ideaId,
                int ideaIndex,
                TagPole pole)
        {
            return new TrackNamingSemanticPosition(
                ideaId,
                "ASPECT_TEST",
                ideaIndex,
                TrackNamingSemanticRole.Solitary,
                TagAxis.Symbolic,
                pole,
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