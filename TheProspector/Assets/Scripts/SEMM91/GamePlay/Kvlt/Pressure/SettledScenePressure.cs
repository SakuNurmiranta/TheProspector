using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Pressure
{
    [Serializable]
    public sealed class SettledScenePressure
    {
        private readonly Dictionary<
            (TagAxis Axis, TagPole Pole),
            ScenePressureEntry
        > entries = new();

        public static SettledScenePressure Empty { get; } =
            new SettledScenePressure(
                Array.Empty<ScenePressureEntry>()
            );

        public int EntryCount =>
            entries.Count;

        public SettledScenePressure(
            IEnumerable<ScenePressureEntry> sourceEntries)
        {
            if (sourceEntries == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceEntries)
                );
            }

            foreach (ScenePressureEntry entry
                     in sourceEntries)
            {
                if (entry == null)
                {
                    throw new ArgumentException(
                        "Settled Scene Pressure cannot contain a null entry.",
                        nameof(sourceEntries)
                    );
                }

                var key =
                    (
                        entry.Axis,
                        entry.Pole
                    );

                if (entries.ContainsKey(key))
                {
                    throw new ArgumentException(
                        "Settled Scene Pressure contains a duplicate Tag key | " +
                        $"axis={entry.Axis} | pole={entry.Pole}",
                        nameof(sourceEntries)
                    );
                }

                if (entry.RawPressure == 0f &&
                    entry.EffectivePressure == 0f)
                {
                    continue;
                }

                entries.Add(
                    key,
                    entry
                );
            }
        }

        public float GetRawPressure(
            TagAxis axis,
            TagPole pole)
        {
            if (entries.TryGetValue(
                    (axis, pole),
                    out ScenePressureEntry entry))
            {
                return entry.RawPressure;
            }

            return 0f;
        }

        public float GetEffectivePressure(
            TagAxis axis,
            TagPole pole)
        {
            if (entries.TryGetValue(
                    (axis, pole),
                    out ScenePressureEntry entry))
            {
                return entry.EffectivePressure;
            }

            return 0f;
        }
    }
}