using System;
using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// One authored contextual-fragment group.
    ///
    /// Group labels remain strings because the source document
    /// deliberately uses different categories for different Tags.
    /// </summary>
    public sealed class TrackNamingContextFragmentGroup
    {
        private readonly string[] _fragments;

        public string GroupLabel { get; }

        public IReadOnlyList<string> Fragments =>
            _fragments;

        public TrackNamingContextFragmentGroup(
            string groupLabel,
            IReadOnlyList<string> fragments)
        {
            if (string.IsNullOrWhiteSpace(groupLabel))
            {
                throw new ArgumentException(
                    "Context-fragment group label cannot be empty.",
                    nameof(groupLabel)
                );
            }

            if (fragments == null)
            {
                throw new ArgumentNullException(
                    nameof(fragments)
                );
            }

            if (fragments.Count == 0)
            {
                throw new ArgumentException(
                    "Context-fragment group cannot be empty.",
                    nameof(fragments)
                );
            }

            GroupLabel = groupLabel.Trim();

            _fragments =
                new string[fragments.Count];

            for (int index = 0;
                 index < fragments.Count;
                 index++)
            {
                string fragment =
                    fragments[index];

                if (string.IsNullOrWhiteSpace(fragment))
                {
                    throw new ArgumentException(
                        "Contextual fragment cannot be empty.",
                        nameof(fragments)
                    );
                }

                _fragments[index] =
                    fragment.Trim();
            }
        }
    }
}