using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Events
{
    /// <summary>
    /// Server-authoritative bounded history of world events,
    /// grouped by their primarily affected entity.
    /// </summary>
    public sealed class WorldEventRegistry
    {
        public const int RetainedTurnCount = 4;

        private readonly Dictionary<
            string,
            List<WorldEventRecord>>
            _recordsByAffectedEntity = new();

        private readonly Dictionary<
            string,
            WorldEventRecord>
            _recordsById = new();

        public void Record(WorldEventRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            if (_recordsById.ContainsKey(record.EventId))
            {
                throw new InvalidOperationException(
                    $"World-event registry already contains " +
                    $"event {record.EventId}."
                );
            }

            if (!_recordsByAffectedEntity.TryGetValue(
                    record.AffectedEntityId,
                    out List<WorldEventRecord> records))
            {
                records = new List<WorldEventRecord>();

                _recordsByAffectedEntity.Add(
                    record.AffectedEntityId,
                    records
                );
            }

            records.Add(record);
            _recordsById.Add(record.EventId, record);

            PruneToLatestFourTurns(records);
        }

        public IReadOnlyList<WorldEventRecord> GetRecent(
            string affectedEntityId)
        {
            if (string.IsNullOrWhiteSpace(affectedEntityId))
                return Array.Empty<WorldEventRecord>();

            if (!_recordsByAffectedEntity.TryGetValue(
                    affectedEntityId,
                    out List<WorldEventRecord> records))
            {
                return Array.Empty<WorldEventRecord>();
            }

            return records.ToArray();
        }

        public bool TryGet(
            string eventId,
            out WorldEventRecord record)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                record = null;
                return false;
            }

            return _recordsById.TryGetValue(
                eventId,
                out record
            );
        }

        public void ClearAffectedEntity(
            string affectedEntityId)
        {
            if (string.IsNullOrWhiteSpace(affectedEntityId))
                return;

            if (!_recordsByAffectedEntity.TryGetValue(
                    affectedEntityId,
                    out List<WorldEventRecord> records))
            {
                return;
            }

            foreach (WorldEventRecord record in records)
            {
                _recordsById.Remove(record.EventId);
            }

            _recordsByAffectedEntity.Remove(
                affectedEntityId
            );
        }

        public void ClearAll()
        {
            _recordsByAffectedEntity.Clear();
            _recordsById.Clear();
        }

        private void PruneToLatestFourTurns(
            List<WorldEventRecord> records)
        {
            int latestTurn = int.MinValue;

            foreach (WorldEventRecord record in records)
            {
                if (record.GlobalTurn > latestTurn)
                    latestTurn = record.GlobalTurn;
            }

            int oldestRetainedTurn =
                latestTurn - RetainedTurnCount + 1;

            for (int index = records.Count - 1;
                 index >= 0;
                 index--)
            {
                WorldEventRecord record = records[index];

                if (record.GlobalTurn >= oldestRetainedTurn)
                    continue;

                records.RemoveAt(index);
                _recordsById.Remove(record.EventId);
            }
        }
    }
}