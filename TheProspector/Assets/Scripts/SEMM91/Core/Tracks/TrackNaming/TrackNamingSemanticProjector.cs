using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Converts authoritative ordered Track Ideas into
    /// immutable naming-side Tag occurrence positions.
    ///
    /// No position selection, reinforcement, or lexical
    /// generation is performed here.
    /// </summary>
    public static class TrackNamingSemanticProjector
    {
        public static IReadOnlyList<
            TrackNamingSemanticPosition> Project(
            Track track)
        {
            if (track == null)
            {
                return Array.Empty<
                    TrackNamingSemanticPosition>();
            }

            List<TrackNamingSemanticPosition> positions =
                new();

            for (int ideaIndex = 0;
                 ideaIndex < track.Ideas.Count;
                 ideaIndex++)
            {
                Idea idea = track.Ideas[ideaIndex];

                if (idea == null)
                {
                    throw new InvalidOperationException(
                        $"Track {track.VhsTrackId} contains " +
                        $"a null Idea at index {ideaIndex}."
                    );
                }

                switch (idea.PayloadType)
                {
                    case IdeaPayloadType.SingleTag:
                        AddSingleTagPosition(
                            positions,
                            idea,
                            ideaIndex
                        );

                        break;

                    case IdeaPayloadType.TagPair:
                        AddTagPairPositions(
                            positions,
                            idea,
                            ideaIndex
                        );

                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Idea {idea.IdeaId} has unsupported " +
                            $"payload type {idea.PayloadType}."
                        );
                }
            }

            return positions;
        }

        private static void AddSingleTagPosition(
            ICollection<TrackNamingSemanticPosition>
                positions,
            Idea idea,
            int ideaIndex)
        {
            TagInstance tag =
                idea.TagInstance;

            positions.Add(
                CreatePosition(
                    idea,
                    ideaIndex,
                    TrackNamingSemanticRole.Solitary,
                    tag
                )
            );
        }

        private static void AddTagPairPositions(
            ICollection<TrackNamingSemanticPosition>
                positions,
            Idea idea,
            int ideaIndex)
        {
            TagPair pair =
                idea.TagPair;

            if (pair == null)
            {
                throw new InvalidOperationException(
                    $"Tag-pair Idea {idea.IdeaId} " +
                    "does not contain a TagPair."
                );
            }

            positions.Add(
                CreatePosition(
                    idea,
                    ideaIndex,
                    TrackNamingSemanticRole.PairDominant,
                    pair.DominantTag
                )
            );

            positions.Add(
                CreatePosition(
                    idea,
                    ideaIndex,
                    TrackNamingSemanticRole.PairSubmissive,
                    pair.SubmissiveTag
                )
            );
        }

        private static TrackNamingSemanticPosition
            CreatePosition(
                Idea idea,
                int ideaIndex,
                TrackNamingSemanticRole role,
                TagInstance tag)
        {
            return new TrackNamingSemanticPosition(
                idea.IdeaId,
                idea.AspectId,
                ideaIndex,
                role,
                tag.axis,
                tag.pole,
                tag.degree
            );
        }
    }
}