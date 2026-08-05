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
        private readonly string[] _surfaceModifiers;
        private readonly string[] _headNouns;
        private readonly string[] _dominantActions;
        private readonly string[] _submissivePhrases;

        public TagDegree Degree { get; }

        public string DegreeLabel { get; }

        public IReadOnlyList<string> SurfaceModifiers =>
            _surfaceModifiers;

        public IReadOnlyList<string> HeadNouns =>
            _headNouns;

        public IReadOnlyList<string> DominantActions =>
            _dominantActions;

        /// <summary>
        /// Stores the fifth-column material from the source
        /// lexicon. Depending on the Tag, these may represent
        /// objects, states, places, persons, or rites.
        /// </summary>
        public IReadOnlyList<string> SubmissivePhrases =>
            _submissivePhrases;

        public TrackNamingDegreeLexicon(
            TagDegree degree,
            string degreeLabel,
            IReadOnlyList<string> surfaceModifiers,
            IReadOnlyList<string> headNouns,
            IReadOnlyList<string> dominantActions,
            IReadOnlyList<string> submissivePhrases)
        {
            ValidateDegree(degree);

            Degree = degree;

            DegreeLabel = RequireText(
                degreeLabel,
                nameof(degreeLabel)
            );

            _surfaceModifiers = CopyRequiredPhrases(
                surfaceModifiers,
                nameof(surfaceModifiers)
            );

            _headNouns = CopyRequiredPhrases(
                headNouns,
                nameof(headNouns)
            );

            _dominantActions = CopyRequiredPhrases(
                dominantActions,
                nameof(dominantActions)
            );

            _submissivePhrases = CopyRequiredPhrases(
                submissivePhrases,
                nameof(submissivePhrases)
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

        private static string[] CopyRequiredPhrases(
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

            string[] copy =
                new string[phrases.Count];

            for (int index = 0;
                 index < phrases.Count;
                 index++)
            {
                copy[index] = RequireText(
                    phrases[index],
                    parameterName
                );
            }

            return copy;
        }
    }
}