using System;
using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Selects the first three distinct Idea positions from an
    /// ordered semantic projection.
    ///
    /// All occurrences belonging to the same Idea remain in the
    /// same selected or overflow partition.
    /// </summary>
    public static class TrackNamingPositionSelector
    {
        public const int MaximumNamingPositions = 3;

        public static TrackNamingPositionSelection Select(
            IReadOnlyList<TrackNamingSemanticPosition>
                projectedOccurrences)
        {
            if (projectedOccurrences == null ||
                projectedOccurrences.Count == 0)
            {
                return new TrackNamingPositionSelection(
                    Array.Empty<
                        TrackNamingSemanticPosition>(),
                    Array.Empty<
                        TrackNamingSemanticPosition>(),
                    0
                );
            }

            List<TrackNamingSemanticPosition>
                selectedOccurrences = new();

            List<TrackNamingSemanticPosition>
                overflowOccurrences = new();

            HashSet<int> encounteredIdeaIndices = new();
            HashSet<int> selectedIdeaIndices = new();

            int selectedPositionCount = 0;

            foreach (
                TrackNamingSemanticPosition occurrence
                in projectedOccurrences)
            {
                if (occurrence == null)
                {
                    throw new InvalidOperationException(
                        "Track naming projection contains " +
                        "a null semantic occurrence."
                    );
                }

                int ideaIndex =
                    occurrence.IdeaIndex;

                if (!encounteredIdeaIndices.Contains(
                        ideaIndex
                    ))
                {
                    encounteredIdeaIndices.Add(ideaIndex);

                    if (selectedPositionCount <
                        MaximumNamingPositions)
                    {
                        selectedIdeaIndices.Add(ideaIndex);
                        selectedPositionCount++;
                    }
                }

                if (selectedIdeaIndices.Contains(ideaIndex))
                {
                    selectedOccurrences.Add(occurrence);
                }
                else
                {
                    overflowOccurrences.Add(occurrence);
                }
            }

            return new TrackNamingPositionSelection(
                selectedOccurrences.ToArray(),
                overflowOccurrences.ToArray(),
                selectedPositionCount
            );
        }
    }
}