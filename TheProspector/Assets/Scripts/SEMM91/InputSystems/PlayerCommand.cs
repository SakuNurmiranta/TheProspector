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
        
        // Contextual selection
        CycleTarget,
        
        // Session/debug
        ForceStartSession,
        QuitSession
    }
}