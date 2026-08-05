using System;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    public struct DemoTapeTrackSemanticDebugRow :
        INetworkSerializable,
        IEquatable<DemoTapeTrackSemanticDebugRow>
    {
        public ulong OwnerClientId;

        public int DemoIndex;
        public int TrackIndex;

        public FixedString64Bytes DemoTapeId;
        public FixedString64Bytes SourceTrackId;
        public FixedString64Bytes DisplayName;

        public float SourceConveyance;
        public float RecordedConveyance;

        public int IdeaCount;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(
                ref OwnerClientId
            );

            serializer.SerializeValue(
                ref DemoIndex
            );

            serializer.SerializeValue(
                ref TrackIndex
            );

            serializer.SerializeValue(
                ref DemoTapeId
            );

            serializer.SerializeValue(
                ref SourceTrackId
            );

            serializer.SerializeValue(
                ref DisplayName
            );

            serializer.SerializeValue(
                ref SourceConveyance
            );

            serializer.SerializeValue(
                ref RecordedConveyance
            );

            serializer.SerializeValue(
                ref IdeaCount
            );
        }

        public bool Equals(
            DemoTapeTrackSemanticDebugRow other)
        {
            return
                OwnerClientId ==
                other.OwnerClientId &&
                DemoIndex ==
                other.DemoIndex &&
                TrackIndex ==
                other.TrackIndex &&
                DemoTapeId.Equals(
                    other.DemoTapeId
                ) &&
                SourceTrackId.Equals(
                    other.SourceTrackId
                ) &&
                DisplayName.Equals(
                    other.DisplayName
                ) &&
                SourceConveyance.Equals(
                    other.SourceConveyance
                ) &&
                RecordedConveyance.Equals(
                    other.RecordedConveyance
                ) &&
                IdeaCount ==
                other.IdeaCount;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is
                    DemoTapeTrackSemanticDebugRow
                    other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(OwnerClientId);
            hash.Add(DemoIndex);
            hash.Add(TrackIndex);
            hash.Add(DemoTapeId);
            hash.Add(SourceTrackId);
            hash.Add(DisplayName);
            hash.Add(SourceConveyance);
            hash.Add(RecordedConveyance);
            hash.Add(IdeaCount);

            return hash.ToHashCode();
        }
    }

    public struct DemoTapeIdeaSemanticDebugRow :
        INetworkSerializable,
        IEquatable<DemoTapeIdeaSemanticDebugRow>
    {
        public ulong OwnerClientId;

        public int DemoIndex;
        public int TrackIndex;
        public int IdeaIndex;

        public FixedString64Bytes DemoTapeId;
        public FixedString64Bytes SourceTrackId;

        public FixedString64Bytes SourceIdeaId;
        public FixedString64Bytes AspectId;

        public byte PayloadTypeValue;

        public FixedString64Bytes SourceEntityId;
        public byte SourceContainerTypeValue;

        public float Conveyance;
        public int TagOccurrenceCount;

        public IdeaPayloadType PayloadType =>
            (IdeaPayloadType)PayloadTypeValue;

        public TagContainerType SourceContainerType =>
            (TagContainerType)SourceContainerTypeValue;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(
                ref OwnerClientId
            );

            serializer.SerializeValue(
                ref DemoIndex
            );

            serializer.SerializeValue(
                ref TrackIndex
            );

            serializer.SerializeValue(
                ref IdeaIndex
            );

            serializer.SerializeValue(
                ref DemoTapeId
            );

            serializer.SerializeValue(
                ref SourceTrackId
            );

            serializer.SerializeValue(
                ref SourceIdeaId
            );

            serializer.SerializeValue(
                ref AspectId
            );

            serializer.SerializeValue(
                ref PayloadTypeValue
            );

            serializer.SerializeValue(
                ref SourceEntityId
            );

            serializer.SerializeValue(
                ref SourceContainerTypeValue
            );

            serializer.SerializeValue(
                ref Conveyance
            );

            serializer.SerializeValue(
                ref TagOccurrenceCount
            );
        }

        public bool Equals(
            DemoTapeIdeaSemanticDebugRow other)
        {
            return
                OwnerClientId ==
                other.OwnerClientId &&
                DemoIndex ==
                other.DemoIndex &&
                TrackIndex ==
                other.TrackIndex &&
                IdeaIndex ==
                other.IdeaIndex &&
                DemoTapeId.Equals(
                    other.DemoTapeId
                ) &&
                SourceTrackId.Equals(
                    other.SourceTrackId
                ) &&
                SourceIdeaId.Equals(
                    other.SourceIdeaId
                ) &&
                AspectId.Equals(
                    other.AspectId
                ) &&
                PayloadTypeValue ==
                other.PayloadTypeValue &&
                SourceEntityId.Equals(
                    other.SourceEntityId
                ) &&
                SourceContainerTypeValue ==
                other.SourceContainerTypeValue &&
                Conveyance.Equals(
                    other.Conveyance
                ) &&
                TagOccurrenceCount ==
                other.TagOccurrenceCount;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is
                    DemoTapeIdeaSemanticDebugRow
                    other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(OwnerClientId);
            hash.Add(DemoIndex);
            hash.Add(TrackIndex);
            hash.Add(IdeaIndex);
            hash.Add(DemoTapeId);
            hash.Add(SourceTrackId);
            hash.Add(SourceIdeaId);
            hash.Add(AspectId);
            hash.Add(PayloadTypeValue);
            hash.Add(SourceEntityId);
            hash.Add(SourceContainerTypeValue);
            hash.Add(Conveyance);
            hash.Add(TagOccurrenceCount);

            return hash.ToHashCode();
        }
    }

    public struct DemoTapeTagSemanticDebugRow :
        INetworkSerializable,
        IEquatable<DemoTapeTagSemanticDebugRow>
    {
        public ulong OwnerClientId;

        public int DemoIndex;
        public int TrackIndex;
        public int IdeaIndex;
        public int TagOccurrenceIndex;

        public FixedString64Bytes DemoTapeId;
        public FixedString64Bytes SourceTrackId;
        public FixedString64Bytes SourceIdeaId;

        public byte AxisValue;
        public byte PoleValue;
        public byte DegreeValue;
        public byte RoleValue;

        public TagAxis Axis =>
            (TagAxis)AxisValue;

        public TagPole Pole =>
            (TagPole)PoleValue;

        public TagDegree Degree =>
            (TagDegree)DegreeValue;

        public DemoTapeTagOccurrenceRole Role =>
            (DemoTapeTagOccurrenceRole)RoleValue;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(
                ref OwnerClientId
            );

            serializer.SerializeValue(
                ref DemoIndex
            );

            serializer.SerializeValue(
                ref TrackIndex
            );

            serializer.SerializeValue(
                ref IdeaIndex
            );

            serializer.SerializeValue(
                ref TagOccurrenceIndex
            );

            serializer.SerializeValue(
                ref DemoTapeId
            );

            serializer.SerializeValue(
                ref SourceTrackId
            );

            serializer.SerializeValue(
                ref SourceIdeaId
            );

            serializer.SerializeValue(
                ref AxisValue
            );

            serializer.SerializeValue(
                ref PoleValue
            );

            serializer.SerializeValue(
                ref DegreeValue
            );

            serializer.SerializeValue(
                ref RoleValue
            );
        }

        public bool Equals(
            DemoTapeTagSemanticDebugRow other)
        {
            return
                OwnerClientId ==
                other.OwnerClientId &&
                DemoIndex ==
                other.DemoIndex &&
                TrackIndex ==
                other.TrackIndex &&
                IdeaIndex ==
                other.IdeaIndex &&
                TagOccurrenceIndex ==
                other.TagOccurrenceIndex &&
                DemoTapeId.Equals(
                    other.DemoTapeId
                ) &&
                SourceTrackId.Equals(
                    other.SourceTrackId
                ) &&
                SourceIdeaId.Equals(
                    other.SourceIdeaId
                ) &&
                AxisValue ==
                other.AxisValue &&
                PoleValue ==
                other.PoleValue &&
                DegreeValue ==
                other.DegreeValue &&
                RoleValue ==
                other.RoleValue;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is
                    DemoTapeTagSemanticDebugRow
                    other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(OwnerClientId);
            hash.Add(DemoIndex);
            hash.Add(TrackIndex);
            hash.Add(IdeaIndex);
            hash.Add(TagOccurrenceIndex);
            hash.Add(DemoTapeId);
            hash.Add(SourceTrackId);
            hash.Add(SourceIdeaId);
            hash.Add(AxisValue);
            hash.Add(PoleValue);
            hash.Add(DegreeValue);
            hash.Add(RoleValue);

            return hash.ToHashCode();
        }
    }
}