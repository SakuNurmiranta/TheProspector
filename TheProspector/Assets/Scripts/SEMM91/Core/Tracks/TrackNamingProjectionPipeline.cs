namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Resolves a Track's complete Idea composition into the
    /// semantic positions that participate in naming.
    ///
    /// Processing order is significant:
    /// 1. Project every Tag occurrence.
    /// 2. Select the first three Idea positions.
    /// 3. Consume exact-match overflow.
    /// 4. Consume adjacent overflow.
    /// </summary>
    public static class TrackNamingProjectionPipeline
    {
        public static TrackNamingPositionSelection Resolve(
            Track track)
        {
            var projectedOccurrences =
                TrackNamingSemanticProjector.Project(track);

            TrackNamingPositionSelection selected =
                TrackNamingPositionSelector.Select(
                    projectedOccurrences
                );

            TrackNamingPositionSelection exactReinforced =
                TrackNamingOverflowReinforcer
                    .ApplyExactMatches(selected);

            return TrackNamingOverflowReinforcer
                .ApplyAdjacencyMatches(
                    exactReinforced
                );
        }
    }
}