using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Immutable lookup over authored track-naming lexicons.
    /// </summary>
    public sealed class TrackNamingLexiconCatalog
    {
        private readonly Dictionary<TagIdentity, TrackNamingTagLexicon>
            _entries;

        public int Count => _entries.Count;

        public TrackNamingLexiconCatalog(
            IReadOnlyList<TrackNamingTagLexicon> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            _entries =
                new Dictionary<TagIdentity, TrackNamingTagLexicon>(
                    entries.Count
                );

            foreach (TrackNamingTagLexicon entry in entries)
            {
                if (entry == null)
                {
                    throw new ArgumentException(
                        "Track naming catalog cannot contain a null entry.",
                        nameof(entries)
                    );
                }

                TagIdentity identity =
                    new TagIdentity(entry.Axis, entry.Pole);

                if (_entries.ContainsKey(identity))
                {
                    throw new ArgumentException(
                        $"Track naming catalog contains duplicate Tag " +
                        $"{entry.Axis}/{entry.Pole}.",
                        nameof(entries)
                    );
                }

                _entries.Add(identity, entry);
            }
        }

        public bool TryGet(
            TagAxis axis,
            TagPole pole,
            out TrackNamingTagLexicon lexicon)
        {
            return _entries.TryGetValue(
                new TagIdentity(axis, pole),
                out lexicon
            );
        }

        public TrackNamingTagLexicon GetRequired(
            TagAxis axis,
            TagPole pole)
        {
            if (TryGet(axis, pole, out TrackNamingTagLexicon lexicon))
            {
                return lexicon;
            }

            throw new KeyNotFoundException(
                $"No track naming lexicon exists for {axis}/{pole}."
            );
        }

        private readonly struct TagIdentity :
            IEquatable<TagIdentity>
        {
            private readonly TagAxis _axis;
            private readonly TagPole _pole;

            public TagIdentity(
                TagAxis axis,
                TagPole pole)
            {
                _axis = axis;
                _pole = pole;
            }

            public bool Equals(TagIdentity other)
            {
                return _axis == other._axis &&
                       _pole == other._pole;
            }

            public override bool Equals(object obj)
            {
                return obj is TagIdentity other &&
                       Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)_axis * 397) ^ (int)_pole;
                }
            }
        }
    }
}
