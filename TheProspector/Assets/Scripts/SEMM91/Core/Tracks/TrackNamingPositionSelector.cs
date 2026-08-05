using System;
using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Selects at most three ordered Tag occurrences from the
    /// complete semantic projection.
    ///
    /// A formal pair contributes two occurrences:
    /// one dominant and one submissive.
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

            for (int index = 0;
                 index < projectedOccurrences.Count;
                 index++)
            {
                TrackNamingSemanticPosition occurrence =
                    projectedOccurrences[index];

                if (occurrence == null)
                {
                    throw new InvalidOperationException(
                        "Track naming projection contains " +
                        "a null semantic occurrence."
                    );
                }

                if (selectedOccurrences.Count <
                    MaximumNamingPositions)
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
                selectedOccurrences.Count
            );
        }
    }
}