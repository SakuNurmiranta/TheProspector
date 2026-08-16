using System;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Standing
{
    /// <summary>
    /// Calibration policy for projecting remembered
    /// SceneRelease history into Scene Standing.
    ///
    /// Defaults intentionally do not live here.
    /// A later Scenario Profile supplies these
    /// coefficients.
    /// </summary>
    public sealed class
        SceneStandingProjectionPolicy
    {
        public float
            CanonLegacyBaseWeight { get; }

        public float
            CanonLegacyBreakthroughDegreeWeight { get; }

        public float
            RejectionScarBaseWeight { get; }

        public float
            RejectionPeakPenetrationWeight { get; }

        public float
            RejectionOutwardOvershootWeight { get; }

        public SceneStandingProjectionPolicy(
            float canonLegacyBaseWeight,
            float canonLegacyBreakthroughDegreeWeight,
            float rejectionScarBaseWeight,
            float rejectionPeakPenetrationWeight,
            float rejectionOutwardOvershootWeight)
        {
            RequirePositiveFinite(
                canonLegacyBaseWeight,
                nameof(canonLegacyBaseWeight)
            );

            RequireNonNegativeFinite(
                canonLegacyBreakthroughDegreeWeight,
                nameof(
                    canonLegacyBreakthroughDegreeWeight
                )
            );

            RequirePositiveFinite(
                rejectionScarBaseWeight,
                nameof(rejectionScarBaseWeight)
            );

            RequireNonNegativeFinite(
                rejectionPeakPenetrationWeight,
                nameof(
                    rejectionPeakPenetrationWeight
                )
            );

            RequireNonNegativeFinite(
                rejectionOutwardOvershootWeight,
                nameof(
                    rejectionOutwardOvershootWeight
                )
            );

            CanonLegacyBaseWeight =
                canonLegacyBaseWeight;

            CanonLegacyBreakthroughDegreeWeight =
                canonLegacyBreakthroughDegreeWeight;

            RejectionScarBaseWeight =
                rejectionScarBaseWeight;

            RejectionPeakPenetrationWeight =
                rejectionPeakPenetrationWeight;

            RejectionOutwardOvershootWeight =
                rejectionOutwardOvershootWeight;
        }

        public float CalculateCanonLegacyWeight(
            int uniqueBreakthroughMagnitude)
        {
            if (uniqueBreakthroughMagnitude < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(
                        uniqueBreakthroughMagnitude
                    )
                );
            }

            double weight =
                CanonLegacyBaseWeight +
                uniqueBreakthroughMagnitude *
                (double)
                CanonLegacyBreakthroughDegreeWeight;

            return RequireCalculatedWeight(
                weight,
                "Canon Legacy"
            );
        }

        public float CalculateRejectionScarWeight(
            SceneReleaseRejectionState rejection)
        {
            if (rejection == null)
            {
                throw new ArgumentNullException(
                    nameof(rejection)
                );
            }

            /*
             * Position increases inward.
             *
             * This measures how far the release had
             * penetrated inward from the rejection
             * boundary before its eventual expulsion.
             */
            float priorInwardPenetration =
                Math.Max(
                    0f,
                    rejection.PeakInwardPosition -
                    rejection.OuterBoundary
                );

            double weight =
                RejectionScarBaseWeight +
                priorInwardPenetration *
                (double)
                RejectionPeakPenetrationWeight +
                rejection.OutwardOvershoot *
                (double)
                RejectionOutwardOvershootWeight;

            return RequireCalculatedWeight(
                weight,
                "Rejection Scar"
            );
        }

        private static float
            RequireCalculatedWeight(
                double value,
                string label)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                value <= 0d ||
                value > float.MaxValue)
            {
                throw new InvalidOperationException(
                    label +
                    " calibration produced an invalid " +
                    "Scene Standing weight."
                );
            }

            return (float)value;
        }

        private static void
            RequirePositiveFinite(
                float value,
                string parameterName)
        {
            if (!IsFinite(value) ||
                value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static void
            RequireNonNegativeFinite(
                float value,
                string parameterName)
        {
            if (!IsFinite(value) ||
                value < 0f)
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