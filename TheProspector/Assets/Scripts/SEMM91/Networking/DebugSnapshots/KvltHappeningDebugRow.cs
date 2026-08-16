using System;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    public struct KvltHappeningDebugRow :
        INetworkSerializable,
        IEquatable<KvltHappeningDebugRow>
    {
        public FixedString128Bytes HappeningId;
        public FixedString64Bytes InstigatorEntityId;
        public FixedString128Bytes AnchorPhysicalNodeId;
        public int CommittedTurn;
        public int LifecycleStateValue;
        public int ParticipantCount;
        public int IntentCount;
        public int BehaviorCount;
        public int HailCount;
        public int ParadigmContestCount;
        public int BeefCount;
        public int NewPoserCount;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref HappeningId);
            serializer.SerializeValue(ref InstigatorEntityId);
            serializer.SerializeValue(ref AnchorPhysicalNodeId);
            serializer.SerializeValue(ref CommittedTurn);
            serializer.SerializeValue(ref LifecycleStateValue);
            serializer.SerializeValue(ref ParticipantCount);
            serializer.SerializeValue(ref IntentCount);
            serializer.SerializeValue(ref BehaviorCount);
            serializer.SerializeValue(ref HailCount);
            serializer.SerializeValue(ref ParadigmContestCount);
            serializer.SerializeValue(ref BeefCount);
            serializer.SerializeValue(ref NewPoserCount);
        }

        public bool Equals(KvltHappeningDebugRow other)
        {
            return HappeningId.Equals(other.HappeningId) &&
                   InstigatorEntityId.Equals(other.InstigatorEntityId) &&
                   AnchorPhysicalNodeId.Equals(other.AnchorPhysicalNodeId) &&
                   CommittedTurn == other.CommittedTurn &&
                   LifecycleStateValue == other.LifecycleStateValue &&
                   ParticipantCount == other.ParticipantCount &&
                   IntentCount == other.IntentCount &&
                   BehaviorCount == other.BehaviorCount &&
                   HailCount == other.HailCount &&
                   ParadigmContestCount == other.ParadigmContestCount &&
                   BeefCount == other.BeefCount &&
                   NewPoserCount == other.NewPoserCount;
        }

        public override bool Equals(object obj) =>
            obj is KvltHappeningDebugRow other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(HappeningId);
            hash.Add(InstigatorEntityId);
            hash.Add(AnchorPhysicalNodeId);
            hash.Add(CommittedTurn);
            hash.Add(LifecycleStateValue);
            hash.Add(ParticipantCount);
            hash.Add(IntentCount);
            hash.Add(BehaviorCount);
            hash.Add(HailCount);
            hash.Add(ParadigmContestCount);
            hash.Add(BeefCount);
            hash.Add(NewPoserCount);
            return hash.ToHashCode();
        }
    }
}
