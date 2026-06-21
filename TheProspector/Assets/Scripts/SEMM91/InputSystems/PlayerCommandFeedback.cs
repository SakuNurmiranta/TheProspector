using System;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.InputSystems
{
    public enum PlayerCommandFeedbackStatus : byte
    {
        None = 0,
        Accepted = 1,
        Rejected = 2
    }

    public struct PlayerCommandFeedback :
        INetworkSerializable,
        IEquatable<PlayerCommandFeedback>
    {
        public uint Sequence;
        public PlayerCommand Command;
        public PlayerCommandFeedbackStatus Status;
        public FixedString128Bytes Message;

        public bool HasValue =>
            Status != PlayerCommandFeedbackStatus.None;

        public static PlayerCommandFeedback Empty =>
            default;

        public static PlayerCommandFeedback Create(
            uint sequence,
            PlayerCommand command,
            PlayerCommandFeedbackStatus status,
            string message)
        {
            return new PlayerCommandFeedback
            {
                Sequence = sequence,
                Command = command,
                Status = status,
                Message = new FixedString128Bytes(
                    Truncate(message, 120)
                )
            };
        }

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref Sequence);
            serializer.SerializeValue(ref Command);
            serializer.SerializeValue(ref Status);
            serializer.SerializeValue(ref Message);
        }

        public bool Equals(
            PlayerCommandFeedback other)
        {
            return Sequence == other.Sequence &&
                   Command == other.Command &&
                   Status == other.Status &&
                   Message.Equals(other.Message);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerCommandFeedback other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Sequence.GetHashCode();
                hash = hash * 31 + Command.GetHashCode();
                hash = hash * 31 + Status.GetHashCode();
                hash = hash * 31 + Message.GetHashCode();

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