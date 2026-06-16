using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Actions.History
{
    public sealed class CharacterActionHistoryRegistry
    {
        public const int RetainedTurnCount = 4;

        private readonly Dictionary<
            string,
            List<CharacterActionRecord>>
            recordsByCharacter = new();

        public void Record(
            CharacterActionRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            ValidateKey(record.ActionKey);

            string characterId =
                record.ActionKey.CharacterEntityId;

            if (!recordsByCharacter.TryGetValue(
                    characterId,
                    out List<CharacterActionRecord> records))
            {
                records = new List<CharacterActionRecord>();

                recordsByCharacter.Add(
                    characterId,
                    records
                );
            }

            if (ContainsActionKey(
                    records,
                    record.ActionKey))
            {
                throw new InvalidOperationException(
                    $"Action history already contains " +
                    $"{record.ActionKey}."
                );
            }

            records.Add(record);

            PruneToLatestFourTurns(records);
        }

        public bool LinkResultingEvent(
            CharacterActionKey actionKey,
            string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return false;

            if (!recordsByCharacter.TryGetValue(
                    actionKey.CharacterEntityId,
                    out List<CharacterActionRecord> records))
            {
                return false;
            }

            foreach (CharacterActionRecord record in records)
            {
                if (record.ActionKey != actionKey)
                    continue;

                return record.TryAttachResultingEvent(
                    eventId
                );
            }

            return false;
        }

        public IReadOnlyList<CharacterActionRecord>
            GetRecent(string characterEntityId)
        {
            if (string.IsNullOrWhiteSpace(characterEntityId))
            {
                return Array.Empty<CharacterActionRecord>();
            }

            if (!recordsByCharacter.TryGetValue(
                    characterEntityId,
                    out List<CharacterActionRecord> records))
            {
                return Array.Empty<CharacterActionRecord>();
            }

            return records.ToArray();
        }

        public void ClearCharacter(
            string characterEntityId)
        {
            if (string.IsNullOrWhiteSpace(characterEntityId))
                return;

            recordsByCharacter.Remove(characterEntityId);
        }

        public void ClearAll()
        {
            recordsByCharacter.Clear();
        }

        private static bool ContainsActionKey(
            List<CharacterActionRecord> records,
            CharacterActionKey actionKey)
        {
            foreach (CharacterActionRecord record in records)
            {
                if (record.ActionKey == actionKey)
                    return true;
            }

            return false;
        }

        private static void ValidateKey(
            CharacterActionKey actionKey)
        {
            if (string.IsNullOrWhiteSpace(
                    actionKey.CharacterEntityId))
            {
                throw new ArgumentException(
                    "Action record requires a character entity ID.",
                    nameof(actionKey)
                );
            }

            if (actionKey.GlobalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionKey),
                    actionKey.GlobalTurn,
                    "Global turn cannot be negative."
                );
            }

            if (actionKey.ActionPosition < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionKey),
                    actionKey.ActionPosition,
                    "Action position must be one or greater."
                );
            }
        }

        private static void PruneToLatestFourTurns(
            List<CharacterActionRecord> records)
        {
            int latestTurn = int.MinValue;

            foreach (CharacterActionRecord record in records)
            {
                if (record.ActionKey.GlobalTurn > latestTurn)
                {
                    latestTurn =
                        record.ActionKey.GlobalTurn;
                }
            }

            int oldestRetainedTurn =
                latestTurn - RetainedTurnCount + 1;

            records.RemoveAll(
                record =>
                    record.ActionKey.GlobalTurn <
                    oldestRetainedTurn
            );
        }
    }
}