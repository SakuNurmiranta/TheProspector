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
        
        public float ResolveAffinity(
            CanonState canon,
            TagAxis axis,
            TagPole pole,
            TagDegree recordedDegree)
        {
            if (canon == null)
            {
                throw new ArgumentNullException(
                    nameof(canon)
                );
            }

            if (TryResolveDirectAffinity(
                    canon,
                    axis,
                    pole,
                    recordedDegree,
                    out float directAffinity))
            {
                return directAffinity;
            }

            TagPole oppositePole =
                GetOppositePole(pole);

            if (canon.HasPrecedent(
                    axis,
                    oppositePole))
            {
                return -1f;
            }

            if (HasAdjacentCanonicalPrecedent(
                    canon,
                    axis,
                    pole))
            {
                return 0.5f;
            }

            return 0f;
        }
        
        private static bool HasAdjacentCanonicalPrecedent(
            CanonState canon,
            TagAxis targetAxis,
            TagPole targetPole)
        {
            foreach (TagAxis canonicalAxis
                     in Enum.GetValues(
                         typeof(TagAxis)))
            {
                foreach (TagPole canonicalPole
                         in Enum.GetValues(
                             typeof(TagPole)))
                {
                    if (!canon.HasPrecedent(
                            canonicalAxis,
                            canonicalPole))
                    {
                        continue;
                    }

                    if (TagAdjacency.AreAdjacent(
                            targetAxis,
                            targetPole,
                            canonicalAxis,
                            canonicalPole))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        private static TagPole GetOppositePole(
            TagPole pole)
        {
            return pole switch
            {
                TagPole.Negative =>
                    TagPole.Positive,

                TagPole.Positive =>
                    TagPole.Negative,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(pole),
                        pole,
                        "Unsupported Tag pole."
                    )
            };
        }
        
        
    }
}