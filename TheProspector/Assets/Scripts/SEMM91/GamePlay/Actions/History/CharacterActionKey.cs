using System;

namespace SEMM91.GamePlay.Actions.History
{
    public readonly struct CharacterActionKey :
        IEquatable<CharacterActionKey>
    {
        public string CharacterEntityId { get; }
        public int GlobalTurn { get; }
        public int ActionPosition { get; }

        public CharacterActionKey(
            string characterEntityId,
            int globalTurn,
            int actionPosition)
        {
            CharacterEntityId = characterEntityId;
            GlobalTurn = globalTurn;
            ActionPosition = actionPosition;
        }

        public bool Equals(CharacterActionKey other)
        {
            return CharacterEntityId == other.CharacterEntityId &&
                   GlobalTurn == other.GlobalTurn &&
                   ActionPosition == other.ActionPosition;
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterActionKey other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                CharacterEntityId,
                GlobalTurn,
                ActionPosition
            );
        }

        public override string ToString()
        {
            return
                $"{CharacterEntityId}:turn={GlobalTurn}:action={ActionPosition}";
        }

        public static bool operator ==(
            CharacterActionKey left,
            CharacterActionKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CharacterActionKey left,
            CharacterActionKey right)
        {
            return !left.Equals(right);
        }
    }
}