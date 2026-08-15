using System;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    public struct KvltHailDebugRow :
        INetworkSerializable,
        IEquatable<KvltHailDebugRow>
    {
        public FixedString128Bytes HappeningId;
        public FixedString128Bytes BehaviorOccurrenceId;
        public FixedString128Bytes HailOccurrenceId;
        public FixedString64Bytes DeclarerEntityId;
        public FixedString64Bytes HailedAspectId;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref HappeningId);
            serializer.SerializeValue(ref BehaviorOccurrenceId);
            serializer.SerializeValue(ref HailOccurrenceId);
            serializer.SerializeValue(ref DeclarerEntityId);
            serializer.SerializeValue(ref HailedAspectId);
        }

        public bool Equals(KvltHailDebugRow other) =>
            HappeningId.Equals(other.HappeningId) &&
            BehaviorOccurrenceId.Equals(other.BehaviorOccurrenceId) &&
            HailOccurrenceId.Equals(other.HailOccurrenceId) &&
            DeclarerEntityId.Equals(other.DeclarerEntityId) &&
            HailedAspectId.Equals(other.HailedAspectId);

        public override bool Equals(object obj) =>
            obj is KvltHailDebugRow other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                HappeningId,
                BehaviorOccurrenceId,
                HailOccurrenceId,
                DeclarerEntityId,
                HailedAspectId);
    }
}
