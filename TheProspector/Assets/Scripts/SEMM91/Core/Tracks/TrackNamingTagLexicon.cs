using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Complete authored naming lexicon for one Tag identity.
    /// </summary>
    public sealed class TrackNamingTagLexicon
    {
        private const int RequiredDegreeCount = 4;

        private readonly TrackNamingDegreeLexicon[]
            _degreeEntries;

        private readonly TrackNamingContextFragmentGroup[]
            _contextFragmentGroups;

        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public IReadOnlyList<TrackNamingDegreeLexicon>
            DegreeEntries =>
                _degreeEntries;

        public IReadOnlyList<TrackNamingContextFragmentGroup>
            ContextFragmentGroups =>
                _contextFragmentGroups;

        public TrackNamingTagLexicon(
            TagAxis axis,
            TagPole pole,
            IReadOnlyList<TrackNamingDegreeLexicon>
                degreeEntries,
            IReadOnlyList<TrackNamingContextFragmentGroup>
                contextFragmentGroups)
        {
            if (degreeEntries == null)
            {
                throw new ArgumentNullException(
                    nameof(degreeEntries)
                );
            }

            if (contextFragmentGroups == null)
            {
                throw new ArgumentNullException(
                    nameof(contextFragmentGroups)
                );
            }

            Axis = axis;
            Pole = pole;

            _degreeEntries =
                BuildDegreeIndex(degreeEntries);

            _contextFragmentGroups =
                CopyContextGroups(
                    contextFragmentGroups
                );
        }

        public TrackNamingDegreeLexicon GetDegreeEntry(
            TagDegree degree)
        {
            int index = (int)degree;

            if (index < 0 ||
                index >= _degreeEntries.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(degree),
                    degree,
                    "Track naming degree must be from 0 to 3."
                );
            }

            return _degreeEntries[index];
        }

        private static TrackNamingDegreeLexicon[]
            BuildDegreeIndex(
                IReadOnlyList<TrackNamingDegreeLexicon>
                    degreeEntries)
        {
            if (degreeEntries.Count !=
                RequiredDegreeCount)
            {
                throw new ArgumentException(
                    "A Tag lexicon must contain exactly " +
                    "one entry for each degree from 0 to 3.",
                    nameof(degreeEntries)
                );
            }

            TrackNamingDegreeLexicon[] indexed =
                new TrackNamingDegreeLexicon[
                    RequiredDegreeCount
                ];

            foreach (
                TrackNamingDegreeLexicon entry
                in degreeEntries)
            {
                if (entry == null)
                {
                    throw new ArgumentException(
                        "Tag lexicon cannot contain a null " +
                        "degree entry.",
                        nameof(degreeEntries)
                    );
                }

                int index = (int)entry.Degree;

                if (index < 0 ||
                    index >= RequiredDegreeCount)
                {
                    throw new ArgumentException(
                        "Tag lexicon contains an invalid degree.",
                        nameof(degreeEntries)
                    );
                }

                if (indexed[index] != null)
                {
                    throw new ArgumentException(
                        $"Tag lexicon contains duplicate " +
                        $"degree {entry.Degree}.",
                        nameof(degreeEntries)
                    );
                }

                indexed[index] = entry;
            }

            for (int index = 0;
                 index < indexed.Length;
                 index++)
            {
                if (indexed[index] == null)
                {
                    throw new ArgumentException(
                        $"Tag lexicon is missing degree {index}.",
                        nameof(degreeEntries)
                    );
                }
            }

            return indexed;
        }

        private static TrackNamingContextFragmentGroup[]
            CopyContextGroups(
                IReadOnlyList<
                    TrackNamingContextFragmentGroup>
                    groups)
        {
            TrackNamingContextFragmentGroup[] copy =
                new TrackNamingContextFragmentGroup[
                    groups.Count
                ];

            for (int index = 0;
                 index < groups.Count;
                 index++)
            {
                if (groups[index] == null)
                {
                    throw new ArgumentException(
                        "Tag lexicon cannot contain a null " +
                        "context-fragment group.",
                        nameof(groups)
                    );
                }

                copy[index] = groups[index];
            }

            return copy;
        }
    }
}