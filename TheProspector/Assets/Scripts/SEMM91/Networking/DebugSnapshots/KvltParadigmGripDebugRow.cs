using System;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    public struct KvltParadigmGripDebugRow :
        INetworkSerializable,
        IEquatable<KvltParadigmGripDebugRow>
    {
        public FixedString64Bytes EntityId;
        public FixedString64Bytes HailAspectId;
        public int ReinforcementCount;
        public int FirstReinforcedTurn;
        public int LastReinforcedTurn;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref EntityId);
            serializer.SerializeValue(ref HailAspectId);
            serializer.SerializeValue(ref ReinforcementCount);
            serializer.SerializeValue(ref FirstReinforcedTurn);
            serializer.SerializeValue(ref LastReinforcedTurn);
        }

        public bool Equals(KvltParadigmGripDebugRow other) =>
            EntityId.Equals(other.EntityId) &&
            HailAspectId.Equals(other.HailAspectId) &&
            ReinforcementCount == other.ReinforcementCount &&
            FirstReinforcedTurn == other.FirstReinforcedTurn &&
            LastReinforcedTurn == other.LastReinforcedTurn;

        public override bool Equals(object obj) =>
            obj is KvltParadigmGripDebugRow other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                EntityId,
                HailAspectId,
                ReinforcementCount,
                FirstReinforcedTurn,
                LastReinforcedTurn);
    }
}
