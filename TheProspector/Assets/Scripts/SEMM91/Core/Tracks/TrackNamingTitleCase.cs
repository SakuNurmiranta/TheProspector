using System;
using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Applies the initial English title-case rules required
    /// by generated track names.
    /// </summary>
    public static class TrackNamingTitleCase
    {
        private static readonly HashSet<string>
            LowercaseInteriorWords =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                )
                {
                    "a",
                    "an",
                    "the",
                    "and",
                    "but",
                    "for",
                    "nor",
                    "or",
                    "as",
                    "at",
                    "by",
                    "from",
                    "in",
                    "into",
                    "of",
                    "on",
                    "onto",
                    "to",
                    "up",
                    "with",
                    "without",
                    "against",
                    "upon",
                    "within",
                    "through",
                    "beneath",
                    "before",
                    "after",
                    "beyond",
                    "during"
                };

        public static string Apply(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string[] words =
                value.Trim().Split(
                    new[] { ' ' },
                    StringSplitOptions
                        .RemoveEmptyEntries
                );

            for (int index = 0;
                 index < words.Length;
                 index++)
            {
                string lowercaseWord =
                    words[index].ToLowerInvariant();

                bool isInteriorWord =
                    index > 0 &&
                    index < words.Length - 1;

                bool remainsLowercase =
                    isInteriorWord &&
                    LowercaseInteriorWords.Contains(
                        lowercaseWord
                    );

                words[index] =
                    remainsLowercase
                        ? lowercaseWord
                        : CapitalizeCompound(
                            lowercaseWord
                        );
            }

            return string.Join(" ", words);
        }

        private static string CapitalizeCompound(
            string word)
        {
            string[] segments =
                word.Split('-');

            for (int index = 0;
                 index < segments.Length;
                 index++)
            {
                segments[index] =
                    CapitalizeWord(
                        segments[index]
                    );
            }

            return string.Join("-", segments);
        }

        private static string CapitalizeWord(
            string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return word;
            }

            if (word.Length == 1)
            {
                return word.ToUpperInvariant();
            }

            return char.ToUpperInvariant(word[0]) +
                   word.Substring(1);
        }
    }
}