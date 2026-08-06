using System;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.InputSystems
{
    public enum PlayerContextTargetKind : byte
    {
        None = 0,
        IdeaSource = 1,
        RehearsalSet = 2
    }

    public struct PlayerContextTargetSummary :
        INetworkSerializable,
        IEquatable<PlayerContextTargetSummary>
    {
        public PlayerContextTargetKind Kind;
        public bool HasTarget;
        public bool HasEmptyTrack;
        public bool CanCycle;
        public FixedString64Bytes DisplayName;

        public static PlayerContextTargetSummary Empty =>
            default;

        public static PlayerContextTargetSummary Create(
            PlayerContextTargetKind kind,
            string displayName,
            bool canCycle,
            bool hasEmptyTrack = false)
        {
            bool hasTarget =
                !string.IsNullOrWhiteSpace(displayName);

            return new PlayerContextTargetSummary
            {
                Kind = kind,
                HasTarget = hasTarget,
                CanCycle = canCycle,
                HasEmptyTrack = hasEmptyTrack,
                DisplayName = new FixedString64Bytes(
                    Truncate(displayName, 60)
                )
            };
        }

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref Kind);
            serializer.SerializeValue(ref HasTarget);
            serializer.SerializeValue(ref CanCycle);
            serializer.SerializeValue(ref HasEmptyTrack);
            serializer.SerializeValue(ref DisplayName);
        }

        public bool Equals(
            PlayerContextTargetSummary other)
        {
            return Kind == other.Kind &&
                   HasTarget == other.HasTarget &&
                   CanCycle == other.CanCycle &&
                   HasEmptyTrack == other.HasEmptyTrack &&
                   DisplayName.Equals(other.DisplayName);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerContextTargetSummary other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Kind.GetHashCode();
                hash = hash * 31 + HasTarget.GetHashCode();
                hash = hash * 31 + CanCycle.GetHashCode();
                hash = hash * 31 + HasEmptyTrack.GetHashCode();
                hash = hash * 31 + DisplayName.GetHashCode();

                return hash;
            }
        }

        private static string Truncate(
            string value,
            int maximumLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maximumLength
                ? value
                : value.Substring(0, maximumLength);
        }
    }
}