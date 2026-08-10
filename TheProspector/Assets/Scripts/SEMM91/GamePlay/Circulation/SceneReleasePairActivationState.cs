using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class SceneReleasePairActivationState
    {
        public SceneReleasePairActivationKey Key { get; }

        public TagDegree
            RecordedDominantDegree { get; }

        public TagDegree
            CurrentActivationDegree { get; private set; }

        public SceneReleasePairActivationState(
            SceneReleasePairActivationKey key,
            TagDegree recordedDominantDegree)
        {
            ValidateDegree(
                recordedDominantDegree
            );

            if (recordedDominantDegree ==
                TagDegree.Neutral)
            {
                throw new ArgumentException(
                    "Active formal pair requires " +
                    "recorded dominant degree >= 1.",
                    nameof(recordedDominantDegree)
                );
            }

            Key =
                key;

            RecordedDominantDegree =
                recordedDominantDegree;

            CurrentActivationDegree =
                TagDegree.Neutral;
        }

        internal bool TryApply(
            TagDegree requestedDegree,
            out TagDegree previousDegree,
            out TagDegree newDegree)
        {
            ValidateDegree(
                requestedDegree
            );

            previousDegree =
                CurrentActivationDegree;

            if ((int)requestedDegree >
                (int)RecordedDominantDegree)
            {
                newDegree =
                    CurrentActivationDegree;

                return false;
            }

            newDegree =
                (int)requestedDegree >
                (int)CurrentActivationDegree
                    ? requestedDegree
                    : CurrentActivationDegree;

            CurrentActivationDegree =
                newDegree;

            return true;
        }

        private static void ValidateDegree(
            TagDegree degree)
        {
            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    degree))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(degree)
                );
            }
        }
    }
}