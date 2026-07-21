namespace SEMM91.InputSystems
{
    public enum PlayerCommand
    {
        // Stance selection
        SelectGestate,
        SelectRehearse,
        SelectPromote,
        ReturnToStanceSelection,
        
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
    
        // Administrative commands
        AdminCreateEmptyRehearsalSet,
        
        // Session/debug
        ForceStartSession,
        QuitSession
    }
}