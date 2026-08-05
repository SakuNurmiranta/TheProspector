using System;
using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Structural interpretation of the semantic occurrences
    /// selected for a generated title.
    ///
    /// This analysis preserves formal pair direction and does
    /// not alter any authoritative or projected semantic data.
    /// </summary>
    public sealed class TrackNamingGrammarAnalysis
    {
        private readonly TrackNamingSemanticPosition[]
            _accumulativePositions;

        public TrackNamingGrammarShape Shape { get; }

        public IReadOnlyList<TrackNamingSemanticPosition>
            AccumulativePositions =>
                _accumulativePositions;

        public TrackNamingSemanticPosition
            PairDominant { get; }

        public TrackNamingSemanticPosition
            PairSubmissive { get; }

        public TrackNamingSemanticPosition
            SurfacePosition { get; }

        public bool HasCompletePair =>
            PairDominant != null &&
            PairSubmissive != null;

        internal TrackNamingGrammarAnalysis(
            TrackNamingGrammarShape shape,
            IReadOnlyList<TrackNamingSemanticPosition>
                accumulativePositions,
            TrackNamingSemanticPosition pairDominant,
            TrackNamingSemanticPosition pairSubmissive,
            TrackNamingSemanticPosition surfacePosition)
        {
            Shape = shape;

            _accumulativePositions =
                CopyPositions(
                    accumulativePositions
                );

            PairDominant = pairDominant;
            PairSubmissive = pairSubmissive;
            SurfacePosition = surfacePosition;
        }

        private static TrackNamingSemanticPosition[]
            CopyPositions(
                IReadOnlyList<TrackNamingSemanticPosition>
                    positions)
        {
            if (positions == null ||
                positions.Count == 0)
            {
                return Array.Empty<
                    TrackNamingSemanticPosition>();
            }

            TrackNamingSemanticPosition[] copy =
                new TrackNamingSemanticPosition[
                    positions.Count
                ];

            for (int index = 0;
                 index < positions.Count;
                 index++)
            {
                if (positions[index] == null)
                {
                    throw new ArgumentException(
                        "Grammar analysis cannot contain " +
                        "a null semantic position.",
                        nameof(positions)
                    );
                }

                copy[index] = positions[index];
            }

            return copy;
        }
    }
}