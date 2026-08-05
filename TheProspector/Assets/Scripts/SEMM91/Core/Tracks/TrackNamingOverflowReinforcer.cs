using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Applies naming-only reinforcement from overflow
    /// occurrences.
    ///
    /// Exact matching must run before adjacency matching.
    /// Authoritative mechanical degrees are never mutated.
    /// </summary>
    public static class TrackNamingOverflowReinforcer
    {
        public static TrackNamingPositionSelection
            ApplyExactMatches(
                TrackNamingPositionSelection selection)
        {
            if (selection == null)
            {
                throw new ArgumentNullException(
                    nameof(selection)
                );
            }

            List<TrackNamingSemanticPosition>
                reinforcedSelected =
                    new(selection.SelectedOccurrences);

            List<TrackNamingSemanticPosition>
                unmatchedOverflow = new();

            foreach (
                TrackNamingSemanticPosition overflow
                in selection.OverflowOccurrences)
            {
                ValidateOverflowOccurrence(overflow);

                int matchingIndex =
                    FindFirstExactMatch(
                        reinforcedSelected,
                        overflow
                    );

                if (matchingIndex < 0)
                {
                    unmatchedOverflow.Add(overflow);
                    continue;
                }

                TrackNamingSemanticPosition target =
                    reinforcedSelected[matchingIndex];

                TagDegree reinforcedDegree =
                    AddDegreesCappedAtThree(
                        target.NamingEffectiveDegree,
                        overflow.MechanicalDegree
                    );

                reinforcedSelected[matchingIndex] =
                    target.WithNamingEffectiveDegree(
                        reinforcedDegree
                    );
            }

            return new TrackNamingPositionSelection(
                reinforcedSelected.ToArray(),
                unmatchedOverflow.ToArray(),
                selection.SelectedPositionCount
            );
        }

        public static TrackNamingPositionSelection
            ApplyAdjacencyMatches(
                TrackNamingPositionSelection selection)
        {
            if (selection == null)
            {
                throw new ArgumentNullException(
                    nameof(selection)
                );
            }

            List<TrackNamingSemanticPosition>
                reinforcedSelected =
                    new(selection.SelectedOccurrences);

            List<TrackNamingSemanticPosition>
                unmatchedOverflow = new();

            foreach (
                TrackNamingSemanticPosition overflow
                in selection.OverflowOccurrences)
            {
                ValidateOverflowOccurrence(overflow);

                int matchingIndex =
                    FindFirstAdjacentMatch(
                        reinforcedSelected,
                        overflow
                    );

                if (matchingIndex < 0)
                {
                    unmatchedOverflow.Add(overflow);
                    continue;
                }

                TrackNamingSemanticPosition target =
                    reinforcedSelected[matchingIndex];

                TagDegree reinforcedDegree =
                    AddOneDegreeCappedAtThree(
                        target.NamingEffectiveDegree
                    );

                reinforcedSelected[matchingIndex] =
                    target.WithNamingEffectiveDegree(
                        reinforcedDegree
                    );
            }

            return new TrackNamingPositionSelection(
                reinforcedSelected.ToArray(),
                unmatchedOverflow.ToArray(),
                selection.SelectedPositionCount
            );
        }

        private static int FindFirstExactMatch(
            IReadOnlyList<TrackNamingSemanticPosition>
                selectedOccurrences,
            TrackNamingSemanticPosition overflow)
        {
            for (int index = 0;
                 index < selectedOccurrences.Count;
                 index++)
            {
                TrackNamingSemanticPosition selected =
                    selectedOccurrences[index];

                ValidateSelectedOccurrence(selected);

                bool sameTag =
                    selected.Axis == overflow.Axis &&
                    selected.Pole == overflow.Pole;

                if (sameTag)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int FindFirstAdjacentMatch(
            IReadOnlyList<TrackNamingSemanticPosition>
                selectedOccurrences,
            TrackNamingSemanticPosition overflow)
        {
            for (int index = 0;
                 index < selectedOccurrences.Count;
                 index++)
            {
                TrackNamingSemanticPosition selected =
                    selectedOccurrences[index];

                ValidateSelectedOccurrence(selected);

                bool adjacent =
                    TagAdjacency.AreAdjacent(
                        selected.Axis,
                        selected.Pole,
                        overflow.Axis,
                        overflow.Pole
                    );

                if (adjacent)
                {
                    return index;
                }
            }

            return -1;
        }

        private static TagDegree AddDegreesCappedAtThree(
            TagDegree currentDegree,
            TagDegree reinforcementDegree)
        {
            int combinedDegree =
                (int)currentDegree +
                (int)reinforcementDegree;

            return ClampDegreeToThree(combinedDegree);
        }

        private static TagDegree
            AddOneDegreeCappedAtThree(
                TagDegree currentDegree)
        {
            int reinforcedDegree =
                (int)currentDegree + 1;

            return ClampDegreeToThree(
                reinforcedDegree
            );
        }

        private static TagDegree ClampDegreeToThree(
            int degree)
        {
            int maximumDegree =
                (int)TagDegree.Transgressive;

            int cappedDegree =
                Math.Min(
                    degree,
                    maximumDegree
                );

            return (TagDegree)cappedDegree;
        }

        private static void ValidateSelectedOccurrence(
            TrackNamingSemanticPosition occurrence)
        {
            if (occurrence == null)
            {
                throw new InvalidOperationException(
                    "Track naming selection contains " +
                    "a null semantic occurrence."
                );
            }
        }

        private static void ValidateOverflowOccurrence(
            TrackNamingSemanticPosition occurrence)
        {
            if (occurrence == null)
            {
                throw new InvalidOperationException(
                    "Track naming overflow contains " +
                    "a null semantic occurrence."
                );
            }
        }
    }
}