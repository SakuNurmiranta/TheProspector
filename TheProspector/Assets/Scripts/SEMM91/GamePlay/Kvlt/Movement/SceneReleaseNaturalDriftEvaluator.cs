using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Calculates ordinary Natural Drift for the
    /// complete frozen qualifying Field population.
    ///
    /// NaturalDrift =
    /// ((S_release - S_field) / 6)
    /// * FieldDriftScale
    /// </summary>
    public sealed class
        SceneReleaseNaturalDriftEvaluator
    {
        public IReadOnlyList<
                SceneReleaseNaturalDriftEvaluation>
            Evaluate(
                SceneFieldSurfaceSnapshot snapshot,
                float fieldDriftScale)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(
                    nameof(snapshot)
                );
            }

            if (float.IsNaN(fieldDriftScale) ||
                float.IsInfinity(fieldDriftScale) ||
                fieldDriftScale < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fieldDriftScale),
                    fieldDriftScale,
                    "Field Drift Scale must be finite " +
                    "and non-negative."
                );
            }

            if (!snapshot.HasQualifyingPopulation)
            {
                return Array.Empty<
                    SceneReleaseNaturalDriftEvaluation>();
            }

            if (!snapshot.MeanSurface.HasValue)
            {
                throw new InvalidOperationException(
                    "Qualifying Field population has " +
                    "no mean Surface."
                );
            }

            float fieldMean =
                snapshot.MeanSurface.Value;

            List<
                SceneReleaseNaturalDriftEvaluation>
                results =
                    new();

            foreach (
                SceneFieldSurfaceEntry entry
                in snapshot.Entries)
            {
                float relativeSurface =
                    entry.Surface -
                    fieldMean;

                float naturalDrift;

                /*
                 * Explicitly preserve the design rule
                 * rather than merely relying on the
                 * arithmetic mean coincidentally
                 * producing zero.
                 */
                if (snapshot.PopulationCount == 1)
                {
                    naturalDrift =
                        0f;
                }
                else
                {
                    naturalDrift =
                        (
                            relativeSurface /
                            6f
                        ) *
                        fieldDriftScale;
                }

                results.Add(
                    new
                        SceneReleaseNaturalDriftEvaluation(
                            entry.SceneReleaseId,
                            entry.SourceDemoTapeId,
                            entry.SourceOwnerEntityId,
                            entry.SceneId,
                            entry.SettledTurn,
                            entry.StartFieldPosition,
                            entry.Surface,
                            fieldMean,
                            snapshot.PopulationCount,
                            fieldDriftScale,
                            naturalDrift
                        )
                );
            }

            return results.ToArray();
        }
    }
}