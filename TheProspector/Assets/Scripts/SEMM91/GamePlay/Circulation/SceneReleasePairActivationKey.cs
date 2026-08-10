using System;

namespace SEMM91.GamePlay.Circulation
{
    public readonly struct SceneReleasePairActivationKey :
        IEquatable<SceneReleasePairActivationKey>
    {
        public string SourceTrackId { get; }

        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public SceneReleasePairActivationKey(
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex)
        {
            SourceTrackId =
                RequireText(
                    sourceTrackId,
                    nameof(sourceTrackId)
                );

            SourceIdeaId =
                RequireText(
                    sourceIdeaId,
                    nameof(sourceIdeaId)
                );

            if (ideaIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ideaIndex)
                );
            }

            IdeaIndex =
                ideaIndex;
        }

        public bool Equals(
            SceneReleasePairActivationKey other)
        {
            return
                SourceTrackId ==
                    other.SourceTrackId &&
                SourceIdeaId ==
                    other.SourceIdeaId &&
                IdeaIndex ==
                    other.IdeaIndex;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is SceneReleasePairActivationKey other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash =
                    hash * 31 +
                    StringComparer.Ordinal.GetHashCode(
                        SourceTrackId
                    );

                hash =
                    hash * 31 +
                    StringComparer.Ordinal.GetHashCode(
                        SourceIdeaId
                    );

                hash =
                    hash * 31 +
                    IdeaIndex;

                return hash;
            }
        }

        public static bool operator ==(
            SceneReleasePairActivationKey left,
            SceneReleasePairActivationKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            SceneReleasePairActivationKey left,
            SceneReleasePairActivationKey right)
        {
            return !left.Equals(right);
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Pair activation provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}