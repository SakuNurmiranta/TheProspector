using SEMM91.GamePlay.Actions;

namespace SEMM91.GamePlay.InfoScope
{
    /// <summary>
    /// Server-authoritative gate controlling which physical-event
    /// projections are replicated to each observer.
    /// </summary>
    public sealed class PhysicalEventVisibilityPolicy
    {
        public bool CanObserve(
            PhysicalEventVisibilityContext context)
        {
            DraftedActionPayload payload =
                context.Payload;

            if (payload == null)
                return false;

            if (payload.ActionType !=
                DraftedActionType
                    .ReleaseLatestDemoToKvlt)
            {
                return false;
            }

            if (!payload.HasPhysicalEventLocation)
                return false;

            /*
             * Initial InfoScope rule:
             * every connected observer receives every valid
             * physical Promotion event.
             *
             * Future disclosure and observation rules replace
             * this unconditional acceptance.
             */
            return true;
        }
    }
}