using NUnit.Framework;
using SEMM91.GamePlay.Actions;

namespace SEMM91.GamePlay.InfoScope.Tests.Editor
{
    public class
        ObservedPhysicalEventSummaryTests
    {
        [Test]
        public void PromotionDraftCreatesPhysicalEventSummary()
        {
            DraftedActionPayload payload =
                DraftedActionPayload
                    .CreatePhysicalPromotionEvent(
                        createdTurn: 2,
                        physicalEventNodeId:
                            "PHYSICAL_NODE_04_04"
                    );

            ObservedPhysicalEventSummary summary =
                ObservedPhysicalEventSummary.Create(
                    payload,
                    ownerClientId: 7,
                    actionPosition: 1,
                    ObservedPhysicalEventPlanState
                        .Drafted
                );

            Assert.That(
                summary.EventId.ToString(),
                Is.EqualTo(payload.PayloadId)
            );

            Assert.That(
                summary.OwnerClientId,
                Is.EqualTo(7)
            );

            Assert.That(
                summary.ActionType,
                Is.EqualTo(
                    DraftedActionType
                        .ReleaseLatestDemoToKvlt
                )
            );

            Assert.That(
                summary.PhysicalNodeId.ToString(),
                Is.EqualTo(
                    "PHYSICAL_NODE_04_04"
                )
            );

            Assert.That(
                summary.CreatedTurn,
                Is.EqualTo(2)
            );

            Assert.That(
                summary.ActionPosition,
                Is.EqualTo(1)
            );

            Assert.That(
                summary.PlanState,
                Is.EqualTo(
                    ObservedPhysicalEventPlanState
                        .Drafted
                )
            );
        }

        [Test]
        public void PlanStateIsPartOfSummaryIdentity()
        {
            DraftedActionPayload payload =
                DraftedActionPayload
                    .CreatePhysicalPromotionEvent(
                        createdTurn: 2,
                        physicalEventNodeId:
                            "PHYSICAL_NODE_04_04"
                    );

            ObservedPhysicalEventSummary drafted =
                ObservedPhysicalEventSummary.Create(
                    payload,
                    ownerClientId: 7,
                    actionPosition: 1,
                    ObservedPhysicalEventPlanState
                        .Drafted
                );

            ObservedPhysicalEventSummary committed =
                ObservedPhysicalEventSummary.Create(
                    payload,
                    ownerClientId: 7,
                    actionPosition: 1,
                    ObservedPhysicalEventPlanState
                        .Committed
                );

            Assert.That(
                drafted.Equals(committed),
                Is.False
            );

            Assert.That(
                drafted.EventId,
                Is.EqualTo(committed.EventId)
            );
        }
    }
}