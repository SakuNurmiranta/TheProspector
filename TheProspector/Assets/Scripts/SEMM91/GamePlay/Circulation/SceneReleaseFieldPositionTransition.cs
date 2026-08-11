using System;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Immutable historical change in one
    /// SceneRelease's authoritative Field Position.
    ///
    /// Positive movement is inward toward the Nexus.
    /// Negative movement is outward toward rejection.
    /// </summary>
    public sealed class
        SceneReleaseFieldPositionTransition
    {
        public
            SceneReleaseFieldPositionTransitionKind
            Kind { get; }

        public float?
            PreviousPosition { get; }

        public float
            NewPosition { get; }

        public float
            AppliedDelta { get; }

        public int
            GlobalTurn { get; }

        public bool IsInitialPlacement =>
            Kind ==
            SceneReleaseFieldPositionTransitionKind
                .InitialFieldPlacement;

        public SceneReleaseFieldPositionTransition(
            SceneReleaseFieldPositionTransitionKind kind,
            float? previousPosition,
            float newPosition,
            int globalTurn)
        {
            if (!Enum.IsDefined(
                    typeof(
                        SceneReleaseFieldPositionTransitionKind
                    ),
                    kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind)
                );
            }

            if (previousPosition.HasValue &&
                !IsFinite(
                    previousPosition.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(previousPosition)
                );
            }

            if (!IsFinite(newPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newPosition)
                );
            }

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            if (kind ==
                SceneReleaseFieldPositionTransitionKind
                    .InitialFieldPlacement)
            {
                if (previousPosition.HasValue)
                {
                    throw new ArgumentException(
                        "Initial Field placement cannot " +
                        "have a previous position.",
                        nameof(previousPosition)
                    );
                }

                AppliedDelta =
                    0f;
            }
            else
            {
                if (!previousPosition.HasValue)
                {
                    throw new ArgumentException(
                        "Settlement movement requires " +
                        "a previous Field Position.",
                        nameof(previousPosition)
                    );
                }

                AppliedDelta =
                    newPosition -
                    previousPosition.Value;
            }

            Kind =
                kind;

            PreviousPosition =
                previousPosition;

            NewPosition =
                newPosition;

            GlobalTurn =
                globalTurn;
        }

        private static bool IsFinite(
            float value)
        {
            return
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}