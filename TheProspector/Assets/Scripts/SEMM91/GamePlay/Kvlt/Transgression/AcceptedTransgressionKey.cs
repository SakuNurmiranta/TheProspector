using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Transgression
{
    /// <summary>
    /// Identity of one KVLT praxis question.
    ///
    /// Degree is deliberately NOT part of identity.
    /// Accepted degree is a ceiling associated with
    /// this Behavior + Tag-direction key.
    /// </summary>
    public readonly struct AcceptedTransgressionKey :
        IEquatable<AcceptedTransgressionKey>
    {
        public string BehaviorTypeId { get; }

        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public AcceptedTransgressionKey(
            string behaviorTypeId,
            TagAxis axis,
            TagPole pole)
        {
            if (string.IsNullOrWhiteSpace(
                    behaviorTypeId))
            {
                throw new ArgumentException(
                    "Accepted Transgression requires " +
                    "a Behavior type identity.",
                    nameof(behaviorTypeId)
                );
            }

            if (!Enum.IsDefined(
                    typeof(TagAxis),
                    axis))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(axis)
                );
            }

            if (!Enum.IsDefined(
                    typeof(TagPole),
                    pole))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pole)
                );
            }

            BehaviorTypeId =
                behaviorTypeId.Trim();

            Axis =
                axis;

            Pole =
                pole;
        }

        public bool Equals(
            AcceptedTransgressionKey other)
        {
            return
                BehaviorTypeId ==
                    other.BehaviorTypeId &&
                Axis == other.Axis &&
                Pole == other.Pole;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is AcceptedTransgressionKey other &&
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
                        BehaviorTypeId
                    );

                hash =
                    hash * 31 +
                    Axis.GetHashCode();

                hash =
                    hash * 31 +
                    Pole.GetHashCode();

                return hash;
            }
        }

        public static bool operator ==(
            AcceptedTransgressionKey left,
            AcceptedTransgressionKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            AcceptedTransgressionKey left,
            AcceptedTransgressionKey right)
        {
            return !left.Equals(right);
        }
    }
}