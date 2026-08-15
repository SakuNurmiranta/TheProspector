using System;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    public struct KvltPoserDebugRow :
        INetworkSerializable,
        IEquatable<KvltPoserDebugRow>
    {
        public FixedString128Bytes DeclarationId;
        public FixedString64Bytes EntityId;
        public FixedString128Bytes SourceHappeningId;
        public FixedString128Bytes SourceBehaviorOccurrenceId;
        public FixedString64Bytes LosingHailAspectId;
        public FixedString64Bytes WinningHailAspectId;
        public int ActiveFromTurn;
        public int ActiveUntilTurnExclusive;
        public int RemainingTurns;
        public bool IsActive;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref DeclarationId);
            serializer.SerializeValue(ref EntityId);
            serializer.SerializeValue(ref SourceHappeningId);
            serializer.SerializeValue(ref SourceBehaviorOccurrenceId);
            serializer.SerializeValue(ref LosingHailAspectId);
            serializer.SerializeValue(ref WinningHailAspectId);
            serializer.SerializeValue(ref ActiveFromTurn);
            serializer.SerializeValue(ref ActiveUntilTurnExclusive);
            serializer.SerializeValue(ref RemainingTurns);
            serializer.SerializeValue(ref IsActive);
        }

        public bool Equals(KvltPoserDebugRow other) =>
            DeclarationId.Equals(other.DeclarationId) &&
            EntityId.Equals(other.EntityId) &&
            SourceHappeningId.Equals(other.SourceHappeningId) &&
            SourceBehaviorOccurrenceId.Equals(
                other.SourceBehaviorOccurrenceId) &&
            LosingHailAspectId.Equals(other.LosingHailAspectId) &&
            WinningHailAspectId.Equals(other.WinningHailAspectId) &&
            ActiveFromTurn == other.ActiveFromTurn &&
            ActiveUntilTurnExclusive == other.ActiveUntilTurnExclusive &&
            RemainingTurns == other.RemainingTurns &&
            IsActive == other.IsActive;

        public override bool Equals(object obj) =>
            obj is KvltPoserDebugRow other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(DeclarationId);
            hash.Add(EntityId);
            hash.Add(SourceHappeningId);
            hash.Add(SourceBehaviorOccurrenceId);
            hash.Add(LosingHailAspectId);
            hash.Add(WinningHailAspectId);
            hash.Add(ActiveFromTurn);
            hash.Add(ActiveUntilTurnExclusive);
            hash.Add(RemainingTurns);
            hash.Add(IsActive);
            return hash.ToHashCode();
        }
    }
}
