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
        
        internal bool HasSameHistoryAs(
            CanonState other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(
                    this,
                    other))
            {
                return true;
            }

            if (records.Count !=
                other.records.Count)
            {
                return false;
            }

            for (int index = 0;
                 index < records.Count;
                 index++)
            {
                CanonPrecedentRecord left =
                    records[index];

                CanonPrecedentRecord right =
                    other.records[index];

                if (left.Axis != right.Axis ||
                    left.Pole != right.Pole ||
                    left.Degree != right.Degree ||
                    left.ProvenanceKind !=
                    right.ProvenanceKind ||
                    left.SourceArtifactId !=
                    right.SourceArtifactId ||
                    left.SourceTrackId !=
                    right.SourceTrackId ||
                    left.SourceIdeaId !=
                    right.SourceIdeaId ||
                    left.EstablishedTurn !=
                    right.EstablishedTurn)
                {
                    return false;
                }
            }

            return true;
        }

        internal void ReplaceWith(
            CanonState source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(
                    nameof(source)
                );
            }

            /*
             * Snapshot first so replacing from ourselves is
             * also safe and so validation/copying completes
             * before the authoritative history is cleared.
             */
            CanonState snapshot =
                source.CreateCopy();

            records.Clear();

            foreach (
                CanonPrecedentRecord record
                in snapshot.records)
            {
                records.Add(
                    record
                );
            }
        }
    }
}