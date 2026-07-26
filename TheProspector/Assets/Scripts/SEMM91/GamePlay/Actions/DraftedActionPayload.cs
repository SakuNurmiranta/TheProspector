using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Actions
{
    [Serializable]
    public class DraftedActionPayload
    {
        public string PayloadId { get; }
        public DraftedActionType ActionType { get; }
        public int CreatedTurn { get; }

        public TagContainerType? IdeaSourceContainerType { get; }

        public string PhysicalEventNodeId { get; }

        public string TargetDemoTapeId { get; }

        public bool HasTargetDemoTape =>
            !string.IsNullOrWhiteSpace(
                TargetDemoTapeId
            );
        
        public bool HasPhysicalEventLocation =>
            !string.IsNullOrWhiteSpace(
                PhysicalEventNodeId
            );

        public DraftedActionPayload(
            DraftedActionType actionType,
            int createdTurn)
            : this(
                actionType,
                createdTurn,
                null,
                null, 
                null
            )
        {
        }

        public DraftedActionPayload(
            DraftedActionType actionType,
            int createdTurn,
            TagContainerType?
                ideaSourceContainerType)
            : this(
                actionType,
                createdTurn,
                ideaSourceContainerType,
                null,
                null
            )
        {
        }

        private DraftedActionPayload(
            DraftedActionType actionType,
            int createdTurn,
            TagContainerType?
                ideaSourceContainerType,
            string physicalEventNodeId,
            string targetDemoTapeId)
        {
            if (actionType !=
                DraftedActionType.CreateIdea &&
                ideaSourceContainerType.HasValue)
            {
                throw new ArgumentException(
                    "Only CreateIdea payloads may specify " +
                    "an Idea source container.",
                    nameof(ideaSourceContainerType)
                );
            }

            bool releasesLatestDemo =
                actionType ==
                DraftedActionType
                    .ReleaseLatestDemoToKvlt;

            bool releasesSelectedDemo =
                actionType ==
                DraftedActionType.ReleaseDemoTape;

            bool isPhysicalPromotionEvent =
                releasesLatestDemo ||
                releasesSelectedDemo;

            if (isPhysicalPromotionEvent &&
                string.IsNullOrWhiteSpace(
                    physicalEventNodeId
                ))
            {
                throw new ArgumentException(
                    "A Promotion release action requires " +
                    "a physical event node.",
                    nameof(physicalEventNodeId)
                );
            }

            if (!isPhysicalPromotionEvent &&
                !string.IsNullOrWhiteSpace(
                    physicalEventNodeId
                ))
            {
                throw new ArgumentException(
                    "This action type does not support a " +
                    "physical event location.",
                    nameof(physicalEventNodeId)
                );
            }

            if (releasesSelectedDemo &&
                string.IsNullOrWhiteSpace(
                    targetDemoTapeId
                ))
            {
                throw new ArgumentException(
                    "ReleaseDemoTape requires a target " +
                    "demo tape ID.",
                    nameof(targetDemoTapeId)
                );
            }

            if (!releasesSelectedDemo &&
                !string.IsNullOrWhiteSpace(
                    targetDemoTapeId
                ))
            {
                throw new ArgumentException(
                    "Only ReleaseDemoTape may specify a " +
                    "target demo tape ID.",
                    nameof(targetDemoTapeId)
                );
            }

            PayloadId =
                Guid.NewGuid().ToString();

            ActionType =
                actionType;

            CreatedTurn =
                createdTurn;

            IdeaSourceContainerType =
                actionType ==
                DraftedActionType.CreateIdea
                    ? ideaSourceContainerType ??
                      TagContainerType.Conviction
                    : null;

            PhysicalEventNodeId =
                isPhysicalPromotionEvent
                    ? physicalEventNodeId
                    : null;

            TargetDemoTapeId =
                releasesSelectedDemo
                    ? targetDemoTapeId
                    : null;
        }

        public static DraftedActionPayload
            CreatePhysicalPromotionEvent(
                int createdTurn,
                string physicalEventNodeId)
        {
            return new DraftedActionPayload(
                DraftedActionType
                    .ReleaseLatestDemoToKvlt,
                createdTurn,
                null,
                physicalEventNodeId,
                null
            );
        }

        public static DraftedActionPayload
            CreateSelectedDemoRelease(
                int createdTurn,
                string physicalEventNodeId,
                string targetDemoTapeId)
        {
            return new DraftedActionPayload(
                DraftedActionType.ReleaseDemoTape,
                createdTurn,
                null,
                physicalEventNodeId,
                targetDemoTapeId
            );
        }

        public override string ToString()
        {
            return $"{ActionType} ({PayloadId})";
        }
    }
}