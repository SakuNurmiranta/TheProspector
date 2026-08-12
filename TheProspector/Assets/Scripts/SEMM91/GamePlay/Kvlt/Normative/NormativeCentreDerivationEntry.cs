using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Normative
{
    /// <summary>
    /// Reconstructable derivation of one settled
    /// Normative Centre affinity.
    /// </summary>
    public sealed class NormativeCentreDerivationEntry
    {
        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree RecordedDegree { get; }

        public bool HasDirectCanon { get; }

        public float CanonicalAffinity { get; }

        public float EffectivePressure { get; }

        public float FinalAffinity { get; }

        public bool PressureChangedAffinity =>
            Math.Abs(
                FinalAffinity -
                CanonicalAffinity
            ) > 0.0001f;

        public NormativeCentreDerivationEntry(
            TagAxis axis,
            TagPole pole,
            TagDegree recordedDegree,
            bool hasDirectCanon,
            float canonicalAffinity,
            float effectivePressure,
            float finalAffinity)
        {
            RequireAffinity(
                canonicalAffinity,
                nameof(canonicalAffinity)
            );

            RequirePressure(
                effectivePressure,
                nameof(effectivePressure)
            );

            RequireAffinity(
                finalAffinity,
                nameof(finalAffinity)
            );

            Axis =
                axis;

            Pole =
                pole;

            RecordedDegree =
                recordedDegree;

            HasDirectCanon =
                hasDirectCanon;

            CanonicalAffinity =
                canonicalAffinity;

            EffectivePressure =
                effectivePressure;

            FinalAffinity =
                finalAffinity;
        }

        private static void RequireAffinity(
            float value,
            string parameterName)
        {
            if (!IsFinite(value) ||
                value < -1f ||
                value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static void RequirePressure(
            float value,
            string parameterName)
        {
            if (!IsFinite(value) ||
                value < -1f ||
                value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static bool IsFinite(
            float value)
        {
            return
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}