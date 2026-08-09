using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Canon;

namespace SEMM91.GamePlay.Kvlt.Normative
{
    public sealed class CanonicalNormativeCentreDeriver
    {
        private readonly CanonicalAffinityResolver
            affinityResolver =
                new CanonicalAffinityResolver();

        public NormativeCentre Derive(
            CanonState canon)
        {
            if (canon == null)
            {
                throw new ArgumentNullException(
                    nameof(canon)
                );
            }

            List<NormativeAffinityEntry> entries =
                new();

            foreach (TagAxis axis
                     in Enum.GetValues(
                         typeof(TagAxis)))
            {
                foreach (TagPole pole
                         in Enum.GetValues(
                             typeof(TagPole)))
                {
                    foreach (TagDegree recordedDegree
                             in Enum.GetValues(
                                 typeof(TagDegree)))
                    {
                        float affinity =
                            affinityResolver
                                .ResolveAffinity(
                                    canon,
                                    axis,
                                    pole,
                                    recordedDegree
                                );

                        if (affinity == 0f)
                        {
                            continue;
                        }

                        entries.Add(
                            new NormativeAffinityEntry(
                                axis,
                                pole,
                                recordedDegree,
                                affinity
                            )
                        );
                    }
                }
            }

            return new NormativeCentre(
                entries
            );
        }
    }
}