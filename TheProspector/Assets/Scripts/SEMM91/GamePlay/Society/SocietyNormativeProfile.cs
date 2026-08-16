using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Society
{
    [Serializable]
    public sealed class SocietyNormativeProfile
    {
        private readonly Dictionary<(TagAxis Axis, TagPole Pole), TagDegree>
            norms = new();

        public int Count =>
            norms.Count;

        public bool TryGetNormativeDegree(
            TagAxis axis,
            TagPole pole,
            out TagDegree degree)
        {
            return norms.TryGetValue(
                (axis, pole),
                out degree
            );
        }

        public float GetNormativeForce(
            TagAxis axis,
            TagPole pole)
        {
            if (!TryGetNormativeDegree(
                    axis,
                    pole,
                    out TagDegree degree))
            {
                return 0f;
            }

            return degree switch
            {
                TagDegree.Neutral => 0.5f,
                TagDegree.Weak => 1f,
                TagDegree.Dominant => 2f,
                TagDegree.Transgressive => 3f,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(degree),
                    degree,
                    "Unsupported Society normative degree."
                )
            };
        }

        public bool SetNormativeDegree(
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            var key =
                (axis, pole);

            if (norms.TryGetValue(
                    key,
                    out TagDegree existingDegree) &&
                existingDegree == degree)
            {
                return false;
            }

            norms[key] = degree;

            return true;
        }

        public bool RemoveNorm(
            TagAxis axis,
            TagPole pole)
        {
            return norms.Remove(
                (axis, pole)
            );
        }
    }
}