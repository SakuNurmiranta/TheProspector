using System;

namespace SEMM91.GamePlay.Kvlt.Normative
{
    /// <summary>
    /// Scenario calibration for blending previously
    /// settled Effective Scene Pressure into the
    /// Canon-derived Normative Centre.
    ///
    /// Effective Pressure magnitude is interpreted as
    /// interpolation strength in [0, 1].
    /// </summary>
    public sealed class NormativePressureBlendPolicy
    {
        public float
            ProvisionalAffinityCeiling { get; }

        public NormativePressureBlendPolicy(
            float provisionalAffinityCeiling)
        {
            if (!IsFinite(
                    provisionalAffinityCeiling) ||
                provisionalAffinityCeiling <= 0f ||
                provisionalAffinityCeiling >= 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(
                        provisionalAffinityCeiling
                    ),
                    "Pressure-derived affinity must " +
                    "remain strictly weaker than " +
                    "direct Canon."
                );
            }

            ProvisionalAffinityCeiling =
                provisionalAffinityCeiling;
        }

        public float Blend(
            float canonicalAffinity,
            float effectivePressure,
            bool hasDirectCanon)
        {
            RequireAffinity(
                canonicalAffinity,
                nameof(canonicalAffinity)
            );

            if (!IsFinite(effectivePressure) ||
                effectivePressure < -1f ||
                effectivePressure > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectivePressure),
                    "Effective Pressure used for " +
                    "Normative derivation must be " +
                    "within [-1, +1]."
                );
            }

            if (effectivePressure == 0f)
            {
                return canonicalAffinity;
            }

            float strength =
                Math.Abs(effectivePressure);

            /*
             * Direct Canon remains primary.
             *
             * Positive Pressure cannot strengthen a
             * directly canonical polarity.
             */
            if (hasDirectCanon)
            {
                if (effectivePressure > 0f)
                {
                    return canonicalAffinity;
                }

                /*
                 * Negative Pressure acts against the
                 * directly canonical polarity.
                 *
                 * If current recorded degree still has
                 * positive Ratchet affinity, Pressure
                 * may weaken it toward neutrality but
                 * cannot invert it.
                 *
                 * If Ratchet already evaluates the
                 * recorded degree as neutral/negative,
                 * counter-pressure may deepen that
                 * insufficiency toward -1.
                 */
                float target =
                    canonicalAffinity > 0f
                        ? 0f
                        : -1f;

                return ClampAffinity(
                    Lerp(
                        canonicalAffinity,
                        target,
                        strength
                    )
                );
            }

            /*
             * Non-direct relationships may move toward
             * a weak provisional Pressure-derived
             * treatment.
             *
             * This is how:
             *
             * Canon-repelled
             * → softened
             * → neutral
             * → weakly attractive
             *
             * becomes possible without Pressure ever
             * becoming direct Canon.
             */
            float pressureTarget =
                effectivePressure > 0f
                    ? ProvisionalAffinityCeiling
                    : -ProvisionalAffinityCeiling;

            return ClampAffinity(
                Lerp(
                    canonicalAffinity,
                    pressureTarget,
                    strength
                )
            );
        }

        private static float Lerp(
            float from,
            float to,
            float amount)
        {
            return
                from +
                (to - from) *
                amount;
        }

        private static float ClampAffinity(
            float value)
        {
            if (value < -1f)
            {
                return -1f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
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

        private static bool IsFinite(
            float value)
        {
            return
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}