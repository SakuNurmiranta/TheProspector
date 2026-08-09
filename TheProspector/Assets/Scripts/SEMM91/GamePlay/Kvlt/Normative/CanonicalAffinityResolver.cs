using System;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Canon;

namespace SEMM91.GamePlay.Kvlt.Normative
{
    public sealed class CanonicalAffinityResolver
    {
        public bool TryResolveDirectAffinity(
            CanonState canon,
            TagAxis axis,
            TagPole pole,
            TagDegree recordedDegree,
            out float affinity)
        {
            if (canon == null)
            {
                throw new ArgumentNullException(
                    nameof(canon)
                );
            }

            if (!canon.TryGetCanonicalDegree(
                    axis,
                    pole,
                    out TagDegree canonicalDegree))
            {
                affinity = 0f;
                return false;
            }

            int gap =
                (int)canonicalDegree -
                (int)recordedDegree;

            affinity =
                ResolveRatchetAffinity(gap);

            return true;
        }

        private static float ResolveRatchetAffinity(
            int gap)
        {
            if (gap <= 0)
            {
                return 1f;
            }

            return gap switch
            {
                1 => 0.5f,
                2 => 0f,
                _ => -0.5f
            };
        }
    }
}