using System;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Inspectable runtime result of Peak-2
    /// ExistingFieldSettlement.
    ///
    /// It contains the exact Scene_t evaluation
    /// environment together with movement and
    /// outer-boundary results.
    /// </summary>
    public sealed class
        KvltExistingFieldRuntimeSettlementResult
    {
        public string SceneId { get; }

        public int SettledTurn { get; }

        public TrackEvaluationEnvironment
            EvaluationEnvironment { get; }

        public KvltExistingFieldMovementSettlementResult
            Movement { get; }

        public KvltExistingFieldBoundarySettlementResult
            Boundary { get; }

        public
            KvltExistingFieldRuntimeSettlementResult(
                string sceneId,
                int settledTurn,
                TrackEvaluationEnvironment
                    evaluationEnvironment,
                KvltExistingFieldMovementSettlementResult
                    movement,
                KvltExistingFieldBoundarySettlementResult
                    boundary)
        {
            if (string.IsNullOrWhiteSpace(
                    sceneId))
            {
                throw new ArgumentException(
                    "Existing-Field runtime scene " +
                    "identity cannot be empty.",
                    nameof(sceneId)
                );
            }

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            EvaluationEnvironment =
                evaluationEnvironment ??
                throw new ArgumentNullException(
                    nameof(evaluationEnvironment)
                );

            Movement =
                movement ??
                throw new ArgumentNullException(
                    nameof(movement)
                );

            Boundary =
                boundary ??
                throw new ArgumentNullException(
                    nameof(boundary)
                );

            SceneId =
                sceneId.Trim();

            SettledTurn =
                settledTurn;

            if (EvaluationEnvironment.SettledTurn !=
                SettledTurn)
            {
                throw new ArgumentException(
                    "Evaluation environment belongs " +
                    "to another settlement turn.",
                    nameof(evaluationEnvironment)
                );
            }

            if (Movement.SceneId !=
                    SceneId ||
                Movement.SettledTurn !=
                    SettledTurn)
            {
                throw new ArgumentException(
                    "Movement result belongs to another " +
                    "runtime settlement.",
                    nameof(movement)
                );
            }

            if (Boundary.SceneId !=
                    SceneId ||
                Boundary.SettledTurn !=
                    SettledTurn)
            {
                throw new ArgumentException(
                    "Boundary result belongs to another " +
                    "runtime settlement.",
                    nameof(boundary)
                );
            }
        }
    }
}