using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    [Serializable]
    public sealed class CanonState
    {
        private readonly List<CanonPrecedentRecord>
            records = new();

        public IReadOnlyList<CanonPrecedentRecord>
            Records =>
                records;

        public bool HasPrecedent(
            TagAxis axis,
            TagPole pole)
        {
            return TryGetCanonicalDegree(
                axis,
                pole,
                out _
            );
        }

        public bool TryGetCanonicalDegree(
            TagAxis axis,
            TagPole pole,
            out TagDegree degree)
        {
            bool found = false;
            degree = TagDegree.Neutral;

            foreach (CanonPrecedentRecord record
                     in records)
            {
                if (record.Axis != axis ||
                    record.Pole != pole)
                {
                    continue;
                }

                if (!found ||
                    record.Degree > degree)
                {
                    degree = record.Degree;
                    found = true;
                }
            }

            return found;
        }

        public bool TryRecordPrecedent(
            TagAxis axis,
            TagPole pole,
            TagDegree degree,
            CanonProvenanceKind provenanceKind,
            string sourceArtifactId,
            string sourceTrackId = null,
            string sourceIdeaId = null,
            int establishedTurn = -1)
        {
            if (TryGetCanonicalDegree(
                    axis,
                    pole,
                    out TagDegree currentDegree) &&
                degree < currentDegree)
            {
                return false;
            }

            if (ContainsMatchingProvenance(
                    axis,
                    pole,
                    degree,
                    provenanceKind,
                    sourceArtifactId,
                    sourceTrackId,
                    sourceIdeaId))
            {
                return false;
            }

            records.Add(
                new CanonPrecedentRecord(
                    axis,
                    pole,
                    degree,
                    provenanceKind,
                    sourceArtifactId,
                    sourceTrackId,
                    sourceIdeaId,
                    establishedTurn
                )
            );

            return true;
        }

        private bool ContainsMatchingProvenance(
            TagAxis axis,
            TagPole pole,
            TagDegree degree,
            CanonProvenanceKind provenanceKind,
            string sourceArtifactId,
            string sourceTrackId,
            string sourceIdeaId)
        {
            foreach (CanonPrecedentRecord record
                     in records)
            {
                if (record.Axis == axis &&
                    record.Pole == pole &&
                    record.Degree == degree &&
                    record.ProvenanceKind ==
                    provenanceKind &&
                    record.SourceArtifactId ==
                    sourceArtifactId &&
                    record.SourceTrackId ==
                    sourceTrackId &&
                    record.SourceIdeaId ==
                    sourceIdeaId)
                {
                    return true;
                }
            }

            return false;
        }
        
        public CanonState CreateCopy()
        {
            CanonState copy =
                new();

            foreach (
                CanonPrecedentRecord record
                in records)
            {
                bool added =
                    copy.TryRecordPrecedent(
                        record.Axis,
                        record.Pole,
                        record.Degree,
                        record.ProvenanceKind,
                        record.SourceArtifactId,
                        record.SourceTrackId,
                        record.SourceIdeaId,
                        record.EstablishedTurn
                    );

                if (!added)
                {
                    throw new InvalidOperationException(
                        "Existing Canon history could not be " +
                        "reproduced while creating a copy."
                    );
                }
            }

            return copy;
        }
    }
}