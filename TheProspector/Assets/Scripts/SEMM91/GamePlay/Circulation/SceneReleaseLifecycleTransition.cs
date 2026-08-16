using System;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class SceneReleaseLifecycleTransition
    {
        public SceneReleaseLifecycleState
            FromState { get; }

        public SceneReleaseLifecycleState
            ToState { get; }

        public int GlobalTurn { get; }

        public SceneReleaseLifecycleTransition(
            SceneReleaseLifecycleState fromState,
            SceneReleaseLifecycleState toState,
            int globalTurn)
        {
            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn),
                    globalTurn,
                    "Lifecycle transition turn cannot be negative."
                );
            }

            if (fromState == toState)
            {
                throw new ArgumentException(
                    "Lifecycle transition must change state."
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