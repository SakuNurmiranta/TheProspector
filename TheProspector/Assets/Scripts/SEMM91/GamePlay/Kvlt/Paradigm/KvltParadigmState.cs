using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltParadigmState
    {
        private readonly
            Dictionary<string, KvltParadigmGripRecord>
            gripByEntityAndAspect =
                new(StringComparer.Ordinal);

        private readonly
            List<KvltParadigmGripRecord>
            gripRecords =
                new();

        private readonly
            List<KvltParadigmBeefRecord>
            beefRecords =
                new();

        private readonly
            List<KvltPoserDeclaration>
            poserDeclarations =
                new();

        private readonly
            HashSet<string>
            poserDeclarationIds =
                new(StringComparer.Ordinal);

        public IReadOnlyList<KvltParadigmGripRecord>
            GripRecords =>
            gripRecords;

        public IReadOnlyList<KvltParadigmBeefRecord>
            BeefRecords =>
            beefRecords;

        public IReadOnlyList<KvltPoserDeclaration>
            PoserDeclarations =>
            poserDeclarations;

        public KvltParadigmGripRecord RecordGrip(
            string entityId,
            string hailAspectId,
            string hailOccurrenceId,
            int reinforcedTurn)
        {
            entityId =
                RequireText(
                    entityId,
                    nameof(entityId)
                );

            hailAspectId =
                RequireText(
                    hailAspectId,
                    nameof(hailAspectId)
                );

            hailOccurrenceId =
                RequireText(
                    hailOccurrenceId,
                    nameof(hailOccurrenceId)
                );

            if (reinforcedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reinforcedTurn)
                );
            }

            string key =
                GripKey(
                    entityId,
                    hailAspectId
                );

            if (gripByEntityAndAspect.TryGetValue(
                    key,
                    out KvltParadigmGripRecord
                        existing))
            {
                existing.Reinforce(
                    hailOccurrenceId,
                    reinforcedTurn
                );

                return existing;
            }

            KvltParadigmGripRecord created =
                new(
                    entityId,
                    hailAspectId,
                    hailOccurrenceId,
                    reinforcedTurn
                );

            gripByEntityAndAspect.Add(
                key,
                created
            );

            gripRecords.Add(created);

            return created;
        }

        public bool TryGetGrip(
            string entityId,
            string hailAspectId,
            out KvltParadigmGripRecord record)
        {
            record = null;

            if (string.IsNullOrWhiteSpace(entityId) ||
                string.IsNullOrWhiteSpace(hailAspectId))
            {
                return false;
            }

            return gripByEntityAndAspect.TryGetValue(
                GripKey(
                    entityId.Trim(),
                    hailAspectId.Trim()
                ),
                out record
            );
        }

        public KvltParadigmBeefRecord RecordBeef(
            KvltParadigmOpposition opposition,
            string behaviorTypeId,
            string happeningId,
            string behaviorOccurrenceId,
            int settledTurn)
        {
            if (opposition == null)
            {
                throw new ArgumentNullException(
                    nameof(opposition)
                );
            }

            behaviorTypeId =
                RequireText(
                    behaviorTypeId,
                    nameof(behaviorTypeId)
                );

            happeningId =
                RequireText(
                    happeningId,
                    nameof(happeningId)
                );

            behaviorOccurrenceId =
                RequireText(
                    behaviorOccurrenceId,
                    nameof(behaviorOccurrenceId)
                );

            foreach (
                KvltParadigmBeefRecord existing
                in beefRecords)
            {
                if (!existing.Matches(
                        opposition,
                        behaviorTypeId))
                {
                    continue;
                }

                existing.Reinforce(
                    happeningId,
                    behaviorOccurrenceId,
                    settledTurn
                );

                return existing;
            }

            KvltParadigmBeefRecord created =
                new(
                    opposition,
                    behaviorTypeId,
                    happeningId,
                    behaviorOccurrenceId,
                    settledTurn
                );

            beefRecords.Add(created);

            return created;
        }

        public bool TryDeclarePoser(
            KvltPoserDeclaration declaration)
        {
            if (declaration == null)
                return false;

            if (poserDeclarationIds.Contains(
                    declaration.DeclarationId))
            {
                return false;
            }

            /*
             * No refresh, restart, stacking or extension
             * while an earlier declaration is active.
             */
            if (HasActivePoserdom(
                    declaration.EntityId,
                    declaration.StartsTurn))
            {
                return false;
            }

            poserDeclarationIds.Add(
                declaration.DeclarationId
            );

            poserDeclarations.Add(
                declaration
            );

            return true;
        }

        public bool HasActivePoserdom(
            string entityId,
            int globalTurn)
        {
            return TryGetActivePoserDeclaration(
                entityId,
                globalTurn,
                out _
            );
        }

        public bool TryGetActivePoserDeclaration(
            string entityId,
            int globalTurn,
            out KvltPoserDeclaration declaration)
        {
            declaration = null;

            if (string.IsNullOrWhiteSpace(entityId) ||
                globalTurn < 0)
            {
                return false;
            }

            string normalized =
                entityId.Trim();

            for (int i =
                     poserDeclarations.Count - 1;
                 i >= 0;
                 i--)
            {
                KvltPoserDeclaration candidate =
                    poserDeclarations[i];

                if (candidate.EntityId ==
                        normalized &&
                    candidate.IsActiveAt(
                        globalTurn))
                {
                    declaration = candidate;
                    return true;
                }
            }

            return false;
        }

        private static string GripKey(
            string entityId,
            string hailAspectId)
        {
            return
                entityId + "\u001f" +
                hailAspectId;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Paradigm state provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}