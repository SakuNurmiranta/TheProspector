using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Transgression
{
    /// <summary>
    /// One occurrence-specific KVLT institutional
    /// question.
    ///
    /// This is deliberately not keyed by SceneRelease,
    /// Track, Idea or Hailed Aspect.
    /// </summary>
    public sealed class AllegianceCrisisQuestion
    {
        public string QuestionId { get; }

        public string HappeningId { get; }

        public string SourceIntentId { get; }

        public string TriggeringActorEntityId { get; }

        public string BehaviorOccurrenceId { get; }

        public bool HasBehaviorOccurrence =>
            !string.IsNullOrWhiteSpace(
                BehaviorOccurrenceId
            );

        public string BehaviorTypeId { get; }

        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree PraxisDegree { get; }

        public int OpenedTurn { get; }

        public AllegianceCrisisQuestion(
            string happeningId,
            string sourceIntentId,
            string triggeringActorEntityId,
            string behaviorTypeId,
            TagAxis axis,
            TagPole pole,
            TagDegree praxisDegree,
            int openedTurn,
            string behaviorOccurrenceId = null)
        {
            HappeningId =
                RequireText(
                    happeningId,
                    nameof(happeningId)
                );

            SourceIntentId =
                RequireText(
                    sourceIntentId,
                    nameof(sourceIntentId)
                );

            TriggeringActorEntityId =
                RequireText(
                    triggeringActorEntityId,
                    nameof(triggeringActorEntityId)
                );

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
                    praxisDegree))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(praxisDegree)
                );
            }

            if (openedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(openedTurn)
                );
            }

            Axis =
                axis;

            Pole =
                pole;

            PraxisDegree =
                praxisDegree;

            OpenedTurn =
                openedTurn;

            BehaviorOccurrenceId =
                string.IsNullOrWhiteSpace(
                    behaviorOccurrenceId)
                    ? null
                    : behaviorOccurrenceId.Trim();

            QuestionId =
                BuildStableQuestionId(
                    HappeningId,
                    SourceIntentId,
                    BehaviorTypeId,
                    Axis,
                    Pole,
                    PraxisDegree
                );
        }

        private static string BuildStableQuestionId(
            string happeningId,
            string sourceIntentId,
            string behaviorTypeId,
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            // Length prefixes prevent ambiguous
            // collisions without requiring these
            // opaque IDs to be parsed later.

            return
                "AC|" +
                Encode(happeningId) + "|" +
                Encode(sourceIntentId) + "|" +
                Encode(behaviorTypeId) + "|" +
                ((int)axis) + "|" +
                ((int)pole) + "|" +
                ((int)degree);
        }

        private static string Encode(
            string value)
        {
            return
                value.Length +
                ":" +
                value;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Allegiance Crisis question " +
                    "provenance cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}