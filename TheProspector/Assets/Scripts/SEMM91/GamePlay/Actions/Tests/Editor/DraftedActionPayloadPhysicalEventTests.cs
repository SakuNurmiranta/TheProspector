using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Actions
    .Tests.Editor
{
    public class
        DraftedActionPayloadPhysicalEventTests
    {
        [Test]
        public void PromotionPayloadRequiresPhysicalNode()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new DraftedActionPayload(
                        DraftedActionType
                            .ReleaseLatestDemoToKvlt,
                        createdTurn: 3
                    )
            );
        }

        [Test]
        public void PromotionPayloadStoresPhysicalNode()
        {
            DraftedActionPayload payload =
                DraftedActionPayload
                    .CreatePhysicalPromotionEvent(
                        createdTurn: 3,
                        physicalEventNodeId:
                            "PHYSICAL_NODE_04_04"
                    );

            Assert.That(
                payload.HasPhysicalEventLocation,
                Is.True
            );

            Assert.That(
                payload.PhysicalEventNodeId,
                Is.EqualTo(
                    "PHYSICAL_NODE_04_04"
                )
            );
        }

        [Test]
        public void PromotionSummaryContainsPhysicalNode()
        {
            DraftedActionPayload payload =
                DraftedActionPayload
                    .CreatePhysicalPromotionEvent(
                        createdTurn: 3,
                        physicalEventNodeId:
                            "PHYSICAL_NODE_04_04"
                    );

            DraftedActionSummary summary =
                DraftedActionSummary.FromPayload(
                    payload
                );

            Assert.That(
                summary.IsOccupied,
                Is.True
            );

            Assert.That(
                summary.HasPhysicalEventLocation,
                Is.True
            );

            Assert.That(
                summary.PhysicalEventNodeId
                    .ToString(),
                Is.EqualTo(
                    "PHYSICAL_NODE_04_04"
                )
            );
        }

        [Test]
        public void NonPhysicalActionHasNoEventLocation()
        {
            DraftedActionPayload payload =
                new DraftedActionPayload(
                    DraftedActionType
                        .RehearseActiveSet,
                    createdTurn: 3
                );

            DraftedActionSummary summary =
                DraftedActionSummary.FromPayload(
                    payload
                );

            Assert.That(
                payload.HasPhysicalEventLocation,
                Is.False
            );

            Assert.That(
                summary.HasPhysicalEventLocation,
                Is.False
            );
        }
    }
}