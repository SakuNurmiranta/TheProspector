using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Transgression
{
    /// <summary>
    /// Authoritative KVLT institutional precedent.
    ///
    /// Coverage is monotonic within Peak 2:
    /// higher accepted severity raises the ceiling;
    /// lower/equal declarations do not create a
    /// second current acceptance.
    /// </summary>
    public sealed class AcceptedTransgressionState
    {
        private readonly
            List<AcceptedTransgressionRecord>
                history =
                    new();

        private readonly
            Dictionary<
                string,
                AcceptedTransgressionRecord>
                recordsById =
                    new(
                        StringComparer.Ordinal
                    );

        private readonly
            Dictionary<
                AcceptedTransgressionKey,
                AcceptedTransgressionRecord>
                currentByKey =
                    new();

        public string KvltEntityId { get; }

        public IReadOnlyList<
            AcceptedTransgressionRecord>
            History =>
                history;

        public AcceptedTransgressionState(
            string kvltEntityId)
        {
            if (string.IsNullOrWhiteSpace(
                    kvltEntityId))
            {
                throw new ArgumentException(
                    "Accepted Transgression state " +
                    "requires KVLT identity.",
                    nameof(kvltEntityId)
                );
            }

            KvltEntityId =
                kvltEntityId.Trim();
        }

        public bool TryAccept(
            AcceptedTransgressionRecord record)
        {
            if (record == null)
            {
                return false;
            }

            if (record.KvltEntityId !=
                KvltEntityId)
            {
                return false;
            }

            if (recordsById.ContainsKey(
                    record.AcceptedTransgressionId))
            {
                return false;
            }

            if (currentByKey.TryGetValue(
                    record.Key,
                    out AcceptedTransgressionRecord
                        current))
            {
                if (DegreeValue(
                        record.AcceptedDegree) <=
                    DegreeValue(
                        current.AcceptedDegree))
                {
                    return false;
                }
            }

            history.Add(
                record
            );

            recordsById.Add(
                record.AcceptedTransgressionId,
                record
            );

            currentByKey[
                record.Key
            ] = record;

            return true;
        }

        public bool IsCovered(
            string behaviorTypeId,
            TagAxis axis,
            TagPole pole,
            TagDegree demonstratedDegree)
        {
            return TryGetCoveringPrecedent(
                behaviorTypeId,
                axis,
                pole,
                demonstratedDegree,
                out _
            );
        }

        public bool TryGetCoveringPrecedent(
            string behaviorTypeId,
            TagAxis axis,
            TagPole pole,
            TagDegree demonstratedDegree,
            out AcceptedTransgressionRecord
                precedent)
        {
            ValidateDegree(
                demonstratedDegree
            );

            AcceptedTransgressionKey key =
                new AcceptedTransgressionKey(
                    behaviorTypeId,
                    axis,
                    pole
                );

            if (!currentByKey.TryGetValue(
                    key,
                    out AcceptedTransgressionRecord
                        current))
            {
                precedent = null;
                return false;
            }

            if (DegreeValue(
                    demonstratedDegree) >
                DegreeValue(
                    current.AcceptedDegree))
            {
                precedent = null;
                return false;
            }

            precedent =
                current;

            return true;
        }

        public bool TryGetCurrent(
            string behaviorTypeId,
            TagAxis axis,
            TagPole pole,
            out AcceptedTransgressionRecord
                current)
        {
            AcceptedTransgressionKey key =
                new AcceptedTransgressionKey(
                    behaviorTypeId,
                    axis,
                    pole
                );

            return currentByKey.TryGetValue(
                key,
                out current
            );
        }

        public bool TryGetById(
            string acceptedTransgressionId,
            out AcceptedTransgressionRecord record)
        {
            if (string.IsNullOrWhiteSpace(
                    acceptedTransgressionId))
            {
                record = null;
                return false;
            }

            return recordsById.TryGetValue(
                acceptedTransgressionId,
                out record
            );
        }

        private static int DegreeValue(
            TagDegree degree)
        {
            ValidateDegree(degree);

            return (int)degree;
        }

        private static void ValidateDegree(
            TagDegree degree)
        {
            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    degree))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(degree)
                );
            }
        }
    }
}