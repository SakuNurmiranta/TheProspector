using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltParadigmState
    {
        private readonly List<KvltParadigmGripRecord>
            gripRecords = new();
        private readonly List<KvltParadigmBeefRecord>
            beefRecords = new();
        private readonly List<KvltPoserDeclaration>
            poserDeclarations = new();

        public IReadOnlyList<KvltParadigmGripRecord>
            GripRecords => gripRecords;
        public IReadOnlyList<KvltParadigmBeefRecord>
            BeefRecords => beefRecords;
        public IReadOnlyList<KvltPoserDeclaration>
            PoserDeclarations => poserDeclarations;

        public KvltParadigmGripRecord ReinforceGrip(
            string entityId,
            string hailAspectId,
            string hailOccurrenceId,
            int reinforcedTurn)
        {
            foreach (KvltParadigmGripRecord record in gripRecords)
            {
                if (record.EntityId != entityId ||
                    record.HailAspectId != hailAspectId)
                    continue;

                record.Reinforce(
                    hailOccurrenceId,
                    reinforcedTurn);
                return record;
            }

            KvltParadigmGripRecord created =
                new KvltParadigmGripRecord(
                    entityId,
                    hailAspectId,
                    hailOccurrenceId,
                    reinforcedTurn);

            gripRecords.Add(created);
            return created;
        }

        public KvltParadigmGripRecord RecordGrip(
            string entityId,
            string hailAspectId,
            string hailOccurrenceId,
            int reinforcedTurn)
        {
            return ReinforceGrip(
                entityId,
                hailAspectId,
                hailOccurrenceId,
                reinforcedTurn);
        }

        public bool TryGetGrip(
            string entityId,
            string hailAspectId,
            out KvltParadigmGripRecord record)
        {
            record = null;

            if (string.IsNullOrWhiteSpace(entityId) ||
                string.IsNullOrWhiteSpace(hailAspectId))
                return false;

            foreach (KvltParadigmGripRecord candidate
                     in gripRecords)
            {
                if (candidate.EntityId == entityId &&
                    candidate.HailAspectId == hailAspectId)
                {
                    record = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetGrip(
            string entityId,
            out KvltParadigmGripRecord record)
        {
            record = null;

            if (string.IsNullOrWhiteSpace(entityId))
                return false;

            foreach (KvltParadigmGripRecord candidate
                     in gripRecords)
            {
                if (candidate.EntityId == entityId)
                {
                    record = candidate;
                    return true;
                }
            }

            return false;
        }

        public KvltParadigmBeefRecord
            RecordOrReinforceBeef(
                KvltParadigmOpposition opposition,
                string behaviorTypeId,
                string happeningId,
                string behaviorOccurrenceId,
                int settledTurn)
        {
            foreach (KvltParadigmBeefRecord record in beefRecords)
            {
                if (!record.Matches(opposition, behaviorTypeId))
                    continue;

                record.Reinforce(
                    happeningId,
                    behaviorOccurrenceId,
                    settledTurn);
                return record;
            }

            KvltParadigmBeefRecord created =
                new KvltParadigmBeefRecord(
                    opposition,
                    behaviorTypeId,
                    happeningId,
                    behaviorOccurrenceId,
                    settledTurn);

            beefRecords.Add(created);
            return created;
        }

        public KvltParadigmBeefRecord RecordBeef(
            KvltParadigmOpposition opposition,
            string behaviorTypeId,
            string happeningId,
            string behaviorOccurrenceId,
            int settledTurn)
        {
            return RecordOrReinforceBeef(
                opposition,
                behaviorTypeId,
                happeningId,
                behaviorOccurrenceId,
                settledTurn);
        }

        public bool TryDeclarePoser(
            KvltPoserDeclaration declaration)
        {
            if (declaration == null)
                throw new ArgumentNullException(
                    nameof(declaration));

            foreach (KvltPoserDeclaration existing in poserDeclarations)
            {
                if (existing.DeclarationId ==
                    declaration.DeclarationId)
                    return false;

                if (existing.EntityId == declaration.EntityId &&
                    existing.IsActiveAt(
                        declaration.ActiveFromTurn))
                    return false;
            }

            poserDeclarations.Add(declaration);
            return true;
        }

        public bool TryRecordPoser(
            KvltPoserDeclaration declaration)
        {
            return TryDeclarePoser(declaration);
        }

        public bool IsPoser(
            string entityId,
            int playableTurn)
        {
            return HasActivePoserdom(entityId, playableTurn);
        }

        public bool HasActivePoserdom(
            string entityId,
            int playableTurn)
        {
            return TryGetActivePoser(
                entityId,
                playableTurn,
                out _);
        }

        public bool TryGetActivePoser(
            string entityId,
            int playableTurn,
            out KvltPoserDeclaration declaration)
        {
            declaration = null;

            if (string.IsNullOrWhiteSpace(entityId) ||
                playableTurn < 0)
                return false;

            foreach (KvltPoserDeclaration candidate
                     in poserDeclarations)
            {
                if (candidate.EntityId == entityId &&
                    candidate.IsActiveAt(playableTurn))
                {
                    declaration = candidate;
                    return true;
                }
            }

            return false;
        }

        public int GetRemainingPoserdomTurns(
            string entityId,
            int playableTurn)
        {
            return TryGetActivePoser(
                    entityId,
                    playableTurn,
                    out KvltPoserDeclaration declaration)
                ? declaration.GetRemainingTurnsAt(playableTurn)
                : 0;
        }
    }
}
