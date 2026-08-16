using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Immutable snapshot of one authoritative formal
    /// pair activation at the moment its SceneRelease
    /// becomes canonized.
    /// </summary>
    public sealed class
        SceneReleaseFrozenPairActivation
    {
        public SceneReleasePairActivationKey
            Key { get; }

        public string SourceTrackId =>
            Key.SourceTrackId;

        public string SourceIdeaId =>
            Key.SourceIdeaId;

        public int IdeaIndex =>
            Key.IdeaIndex;

        public TagDegree
            RecordedDominantDegree { get; }

        public TagDegree
            FinalActivationDegree { get; }

        public SceneReleaseFrozenPairActivation(
            SceneReleasePairActivationKey key,
            TagDegree recordedDominantDegree,
            TagDegree finalActivationDegree)
        {
            ValidateDegree(
                recordedDominantDegree,
                nameof(recordedDominantDegree)
            );

            ValidateDegree(
                finalActivationDegree,
                nameof(finalActivationDegree)
            );

            if (recordedDominantDegree ==
                TagDegree.Neutral)
            {
                throw new ArgumentException(
                    "Frozen formal-pair activation " +
                    "requires recorded degree >= 1.",
                    nameof(recordedDominantDegree)
                );
            }

            if ((int)finalActivationDegree >
                (int)recordedDominantDegree)
            {
                throw new ArgumentException(
                    "Frozen activation cannot exceed " +
                    "recorded dominant degree.",
                    nameof(finalActivationDegree)
                );
            }

            Key =
                key;

            RecordedDominantDegree =
                recordedDominantDegree;

            FinalActivationDegree =
                finalActivationDegree;
        }

        private static void ValidateDegree(
            TagDegree degree,
            string parameterName)
        {
            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    degree))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }
    }
}