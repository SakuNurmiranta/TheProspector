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
        public int StartsTurn { get; }
        public int DurationTurns { get; }
        public int ExpiresAtTurnExclusive { get; }

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
            DeclarationId =
                RequireText(
                    declarationId,
                    nameof(declarationId)
                );

            EntityId =
                RequireText(
                    entityId,
                    nameof(entityId)
                );

            SourceHappeningId =
                RequireText(
                    sourceHappeningId,
                    nameof(sourceHappeningId)
                );

            SourceBehaviorOccurrenceId =
                RequireText(
                    sourceBehaviorOccurrenceId,
                    nameof(sourceBehaviorOccurrenceId)
                );

            LosingHailAspectId =
                RequireText(
                    losingHailAspectId,
                    nameof(losingHailAspectId)
                );

            WinningHailAspectId =
                RequireText(
                    winningHailAspectId,
                    nameof(winningHailAspectId)
                );

            if (LosingHailAspectId ==
                WinningHailAspectId)
            {
                throw new ArgumentException(
                    "Winning and losing paradigms " +
                    "must be distinct."
                );
            }

            if (declaredDuringTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(declaredDuringTurn)
                );
            }

            if (durationTurns <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationTurns)
                );
            }

            DeclaredDuringTurn =
                declaredDuringTurn;

            StartsTurn =
                declaredDuringTurn + 1;

            DurationTurns =
                durationTurns;

            ExpiresAtTurnExclusive =
                StartsTurn + durationTurns;
        }

        public bool IsActiveAt(int globalTurn)
        {
            return
                globalTurn >= StartsTurn &&
                globalTurn <
                    ExpiresAtTurnExclusive;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Poser provenance cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}