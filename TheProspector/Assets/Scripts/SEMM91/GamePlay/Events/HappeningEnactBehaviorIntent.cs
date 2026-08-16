using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Events
{
    public sealed class HappeningEnactBehaviorIntent :
        HappeningParticipantIntent
    {
        public string BehaviorTypeId { get; }

        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree IntendedDegree { get; }

        public string IntendedHailedAspectId { get; }

        public bool HasIntendedHail =>
            !string.IsNullOrWhiteSpace(
                IntendedHailedAspectId
            );

        public HappeningEnactBehaviorIntent(
            string intentId,
            string happeningId,
            string contextId,
            string actorEntityId,
            int declaredTurn,
            string behaviorTypeId,
            TagAxis axis,
            TagPole pole,
            TagDegree intendedDegree,
            string intendedHailedAspectId = null)
            : base(
                intentId,
                happeningId,
                contextId,
                actorEntityId,
                declaredTurn,
                HappeningIntentKind.EnactBehavior
            )
        {
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
                    intendedDegree))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(intendedDegree)
                );
            }

            Axis =
                axis;

            Pole =
                pole;

            IntendedDegree =
                intendedDegree;

            IntendedHailedAspectId =
                string.IsNullOrWhiteSpace(
                    intendedHailedAspectId)
                    ? null
                    : intendedHailedAspectId.Trim();
        }
    }
}