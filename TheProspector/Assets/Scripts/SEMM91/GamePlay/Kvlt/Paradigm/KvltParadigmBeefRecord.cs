using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltParadigmBeefRecord
    {
        private readonly List<string> behaviorOccurrenceIds =
            new();

        public KvltParadigmOpposition Opposition { get; }
        public string BehaviorTypeId { get; }
        public string FirstHappeningId { get; }
        public string LastHappeningId { get; private set; }
        public IReadOnlyList<string> BehaviorOccurrenceIds =>
            behaviorOccurrenceIds;
        public int ReinforcementCount =>
            behaviorOccurrenceIds.Count;
        public int FirstSettledTurn { get; }
        public int LastSettledTurn { get; private set; }
        public string FirstHailAspectId =>
            Opposition.FirstHailAspectId;
        public string SecondHailAspectId =>
            Opposition.SecondHailAspectId;
        public string HappeningId => FirstHappeningId;
        public string LatestBehaviorOccurrenceId =>
            behaviorOccurrenceIds[behaviorOccurrenceIds.Count - 1];
        public int ReinforcedCount => ReinforcementCount;

        internal KvltParadigmBeefRecord(
            KvltParadigmOpposition opposition,
            string behaviorTypeId,
            string happeningId,
            string behaviorOccurrenceId,
            int settledTurn)
        {
            Opposition = opposition ??
                throw new ArgumentNullException(nameof(opposition));
            BehaviorTypeId = RequireText(
                behaviorTypeId,
                nameof(behaviorTypeId));
            FirstHappeningId = RequireText(
                happeningId,
                nameof(happeningId));
            LastHappeningId = FirstHappeningId;

            behaviorOccurrenceIds.Add(
                RequireText(
                    behaviorOccurrenceId,
                    nameof(behaviorOccurrenceId)));

            if (settledTurn < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn));

            FirstSettledTurn = settledTurn;
            LastSettledTurn = settledTurn;
        }

        internal bool Matches(
            KvltParadigmOpposition opposition,
            string behaviorTypeId)
        {
            return opposition != null &&
                   Opposition.Matches(
                       opposition.FirstHailAspectId,
                       opposition.SecondHailAspectId) &&
                   BehaviorTypeId == behaviorTypeId;
        }

        internal bool Reinforce(
            string happeningId,
            string behaviorOccurrenceId,
            int settledTurn)
        {
            string normalizedHappeningId = RequireText(
                happeningId,
                nameof(happeningId));
            string normalized = RequireText(
                behaviorOccurrenceId,
                nameof(behaviorOccurrenceId));

            if (settledTurn < LastSettledTurn)
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn));

            if (behaviorOccurrenceIds.Contains(normalized))
                return false;

            behaviorOccurrenceIds.Add(normalized);
            LastHappeningId = normalizedHappeningId;
            LastSettledTurn = settledTurn;
            return true;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "Beef provenance cannot be empty.",
                    parameterName);

            return value.Trim();
        }
    }
}
