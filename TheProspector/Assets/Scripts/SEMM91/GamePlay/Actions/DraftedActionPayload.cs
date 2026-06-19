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

        public DraftedActionPayload(
            DraftedActionType actionType,
            int createdTurn)
        : this (
            actionType, 
            createdTurn, 
            null
            )
        {
        }

        public DraftedActionPayload(
            DraftedActionType actionType,
            int createdTurn,
            TagContainerType? ideaSourceContainerType)
        {
            if (actionType != DraftedActionType.CreateIdea &&
                ideaSourceContainerType.HasValue)
            {
                throw new ArgumentException(
                    "Only CreateIdea payloads may specify an Idea source container.",
                    nameof(ideaSourceContainerType)
                );
            }

            PayloadId = Guid.NewGuid().ToString();
            ActionType = actionType;
            CreatedTurn = createdTurn;

            IdeaSourceContainerType =
                actionType == DraftedActionType.CreateIdea
                    ? ideaSourceContainerType ??
                      TagContainerType.Conviction
                    : null;
        }
        
        public override string ToString()
        {
            return $"{ActionType} ({PayloadId})";
        }
    }
}