using System;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Complete calibrated policy required by one
    /// Camp-3 KVLT Scene settlement.
    ///
    /// These values will later come from Scenario
    /// Profile rather than being hard-coded here.
    /// </summary>
    public sealed class KvltSceneSettlementPolicy
    {
        public float FieldDriftScale { get; }

        public float BreakthroughDriftMultiplier { get; }

        public float NexusBoundary { get; }

        public float OuterBoundary { get; }

        public SceneStandingProjectionPolicy
            StandingProjectionPolicy { get; }

        public ScenePressureRebuildPolicy
            PressureRebuildPolicy { get; }

        public NormativePressureBlendPolicy
            NormativePressureBlendPolicy { get; }

        public KvltSceneSettlementPolicy(
            float fieldDriftScale,
            float breakthroughDriftMultiplier,
            float nexusBoundary,
            float outerBoundary,
            SceneStandingProjectionPolicy
                standingProjectionPolicy,
            ScenePressureRebuildPolicy
                pressureRebuildPolicy,
            NormativePressureBlendPolicy
                normativePressureBlendPolicy)
        {
            RequireNonNegativeFinite(
                fieldDriftScale,
                nameof(fieldDriftScale)
            );

            RequireNonNegativeFinite(
                breakthroughDriftMultiplier,
                nameof(breakthroughDriftMultiplier)
            );

            RequireFinite(
                nexusBoundary,
                nameof(nexusBoundary)
            );

            RequireFinite(
                outerBoundary,
                nameof(outerBoundary)
            );

            if (outerBoundary >= nexusBoundary)
            {
                throw new ArgumentException(
                    "Outer Boundary must remain outward " +
                    "from the Nexus Boundary."
                );
            }

            StandingProjectionPolicy =
                standingProjectionPolicy ??
                throw new ArgumentNullException(
                    nameof(standingProjectionPolicy)
                );

            PressureRebuildPolicy =
                pressureRebuildPolicy ??
                throw new ArgumentNullException(
                    nameof(pressureRebuildPolicy)
                );

            NormativePressureBlendPolicy =
                normativePressureBlendPolicy ??
                throw new ArgumentNullException(
                    nameof(normativePressureBlendPolicy)
                );

            FieldDriftScale =
                fieldDriftScale;

            BreakthroughDriftMultiplier =
                breakthroughDriftMultiplier;

            NexusBoundary =
                nexusBoundary;

            OuterBoundary =
                outerBoundary;
        }

        private static void RequireFinite(
            float value,
            string parameterName)
        {
            if (float.IsNaN(value) ||
                float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static void RequireNonNegativeFinite(
            float value,
            string parameterName)
        {
            RequireFinite(
                value,
                parameterName
            );

            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }
    }
}