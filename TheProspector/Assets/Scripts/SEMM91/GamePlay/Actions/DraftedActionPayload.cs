using System;

namespace SEMM91.GamePlay.Actions
{
    
    [Serializable]
    public class DraftedActionPayload
    {
        public string PayloadId { get; }
        public DraftedActionType ActionType { get; }
        public int CreatedTurn { get; }

        public DraftedActionPayload(
            DraftedActionType actionType,
            int createdTurn)
        {
            PayloadId = Guid.NewGuid().ToString();
            ActionType = actionType;
            CreatedTurn = createdTurn;
        }

        public override string ToString()
        {
            return $"{ActionType} ({PayloadId})";
        }
    }
}