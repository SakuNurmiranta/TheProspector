using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackTrveCapabilityEvaluator
    {
        public TrackTrveCapabilityEvaluation Evaluate(
            DemoTapeTrackSnapshot track,
            TrackSocietyBackingEvaluation societyBacking)
        {
            if (track == null)
            {
                throw new ArgumentNullException(
                    nameof(track)
                );
            }

            if (societyBacking == null)
            {
                throw new ArgumentNullException(
                    nameof(societyBacking)
                );
            }

            if (track.SourceTrackId !=
                societyBacking.SourceTrackId)
            {
                throw new ArgumentException(
                    "Track and SocietyBacking evaluation " +
                    "must refer to the same Track.",
                    nameof(societyBacking)
                );
            }

            if (track.IdeaSnapshots.Count !=
                societyBacking.IdeaEvaluations.Count)
            {
                throw new ArgumentException(
                    "Track and SocietyBacking evaluation " +
                    "contain different Idea counts.",
                    nameof(societyBacking)
                );
            }

            List<IdeaTrveCapabilityEvaluation>
                results =
                    new();

            for (int index = 0;
                 index < track.IdeaSnapshots.Count;
                 index++)
            {
                DemoTapeIdeaSnapshot idea =
                    track.IdeaSnapshots[index];

                IdeaSocietyBackingEvaluation backing =
                    societyBacking
                        .IdeaEvaluations[index];

                if (idea.SourceIdeaId !=
                    backing.SourceIdeaId)
                {
                    throw new ArgumentException(
                        "Track and SocietyBacking evaluation " +
                        "Idea order or identity does not match.",
                        nameof(societyBacking)
                    );
                }

                results.Add(
                    EvaluateIdea(
                        idea,
                        backing
                    )
                );
            }

            return new TrackTrveCapabilityEvaluation(
                track.SourceTrackId,
                results
            );
        }

        private static
            IdeaTrveCapabilityEvaluation
            EvaluateIdea(
                DemoTapeIdeaSnapshot idea,
                IdeaSocietyBackingEvaluation backing)
        {
            if (idea.PayloadType ==
                IdeaPayloadType.SingleTag)
            {
                return new IdeaTrveCapabilityEvaluation(
                    idea.SourceIdeaId,
                    idea.IdeaIndex,
                    false,
                    PairIntegrity.NotApplicable,
                    0,
                    0f,
                    false,
                    0f
                );
            }

            if (idea.PayloadType !=
                IdeaPayloadType.TagPair)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idea.PayloadType),
                    idea.PayloadType,
                    "Unsupported Idea payload type."
                );
            }

            DemoTapeTagOccurrenceSnapshot dominant =
                FindDominantOccurrence(
                    idea
                );

            int dominantDegree =
                (int)dominant.Degree;

            PairIntegrity integrity =
                PairIntegrity.Intact;

            bool capable =
                dominantDegree >= 1 &&
                backing.SocietyBacking > 0f &&
                integrity == PairIntegrity.Intact;

            float potential =
                capable
                    ? dominantDegree +
                      backing.SocietyBacking
                    : 0f;

            return new IdeaTrveCapabilityEvaluation(
                idea.SourceIdeaId,
                idea.IdeaIndex,
                true,
                integrity,
                dominantDegree,
                backing.SocietyBacking,
                capable,
                potential
            );
        }

        private static
            DemoTapeTagOccurrenceSnapshot
            FindDominantOccurrence(
                DemoTapeIdeaSnapshot idea)
        {
            foreach (DemoTapeTagOccurrenceSnapshot
                     occurrence
                     in idea.TagOccurrences)
            {
                if (occurrence.Role ==
                    DemoTapeTagOccurrenceRole
                        .PairDominant)
                {
                    return occurrence;
                }
            }

            throw new InvalidOperationException(
                "Formal Tag-Pair snapshot has no " +
                "dominant occurrence | " +
                $"idea={idea.SourceIdeaId}"
            );
        }
    }
}