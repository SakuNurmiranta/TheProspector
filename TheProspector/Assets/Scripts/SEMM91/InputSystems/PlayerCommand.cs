namespace SEMM91.InputSystems
{
    public enum PlayerCommand
    {
        // Stance selection
        SelectGestate,
        SelectRehearse,
        SelectPromote,
        
        // Stancelass commands
        Dream,
        
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
        QuitSession
    }
}