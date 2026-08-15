using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltParadigmGripRecord
    {
        private readonly List<string> hailOccurrenceIds =
            new();

        public string EntityId { get; }
        public string HailAspectId { get; }
        public IReadOnlyList<string> HailOccurrenceIds =>
            hailOccurrenceIds;
        public int ReinforcementCount =>
            hailOccurrenceIds.Count;
        public int FirstReinforcedTurn { get; }
        public int LastReinforcedTurn { get; private set; }
        public int GripCount => ReinforcementCount;
        public int ReinforcedCount => ReinforcementCount;
        public int FirstTurn => FirstReinforcedTurn;
        public int LastTurn => LastReinforcedTurn;
        public string LastHailOccurrenceId =>
            hailOccurrenceIds[hailOccurrenceIds.Count - 1];

        internal KvltParadigmGripRecord(
            string entityId,
            string hailAspectId,
            string hailOccurrenceId,
            int reinforcedTurn)
        {
            EntityId = RequireText(entityId, nameof(entityId));
            HailAspectId = RequireText(
                hailAspectId,
                nameof(hailAspectId));

            hailOccurrenceIds.Add(
                RequireText(
                    hailOccurrenceId,
                    nameof(hailOccurrenceId)));

            if (reinforcedTurn < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(reinforcedTurn));

            FirstReinforcedTurn = reinforcedTurn;
            LastReinforcedTurn = reinforcedTurn;
        }

        internal bool Reinforce(
            string hailOccurrenceId,
            int reinforcedTurn)
        {
            string normalized = RequireText(
                hailOccurrenceId,
                nameof(hailOccurrenceId));

            if (reinforcedTurn < LastReinforcedTurn)
                throw new ArgumentOutOfRangeException(
                    nameof(reinforcedTurn));

            if (hailOccurrenceIds.Contains(normalized))
                return false;

            hailOccurrenceIds.Add(normalized);
            LastReinforcedTurn = reinforcedTurn;
            return true;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "Grip provenance cannot be empty.",
                    parameterName);

            return value.Trim();
        }
    }
}
