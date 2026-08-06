namespace SEMM91.Core.Tracks
{
    public enum TrackNamingTitleValidationFailure
    {
        None = 0,
        EmptyTitle = 1,
        ExceedsMaximumWordCount = 2,
        RepeatedContentWord = 3,
        RepeatedFunctionWord = 4,
        RepeatedFunctionSequence = 5
    }
}