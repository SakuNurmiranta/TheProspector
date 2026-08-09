using System;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    /// <summary>
    /// Immutable scene-level context consumed by Track
    /// legitimacy evaluation.
    ///
    /// The environment snapshots mutable Society state
    /// at construction time and references only immutable
    /// NormativeCentre instances.
    /// </summary>
    public sealed class TrackEvaluationEnvironment
    {
        private readonly SocietyNormativeProfile
            societyNormsSnapshot =
                new SocietyNormativeProfile();

        public int SettledTurn { get; }

        /// <summary>
        /// Current scene interpretation:
        /// Canon + previously settled Effective Pressure.
        /// </summary>
        public NormativeCentre CurrentNormativeCentre
        {
            get;
        }

        /// <summary>
        /// Canon-only interpretation used by
        /// Influence Potential.
        /// </summary>
        public NormativeCentre CanonNormativeCentre
        {
            get;
        }

        public TrackEvaluationEnvironment(
            int settledTurn,
            NormativeCentre currentNormativeCentre,
            NormativeCentre canonNormativeCentre,
            SocietyNormativeProfile societyNorms)
        {
            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn),
                    settledTurn,
                    "Settled turn cannot be negative."
                );
            }

            CurrentNormativeCentre =
                currentNormativeCentre ??
                throw new ArgumentNullException(
                    nameof(currentNormativeCentre)
                );

            CanonNormativeCentre =
                canonNormativeCentre ??
                throw new ArgumentNullException(
                    nameof(canonNormativeCentre)
                );

            if (societyNorms == null)
            {
                throw new ArgumentNullException(
                    nameof(societyNorms)
                );
            }

            SettledTurn = settledTurn;

            CopySocietyNorms(
                societyNorms
            );
        }

        public bool TryGetSocietyNormativeDegree(
            TagAxis axis,
            TagPole pole,
            out TagDegree degree)
        {
            return societyNormsSnapshot
                .TryGetNormativeDegree(
                    axis,
                    pole,
                    out degree
                );
        }

        public float GetSocietyNormativeForce(
            TagAxis axis,
            TagPole pole)
        {
            return societyNormsSnapshot
                .GetNormativeForce(
                    axis,
                    pole
                );
        }

        private void CopySocietyNorms(
            SocietyNormativeProfile source)
        {
            foreach (TagAxis axis
                     in Enum.GetValues(
                         typeof(TagAxis)))
            {
                foreach (TagPole pole
                         in Enum.GetValues(
                             typeof(TagPole)))
                {
                    if (!source.TryGetNormativeDegree(
                            axis,
                            pole,
                            out TagDegree degree))
                    {
                        continue;
                    }

                    societyNormsSnapshot
                        .SetNormativeDegree(
                            axis,
                            pole,
                            degree
                        );
                }
            }
        }
    }
}