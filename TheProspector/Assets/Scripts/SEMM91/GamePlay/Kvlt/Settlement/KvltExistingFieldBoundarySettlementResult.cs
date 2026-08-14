using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Inspectable result of the existing-Field
    /// outer-boundary phase.
    ///
    /// Evaluations include both surviving and
    /// rejected resident Field releases.
    ///
    /// RejectionApplications contain only releases
    /// that actually crossed the outer boundary.
    /// </summary>
    public sealed class
        KvltExistingFieldBoundarySettlementResult
    {
        private readonly
            SceneReleaseOuterBoundaryEvaluation[]
            boundaryEvaluations;

        private readonly
            SceneReleaseOuterBoundaryApplication[]
            rejectionApplications;

        public string SceneId { get; }

        public int SettledTurn { get; }

        public IReadOnlyList<
                SceneReleaseOuterBoundaryEvaluation>
            BoundaryEvaluations =>
            boundaryEvaluations;

        public IReadOnlyList<
                SceneReleaseOuterBoundaryApplication>
            RejectionApplications =>
            rejectionApplications;

        public int RejectedCount =>
            rejectionApplications.Length;

        public
            KvltExistingFieldBoundarySettlementResult(
                string sceneId,
                int settledTurn,
                IReadOnlyList<
                    SceneReleaseOuterBoundaryEvaluation>
                    sourceBoundaryEvaluations,
                IReadOnlyList<
                    SceneReleaseOuterBoundaryApplication>
                    sourceRejectionApplications)
        {
            if (string.IsNullOrWhiteSpace(
                    sceneId))
            {
                throw new ArgumentException(
                    "Boundary settlement scene " +
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

            SceneId =
                sceneId.Trim();

            SettledTurn =
                settledTurn;

            boundaryEvaluations =
                Copy(
                    sourceBoundaryEvaluations,
                    nameof(
                        sourceBoundaryEvaluations
                    )
                );

            rejectionApplications =
                Copy(
                    sourceRejectionApplications,
                    nameof(
                        sourceRejectionApplications
                    )
                );
        }

        private static T[] Copy<T>(
            IReadOnlyList<T> source,
            string parameterName)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(
                    parameterName
                );
            }

            T[] result =
                new T[source.Count];

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                result[index] =
                    source[index] ??
                    throw new ArgumentException(
                        "Boundary settlement result " +
                        "cannot contain null.",
                        parameterName
                    );
            }

            return result;
        }
    }
}