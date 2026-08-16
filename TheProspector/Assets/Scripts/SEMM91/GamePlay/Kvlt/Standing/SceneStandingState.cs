using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Standing
{
    /// <summary>
    /// Chronological settled Scene Standing history
    /// for one player/band in one scene.
    ///
    /// Evaluations are immutable snapshots. A later
    /// settlement never rewrites an earlier standing.
    /// </summary>
    public sealed class SceneStandingState
    {
        private readonly
            List<SceneStandingEvaluation>
            history =
                new();

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public IReadOnlyList<
                SceneStandingEvaluation>
            History =>
            history;

        public SceneStandingEvaluation
            CurrentEvaluation =>
            history.Count == 0
                ? null
                : history[history.Count - 1];

        public bool HasSettledEvaluation =>
            CurrentEvaluation != null;

        public bool HasCurrentStanding =>
            CurrentEvaluation != null &&
            CurrentEvaluation.HasStanding;

        public float? CurrentStanding =>
            CurrentEvaluation?.Standing;

        public int LastSettledTurn =>
            CurrentEvaluation?.SettledTurn ?? -1;

        public SceneStandingState(
            string sourceOwnerEntityId,
            string sceneId)
        {
            SourceOwnerEntityId =
                RequireText(
                    sourceOwnerEntityId,
                    nameof(sourceOwnerEntityId)
                );

            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );
        }

        public bool TryRecord(
            SceneStandingEvaluation evaluation)
        {
            if (evaluation == null)
            {
                return false;
            }

            if (evaluation.SourceOwnerEntityId !=
                SourceOwnerEntityId)
            {
                return false;
            }

            if (evaluation.SceneId !=
                SceneId)
            {
                return false;
            }

            /*
             * Settled Standing history is immutable
             * and strictly chronological.
             */
            if (LastSettledTurn >= 0 &&
                evaluation.SettledTurn <=
                LastSettledTurn)
            {
                return false;
            }

            history.Add(
                evaluation
            );

            return true;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Scene Standing state identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}