using System;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltParadigmGripRecord
    {
        public string EntityId { get; }
        public string HailAspectId { get; }

        public string FirstHailOccurrenceId { get; }
        public string LastHailOccurrenceId { get; private set; }

        public int FirstReinforcedTurn { get; }
        public int LastReinforcedTurn { get; private set; }

        public int ReinforcementCount { get; private set; }

        internal KvltParadigmGripRecord(
            string entityId,
            string hailAspectId,
            string hailOccurrenceId,
            int reinforcedTurn)
        {
            EntityId = entityId;
            HailAspectId = hailAspectId;

            FirstHailOccurrenceId =
                hailOccurrenceId;

            LastHailOccurrenceId =
                hailOccurrenceId;

            FirstReinforcedTurn =
                reinforcedTurn;

            LastReinforcedTurn =
                reinforcedTurn;

            ReinforcementCount = 1;
        }

        internal void Reinforce(
            string hailOccurrenceId,
            int reinforcedTurn)
        {
            if (reinforcedTurn <
                LastReinforcedTurn)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reinforcedTurn)
                );
            }

            LastHailOccurrenceId =
                hailOccurrenceId;

            LastReinforcedTurn =
                reinforcedTurn;

            ReinforcementCount++;
        }
    }
}