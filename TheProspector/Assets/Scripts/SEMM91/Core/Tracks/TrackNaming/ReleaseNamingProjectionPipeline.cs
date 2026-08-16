using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Builds release-level naming selections from all Tracks
    /// contained in one RehearsalSet.
    ///
    /// Primary projection uses only existing TRVE-eligible
    /// formal Tag-Pair Ideas.
    ///
    /// Regular fallback projection uses the complete ordinary
    /// Track naming semantics, including solitary Tags.
    ///
    /// This class does not generate or mutate titles.
    /// </summary>
    public static class ReleaseNamingProjectionPipeline
    {
        public static TrackNamingPositionSelection
            ResolveTrvePreferred(
                RehearsalSet rehearsalSet)
        {
            if (rehearsalSet == null)
            {
                throw new ArgumentNullException(
                    nameof(rehearsalSet)
                );
            }

            List<TrackNamingSemanticPosition>
                projectedOccurrences =
                    new();

            foreach (Track track in rehearsalSet.VhsTracks)
            {
                AppendTrveEligiblePositions(
                    track,
                    projectedOccurrences
                );
            }

            return ResolveProjectedOccurrences(
                projectedOccurrences
            );
        }

        public static TrackNamingPositionSelection
            ResolveRegularFallback(
                RehearsalSet rehearsalSet)
        {
            if (rehearsalSet == null)
            {
                throw new ArgumentNullException(
                    nameof(rehearsalSet)
                );
            }

            List<TrackNamingSemanticPosition>
                projectedOccurrences =
                    new();

            foreach (Track track in rehearsalSet.VhsTracks)
            {
                IReadOnlyList<
                    TrackNamingSemanticPosition>
                    trackProjection =
                        TrackNamingSemanticProjector
                            .Project(track);

                foreach (
                    TrackNamingSemanticPosition position
                    in trackProjection)
                {
                    projectedOccurrences.Add(position);
                }
            }

            return ResolveProjectedOccurrences(
                projectedOccurrences
            );
        }

        private static void AppendTrveEligiblePositions(
            Track track,
            ICollection<TrackNamingSemanticPosition>
                destination)
        {
            if (track == null)
            {
                return;
            }

            HashSet<string> eligibleIdeaIds =
                new();

            foreach (Idea idea in track.Ideas)
            {
                if (idea == null)
                {
                    continue;
                }

                if (!idea.IsTRVEEligible())
                {
                    continue;
                }

                eligibleIdeaIds.Add(
                    idea.IdeaId
                );
            }

            if (eligibleIdeaIds.Count == 0)
            {
                return;
            }

            IReadOnlyList<TrackNamingSemanticPosition>
                trackProjection =
                    TrackNamingSemanticProjector
                        .Project(track);

            foreach (
                TrackNamingSemanticPosition position
                in trackProjection)
            {
                if (!eligibleIdeaIds.Contains(
                        position.SourceIdeaId
                    ))
                {
                    continue;
                }

                destination.Add(position);
            }
        }

        private static TrackNamingPositionSelection
            ResolveProjectedOccurrences(
                IReadOnlyList<
                    TrackNamingSemanticPosition>
                    projectedOccurrences)
        {
            TrackNamingPositionSelection selected =
                TrackNamingPositionSelector.Select(
                    projectedOccurrences
                );

            TrackNamingPositionSelection
                exactReinforced =
                    TrackNamingOverflowReinforcer
                        .ApplyExactMatches(selected);

            return TrackNamingOverflowReinforcer
                .ApplyAdjacencyMatches(
                    exactReinforced
                );
        }
    }
}