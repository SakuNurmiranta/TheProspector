using System;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    public struct KvltCanonPrecedentDebugRow :
        INetworkSerializable,
        IEquatable<KvltCanonPrecedentDebugRow>
    {
        public byte AxisValue;
        public byte PoleValue;
        public byte DegreeValue;
        public byte ProvenanceKindValue;
        public FixedString64Bytes SourceArtifactId;
        public FixedString64Bytes SourceTrackId;
        public FixedString64Bytes SourceIdeaId;
        public int EstablishedTurn;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref AxisValue);
            serializer.SerializeValue(ref PoleValue);
            serializer.SerializeValue(ref DegreeValue);
            serializer.SerializeValue(ref ProvenanceKindValue);
            serializer.SerializeValue(ref SourceArtifactId);
            serializer.SerializeValue(ref SourceTrackId);
            serializer.SerializeValue(ref SourceIdeaId);
            serializer.SerializeValue(ref EstablishedTurn);
        }

        public bool Equals(
            KvltCanonPrecedentDebugRow other)
        {
            return AxisValue == other.AxisValue &&
                   PoleValue == other.PoleValue &&
                   DegreeValue == other.DegreeValue &&
                   ProvenanceKindValue ==
                       other.ProvenanceKindValue &&
                   SourceArtifactId.Equals(
                       other.SourceArtifactId
                   ) &&
                   SourceTrackId.Equals(
                       other.SourceTrackId
                   ) &&
                   SourceIdeaId.Equals(
                       other.SourceIdeaId
                   ) &&
                   EstablishedTurn ==
                       other.EstablishedTurn;
        }

        public override bool Equals(object obj)
        {
            return obj is KvltCanonPrecedentDebugRow other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                AxisValue,
                PoleValue,
                DegreeValue,
                ProvenanceKindValue,
                SourceArtifactId,
                SourceTrackId,
                SourceIdeaId,
                EstablishedTurn
            );
        }
    }
}