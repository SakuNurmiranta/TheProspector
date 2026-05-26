namespace SEMM91.InputSystems
{
    public enum PlayerCommand
    {
        // Stance selection
        SelectGestate,
        SelectRehearse,
        SelectPromote,
        
        // Drafting/action-economy commands
        DraftAction,
        UndoDraftAction,
        CommitTurn,
        
        DraftPrimaryAction,
        DraftSecondaryAction,
        DraftTertiaryAction,
        DraftRestAction,
        
        // Administrative commands
        AdminCreateEmptyRehearsalSet,
        AdminCycleActiveRehearsalSet,
        
        // Session/debug
        QuitSession
    }
}