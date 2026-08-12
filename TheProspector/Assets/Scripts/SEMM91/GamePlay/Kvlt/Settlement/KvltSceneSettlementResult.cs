using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Movement;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.Kvlt.Standing;
using SEMM91.GamePlay.Score;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Complete inspectable output of one deterministic
    /// Camp-3 Scene settlement.
    ///
    /// NextCanon, NextPressure and NextNormativeCentre
    /// are next-state outputs. They were not permitted
    /// to feed back into this settlement's movement.
    /// </summary>
    public sealed class KvltSceneSettlementResult
    {
        private readonly
            SceneReleaseLegitimacyEvaluation[]
            legitimacyEvaluations;

        private readonly
            SceneReleaseMovementApplication[]
            movementApplications;

        private readonly
            SceneReleaseNexusBoundaryEvaluation[]
            nexusEvaluations;

        private readonly
            SceneReleaseCanonFreezeApplication[]
            canonFreezeApplications;

        private readonly
            SceneReleaseOuterBoundaryApplication[]
            rejectionApplications;

        private readonly
            SceneStandingEvaluation[]
            standingEvaluations;

        private readonly ScoreEvent[]
            scoreEvents;

        public string SceneId { get; }

        public int SettledTurn { get; }

        public CanonState SceneStartCanon { get; }

        public CanonState NextCanon { get; }

        public CanonSimultaneousMergeEvaluation
            CanonMerge { get; }

        public SceneFieldSurfaceSnapshot
            FieldSurfaceSnapshot { get; }

        public IReadOnlyList<
                SceneReleaseLegitimacyEvaluation>
            LegitimacyEvaluations =>
            legitimacyEvaluations;

        public IReadOnlyList<
                SceneReleaseMovementApplication>
            MovementApplications =>
            movementApplications;

        public IReadOnlyList<
                SceneReleaseNexusBoundaryEvaluation>
            NexusEvaluations =>
            nexusEvaluations;

        public IReadOnlyList<
                SceneReleaseCanonFreezeApplication>
            CanonFreezeApplications =>
            canonFreezeApplications;

        public IReadOnlyList<
                SceneReleaseOuterBoundaryApplication>
            RejectionApplications =>
            rejectionApplications;

        public IReadOnlyList<
                SceneStandingEvaluation>
            StandingEvaluations =>
            standingEvaluations;

        public ScenePressureRebuildEvaluation
            NextPressure { get; }

        public NormativeCentreDerivationEvaluation
            NextNormativeCentre { get; }

        public NormativeCentre
            NextCanonNormativeCentre { get; }

        public IReadOnlyList<ScoreEvent>
            ScoreEvents =>
            scoreEvents;

        public bool HasCanonization =>
            CanonMerge != null;

        public KvltSceneSettlementResult(
            string sceneId,
            int settledTurn,
            CanonState sceneStartCanon,
            CanonState nextCanon,
            CanonSimultaneousMergeEvaluation
                canonMerge,
            SceneFieldSurfaceSnapshot
                fieldSurfaceSnapshot,
            IReadOnlyList<
                SceneReleaseLegitimacyEvaluation>
                sourceLegitimacyEvaluations,
            IReadOnlyList<
                SceneReleaseMovementApplication>
                sourceMovementApplications,
            IReadOnlyList<
                SceneReleaseNexusBoundaryEvaluation>
                sourceNexusEvaluations,
            IReadOnlyList<
                SceneReleaseCanonFreezeApplication>
                sourceCanonFreezeApplications,
            IReadOnlyList<
                SceneReleaseOuterBoundaryApplication>
                sourceRejectionApplications,
            IReadOnlyList<
                SceneStandingEvaluation>
                sourceStandingEvaluations,
            ScenePressureRebuildEvaluation
                nextPressure,
            NormativeCentreDerivationEvaluation
                nextNormativeCentre,
            NormativeCentre
                nextCanonNormativeCentre,
            IReadOnlyList<ScoreEvent>
                sourceScoreEvents)
        {
            if (string.IsNullOrWhiteSpace(sceneId))
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

            NextCanon =
                nextCanon ??
                throw new ArgumentNullException(
                    nameof(nextCanon)
                );

            FieldSurfaceSnapshot =
                fieldSurfaceSnapshot ??
                throw new ArgumentNullException(
                    nameof(fieldSurfaceSnapshot)
                );

            NextPressure =
                nextPressure ??
                throw new ArgumentNullException(
                    nameof(nextPressure)
                );

            NextNormativeCentre =
                nextNormativeCentre ??
                throw new ArgumentNullException(
                    nameof(nextNormativeCentre)
                );

            NextCanonNormativeCentre =
                nextCanonNormativeCentre ??
                throw new ArgumentNullException(
                    nameof(nextCanonNormativeCentre)
                );

            SceneId =
                sceneId.Trim();

            SettledTurn =
                settledTurn;

            CanonMerge =
                canonMerge;

            legitimacyEvaluations =
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

            nexusEvaluations =
                Copy(
                    sourceNexusEvaluations,
                    nameof(
                        sourceNexusEvaluations
                    )
                );

            canonFreezeApplications =
                Copy(
                    sourceCanonFreezeApplications,
                    nameof(
                        sourceCanonFreezeApplications
                    )
                );

            rejectionApplications =
                Copy(
                    sourceRejectionApplications,
                    nameof(
                        sourceRejectionApplications
                    )
                );

            standingEvaluations =
                Copy(
                    sourceStandingEvaluations,
                    nameof(
                        sourceStandingEvaluations
                    )
                );

            scoreEvents =
                Copy(
                    sourceScoreEvents,
                    nameof(sourceScoreEvents)
                );
        }

        public bool TryGetStanding(
            string ownerEntityId,
            out SceneStandingEvaluation standing)
        {
            standing =
                null;

            if (string.IsNullOrWhiteSpace(
                    ownerEntityId))
            {
                return false;
            }

            foreach (
                SceneStandingEvaluation candidate
                in standingEvaluations)
            {
                if (candidate.SourceOwnerEntityId ==
                    ownerEntityId)
                {
                    standing =
                        candidate;

                    return true;
                }
            }

            return false;
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
                        "Settlement result collection " +
                        "cannot contain null.",
                        parameterName
                    );
            }

            return result;
        }
    }
}