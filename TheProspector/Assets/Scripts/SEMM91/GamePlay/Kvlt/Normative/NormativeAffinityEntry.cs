using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Normative
{
    [Serializable]
    public sealed class NormativeAffinityEntry
    {
        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree RecordedDegree { get; }

        public float Affinity { get; }

        public NormativeAffinityEntry(
            TagAxis axis,
            TagPole pole,
            TagDegree recordedDegree,
            float affinity)
        {
            if (float.IsNaN(affinity) ||
                float.IsInfinity(affinity) ||
                affinity < -1f ||
                affinity > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(affinity),
                    affinity,
                    "Normative affinity must be within [-1, +1]."
                );
            }

            Axis = axis;
            Pole = pole;
            RecordedDegree = recordedDegree;
            Affinity = affinity;
        }
    }
}