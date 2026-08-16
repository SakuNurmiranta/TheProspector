namespace SEMM91.GamePlay.Events
{
    public sealed class HappeningOpposeIntent :
        HappeningParticipantIntent
    {
        public string TargetIntentId { get; }

        public HappeningOpposeIntent(
            string intentId,
            string happeningId,
            string contextId,
            string actorEntityId,
            int declaredTurn,
            string targetIntentId)
            : base(
                intentId,
                happeningId,
                contextId,
                actorEntityId,
                declaredTurn,
                HappeningIntentKind.Oppose
            )
        {
            TargetIntentId =
                RequireText(
                    targetIntentId,
                    nameof(targetIntentId)
                );

            if (TargetIntentId == IntentId)
            {
                throw new System.ArgumentException(
                    "An Intent cannot oppose itself.",
                    nameof(targetIntentId)
                );
            }
        }
    }
}