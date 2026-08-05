using System;
using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Performs structural validation on a rendered title.
    ///
    /// Lexical-family validation is intentionally excluded until
    /// authored family identifiers exist in the lexicon contract.
    /// </summary>
    public static class TrackNamingTitleValidator
    {
        public const int MaximumVisibleWordCount = 12;

        private static readonly HashSet<string>
            FunctionWords =
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

        public static bool TryValidate(
            string title,
            out TrackNamingTitleValidationFailure failure)
        {
            failure =
                TrackNamingTitleValidationFailure.None;

            if (string.IsNullOrWhiteSpace(title))
            {
                failure =
                    TrackNamingTitleValidationFailure
                        .EmptyTitle;

                return false;
            }

            string[] visibleWords =
                title.Trim().Split(
                    new[] { ' ' },
                    StringSplitOptions
                        .RemoveEmptyEntries
                );

            if (visibleWords.Length >
                MaximumVisibleWordCount)
            {
                failure =
                    TrackNamingTitleValidationFailure
                        .ExceedsMaximumWordCount;

                return false;
            }

            string[] normalizedWords =
                new string[visibleWords.Length];

            for (int index = 0;
                 index < visibleWords.Length;
                 index++)
            {
                normalizedWords[index] =
                    NormalizeWord(
                        visibleWords[index]
                    );
            }

            if (ContainsRepeatedContentWord(
                    normalizedWords
                ))
            {
                failure =
                    TrackNamingTitleValidationFailure
                        .RepeatedContentWord;

                return false;
            }

            if (ContainsImmediatelyRepeatedFunctionWord(
                    normalizedWords
                ))
            {
                failure =
                    TrackNamingTitleValidationFailure
                        .RepeatedFunctionWord;

                return false;
            }

            if (ContainsRepeatedFunctionSequence(
                    normalizedWords
                ))
            {
                failure =
                    TrackNamingTitleValidationFailure
                        .RepeatedFunctionSequence;

                return false;
            }

            return true;
        }

        private static bool ContainsRepeatedContentWord(
            IReadOnlyList<string> words)
        {
            HashSet<string> encounteredContentWords =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (string word in words)
            {
                if (string.IsNullOrEmpty(word) ||
                    FunctionWords.Contains(word))
                {
                    continue;
                }

                if (!encounteredContentWords.Add(word))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool
            ContainsImmediatelyRepeatedFunctionWord(
                IReadOnlyList<string> words)
        {
            for (int index = 1;
                 index < words.Count;
                 index++)
            {
                string previous =
                    words[index - 1];

                string current =
                    words[index];

                if (!FunctionWords.Contains(previous) ||
                    !FunctionWords.Contains(current))
                {
                    continue;
                }

                if (string.Equals(
                        previous,
                        current,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsRepeatedFunctionSequence(
            IReadOnlyList<string> words)
        {
            /*
             * Detects adjacent repeated two-word function
             * sequences such as:
             *
             * within the within the tomb
             * of the of the sacred face
             */
            for (int index = 0;
                 index <= words.Count - 4;
                 index++)
            {
                string first =
                    words[index];

                string second =
                    words[index + 1];

                string repeatedFirst =
                    words[index + 2];

                string repeatedSecond =
                    words[index + 3];

                if (!FunctionWords.Contains(first) ||
                    !FunctionWords.Contains(second))
                {
                    continue;
                }

                bool firstMatches =
                    string.Equals(
                        first,
                        repeatedFirst,
                        StringComparison.OrdinalIgnoreCase
                    );

                bool secondMatches =
                    string.Equals(
                        second,
                        repeatedSecond,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (firstMatches && secondMatches)
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeWord(
            string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return string.Empty;
            }

            return word
                .Trim(
                    '.',
                    ',',
                    ':',
                    ';',
                    '!',
                    '?',
                    '"',
                    '\'',
                    '(',
                    ')',
                    '[',
                    ']'
                )
                .ToLowerInvariant();
        }
    }
}