namespace SEMM91.GamePlay.Actions
{
    /// <summary>
    /// One authoritative position in a character's action sequence - a thing they did, for the record.
    /// Also, opens the possibility for both an explicit and an implicit variation rest action.
    /// </summary>
    
    public sealed class CommittedActionSlot
    {
        public int ActionPosition { get; }
        
        public DraftedActionType ActionType { get; }
        
        /// <summary>
        /// True when the action was supplied for an empty base action position, common use-case: negative draft-space originating rest
        /// </summary>
        public bool IsImplicit { get; } //note: implicit rests could cause random events as the character has unmapped "free time".
                                        //We should probably call this something else, such as Rest (explicit) vs Relax (implicit).
        
        /// <summary>
        /// Explicit drafted action. Null for rest, when it is implicit.
        /// </summary>
        public DraftedActionPayload SourcePayload { get; }

        internal CommittedActionSlot(
            int actionPosition,
            DraftedActionType actionType,
            bool isImplicit,
            DraftedActionPayload sourcePayload)
        {
            ActionPosition = actionPosition;
            ActionType = actionType;
            IsImplicit = isImplicit;
            SourcePayload = sourcePayload;
        }
    }
}