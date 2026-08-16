using System;

namespace SEMM91.GamePlay.Kvlt.Transgression
{
    public sealed class AllegianceCrisisResolution
    {
        public AllegianceCrisisOutcome Outcome { get; }

        public int KvltVoteCount { get; }

        public int SocietyVoteCount { get; }

        public int WeightedKvltVoteCount { get; }

        public int WeightedSocietyVoteCount { get; }

        public float CohesionFracture { get; }

        public bool KeeperVoted { get; }

        public AllegianceChoice? KeeperChoice { get; }

        public int ResolvedTurn { get; }

        public AllegianceCrisisResolution(
            AllegianceCrisisOutcome outcome,
            int kvltVoteCount,
            int societyVoteCount,
            int weightedKvltVoteCount,
            int weightedSocietyVoteCount,
            float cohesionFracture,
            bool keeperVoted,
            AllegianceChoice? keeperChoice,
            int resolvedTurn)
        {
            if (!Enum.IsDefined(
                    typeof(AllegianceCrisisOutcome),
                    outcome))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(outcome)
                );
            }

            if (kvltVoteCount < 0 ||
                societyVoteCount < 0 ||
                weightedKvltVoteCount < 0 ||
                weightedSocietyVoteCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "Vote counts cannot be negative."
                );
            }

            if (cohesionFracture < 0f ||
                cohesionFracture > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cohesionFracture)
                );
            }

            if (keeperVoted !=
                keeperChoice.HasValue)
            {
                throw new ArgumentException(
                    "Keeper vote flag and choice " +
                    "must agree."
                );
            }

            if (resolvedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resolvedTurn)
                );
            }

            Outcome =
                outcome;

            KvltVoteCount =
                kvltVoteCount;

            SocietyVoteCount =
                societyVoteCount;

            WeightedKvltVoteCount =
                weightedKvltVoteCount;

            WeightedSocietyVoteCount =
                weightedSocietyVoteCount;

            CohesionFracture =
                cohesionFracture;

            KeeperVoted =
                keeperVoted;

            KeeperChoice =
                keeperChoice;

            ResolvedTurn =
                resolvedTurn;
        }
    }
}