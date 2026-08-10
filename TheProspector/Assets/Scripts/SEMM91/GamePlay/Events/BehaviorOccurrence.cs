using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Events
{
    /// <summary>
    /// Durable factual record of a Behavior that
    /// actually occurred inside a Happening.
    ///
    /// Intent alone never creates this record.
    /// </summary>
    public sealed class BehaviorOccurrence
    {
        private readonly string[]
            actorEntityIds;

        public string BehaviorOccurrenceId { get; }

        public string HappeningId { get; }

        public string ContextId { get; }

        public IReadOnlyList<string>
            ActorEntityIds =>
                actorEntityIds;

        public string BehaviorTypeId { get; }

        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree DemonstratedDegree { get; }

        public string SourceIntentId { get; }

        public int ResolvedTurn { get; }

        public BehaviorOccurrence(
            string behaviorOccurrenceId,
            string happeningId,
            string contextId,
            IEnumerable<string> actorEntityIds,
            string behaviorTypeId,
            TagAxis axis,
            TagPole pole,
            TagDegree demonstratedDegree,
            string sourceIntentId,
            int resolvedTurn)
        {
            BehaviorOccurrenceId =
                RequireText(
                    behaviorOccurrenceId,
                    nameof(behaviorOccurrenceId)
                );

            HappeningId =
                RequireText(
                    happeningId,
                    nameof(happeningId)
                );

            ContextId =
                RequireText(
                    contextId,
                    nameof(contextId)
                );

            if (actorEntityIds == null)
            {
                throw new ArgumentNullException(
                    nameof(actorEntityIds)
                );
            }

            List<string> actors =
                new();

            HashSet<string> uniqueActors =
                new(
                    StringComparer.Ordinal
                );

            foreach (string actorEntityId
                     in actorEntityIds)
            {
                string normalized =
                    RequireText(
                        actorEntityId,
                        nameof(actorEntityIds)
                    );

                if (!uniqueActors.Add(
                        normalized))
                {
                    throw new ArgumentException(
                        "BehaviorOccurrence cannot " +
                        "contain the same actor twice.",
                        nameof(actorEntityIds)
                    );
                }

                actors.Add(
                    normalized
                );
            }

            if (actors.Count == 0)
            {
                throw new ArgumentException(
                    "BehaviorOccurrence requires at " +
                    "least one actual actor.",
                    nameof(actorEntityIds)
                );
            }

            BehaviorTypeId =
                RequireText(
                    behaviorTypeId,
                    nameof(behaviorTypeId)
                );

            if (!Enum.IsDefined(
                    typeof(TagAxis),
                    axis))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(axis)
                );
            }

            if (!Enum.IsDefined(
                    typeof(TagPole),
                    pole))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pole)
                );
            }

            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    demonstratedDegree))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(demonstratedDegree)
                );
            }

            SourceIntentId =
                RequireText(
                    sourceIntentId,
                    nameof(sourceIntentId)
                );

            if (resolvedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resolvedTurn)
                );
            }

            this.actorEntityIds =
                actors.ToArray();

            Axis =
                axis;

            Pole =
                pole;

            DemonstratedDegree =
                demonstratedDegree;

            ResolvedTurn =
                resolvedTurn;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "BehaviorOccurrence provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}