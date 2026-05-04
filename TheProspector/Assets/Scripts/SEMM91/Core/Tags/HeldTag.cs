using System;
using UnityEngine;

namespace SEMM91.Core.Tags
{
    [Serializable]
    public class HeldTag
    {
        [SerializeField] private TagInstance tagInstance;
        [SerializeField] private HeldTagState state;
        [SerializeField] private bool isEvaporating; // [ 0, 1]
        [SerializeField] private byte remainingTurns;
        [SerializeField] private string instabilityReason;
        
            public TagInstance TagInstance => tagInstance; 
            public HeldTagState State => state; 
            public bool IsEvaporating => isEvaporating;
            public byte RemainingTurns => remainingTurns;
            public string InstabilityReason => instabilityReason;

            public HeldTag(TagInstance tagInstance)
            {
                this.tagInstance = tagInstance;
                state = HeldTagState.Stable;
                isEvaporating = false;
                remainingTurns = 0;
                instabilityReason = string.Empty;
            }

            public HeldTag(TagInstance tagInstance, HeldTagState state, byte remainingTurns, string instabilityReason)
            {
                this.tagInstance = tagInstance;
                this.state = state;
                this.isEvaporating = isEvaporating;
                this.remainingTurns = remainingTurns;
                this.instabilityReason = instabilityReason;
            }

            public void MarkUnstable(byte turnsUntilEvaporate, string reason)
            {
                state = HeldTagState.Unstable;
                isEvaporating = true;
                remainingTurns = turnsUntilEvaporate;
                instabilityReason = reason;
            }

            public bool ShouldEvaporateAtTurnBoundary()
            {
                return IsEvaporating;
            }
            
            public override string ToString()
            {
                string timerText = isEvaporating ? $"({remainingTurns} turns)" : string.Empty;
                return $"{tagInstance} | {state} | remainingTurns={timerText} | reason = {instabilityReason}";
            }
    }
}