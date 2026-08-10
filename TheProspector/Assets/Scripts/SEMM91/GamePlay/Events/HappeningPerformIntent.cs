namespace SEMM91.GamePlay.Events
{
    public sealed class HappeningPerformIntent :
        HappeningParticipantIntent
    {
        public string TargetSceneReleaseId { get; }

        public HappeningPerformIntent(
            string intentId,
            string happeningId,
            string contextId,
            string actorEntityId,
            int declaredTurn,
            string targetSceneReleaseId)
            : base(
                intentId,
                happeningId,
                contextId,
                actorEntityId,
                declaredTurn,
                HappeningIntentKind.Perform
            )
        {
            TargetSceneReleaseId =
                RequireText(
                    targetSceneReleaseId,
                    nameof(targetSceneReleaseId)
                );
        }
    }
}