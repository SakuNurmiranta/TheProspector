using System;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltParadigmBeefRecord
    {
        public string FirstHailAspectId { get; }
        public string SecondHailAspectId { get; }
        public string BehaviorTypeId { get; }

        public string FirstHappeningId { get; }
        public string LastHappeningId { get; private set; }

        public string FirstBehaviorOccurrenceId { get; }
        public string LastBehaviorOccurrenceId { get; private set; }

        public int EstablishedTurn { get; }
        public int LastReinforcedTurn { get; private set; }

        public int ReinforcementCount { get; private set; }

        internal KvltParadigmBeefRecord(
            KvltParadigmOpposition opposition,
            string behaviorTypeId,
            string happeningId,
            string behaviorOccurrenceId,
            int settledTurn)
        {
            FirstHailAspectId =
                opposition.FirstHailAspectId;

            SecondHailAspectId =
                opposition.SecondHailAspectId;

            BehaviorTypeId =
                behaviorTypeId;

            FirstHappeningId =
                happeningId;

            LastHappeningId =
                happeningId;

            FirstBehaviorOccurrenceId =
                behaviorOccurrenceId;

            LastBehaviorOccurrenceId =
                behaviorOccurrenceId;

            EstablishedTurn =
                settledTurn;

            LastReinforcedTurn =
                settledTurn;

            ReinforcementCount = 1;
        }

        internal bool Matches(
            KvltParadigmOpposition opposition,
            string behaviorTypeId)
        {
            return
                FirstHailAspectId ==
                    opposition.FirstHailAspectId &&
                SecondHailAspectId ==
                    opposition.SecondHailAspectId &&
                BehaviorTypeId ==
                    behaviorTypeId;
        }

        internal void Reinforce(
            string happeningId,
            string behaviorOccurrenceId,
            int settledTurn)
        {
            if (settledTurn <
                LastReinforcedTurn)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            LastHappeningId =
                happeningId;

            LastBehaviorOccurrenceId =
                behaviorOccurrenceId;

            LastReinforcedTurn =
                settledTurn;

            ReinforcementCount++;
        }
    }
}