using System;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    /// <summary>
    /// One formally paired, same-axis, opposing-pole semantic
    /// structure preserved inside a recorded DemoTape.
    ///
    /// This represents structural TRVE potential only.
    /// It does not represent Society validation or activation.
    /// </summary>
    public readonly struct DemoTapeStructuralPairCandidate
    {
        public ulong OwnerClientId { get; }
        public int DemoIndex { get; }
        public int TrackIndex { get; }
        public int IdeaIndex { get; }

        public FixedString64Bytes DemoTapeId { get; }
        public FixedString64Bytes SourceTrackId { get; }
        public FixedString64Bytes SourceIdeaId { get; }

        public TagAxis Axis { get; }
        public TagPole DominantPole { get; }
        public TagDegree DominantDegree { get; }
        public TagPole SubmissivePole { get; }
        public TagDegree SubmissiveDegree { get; }

        public DemoTapeStructuralPairCandidate(
            DemoTapeIdeaSemanticDebugRow idea,
            DemoTapeTagSemanticDebugRow dominant,
            DemoTapeTagSemanticDebugRow submissive)
        {
            OwnerClientId = idea.OwnerClientId;
            DemoIndex = idea.DemoIndex;
            TrackIndex = idea.TrackIndex;
            IdeaIndex = idea.IdeaIndex;

            DemoTapeId = idea.DemoTapeId;
            SourceTrackId = idea.SourceTrackId;
            SourceIdeaId = idea.SourceIdeaId;

            Axis = dominant.Axis;
            DominantPole = dominant.Pole;
            DominantDegree = dominant.Degree;
            SubmissivePole = submissive.Pole;
            SubmissiveDegree = submissive.Degree;
        }
    }

    /// <summary>
    /// Queries the replicated DemoTape semantic read model for
    /// formally paired anti-tag structures.
    /// </summary>
    public static class DemoTapeStructuralPairQuery
    {
        public static bool TryFindFirst(
            FixedString64Bytes demoTapeId,
            NetworkList<DemoTapeIdeaSemanticDebugRow> ideaRows,
            NetworkList<DemoTapeTagSemanticDebugRow> tagRows,
            out DemoTapeStructuralPairCandidate candidate)
        {
            if (ideaRows == null)
            {
                throw new ArgumentNullException(
                    nameof(ideaRows)
                );
            }

            if (tagRows == null)
            {
                throw new ArgumentNullException(
                    nameof(tagRows)
                );
            }

            for (int ideaRowIndex = 0;
                 ideaRowIndex < ideaRows.Count;
                 ideaRowIndex++)
            {
                DemoTapeIdeaSemanticDebugRow idea =
                    ideaRows[ideaRowIndex];

                if (!idea.DemoTapeId.Equals(demoTapeId))
                    continue;

                if (idea.PayloadType !=
                    IdeaPayloadType.TagPair)
                {
                    continue;
                }

                if (idea.TagOccurrenceCount != 2)
                    continue;

                bool hasDominant = false;
                bool hasSubmissive = false;

                DemoTapeTagSemanticDebugRow dominant =
                    default;

                DemoTapeTagSemanticDebugRow submissive =
                    default;

                for (int tagRowIndex = 0;
                     tagRowIndex < tagRows.Count;
                     tagRowIndex++)
                {
                    DemoTapeTagSemanticDebugRow tag =
                        tagRows[tagRowIndex];

                    if (!BelongsToIdea(tag, idea))
                        continue;

                    if (tag.TagOccurrenceIndex == 0 &&
                        tag.Role ==
                        DemoTapeTagOccurrenceRole.PairDominant)
                    {
                        dominant = tag;
                        hasDominant = true;
                        continue;
                    }

                    if (tag.TagOccurrenceIndex == 1 &&
                        tag.Role ==
                        DemoTapeTagOccurrenceRole.PairSubmissive)
                    {
                        submissive = tag;
                        hasSubmissive = true;
                    }
                }

                if (!hasDominant || !hasSubmissive)
                    continue;

                if (dominant.Axis != submissive.Axis)
                    continue;

                if (dominant.Pole == submissive.Pole)
                    continue;

                candidate =
                    new DemoTapeStructuralPairCandidate(
                        idea,
                        dominant,
                        submissive
                    );

                return true;
            }

            candidate = default;
            return false;
        }

        private static bool BelongsToIdea(
            DemoTapeTagSemanticDebugRow tag,
            DemoTapeIdeaSemanticDebugRow idea)
        {
            return
                tag.OwnerClientId ==
                idea.OwnerClientId &&

                tag.DemoIndex ==
                idea.DemoIndex &&

                tag.TrackIndex ==
                idea.TrackIndex &&

                tag.IdeaIndex ==
                idea.IdeaIndex &&

                tag.DemoTapeId.Equals(
                    idea.DemoTapeId
                ) &&

                tag.SourceTrackId.Equals(
                    idea.SourceTrackId
                ) &&

                tag.SourceIdeaId.Equals(
                    idea.SourceIdeaId
                );
        }
    }
}