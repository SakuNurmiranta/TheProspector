namespace SEMM91.GamePlay.Events
{
    public sealed class HappeningSupportIntent :
        HappeningParticipantIntent
    {
        public string TargetIntentId { get; }

        public HappeningSupportIntent(
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
                HappeningIntentKind.Support
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
                    "An Intent cannot support itself.",
                    nameof(targetIntentId)
                );
            }
        }
    }
}