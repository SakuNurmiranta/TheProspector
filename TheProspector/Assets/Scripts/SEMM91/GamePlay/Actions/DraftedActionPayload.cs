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
                null
            )
        {
        }

        public DraftedActionPayload(
            DraftedActionType actionType,
            int createdTurn,
            TagContainerType? ideaSourceContainerType)
            : this(
                actionType,
                createdTurn,
                ideaSourceContainerType,
                null
            )
        {
        }

        private DraftedActionPayload(
            DraftedActionType actionType,
            int createdTurn,
            TagContainerType? ideaSourceContainerType,
            string physicalEventNodeId)
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

            bool isPhysicalPromotionEvent =
                actionType ==
                DraftedActionType
                    .ReleaseLatestDemoToKvlt;

            if (isPhysicalPromotionEvent &&
                string.IsNullOrWhiteSpace(
                    physicalEventNodeId
                ))
            {
                throw new ArgumentException(
                    "A generic Promotion action requires " +
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
                physicalEventNodeId
            );
        }
        public override string ToString()
        {
            return $"{ActionType} ({PayloadId})";
        }
    }
}