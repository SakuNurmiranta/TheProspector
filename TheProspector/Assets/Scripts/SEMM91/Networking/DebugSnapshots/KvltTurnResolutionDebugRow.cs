using System;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    public struct KvltTurnResolutionDebugRow :
        INetworkSerializable,
        IEquatable<KvltTurnResolutionDebugRow>
    {
        public FixedString64Bytes SceneId;
        public int SettledTurn;
        public int PublishedTurn;
        public bool IsYearEnd;

        public int MovementCount;
        public int RejectedCount;
        public int HappeningCount;
        public int CrisisCount;
        public int CoveredActivationCount;
        public int CrisisLegitimizedCount;
        public int PendingStoredCount;
        public int PendingRedeemedCount;
        public int AcceptedPrecedentsRaised;
        public int ParadigmContestCount;
        public int ParadigmBeefCount;
        public int NewPoserDeclarationCount;
        public int PostHappeningLegitimacyCount;
        public int CanonBreakthroughCount;
        public int CanonizedReleaseCount;
        public int ScoreEventCount;
        public int StandingOwnerCount;
        public int IngressedReleaseCount;
        public int PressureContributionCount;

        public bool HasKeeperTransition;
        public ulong PreviousKeeperClientId;
        public ulong NextKeeperClientId;
        public int HistoricalCanonTransitionCount;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref SceneId);
            serializer.SerializeValue(ref SettledTurn);
            serializer.SerializeValue(ref PublishedTurn);
            serializer.SerializeValue(ref IsYearEnd);
            serializer.SerializeValue(ref MovementCount);
            serializer.SerializeValue(ref RejectedCount);
            serializer.SerializeValue(ref HappeningCount);
            serializer.SerializeValue(ref CrisisCount);
            serializer.SerializeValue(ref CoveredActivationCount);
            serializer.SerializeValue(ref CrisisLegitimizedCount);
            serializer.SerializeValue(ref PendingStoredCount);
            serializer.SerializeValue(ref PendingRedeemedCount);
            serializer.SerializeValue(ref AcceptedPrecedentsRaised);
            serializer.SerializeValue(ref ParadigmContestCount);
            serializer.SerializeValue(ref ParadigmBeefCount);
            serializer.SerializeValue(ref NewPoserDeclarationCount);
            serializer.SerializeValue(ref PostHappeningLegitimacyCount);
            serializer.SerializeValue(ref CanonBreakthroughCount);
            serializer.SerializeValue(ref CanonizedReleaseCount);
            serializer.SerializeValue(ref ScoreEventCount);
            serializer.SerializeValue(ref StandingOwnerCount);
            serializer.SerializeValue(ref IngressedReleaseCount);
            serializer.SerializeValue(ref PressureContributionCount);
            serializer.SerializeValue(ref HasKeeperTransition);
            serializer.SerializeValue(ref PreviousKeeperClientId);
            serializer.SerializeValue(ref NextKeeperClientId);
            serializer.SerializeValue(ref HistoricalCanonTransitionCount);
        }

        public bool Equals(
            KvltTurnResolutionDebugRow other)
        {
            return
                SceneId.Equals(other.SceneId) &&
                SettledTurn == other.SettledTurn &&
                PublishedTurn == other.PublishedTurn &&
                IsYearEnd == other.IsYearEnd &&
                MovementCount == other.MovementCount &&
                RejectedCount == other.RejectedCount &&
                HappeningCount == other.HappeningCount &&
                CrisisCount == other.CrisisCount &&
                CoveredActivationCount == other.CoveredActivationCount &&
                CrisisLegitimizedCount == other.CrisisLegitimizedCount &&
                PendingStoredCount == other.PendingStoredCount &&
                PendingRedeemedCount == other.PendingRedeemedCount &&
                AcceptedPrecedentsRaised == other.AcceptedPrecedentsRaised &&
                ParadigmContestCount == other.ParadigmContestCount &&
                ParadigmBeefCount == other.ParadigmBeefCount &&
                NewPoserDeclarationCount == other.NewPoserDeclarationCount &&
                PostHappeningLegitimacyCount == other.PostHappeningLegitimacyCount &&
                CanonBreakthroughCount == other.CanonBreakthroughCount &&
                CanonizedReleaseCount == other.CanonizedReleaseCount &&
                ScoreEventCount == other.ScoreEventCount &&
                StandingOwnerCount == other.StandingOwnerCount &&
                IngressedReleaseCount == other.IngressedReleaseCount &&
                PressureContributionCount == other.PressureContributionCount &&
                HasKeeperTransition == other.HasKeeperTransition &&
                PreviousKeeperClientId == other.PreviousKeeperClientId &&
                NextKeeperClientId == other.NextKeeperClientId &&
                HistoricalCanonTransitionCount == other.HistoricalCanonTransitionCount;
        }

        public override bool Equals(object obj)
        {
            return obj is KvltTurnResolutionDebugRow other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(SceneId);
            hash.Add(SettledTurn);
            hash.Add(PublishedTurn);
            hash.Add(IsYearEnd);
            hash.Add(MovementCount);
            hash.Add(RejectedCount);
            hash.Add(HappeningCount);
            hash.Add(CrisisCount);
            hash.Add(CoveredActivationCount);
            hash.Add(CrisisLegitimizedCount);
            hash.Add(PendingStoredCount);
            hash.Add(PendingRedeemedCount);
            hash.Add(AcceptedPrecedentsRaised);
            hash.Add(ParadigmContestCount);
            hash.Add(ParadigmBeefCount);
            hash.Add(NewPoserDeclarationCount);
            hash.Add(PostHappeningLegitimacyCount);
            hash.Add(CanonBreakthroughCount);
            hash.Add(CanonizedReleaseCount);
            hash.Add(ScoreEventCount);
            hash.Add(StandingOwnerCount);
            hash.Add(IngressedReleaseCount);
            hash.Add(PressureContributionCount);
            hash.Add(HasKeeperTransition);
            hash.Add(PreviousKeeperClientId);
            hash.Add(NextKeeperClientId);
            hash.Add(HistoricalCanonTransitionCount);
            return hash.ToHashCode();
        }
    }
}
