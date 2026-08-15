using System;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    public struct KvltParadigmBeefDebugRow :
        INetworkSerializable,
        IEquatable<KvltParadigmBeefDebugRow>
    {
        public FixedString64Bytes FirstHailAspectId;
        public FixedString64Bytes SecondHailAspectId;
        public FixedString128Bytes BehaviorTypeId;
        public FixedString128Bytes FirstHappeningId;
        public FixedString128Bytes LatestBehaviorOccurrenceId;
        public int ReinforcementCount;
        public int FirstSettledTurn;
        public int LastSettledTurn;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref FirstHailAspectId);
            serializer.SerializeValue(ref SecondHailAspectId);
            serializer.SerializeValue(ref BehaviorTypeId);
            serializer.SerializeValue(ref FirstHappeningId);
            serializer.SerializeValue(ref LatestBehaviorOccurrenceId);
            serializer.SerializeValue(ref ReinforcementCount);
            serializer.SerializeValue(ref FirstSettledTurn);
            serializer.SerializeValue(ref LastSettledTurn);
        }

        public bool Equals(KvltParadigmBeefDebugRow other) =>
            FirstHailAspectId.Equals(other.FirstHailAspectId) &&
            SecondHailAspectId.Equals(other.SecondHailAspectId) &&
            BehaviorTypeId.Equals(other.BehaviorTypeId) &&
            FirstHappeningId.Equals(other.FirstHappeningId) &&
            LatestBehaviorOccurrenceId.Equals(
                other.LatestBehaviorOccurrenceId) &&
            ReinforcementCount == other.ReinforcementCount &&
            FirstSettledTurn == other.FirstSettledTurn &&
            LastSettledTurn == other.LastSettledTurn;

        public override bool Equals(object obj) =>
            obj is KvltParadigmBeefDebugRow other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(FirstHailAspectId);
            hash.Add(SecondHailAspectId);
            hash.Add(BehaviorTypeId);
            hash.Add(FirstHappeningId);
            hash.Add(LatestBehaviorOccurrenceId);
            hash.Add(ReinforcementCount);
            hash.Add(FirstSettledTurn);
            hash.Add(LastSettledTurn);
            return hash.ToHashCode();
        }
    }
}
