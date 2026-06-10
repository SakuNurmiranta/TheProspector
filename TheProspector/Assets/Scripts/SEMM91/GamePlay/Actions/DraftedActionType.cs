namespace SEMM91.GamePlay.Actions
{
    /// <summary>
    /// Map out every viable action here, categorize by using integer addresses in the scale of a three digits
    /// </summary>
    public enum DraftedActionType
    {
        None = 0,
        
        // System / fallback: 100 - 199
        Rest = 100,
        Wait = 110,
        
        // Gestation: 200 - 299
        AcquireTag = 200,
        AcquireAspect = 210,
        CreateIdea = 220,
        CreateTagPairIdea = 230,
        RipIdeaFromDemo = 240,
        ScryDemoTape = 250,
        
        
        // Rehearsal / arrangement: 300 - 399
        CreateNewRehearsalSet = 300,
        SelectActiveRehearsalSet = 310,
        ComposeIdeaIntoActiveSet = 320,
        AddIdeaToTrack = 330,
        DuplicateTrackIntoSet = 340,
        DeclareTrackComposed = 350,
        RehearseActiveSet = 360,
        
        
        // Recording: 400 - 499
        RecordActiveSetToDemo = 400,
        OverwriteCassette = 410,
        AcquireCassette = 420,
        
        
        // Promotion: 500 - 599
        ReleaseDemoTape = 500,
        ReleaseLatestDemoToKvlt = 501, //this is for a scaffolding build
        ImproveScenePresence = 510,
        PromoteTrack = 520,
        ActivateTrveTagPair = 530,
        PoseReaction = 540,
        PlayGig = 550,
        VandalizePromotion = 560,
        
        // Keeper: 600 - 699
        KeeperBoostDemo = 600,
        KeeperSuppressDemo = 610,
        KissTheRing = 620,
        NoTrueScotsman = 630,
        GuiltByAssociation = 640,
        AdHominem = 650,
        
        // Stance-neutral: 700 - 799
        AdjustInstinctualAxis = 700,
        MoveBase = 710,
        LayLow = 720,
        Hide = 730,
        ReactToGossip = 740,
        
        
        // Debug / temporary: 900 - 999
        DebugCycleActiveRehearsalSet = 900,
        DebugCreateNewRehearsalSet = 910,
        
        DebugPlaceholderGestationSecondary = 920,
        DebugPlaceholderPromotionPrimary = 930,
        DebugPlaceholderPromotionSecondary = 940,
    }
}