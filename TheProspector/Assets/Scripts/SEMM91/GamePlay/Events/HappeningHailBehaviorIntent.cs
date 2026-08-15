using System;

namespace SEMM91.GamePlay.Events
{
    /// <summary>
    /// Declares an Aspect commitment to the same factual
    /// Behavior targeted by another EnactBehavior intent.
    /// This is the runtime seam that permits several Hails
    /// to contest one occurrence without inventing duplicate
    /// praxis facts.
    /// </summary>
    public sealed class HappeningHailBehaviorIntent :
        HappeningParticipantIntent
    {
        public string TargetBehaviorIntentId { get; }
        public string HailedAspectId { get; }

        public HappeningHailBehaviorIntent(
            string intentId,
            string happeningId,
            string contextId,
            string actorEntityId,
            int declaredTurn,
            string targetBehaviorIntentId,
            string hailedAspectId)
            : base(
                intentId,
                happeningId,
                contextId,
                actorEntityId,
                declaredTurn,
                HappeningIntentKind.EnactBehavior)
        {
            TargetBehaviorIntentId = RequireText(
                targetBehaviorIntentId,
                nameof(targetBehaviorIntentId));
            HailedAspectId = RequireText(
                hailedAspectId,
                nameof(hailedAspectId));
        }
    }
}
