using System;
using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Result of selecting at most three ordered Tag occurrences
    /// that may participate directly in a generated title.
    ///
    /// Dominant and submissive members of a pair are separate
    /// semantic positions.
    /// </summary>
    public sealed class TrackNamingPositionSelection
    {
        public IReadOnlyList<TrackNamingSemanticPosition>
            SelectedOccurrences
        {
            get;
        }

        public IReadOnlyList<TrackNamingSemanticPosition>
            OverflowOccurrences
        {
            get;
        }

        public int SelectedPositionCount
        {
            get;
        }

        public TrackNamingPositionSelection(
            IReadOnlyList<TrackNamingSemanticPosition>
                selectedOccurrences,
            IReadOnlyList<TrackNamingSemanticPosition>
                overflowOccurrences,
            int selectedPositionCount)
        {
            SelectedOccurrences =
                selectedOccurrences ??
                Array.Empty<TrackNamingSemanticPosition>();

            OverflowOccurrences =
                overflowOccurrences ??
                Array.Empty<TrackNamingSemanticPosition>();

            SelectedPositionCount =
                selectedPositionCount;
        }
    }
}