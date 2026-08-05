using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Authored lexical material for one Tag at one degree.
    /// </summary>
    public sealed class TrackNamingDegreeLexicon
    {
        private readonly TrackNamingLexicalEntry[]
            _surfaceModifierEntries;

        private readonly TrackNamingLexicalEntry[]
            _headNounEntries;

        private readonly TrackNamingLexicalEntry[]
            _dominantActionEntries;

        private readonly TrackNamingLexicalEntry[]
            _submissivePhraseEntries;

        /*
         * Backward-compatible visible-text projections.
         * Existing consumers and catalog data can continue
         * using the original string-based API.
         */
        private readonly string[] _surfaceModifiers;
        private readonly string[] _headNouns;
        private readonly string[] _dominantActions;
        private readonly string[] _submissivePhrases;

        public TagDegree Degree { get; }

        public string DegreeLabel { get; }

        public IReadOnlyList<
            TrackNamingLexicalEntry>
            SurfaceModifierEntries =>
                _surfaceModifierEntries;

        public IReadOnlyList<
            TrackNamingLexicalEntry>
            HeadNounEntries =>
                _headNounEntries;

        public IReadOnlyList<
            TrackNamingLexicalEntry>
            DominantActionEntries =>
                _dominantActionEntries;

        public IReadOnlyList<
            TrackNamingLexicalEntry>
            SubmissivePhraseEntries =>
                _submissivePhraseEntries;

        public IReadOnlyList<string>
            SurfaceModifiers =>
                _surfaceModifiers;

        public IReadOnlyList<string>
            HeadNouns =>
                _headNouns;

        public IReadOnlyList<string>
            DominantActions =>
                _dominantActions;

        public IReadOnlyList<string>
            SubmissivePhrases =>
                _submissivePhrases;

        /// <summary>
        /// Backward-compatible constructor for the existing
        /// authored catalog.
        ///
        /// Each string receives an exact-text fallback family.
        /// </summary>
        public TrackNamingDegreeLexicon(
            TagDegree degree,
            string degreeLabel,
            IReadOnlyList<string> surfaceModifiers,
            IReadOnlyList<string> headNouns,
            IReadOnlyList<string> dominantActions,
            IReadOnlyList<string> submissivePhrases)
            : this(
                degree,
                degreeLabel,
                WrapPhrases(
                    surfaceModifiers,
                    nameof(surfaceModifiers)
                ),
                WrapPhrases(
                    headNouns,
                    nameof(headNouns)
                ),
                WrapPhrases(
                    dominantActions,
                    nameof(dominantActions)
                ),
                WrapPhrases(
                    submissivePhrases,
                    nameof(submissivePhrases)
                )
            )
        {
        }

        /// <summary>
        /// Metadata-aware constructor for authored lexical
        /// entries with explicit family identities.
        /// </summary>
        public TrackNamingDegreeLexicon(
            TagDegree degree,
            string degreeLabel,
            IReadOnlyList<TrackNamingLexicalEntry>
                surfaceModifiers,
            IReadOnlyList<TrackNamingLexicalEntry>
                headNouns,
            IReadOnlyList<TrackNamingLexicalEntry>
                dominantActions,
            IReadOnlyList<TrackNamingLexicalEntry>
                submissivePhrases)
        {
            ValidateDegree(degree);

            Degree = degree;

            DegreeLabel = RequireText(
                degreeLabel,
                nameof(degreeLabel)
            );

            _surfaceModifierEntries =
                CopyRequiredEntries(
                    surfaceModifiers,
                    nameof(surfaceModifiers)
                );

            _headNounEntries =
                CopyRequiredEntries(
                    headNouns,
                    nameof(headNouns)
                );

            _dominantActionEntries =
                CopyRequiredEntries(
                    dominantActions,
                    nameof(dominantActions)
                );

            _submissivePhraseEntries =
                CopyRequiredEntries(
                    submissivePhrases,
                    nameof(submissivePhrases)
                );

            _surfaceModifiers =
                CopyVisibleText(
                    _surfaceModifierEntries
                );

            _headNouns =
                CopyVisibleText(
                    _headNounEntries
                );

            _dominantActions =
                CopyVisibleText(
                    _dominantActionEntries
                );

            _submissivePhrases =
                CopyVisibleText(
                    _submissivePhraseEntries
                );
        }

        private static void ValidateDegree(
            TagDegree degree)
        {
            int numericDegree = (int)degree;

            if (numericDegree < 0 ||
                numericDegree >
                (int)TagDegree.Transgressive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(degree),
                    degree,
                    "Track naming degree must be from 0 to 3."
                );
            }
        }

        private static TrackNamingLexicalEntry[]
            WrapPhrases(
                IReadOnlyList<string> phrases,
                string parameterName)
        {
            if (phrases == null)
            {
                throw new ArgumentNullException(
                    parameterName
                );
            }

            if (phrases.Count == 0)
            {
                throw new ArgumentException(
                    "An authored phrase list cannot be empty.",
                    parameterName
                );
            }

            TrackNamingLexicalEntry[] entries =
                new TrackNamingLexicalEntry[
                    phrases.Count
                ];

            for (int index = 0;
                 index < phrases.Count;
                 index++)
            {
                entries[index] =
                    new TrackNamingLexicalEntry(
                        phrases[index]
                    );
            }

            return entries;
        }

        private static TrackNamingLexicalEntry[]
            CopyRequiredEntries(
                IReadOnlyList<
                    TrackNamingLexicalEntry> entries,
                string parameterName)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(
                    parameterName
                );
            }

            if (entries.Count == 0)
            {
                throw new ArgumentException(
                    "An authored lexical-entry list " +
                    "cannot be empty.",
                    parameterName
                );
            }

            TrackNamingLexicalEntry[] copy =
                new TrackNamingLexicalEntry[
                    entries.Count
                ];

            for (int index = 0;
                 index < entries.Count;
                 index++)
            {
                if (entries[index] == null)
                {
                    throw new ArgumentException(
                        "A lexical-entry list cannot contain " +
                        "a null entry.",
                        parameterName
                    );
                }

                copy[index] = entries[index];
            }

            return copy;
        }

        private static string[] CopyVisibleText(
            IReadOnlyList<
                TrackNamingLexicalEntry> entries)
        {
            string[] copy =
                new string[entries.Count];

            for (int index = 0;
                 index < entries.Count;
                 index++)
            {
                copy[index] =
                    entries[index].Text;
            }

            return copy;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Authored lexical text cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}