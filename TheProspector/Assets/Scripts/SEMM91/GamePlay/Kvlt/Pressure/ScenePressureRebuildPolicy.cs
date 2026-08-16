using System;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Canon;

namespace SEMM91.GamePlay.Kvlt.Pressure
{
    /// <summary>
    /// Scenario calibration for converting aggregated
    /// Raw Field Pressure into the bounded Effective
    /// Pressure delta consumed by next-turn Normative
    /// Centre derivation.
    ///
    /// Effective Pressure remains weaker than direct
    /// Canon affinity.
    /// </summary>
    public sealed class ScenePressureRebuildPolicy
    {
        public float NeutralDirectionScale { get; }

        public float CounterCanonicalScale { get; }

        public float MaxAbsoluteEffectivePressure
        {
            get;
        }

        public ScenePressureRebuildPolicy(
            float neutralDirectionScale,
            float counterCanonicalScale,
            float maxAbsoluteEffectivePressure)
        {
            RequireNonNegativeFinite(
                neutralDirectionScale,
                nameof(neutralDirectionScale)
            );

            RequireNonNegativeFinite(
                counterCanonicalScale,
                nameof(counterCanonicalScale)
            );

            if (!IsFinite(
                    maxAbsoluteEffectivePressure) ||
                maxAbsoluteEffectivePressure <= 0f ||
                maxAbsoluteEffectivePressure >= 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(
                        maxAbsoluteEffectivePressure
                    ),
                    "Effective Pressure must remain " +
                    "strictly weaker than direct " +
                    "canonical affinity."
                );
            }

            NeutralDirectionScale =
                neutralDirectionScale;

            CounterCanonicalScale =
                counterCanonicalScale;

            MaxAbsoluteEffectivePressure =
                maxAbsoluteEffectivePressure;
        }

        public float ResolveEffectivePressure(
            CanonState canon,
            TagAxis axis,
            TagPole pole,
            float rawPressure)
        {
            if (canon == null)
            {
                throw new ArgumentNullException(
                    nameof(canon)
                );
            }

            if (!IsFinite(rawPressure))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rawPressure)
                );
            }

            if (rawPressure == 0f)
            {
                return 0f;
            }

            TagPole opposite =
                GetOppositePole(
                    pole
                );

            bool hasDirectCanon =
                canon.HasPrecedent(
                    axis,
                    pole
                );

            bool hasOppositeCanon =
                canon.HasPrecedent(
                    axis,
                    opposite
                );

            /*
             * If both polarities have direct canonical
             * precedent, the axis is already
             * institutionally represented in both
             * directions. Temporary corpus Pressure
             * does not strengthen either side.
             */
            if (hasDirectCanon &&
                hasOppositeCanon)
            {
                return 0f;
            }

            float scale;

            if (rawPressure > 0f)
            {
                /*
                 * Positive Pressure toward an already
                 * canonical direction cannot make it
                 * "more canonical."
                 */
                if (hasDirectCanon)
                {
                    return 0f;
                }

                /*
                 * Positive Pressure toward a polarity
                 * rejected by opposite Canon may
                 * soften that rejection.
                 */
                scale =
                    hasOppositeCanon
                        ? CounterCanonicalScale
                        : NeutralDirectionScale;
            }
            else
            {
                /*
                 * Negative Pressure against a polarity
                 * already opposed by opposite Canon
                 * merely reinforces the same
                 * institutional direction.
                 */
                if (hasOppositeCanon)
                {
                    return 0f;
                }

                /*
                 * Negative Pressure against a directly
                 * canonical polarity is meaningful
                 * counter-pressure.
                 */
                scale =
                    hasDirectCanon
                        ? CounterCanonicalScale
                        : NeutralDirectionScale;
            }

            float effective =
                rawPressure *
                scale;

            return Math.Clamp(
                effective,
                -MaxAbsoluteEffectivePressure,
                MaxAbsoluteEffectivePressure
            );
        }

        private static TagPole GetOppositePole(
            TagPole pole)
        {
            return pole switch
            {
                TagPole.Negative =>
                    TagPole.Positive,

                TagPole.Positive =>
                    TagPole.Negative,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(pole)
                    )
            };
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