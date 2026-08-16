using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Pressure;

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

        public NormativeCentreDerivationEvaluation
            DeriveWithPressure(
                CanonState canon,
                ScenePressureRebuildEvaluation
                    pressureRebuild,
                NormativePressureBlendPolicy policy)
        {
            if (canon == null)
            {
                throw new ArgumentNullException(
                    nameof(canon)
                );
            }

            if (pressureRebuild == null)
            {
                throw new ArgumentNullException(
                    nameof(pressureRebuild)
                );
            }

            if (policy == null)
            {
                throw new ArgumentNullException(
                    nameof(policy)
                );
            }

            List<NormativeAffinityEntry>
                centreEntries =
                    new();

            List<NormativeCentreDerivationEntry>
                derivationEntries =
                    new();

            foreach (
                TagAxis axis
                in Enum.GetValues(
                    typeof(TagAxis)))
            {
                foreach (
                    TagPole pole
                    in Enum.GetValues(
                        typeof(TagPole)))
                {
                    float effectivePressure =
                        pressureRebuild
                            .Pressure
                            .GetEffectivePressure(
                                axis,
                                pole
                            );

                    foreach (
                        TagDegree recordedDegree
                        in Enum.GetValues(
                            typeof(TagDegree)))
                    {
                        float canonicalAffinity =
                            affinityResolver.ResolveAffinity(
                                canon,
                                axis,
                                pole,
                                recordedDegree
                            );

                        bool hasDirectCanon =
                            affinityResolver
                                .TryResolveDirectAffinity(
                                    canon,
                                    axis,
                                    pole,
                                    recordedDegree,
                                    out _
                                );

                        float finalAffinity =
                            policy.Blend(
                                canonicalAffinity,
                                effectivePressure,
                                hasDirectCanon
                            );

                        derivationEntries.Add(
                            new
                                NormativeCentreDerivationEntry(
                                    axis,
                                    pole,
                                    recordedDegree,
                                    hasDirectCanon,
                                    canonicalAffinity,
                                    effectivePressure,
                                    finalAffinity
                                )
                        );

                        if (finalAffinity == 0f)
                        {
                            continue;
                        }

                        centreEntries.Add(
                            new NormativeAffinityEntry(
                                axis,
                                pole,
                                recordedDegree,
                                finalAffinity
                            )
                        );
                    }
                }
            }

            return new
                NormativeCentreDerivationEvaluation(
                    pressureRebuild.SceneId,
                    pressureRebuild.SettledTurn,
                    new NormativeCentre(
                        centreEntries
                    ),
                    derivationEntries
                );
        }
    }
}