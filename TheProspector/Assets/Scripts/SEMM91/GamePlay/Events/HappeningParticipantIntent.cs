using System;

namespace SEMM91.GamePlay.Events
{
    public abstract class HappeningParticipantIntent
    {
        public string IntentId { get; }

        public string HappeningId { get; }

        public string ContextId { get; }

        public string ActorEntityId { get; }

        public int DeclaredTurn { get; }

        public HappeningIntentKind Kind { get; }

        protected HappeningParticipantIntent(
            string intentId,
            string happeningId,
            string contextId,
            string actorEntityId,
            int declaredTurn,
            HappeningIntentKind kind)
        {
            IntentId =
                RequireText(
                    intentId,
                    nameof(intentId)
                );

            HappeningId =
                RequireText(
                    happeningId,
                    nameof(happeningId)
                );

            ContextId =
                RequireText(
                    contextId,
                    nameof(contextId)
                );

            ActorEntityId =
                RequireText(
                    actorEntityId,
                    nameof(actorEntityId)
                );

            if (declaredTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(declaredTurn),
                    declaredTurn,
                    "Intent turn cannot be negative."
                );
            }

            if (!Enum.IsDefined(
                    typeof(HappeningIntentKind),
                    kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    kind,
                    "Invalid Happening Intent kind."
                );
            }

            DeclaredTurn =
                declaredTurn;

            Kind =
                kind;
        }

        protected static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Happening Intent provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}