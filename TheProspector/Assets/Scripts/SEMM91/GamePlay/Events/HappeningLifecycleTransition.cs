using System;

namespace SEMM91.GamePlay.Events
{
    public sealed class HappeningLifecycleTransition
    {
        public HappeningLifecycleState
            FromState { get; }

        public HappeningLifecycleState
            ToState { get; }

        public int GlobalTurn { get; }

        public HappeningLifecycleTransition(
            HappeningLifecycleState fromState,
            HappeningLifecycleState toState,
            int globalTurn)
        {
            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn),
                    globalTurn,
                    "Happening lifecycle turn cannot be negative."
                );
            }

            if (fromState == toState)
            {
                throw new ArgumentException(
                    "Happening lifecycle transition " +
                    "must change state."
                );
            }

            FromState =
                fromState;

            ToState =
                toState;

            GlobalTurn =
                globalTurn;
        }
    }
}