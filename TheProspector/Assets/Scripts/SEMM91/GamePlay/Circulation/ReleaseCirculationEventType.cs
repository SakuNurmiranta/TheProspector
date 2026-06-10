namespace SEMM91.GamePlay.Circulation
{
    public enum ReleaseCirculationEventType
    {
        None = 0,

        ReleaseReachedNexus = 100,
        ReleaseReachedTrustedTraders = 200,
        ReleaseEnteredPeriphery = 300,
        ReleaseLeakedToSociety = 400
    }
}