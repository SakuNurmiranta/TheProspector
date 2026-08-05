using System;
using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Result of selecting the Idea positions that participate
    /// directly in a generated track title.
    ///
    /// A selected paired Idea contributes two occurrences while
    /// still counting as one naming position.
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