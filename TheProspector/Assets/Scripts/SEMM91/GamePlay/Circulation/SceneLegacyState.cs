namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Institutional relationship between a scene release and KVLT.
    ///
    /// Active releases participate in the dynamic scene.
    /// A CanonizationSubject is being institutionalized during
    /// its owner's Keeper tenure.
    /// A Canonized release has become permanent scene reference
    /// material.
    /// </summary>
    public enum SceneLegacyState : byte
    {
        Active = 0,
        CanonizationSubject = 1,
        Canonized = 2
    }
}