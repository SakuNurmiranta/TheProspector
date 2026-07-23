using SEMM91.GamePlay.Actions;

namespace SEMM91.GamePlay.InfoScope
{
    /// <summary>
    /// Server-only input for determining whether one observer
    /// may receive information about a physical event.
    /// </summary>
    public readonly struct PhysicalEventVisibilityContext
    {
        public ulong ObserverClientId { get; }

        public ulong EventOwnerClientId { get; }

        public DraftedActionPayload Payload { get; }

        public ObservedPhysicalEventPlanState PlanState
        {
            get;
        }

        public PhysicalEventVisibilityContext(
            ulong observerClientId,
            ulong eventOwnerClientId,
            DraftedActionPayload payload,
            ObservedPhysicalEventPlanState planState)
        {
            ObserverClientId =
                observerClientId;

            EventOwnerClientId =
                eventOwnerClientId;

            Payload =
                payload;

            PlanState =
                planState;
        }
    }
}