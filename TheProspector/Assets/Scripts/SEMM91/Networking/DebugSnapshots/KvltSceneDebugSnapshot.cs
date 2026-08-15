using System;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    public struct KvltSceneDebugSnapshot :
        INetworkSerializable,
        IEquatable<KvltSceneDebugSnapshot>
    {
        public bool HasState;
        public int CurrentTurn;
        public int LastSettledTurn;
        public int PublishedTurn;
        public int RuntimePhaseValue;

        public int CanonPrecedentCount;
        public int PressureEntryCount;
        public int NormativeAffinityCount;
        public int ScoreEventCount;
        public int StandingOwnerCount;

        public int SceneReleaseCount;
        public int FieldReleaseCount;
        public int CanonRetainedCount;
        public int HistoricalCanonCount;

        public ulong KeeperClientId;
        public FixedString64Bytes KeeperTenureId;
        public float KeeperPull;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref HasState);
            serializer.SerializeValue(ref CurrentTurn);
            serializer.SerializeValue(ref LastSettledTurn);
            serializer.SerializeValue(ref PublishedTurn);
            serializer.SerializeValue(ref RuntimePhaseValue);
            serializer.SerializeValue(ref CanonPrecedentCount);
            serializer.SerializeValue(ref PressureEntryCount);
            serializer.SerializeValue(ref NormativeAffinityCount);
            serializer.SerializeValue(ref ScoreEventCount);
            serializer.SerializeValue(ref StandingOwnerCount);
            serializer.SerializeValue(ref SceneReleaseCount);
            serializer.SerializeValue(ref FieldReleaseCount);
            serializer.SerializeValue(ref CanonRetainedCount);
            serializer.SerializeValue(ref HistoricalCanonCount);
            serializer.SerializeValue(ref KeeperClientId);
            serializer.SerializeValue(ref KeeperTenureId);
            serializer.SerializeValue(ref KeeperPull);
        }

        public bool Equals(KvltSceneDebugSnapshot other)
        {
            return
                HasState == other.HasState &&
                CurrentTurn == other.CurrentTurn &&
                LastSettledTurn == other.LastSettledTurn &&
                PublishedTurn == other.PublishedTurn &&
                RuntimePhaseValue == other.RuntimePhaseValue &&
                CanonPrecedentCount == other.CanonPrecedentCount &&
                PressureEntryCount == other.PressureEntryCount &&
                NormativeAffinityCount == other.NormativeAffinityCount &&
                ScoreEventCount == other.ScoreEventCount &&
                StandingOwnerCount == other.StandingOwnerCount &&
                SceneReleaseCount == other.SceneReleaseCount &&
                FieldReleaseCount == other.FieldReleaseCount &&
                CanonRetainedCount == other.CanonRetainedCount &&
                HistoricalCanonCount == other.HistoricalCanonCount &&
                KeeperClientId == other.KeeperClientId &&
                KeeperTenureId.Equals(other.KeeperTenureId) &&
                KeeperPull.Equals(other.KeeperPull);
        }

        public override bool Equals(object obj)
        {
            return obj is KvltSceneDebugSnapshot other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(HasState);
            hash.Add(CurrentTurn);
            hash.Add(LastSettledTurn);
            hash.Add(PublishedTurn);
            hash.Add(RuntimePhaseValue);
            hash.Add(CanonPrecedentCount);
            hash.Add(PressureEntryCount);
            hash.Add(NormativeAffinityCount);
            hash.Add(ScoreEventCount);
            hash.Add(StandingOwnerCount);
            hash.Add(SceneReleaseCount);
            hash.Add(FieldReleaseCount);
            hash.Add(CanonRetainedCount);
            hash.Add(HistoricalCanonCount);
            hash.Add(KeeperClientId);
            hash.Add(KeeperTenureId);
            hash.Add(KeeperPull);
            return hash.ToHashCode();
        }
    }
}