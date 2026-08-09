using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    [Serializable]
    public sealed class CanonPrecedentRecord
    {
        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree Degree { get; }

        public CanonProvenanceKind ProvenanceKind { get; }

        public string SourceArtifactId { get; }

        public string SourceTrackId { get; }

        public string SourceIdeaId { get; }

        public int EstablishedTurn { get; }

        public CanonPrecedentRecord(
            TagAxis axis,
            TagPole pole,
            TagDegree degree,
            CanonProvenanceKind provenanceKind,
            string sourceArtifactId,
            string sourceTrackId = null,
            string sourceIdeaId = null,
            int establishedTurn = -1)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceArtifactId))
            {
                throw new ArgumentException(
                    "Canon provenance requires a source artifact ID.",
                    nameof(sourceArtifactId)
                );
            }

            if (establishedTurn < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(establishedTurn),
                    establishedTurn,
                    "Established turn cannot be below -1."
                );
            }

            Axis = axis;
            Pole = pole;
            Degree = degree;
            ProvenanceKind = provenanceKind;

            SourceArtifactId =
                sourceArtifactId.Trim();

            SourceTrackId =
                string.IsNullOrWhiteSpace(sourceTrackId)
                    ? null
                    : sourceTrackId.Trim();

            SourceIdeaId =
                string.IsNullOrWhiteSpace(sourceIdeaId)
                    ? null
                    : sourceIdeaId.Trim();

            EstablishedTurn = establishedTurn;
        }
    }
}