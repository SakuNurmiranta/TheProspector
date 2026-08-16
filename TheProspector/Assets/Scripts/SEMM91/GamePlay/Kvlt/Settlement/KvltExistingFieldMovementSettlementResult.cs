using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Immutable inspectable output of the frozen
    /// existing-Field movement pass.
    ///
    /// These legitimacy and breakthrough evaluations
    /// belong specifically to movement-time Scene_t.
    ///
    /// They must not later be treated as post-Happening
    /// year-end canonization screening.
    /// </summary>
    public sealed class
        KvltExistingFieldMovementSettlementResult
    {
        private readonly
            SceneReleaseLegitimacyEvaluation[]
            movementLegitimacyEvaluations;

        private readonly
            SceneReleaseMovementApplication[]
            movementApplications;

        private readonly
            Dictionary<
                string,
                SceneReleaseLegitimacyEvaluation>
            movementLegitimacyByRelease;

        private readonly
            Dictionary<
                string,
                SceneReleaseCanonBreakthroughEvaluation>
            movementBreakthroughsByRelease;

        public string SceneId { get; }

        public int SettledTurn { get; }

        public CanonState SceneStartCanon { get; }

        public SceneFieldSurfaceSnapshot
            FieldSurfaceSnapshot { get; }

        public IReadOnlyList<
                SceneReleaseLegitimacyEvaluation>
            MovementLegitimacyEvaluations =>
            movementLegitimacyEvaluations;

        public IReadOnlyList<
                SceneReleaseMovementApplication>
            MovementApplications =>
            movementApplications;

        public IReadOnlyDictionary<
                string,
                SceneReleaseLegitimacyEvaluation>
            MovementLegitimacyByRelease =>
            movementLegitimacyByRelease;

        public IReadOnlyDictionary<
                string,
                SceneReleaseCanonBreakthroughEvaluation>
            MovementBreakthroughsByRelease =>
            movementBreakthroughsByRelease;

        public
            KvltExistingFieldMovementSettlementResult(
                string sceneId,
                int settledTurn,
                CanonState sceneStartCanon,
                SceneFieldSurfaceSnapshot
                    fieldSurfaceSnapshot,
                IReadOnlyList<
                    SceneReleaseLegitimacyEvaluation>
                    sourceLegitimacyEvaluations,
                IReadOnlyList<
                    SceneReleaseMovementApplication>
                    sourceMovementApplications,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseLegitimacyEvaluation>
                    sourceLegitimacyByRelease,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseCanonBreakthroughEvaluation>
                    sourceBreakthroughsByRelease)
        {
            if (string.IsNullOrWhiteSpace(
                    sceneId))
            {
                throw new ArgumentException(
                    "Scene identity cannot be empty.",
                    nameof(sceneId)
                );
            }

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            SceneStartCanon =
                sceneStartCanon ??
                throw new ArgumentNullException(
                    nameof(sceneStartCanon)
                );

            FieldSurfaceSnapshot =
                fieldSurfaceSnapshot ??
                throw new ArgumentNullException(
                    nameof(fieldSurfaceSnapshot)
                );

            SceneId =
                sceneId.Trim();

            SettledTurn =
                settledTurn;

            movementLegitimacyEvaluations =
                Copy(
                    sourceLegitimacyEvaluations,
                    nameof(
                        sourceLegitimacyEvaluations
                    )
                );

            movementApplications =
                Copy(
                    sourceMovementApplications,
                    nameof(
                        sourceMovementApplications
                    )
                );

            movementLegitimacyByRelease =
                CopyDictionary(
                    sourceLegitimacyByRelease,
                    nameof(
                        sourceLegitimacyByRelease
                    )
                );

            movementBreakthroughsByRelease =
                CopyDictionary(
                    sourceBreakthroughsByRelease,
                    nameof(
                        sourceBreakthroughsByRelease
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
                        "Movement result collection " +
                        "cannot contain null.",
                        parameterName
                    );
            }

            return result;
        }

        private static Dictionary<string, T>
            CopyDictionary<T>(
                IReadOnlyDictionary<string, T> source,
                string parameterName)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(
                    parameterName
                );
            }

            Dictionary<string, T> result =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                KeyValuePair<string, T> pair
                in source)
            {
                if (string.IsNullOrWhiteSpace(
                        pair.Key) ||
                    pair.Value == null)
                {
                    throw new ArgumentException(
                        "Movement result lookup contains " +
                        "invalid provenance.",
                        parameterName
                    );
                }

                result.Add(
                    pair.Key,
                    pair.Value
                );
            }

            return result;
        }
    }
}