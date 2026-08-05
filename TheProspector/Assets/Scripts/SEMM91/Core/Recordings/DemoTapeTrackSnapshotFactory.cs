using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;

namespace SEMM91.Core.Recordings
{
    /// <summary>
    /// Creates immutable recording-time semantic snapshots from
    /// mutable rehearsal Tracks.
    /// </summary>
    public static class DemoTapeTrackSnapshotFactory
    {
        public static DemoTapeTrackSnapshot Create(
            Track sourceTrack,
            float recordedConveyance)
        {
            if (sourceTrack == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceTrack)
                );
            }

            List<DemoTapeIdeaSnapshot> ideaSnapshots =
                new List<DemoTapeIdeaSnapshot>(
                    sourceTrack.Ideas.Count
                );

            for (int ideaIndex = 0;
                 ideaIndex < sourceTrack.Ideas.Count;
                 ideaIndex++)
            {
                Idea idea =
                    sourceTrack.Ideas[ideaIndex];

                if (idea == null)
                {
                    throw new InvalidOperationException(
                        $"Track {sourceTrack.VhsTrackId} " +
                        $"contains a null Idea at index " +
                        $"{ideaIndex}."
                    );
                }

                ideaSnapshots.Add(
                    CreateIdeaSnapshot(
                        idea,
                        ideaIndex
                    )
                );
            }

            return new DemoTapeTrackSnapshot(
                sourceTrack.VhsTrackId,
                sourceTrack.DisplayName,
                sourceTrack.Conveyance,
                recordedConveyance,
                ideaSnapshots
            );
        }

        private static DemoTapeIdeaSnapshot
            CreateIdeaSnapshot(
                Idea idea,
                int ideaIndex)
        {
            IReadOnlyList<
                DemoTapeTagOccurrenceSnapshot>
                occurrences;

            switch (idea.PayloadType)
            {
                case IdeaPayloadType.SingleTag:
                    occurrences =
                        CreateSingleTagOccurrences(
                            idea
                        );

                    break;

                case IdeaPayloadType.TagPair:
                    occurrences =
                        CreateTagPairOccurrences(
                            idea
                        );

                    break;

                default:
                    throw new InvalidOperationException(
                        $"Idea {idea.IdeaId} has unsupported " +
                        $"payload type {idea.PayloadType}."
                    );
            }

            return new DemoTapeIdeaSnapshot(
                idea.IdeaId,
                ideaIndex,
                idea.AspectId,
                idea.PayloadType,
                idea.SourceEntityId,
                idea.SourceContainerType,
                idea.Conveyance,
                occurrences
            );
        }

        private static IReadOnlyList<
            DemoTapeTagOccurrenceSnapshot>
            CreateSingleTagOccurrences(
                Idea idea)
        {
            TagInstance tag =
                idea.TagInstance;

            return new[]
            {
                CreateOccurrence(
                    tag,
                    DemoTapeTagOccurrenceRole.Solitary
                )
            };
        }

        private static IReadOnlyList<
            DemoTapeTagOccurrenceSnapshot>
            CreateTagPairOccurrences(
                Idea idea)
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

            return new[]
            {
                CreateOccurrence(
                    pair.DominantTag,
                    DemoTapeTagOccurrenceRole
                        .PairDominant
                ),
                CreateOccurrence(
                    pair.SubmissiveTag,
                    DemoTapeTagOccurrenceRole
                        .PairSubmissive
                )
            };
        }

        private static
            DemoTapeTagOccurrenceSnapshot
            CreateOccurrence(
                TagInstance tag,
                DemoTapeTagOccurrenceRole role)
        {
            return new DemoTapeTagOccurrenceSnapshot(
                tag.axis,
                tag.pole,
                tag.degree,
                role
            );
        }
    }
}