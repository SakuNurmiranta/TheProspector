using System;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltPoserDeclaration
    {
        public string DeclarationId { get; }
        public string EntityId { get; }
        public string SourceHappeningId { get; }
        public string SourceBehaviorOccurrenceId { get; }
        public string LosingHailAspectId { get; }
        public string WinningHailAspectId { get; }
        public int DeclaredDuringTurn { get; }
        public int DurationTurns { get; }
        public int ActiveFromTurn => DeclaredDuringTurn + 1;
        public int StartsTurn => ActiveFromTurn;
        public int ActiveUntilTurnExclusive =>
            ActiveFromTurn + DurationTurns;
        public int StartsAtTurn => ActiveFromTurn;
        public int ExpiresAtTurnExclusive =>
            ActiveUntilTurnExclusive;
        public int PoserdomTurns => DurationTurns;

        public KvltPoserDeclaration(
            string declarationId,
            string entityId,
            string sourceHappeningId,
            string sourceBehaviorOccurrenceId,
            string losingHailAspectId,
            string winningHailAspectId,
            int declaredDuringTurn,
            int durationTurns)
        {
            DeclarationId = RequireText(
                declarationId,
                nameof(declarationId));
            EntityId = RequireText(entityId, nameof(entityId));
            SourceHappeningId = RequireText(
                sourceHappeningId,
                nameof(sourceHappeningId));
            SourceBehaviorOccurrenceId = RequireText(
                sourceBehaviorOccurrenceId,
                nameof(sourceBehaviorOccurrenceId));
            LosingHailAspectId = RequireText(
                losingHailAspectId,
                nameof(losingHailAspectId));
            WinningHailAspectId = RequireText(
                winningHailAspectId,
                nameof(winningHailAspectId));

            if (declaredDuringTurn < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(declaredDuringTurn));

            if (durationTurns <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(durationTurns));

            DeclaredDuringTurn = declaredDuringTurn;
            DurationTurns = durationTurns;
        }

        public bool IsActiveAt(int playableTurn)
        {
            return playableTurn >= ActiveFromTurn &&
                   playableTurn < ActiveUntilTurnExclusive;
        }

        public int GetRemainingTurnsAt(int playableTurn)
        {
            if (!IsActiveAt(playableTurn))
                return 0;

            return ActiveUntilTurnExclusive - playableTurn;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "Poser provenance cannot be empty.",
                    parameterName);

            return value.Trim();
        }
    }
}
