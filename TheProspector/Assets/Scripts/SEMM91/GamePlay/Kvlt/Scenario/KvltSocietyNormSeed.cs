using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    /// <summary>
    /// Immutable Scenario declaration for one
    /// starting Society normative degree.
    /// </summary>
    public sealed class KvltSocietyNormSeed
    {
        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree Degree { get; }

        public KvltSocietyNormSeed(
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
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

            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    degree))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(degree)
                );
            }

            Axis = axis;
            Pole = pole;
            Degree = degree;
        }
    }
}