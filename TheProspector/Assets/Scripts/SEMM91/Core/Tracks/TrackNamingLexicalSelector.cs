using System;
using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Deterministically selects authored lexical material for
    /// one resolved semantic occurrence.
    /// </summary>
    public static class TrackNamingLexicalSelector
    {
        private const uint HashOffsetBasis =
            2166136261u;

        private const uint HashPrime =
            16777619u;

        public static string Select(
            TrackNamingLexiconCatalog catalog,
            TrackNamingSemanticPosition position,
            TrackNamingLexicalCategory category,
            int variationIndex = 0)
        {
            return SelectEntry(
                catalog,
                position,
                category,
                variationIndex
            ).Text;
        }

        public static TrackNamingLexicalEntry SelectEntry(
            TrackNamingLexiconCatalog catalog,
            TrackNamingSemanticPosition position,
            TrackNamingLexicalCategory category,
            int variationIndex = 0)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(
                    nameof(catalog)
                );
            }

            if (position == null)
            {
                throw new ArgumentNullException(
                    nameof(position)
                );
            }

            if (variationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(variationIndex),
                    variationIndex,
                    "Variation index cannot be negative."
                );
            }

            TrackNamingTagLexicon tagLexicon =
                catalog.GetRequired(
                    position.Axis,
                    position.Pole
                );

            TrackNamingDegreeLexicon degreeLexicon =
                tagLexicon.GetDegreeEntry(
                    position.NamingEffectiveDegree
                );

            IReadOnlyList<TrackNamingLexicalEntry>
                entries =
                    GetEntries(
                        degreeLexicon,
                        category
                    );

            int entryIndex =
                SelectEntryIndex(
                    position,
                    category,
                    variationIndex,
                    entries.Count
                );

            return entries[entryIndex];
        }

        private static IReadOnlyList<
            TrackNamingLexicalEntry> GetEntries(
                TrackNamingDegreeLexicon degreeLexicon,
                TrackNamingLexicalCategory category)
        {
            switch (category)
            {
                case TrackNamingLexicalCategory
                    .SurfaceModifier:

                    return degreeLexicon
                        .SurfaceModifierEntries;

                case TrackNamingLexicalCategory
                    .HeadNoun:

                    return degreeLexicon
                        .HeadNounEntries;

                case TrackNamingLexicalCategory
                    .DominantAction:

                    return degreeLexicon
                        .DominantActionEntries;

                case TrackNamingLexicalCategory
                    .SubmissivePhrase:

                    return degreeLexicon
                        .SubmissivePhraseEntries;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(category),
                        category,
                        "Unsupported track-naming lexical category."
                    );
            }
        }

        private static int SelectEntryIndex(
            TrackNamingSemanticPosition position,
            TrackNamingLexicalCategory category,
            int variationIndex,
            int entryCount)
        {
            if (entryCount <= 0)
            {
                throw new InvalidOperationException(
                    "Cannot select from an empty lexical list."
                );
            }

            uint hash = HashOffsetBasis;

            hash = Mix(hash, (int)position.Axis);
            hash = Mix(hash, (int)position.Pole);

            hash = Mix(
                hash,
                (int)position.NamingEffectiveDegree
            );

            hash = Mix(hash, position.IdeaIndex);
            hash = Mix(hash, (int)position.Role);
            hash = Mix(hash, (int)category);
            hash = Mix(hash, variationIndex);

            return (int)(
                hash % (uint)entryCount
            );
        }

        private static uint Mix(
            uint currentHash,
            int value)
        {
            unchecked
            {
                currentHash ^= (uint)value;
                currentHash *= HashPrime;

                return currentHash;
            }
        }
    }
}