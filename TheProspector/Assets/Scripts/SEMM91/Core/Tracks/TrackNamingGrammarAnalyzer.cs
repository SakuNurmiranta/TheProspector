using System;
using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Determines whether selected naming positions use
    /// accumulative grammar or contain a complete formal pair.
    /// </summary>
    public static class TrackNamingGrammarAnalyzer
    {
        public static TrackNamingGrammarAnalysis Analyze(
            TrackNamingPositionSelection selection)
        {
            if (selection == null)
            {
                throw new ArgumentNullException(
                    nameof(selection)
                );
            }

            IReadOnlyList<TrackNamingSemanticPosition>
                selected =
                    selection.SelectedOccurrences;

            if (selected.Count >
                TrackNamingPositionSelector
                    .MaximumNamingPositions)
            {
                throw new InvalidOperationException(
                    "Grammar analysis received more than " +
                    "three represented semantic positions."
                );
            }

            ValidateSelectedOrder(selected);

            int pairDominantIndex = -1;
            int pairSubmissiveIndex = -1;

            for (int index = 0;
                 index < selected.Count - 1;
                 index++)
            {
                TrackNamingSemanticPosition current =
                    selected[index];

                TrackNamingSemanticPosition next =
                    selected[index + 1];

                bool formsCompletePair =
                    current.Role ==
                    TrackNamingSemanticRole.PairDominant &&

                    next.Role ==
                    TrackNamingSemanticRole.PairSubmissive &&

                    string.Equals(
                        current.SourceIdeaId,
                        next.SourceIdeaId,
                        StringComparison.Ordinal
                    );

                if (!formsCompletePair)
                    continue;

                if (pairDominantIndex >= 0)
                {
                    throw new InvalidOperationException(
                        "A three-position grammar selection " +
                        "cannot contain two complete pairs."
                    );
                }

                pairDominantIndex = index;
                pairSubmissiveIndex = index + 1;
            }

            if (pairDominantIndex < 0)
            {
                return CreateAccumulativeAnalysis(
                    selected
                );
            }

            TrackNamingSemanticPosition surface = null;

            for (int index = 0;
                 index < selected.Count;
                 index++)
            {
                if (index == pairDominantIndex ||
                    index == pairSubmissiveIndex)
                {
                    continue;
                }

                if (surface != null)
                {
                    throw new InvalidOperationException(
                        "Pair grammar received more than one " +
                        "surface position."
                    );
                }

                surface = selected[index];
            }

            TrackNamingGrammarShape shape =
                surface == null
                    ? TrackNamingGrammarShape.Pair
                    : TrackNamingGrammarShape
                        .PairWithSurface;

            return new TrackNamingGrammarAnalysis(
                shape,
                Array.Empty<
                    TrackNamingSemanticPosition>(),
                selected[pairDominantIndex],
                selected[pairSubmissiveIndex],
                surface
            );
        }

        private static void ValidateSelectedOrder(
            IReadOnlyList<TrackNamingSemanticPosition>
                selected)
        {
            for (int index = 0;
                 index < selected.Count;
                 index++)
            {
                TrackNamingSemanticPosition current =
                    selected[index];

                if (current == null)
                {
                    throw new InvalidOperationException(
                        "Grammar selection contains a null " +
                        "semantic position."
                    );
                }

                if (current.Role ==
                    TrackNamingSemanticRole.PairSubmissive)
                {
                    bool hasMatchingDominantBefore =
                        index > 0 &&

                        selected[index - 1].Role ==
                        TrackNamingSemanticRole
                            .PairDominant &&

                        string.Equals(
                            selected[index - 1]
                                .SourceIdeaId,
                            current.SourceIdeaId,
                            StringComparison.Ordinal
                        );

                    if (!hasMatchingDominantBefore)
                    {
                        throw new InvalidOperationException(
                            "A selected pair-submissive " +
                            "position lacks its immediately " +
                            "preceding dominant occurrence."
                        );
                    }
                }

                if (current.Role ==
                        TrackNamingSemanticRole
                            .PairDominant &&
                    index < selected.Count - 1)
                {
                    TrackNamingSemanticPosition next =
                        selected[index + 1];

                    bool hasMatchingSubmissiveAfter =
                        next.Role ==
                        TrackNamingSemanticRole
                            .PairSubmissive &&

                        string.Equals(
                            current.SourceIdeaId,
                            next.SourceIdeaId,
                            StringComparison.Ordinal
                        );

                    if (!hasMatchingSubmissiveAfter)
                    {
                        throw new InvalidOperationException(
                            "A pair-dominant occurrence may be " +
                            "unpaired only at the final naming " +
                            "position boundary."
                        );
                    }
                }
            }
        }

        private static TrackNamingGrammarAnalysis
            CreateAccumulativeAnalysis(
                IReadOnlyList<
                    TrackNamingSemanticPosition>
                    selected)
        {
            TrackNamingGrammarShape shape;

            switch (selected.Count)
            {
                case 0:
                    shape =
                        TrackNamingGrammarShape.Empty;
                    break;

                case 1:
                    shape =
                        TrackNamingGrammarShape
                            .OneAccumulative;
                    break;

                case 2:
                    shape =
                        TrackNamingGrammarShape
                            .TwoAccumulative;
                    break;

                case 3:
                    shape =
                        TrackNamingGrammarShape
                            .ThreeAccumulative;
                    break;

                default:
                    throw new InvalidOperationException(
                        "Unsupported accumulative position count."
                    );
            }

            return new TrackNamingGrammarAnalysis(
                shape,
                selected,
                null,
                null,
                null
            );
        }
    }
}