using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Pressure
{
    [Serializable]
    public sealed class ScenePressureEntry
    {
        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public float RawPressure { get; }

        public float EffectivePressure { get; }

        public ScenePressureEntry(
            TagAxis axis,
            TagPole pole,
            float rawPressure,
            float effectivePressure)
        {
            if (!IsFinite(rawPressure))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rawPressure),
                    rawPressure,
                    "Raw Scene Pressure must be finite."
                );
            }

            if (!IsFinite(effectivePressure))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectivePressure),
                    effectivePressure,
                    "Effective Scene Pressure must be finite."
                );
            }

            Axis = axis;
            Pole = pole;
            RawPressure = rawPressure;
            EffectivePressure = effectivePressure;
        }

        private static bool IsFinite(
            float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }
}