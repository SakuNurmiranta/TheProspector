using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;

namespace SEMM91.Core.Recordings
{
    /// <summary>
    /// Immutable recording-time copy of one ordered Idea and
    /// its complete Tag payload.
    ///
    /// Tag occurrence order is preserved by TagOccurrences.
    /// </summary>
    [Serializable]
    public sealed class DemoTapeIdeaSnapshot
    {
        private readonly
            DemoTapeTagOccurrenceSnapshot[]
                _tagOccurrences;

        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public string AspectId { get; }

        public IdeaPayloadType PayloadType { get; }

        public string SourceEntityId { get; }

        public TagContainerType SourceContainerType { get; }

        public float Conveyance { get; }

        public IReadOnlyList<
            DemoTapeTagOccurrenceSnapshot>
            TagOccurrences =>
                _tagOccurrences;

        public DemoTapeIdeaSnapshot(
            string sourceIdeaId,
            int ideaIndex,
            string aspectId,
            IdeaPayloadType payloadType,
            string sourceEntityId,
            TagContainerType sourceContainerType,
            float conveyance,
            IReadOnlyList<
                DemoTapeTagOccurrenceSnapshot>
                tagOccurrences)
        {
            SourceIdeaId = RequireText(
                sourceIdeaId,
                nameof(sourceIdeaId)
            );

            if (ideaIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ideaIndex),
                    ideaIndex,
                    "Idea index cannot be negative."
                );
            }

            IdeaIndex = ideaIndex;

            AspectId = RequireText(
                aspectId,
                nameof(aspectId)
            );

            PayloadType = payloadType;

            SourceEntityId = RequireText(
                sourceEntityId,
                nameof(sourceEntityId)
            );

            SourceContainerType =
                sourceContainerType;

            Conveyance = conveyance;

            _tagOccurrences =
                CopyAndValidateOccurrences(
                    payloadType,
                    tagOccurrences
                );
        }

        private static
            DemoTapeTagOccurrenceSnapshot[]
            CopyAndValidateOccurrences(
                IdeaPayloadType payloadType,
                IReadOnlyList<
                    DemoTapeTagOccurrenceSnapshot>
                    occurrences)
        {
            if (occurrences == null)
            {
                throw new ArgumentNullException(
                    nameof(occurrences)
                );
            }

            DemoTapeTagOccurrenceSnapshot[] copy =
                new DemoTapeTagOccurrenceSnapshot[
                    occurrences.Count
                ];

            for (int index = 0;
                 index < occurrences.Count;
                 index++)
            {
                if (occurrences[index] == null)
                {
                    throw new ArgumentException(
                        "Idea snapshot cannot contain a " +
                        "null Tag occurrence.",
                        nameof(occurrences)
                    );
                }

                copy[index] = occurrences[index];
            }

            switch (payloadType)
            {
                case IdeaPayloadType.SingleTag:
                    ValidateSingleTagPayload(copy);
                    break;

                case IdeaPayloadType.TagPair:
                    ValidateTagPairPayload(copy);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(payloadType),
                        payloadType,
                        "Unsupported Idea payload type."
                    );
            }

            return copy;
        }

        private static void ValidateSingleTagPayload(
            IReadOnlyList<
                DemoTapeTagOccurrenceSnapshot>
                occurrences)
        {
            bool valid =
                occurrences.Count == 1 &&
                occurrences[0].Role ==
                DemoTapeTagOccurrenceRole.Solitary;

            if (!valid)
            {
                throw new ArgumentException(
                    "A single-tag Idea snapshot must contain " +
                    "exactly one solitary Tag occurrence.",
                    nameof(occurrences)
                );
            }
        }

        private static void ValidateTagPairPayload(
            IReadOnlyList<
                DemoTapeTagOccurrenceSnapshot>
                occurrences)
        {
            bool valid =
                occurrences.Count == 2 &&

                occurrences[0].Role ==
                DemoTapeTagOccurrenceRole.PairDominant &&

                occurrences[1].Role ==
                DemoTapeTagOccurrenceRole.PairSubmissive;

            if (!valid)
            {
                throw new ArgumentException(
                    "A tag-pair Idea snapshot must contain " +
                    "dominant then submissive occurrences.",
                    nameof(occurrences)
                );
            }
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Snapshot identity cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}