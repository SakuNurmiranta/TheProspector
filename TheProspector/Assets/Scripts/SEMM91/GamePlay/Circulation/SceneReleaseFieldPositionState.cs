using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Authoritative current Field Position for one
    /// successfully fettered SceneRelease.
    ///
    /// Larger values are further inward.
    /// Smaller values are further outward.
    ///
    /// Absolute scene boundaries are deliberately
    /// not owned by this state object.
    /// </summary>
    public sealed class
        SceneReleaseFieldPositionState
    {
        private readonly
            List<
                SceneReleaseFieldPositionTransition>
            transitions =
                new();

        public float
            CurrentPosition { get; private set; }

        public float
            PeakInwardPosition { get; private set; }

        public int
            EstablishedTurn { get; }

        public int
            LastMovementTurn { get; private set; }

        public IReadOnlyList<
                SceneReleaseFieldPositionTransition>
            Transitions =>
            transitions;

        public SceneReleaseFieldPositionState(
            float initialPosition,
            int establishedTurn)
        {
            if (!IsFinite(initialPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialPosition)
                );
            }

            if (establishedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(establishedTurn)
                );
            }

            CurrentPosition =
                initialPosition;

            PeakInwardPosition =
                initialPosition;

            EstablishedTurn =
                establishedTurn;

            LastMovementTurn =
                -1;

            transitions.Add(
                new SceneReleaseFieldPositionTransition(
                    SceneReleaseFieldPositionTransitionKind
                        .InitialFieldPlacement,
                    null,
                    initialPosition,
                    establishedTurn
                )
            );
        }

        internal bool
            TryApplySettlementMovement(
                float delta,
                int globalTurn,
                out
                    SceneReleaseFieldPositionTransition
                    transition)
        {
            transition =
                null;

            if (!IsFinite(delta))
            {
                return false;
            }

            if (globalTurn <
                EstablishedTurn)
            {
                return false;
            }

            /*
             * Once settlement movement has begun,
             * movement history must remain strictly
             * chronological.
             *
             * This simultaneously rejects:
             *
             * - a second movement in the same turn
             * - a movement retroactively applied to
             *   an earlier turn
             */
            if (LastMovementTurn >= 0 &&
                globalTurn <=
                LastMovementTurn)
            {
                return false;
            }

            float nextPosition =
                CurrentPosition +
                delta;

            if (!IsFinite(nextPosition))
            {
                return false;
            }

            transition =
                new SceneReleaseFieldPositionTransition(
                    SceneReleaseFieldPositionTransitionKind
                        .SettlementMovement,
                    CurrentPosition,
                    nextPosition,
                    globalTurn
                );

            CurrentPosition =
                nextPosition;

            if (CurrentPosition >
                PeakInwardPosition)
            {
                PeakInwardPosition =
                    CurrentPosition;
            }

            LastMovementTurn =
                globalTurn;

            transitions.Add(
                transition
            );

            return true;
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