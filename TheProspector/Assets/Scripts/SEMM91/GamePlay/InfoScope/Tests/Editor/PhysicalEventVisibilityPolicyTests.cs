using NUnit.Framework;
using SEMM91.GamePlay.Actions;

namespace SEMM91.GamePlay.InfoScope
    .Tests.Editor
{
    public class
        PhysicalEventVisibilityPolicyTests
    {
        private PhysicalEventVisibilityPolicy
            _policy;

        [SetUp]
        public void SetUp()
        {
            _policy =
                new PhysicalEventVisibilityPolicy();
        }

        [Test]
        public void NullPayloadIsRejected()
        {
            PhysicalEventVisibilityContext context =
                new PhysicalEventVisibilityContext(
                    observerClientId: 2,
                    eventOwnerClientId: 1,
                    payload: null,
                    ObservedPhysicalEventPlanState
                        .Drafted
                );

            Assert.That(
                _policy.CanObserve(context),
                Is.False
            );
        }

        [Test]
        public void NonPhysicalActionIsRejected()
        {
            DraftedActionPayload payload =
                new DraftedActionPayload(
                    DraftedActionType.CreateIdea,
                    createdTurn: 0
                );

            PhysicalEventVisibilityContext context =
                new PhysicalEventVisibilityContext(
                    observerClientId: 2,
                    eventOwnerClientId: 1,
                    payload,
                    ObservedPhysicalEventPlanState
                        .Drafted
                );

            Assert.That(
                _policy.CanObserve(context),
                Is.False
            );
        }

        [TestCase(
            ObservedPhysicalEventPlanState.Drafted
        )]
        [TestCase(
            ObservedPhysicalEventPlanState.Committed
        )]
        public void ValidPromotionEventIsCurrentlyVisible(
            ObservedPhysicalEventPlanState planState)
        {
            DraftedActionPayload payload =
                DraftedActionPayload
                    .CreatePhysicalPromotionEvent(
                        createdTurn: 0,
                        physicalEventNodeId:
                            "PHYSICAL_NODE_04_04"
                    );

            PhysicalEventVisibilityContext context =
                new PhysicalEventVisibilityContext(
                    observerClientId: 2,
                    eventOwnerClientId: 1,
                    payload,
                    planState
                );

            Assert.That(
                _policy.CanObserve(context),
                Is.True
            );
        }

        [Test]
        public void OwnerCanObserveOwnPromotionEvent()
        {
            DraftedActionPayload payload =
                DraftedActionPayload
                    .CreatePhysicalPromotionEvent(
                        createdTurn: 0,
                        physicalEventNodeId:
                            "PHYSICAL_NODE_04_04"
                    );

            PhysicalEventVisibilityContext context =
                new PhysicalEventVisibilityContext(
                    observerClientId: 1,
                    eventOwnerClientId: 1,
                    payload,
                    ObservedPhysicalEventPlanState
                        .Drafted
                );

            Assert.That(
                _policy.CanObserve(context),
                Is.True
            );
        }
    }
}