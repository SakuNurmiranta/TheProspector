using System;
using System.Text;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// One authored lexical realization available to the
    /// track-name generator.
    ///
    /// Family identity is separate from visible text so related
    /// forms such as hate, hateful, and hatred can later be
    /// rejected as collisions.
    /// </summary>
    public sealed class TrackNamingLexicalEntry
    {
        public string Text { get; }

        public string FamilyId { get; }

        public bool HasAuthoredFamilyId { get; }

        public TrackNamingLexicalEntry(
            string text,
            string familyId = null)
        {
            Text = RequireText(
                text,
                nameof(text)
            );

            if (string.IsNullOrWhiteSpace(familyId))
            {
                FamilyId =
                    BuildExactTextFamilyId(Text);

                HasAuthoredFamilyId = false;
            }
            else
            {
                FamilyId =
                    NormalizeFamilyId(familyId);

                HasAuthoredFamilyId = true;
            }
        }

        private static string BuildExactTextFamilyId(
            string text)
        {
            return "exact:" +
                   NormalizeFamilyId(text);
        }

        private static string NormalizeFamilyId(
            string value)
        {
            string trimmed =
                RequireText(
                    value,
                    nameof(value)
                );

            StringBuilder normalized = new();

            bool previousWasSeparator = false;

            foreach (char character in trimmed)
            {
                if (char.IsLetterOrDigit(character))
                {
                    normalized.Append(
                        char.ToLowerInvariant(character)
                    );

                    previousWasSeparator = false;
                    continue;
                }

                if (previousWasSeparator)
                    continue;

                normalized.Append('-');
                previousWasSeparator = true;
            }

            while (normalized.Length > 0 &&
                   normalized[normalized.Length - 1] == '-')
            {
                normalized.Length--;
            }

            if (normalized.Length == 0)
            {
                throw new ArgumentException(
                    "Lexical-family identity contains no " +
                    "usable characters.",
                    nameof(value)
                );
            }

            return normalized.ToString();
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Lexical text cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}