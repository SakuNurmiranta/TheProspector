namespace SEMM91.InputSystems
{
    public enum PlayerCommand
    {
        // Stance selection
        SelectGestate,
        SelectRehearse,
        SelectPromote,
        ReturnToStanceSelection,
        
        // rehearsal actions
        CreateEmptyRehearsalSet,
        CreateNewTrack, 
        AddIdeaToCurrentTrack,
        
        // Stancelass commands
        Dream,
        
        // Keeper commands
        KeeperBoostVisibility,
        KeeperSuppressVisibility,
        
        // Drafting/action-economy commands
        DraftAction,
        UndoDraftAction,
        CommitTurn,
        
        DraftPrimaryAction,
        DraftSecondaryAction,
        DraftTertiaryAction,
        DraftRestAction,
        
        // Contextual selection and action
        CycleTarget,
        ContextualCreate,
        
        // Session/debug
        ForceStartSession,
        QuitSession
    }
}