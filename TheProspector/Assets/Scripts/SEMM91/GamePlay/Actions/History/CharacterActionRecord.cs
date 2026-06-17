using System.Collections.Generic;

namespace SEMM91.GamePlay.Actions.History
{
    public sealed class CharacterActionRecord
    {
        private readonly List<string> resultingEventIds = new();

        public CharacterActionKey ActionKey { get; }

        public ulong ClientId { get; }
        public int RoundIndex { get; }
        public BandStance Stance { get; }
       
        public DraftedActionType ActionType { get; }
        public bool IsImplicit { get; }

        /// <summary>
        /// True or false when the authoritative resolver reports
        /// an outcome. Null when the current resolver does not
        /// expose success or failure.
        /// </summary>
        public bool? WasSuccessful { get; }

        public bool WasThirdAction =>
            ActionKey.ActionPosition == 3;

        /// <summary>
        /// IDs of events caused by or associated with this action.
        ///
        /// The event records themselves will eventually belong to
        /// a separate event registry. This action record stores only
        /// their causal references.
        /// </summary>
        public IReadOnlyList<string> ResultingEventIds =>
            resultingEventIds;

        public CharacterActionRecord(
            CharacterActionKey actionKey,
            ulong clientId,
            int roundIndex,
            BandStance stance,
            DraftedActionType actionType,
            bool isImplicit,
            bool? wasSuccessful)
        {
            ActionKey = actionKey;
            ClientId = clientId;
            RoundIndex = roundIndex;
            Stance = stance;
            ActionType = actionType;
            IsImplicit = isImplicit;
            WasSuccessful = wasSuccessful;
        }
        
       
        internal bool TryAttachResultingEvent(
            string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return false;

            if (resultingEventIds.Contains(eventId))
                return false;

            resultingEventIds.Add(eventId);
            return true;
        }
    }
}