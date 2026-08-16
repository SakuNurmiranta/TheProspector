using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Keeper;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Settlement;
using SEMM91.GamePlay.Score;

namespace SEMM91.GamePlay.Kvlt.TurnFlow
{
    /// <summary>
    /// Immutable authoritative causal record for one
    /// completely resolved Peak-2 turn.
    ///
    /// Referenced settlement results preserve exact
    /// provenance. Counts are read-model conveniences.
    /// </summary>
    public sealed class KvltTurnResolutionRecord
    {
        private readonly YearInfluenceEvaluation[]
            yearInfluence;

        private readonly
            SceneReleaseCanonTenureTransitionApplication[]
            canonTenureTransitions;

        public KvltTurnChronologyPlan Chronology { get; }

        public KvltExistingFieldRuntimeSettlementResult
            ExistingField { get; }

        public KvltHappeningConsequenceSettlementResult
            Happening { get; }

        public KvltPostHappeningCanonizationScreeningResult
            CanonizationScreening { get; }

        public KvltCanonizationSettlementResult
            Canonization { get; }

        public KvltTurnScoreSettlementResult Score { get; }

        public IReadOnlyList<YearInfluenceEvaluation>
            YearInfluence => yearInfluence;

        public KeeperTransitionResult?
            KeeperTransition { get; }

        public IReadOnlyList<
                SceneReleaseCanonTenureTransitionApplication>
            CanonTenureTransitions =>
            canonTenureTransitions;

        public KvltSettledSceneStandingResult
            Standing { get; }

        public KvltNextTurnIngressSettlementResult
            Ingress { get; }

        public KvltNextSceneEnvironmentSettlementResult
            NextEnvironment { get; }

        public string SceneId =>
            ExistingField.SceneId;

        public int SettledTurn =>
            Chronology.CompletedTurn;

        public int PublishedTurn =>
            Chronology.NextTurn;

        public bool IsYearEnd =>
            Chronology.IsYearEnd;

        public int MovementCount =>
            ExistingField.Movement
                .MovementApplications.Count;

        public int RejectedCount =>
            ExistingField.Boundary.RejectedCount;

        public int HappeningCount =>
            Happening.SettledHappenings.Count;

        public int CrisisCount =>
            Happening.CrisisSettlements.Count;

        public int CanonizedReleaseCount =>
            Canonization?.FreezeApplications.Count ?? 0;

        public int ScoreEventCount =>
            Score.EventCount;

        public int IngressedReleaseCount =>
            Ingress.IngressedCount;

        public KvltTurnResolutionRecord(
            KvltTurnChronologyPlan chronology,
            KvltExistingFieldRuntimeSettlementResult
                existingField,
            KvltHappeningConsequenceSettlementResult
                happening,
            KvltPostHappeningCanonizationScreeningResult
                canonizationScreening,
            KvltCanonizationSettlementResult
                canonization,
            KvltTurnScoreSettlementResult score,
            IReadOnlyList<YearInfluenceEvaluation>
                sourceYearInfluence,
            KeeperTransitionResult? keeperTransition,
            IReadOnlyList<
                SceneReleaseCanonTenureTransitionApplication>
                sourceCanonTenureTransitions,
            KvltSettledSceneStandingResult standing,
            KvltNextTurnIngressSettlementResult ingress,
            KvltNextSceneEnvironmentSettlementResult
                nextEnvironment)
        {
            Chronology = chronology ??
                throw new ArgumentNullException(
                    nameof(chronology)
                );

            ExistingField = existingField ??
                throw new ArgumentNullException(
                    nameof(existingField)
                );

            Happening = happening ??
                throw new ArgumentNullException(
                    nameof(happening)
                );

            CanonizationScreening =
                canonizationScreening ??
                throw new ArgumentNullException(
                    nameof(canonizationScreening)
                );

            Score = score ??
                throw new ArgumentNullException(
                    nameof(score)
                );

            Standing = standing ??
                throw new ArgumentNullException(
                    nameof(standing)
                );

            Ingress = ingress ??
                throw new ArgumentNullException(
                    nameof(ingress)
                );

            NextEnvironment = nextEnvironment ??
                throw new ArgumentNullException(
                    nameof(nextEnvironment)
                );

            if (sourceYearInfluence == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceYearInfluence)
                );
            }

            if (sourceCanonTenureTransitions == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceCanonTenureTransitions)
                );
            }

            Canonization = canonization;
            KeeperTransition = keeperTransition;

            ValidateTurnIdentity();

            if (IsYearEnd)
            {
                if (Canonization == null ||
                    !KeeperTransition.HasValue ||
                    !KeeperTransition.Value.HasResult ||
                    !CanonizationScreening
                        .SceneStartCanon
                        .HasSameHistoryAs(
                            Canonization.SceneStartCanon
                        ))
                {
                    throw new ArgumentException(
                        "Year-end turn record requires " +
                        "matching Canon and Keeper settlement."
                    );
                }
            }
            else if (Canonization != null ||
                     KeeperTransition.HasValue ||
                     sourceYearInfluence.Count != 0 ||
                     sourceCanonTenureTransitions.Count != 0)
            {
                throw new ArgumentException(
                    "Non-year-end turn record cannot " +
                    "contain annual settlement state."
                );
            }

            foreach (YearInfluenceEvaluation evaluation
                     in sourceYearInfluence)
            {
                if (evaluation == null ||
                    evaluation.SceneId != SceneId ||
                    evaluation.FirstTurnInclusive !=
                        SettledTurn -
                        Chronology.TurnsPerYear + 1 ||
                    evaluation.LastTurnInclusive !=
                        SettledTurn)
                {
                    throw new ArgumentException(
                        "YearInfluence belongs to another " +
                        "completed scene year.",
                        nameof(sourceYearInfluence)
                    );
                }
            }

            foreach (
                SceneReleaseCanonTenureTransitionApplication
                    transition
                in sourceCanonTenureTransitions)
            {
                if (transition == null ||
                    transition.GlobalTurn != SettledTurn)
                {
                    throw new ArgumentException(
                        "Canon-tenure transition belongs " +
                        "to another turn.",
                        nameof(
                            sourceCanonTenureTransitions
                        )
                    );
                }
            }

            yearInfluence = Copy(
                sourceYearInfluence,
                nameof(sourceYearInfluence)
            );

            canonTenureTransitions = Copy(
                sourceCanonTenureTransitions,
                nameof(sourceCanonTenureTransitions)
            );
        }

        private void ValidateTurnIdentity()
        {
            string sceneId = ExistingField.SceneId;
            int turn = Chronology.CompletedTurn;
            int nextTurn = Chronology.NextTurn;

            if (ExistingField.SettledTurn != turn ||
                Happening.GlobalTurn != turn ||
                Score.SceneId != sceneId ||
                Score.GlobalTurn != turn ||
                Standing.SceneId != sceneId ||
                Standing.SettledTurn != turn ||
                Ingress.SceneId != sceneId ||
                Ingress.CompletedTurn != turn ||
                Ingress.PlacementTurn != nextTurn ||
                NextEnvironment.SceneId != sceneId ||
                NextEnvironment.CompletedTurn != turn ||
                NextEnvironment.PublishedTurn != nextTurn ||
                CanonizationScreening.SceneId != sceneId ||
                CanonizationScreening.SettledTurn != turn ||
                (Canonization != null &&
                 (Canonization.SceneId != sceneId ||
                  Canonization.SettledTurn != turn)))
            {
                throw new ArgumentException(
                    "Turn-resolution components do not " +
                    "describe one chronological scene " +
                    "boundary."
                );
            }
        }

        private static T[] Copy<T>(
            IReadOnlyList<T> source,
            string parameterName)
            where T : class
        {
            T[] result = new T[source.Count];

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                result[index] = source[index] ??
                    throw new ArgumentException(
                        "Turn-resolution history cannot " +
                        "contain null.",
                        parameterName
                    );
            }

            return result;
        }
    }
}