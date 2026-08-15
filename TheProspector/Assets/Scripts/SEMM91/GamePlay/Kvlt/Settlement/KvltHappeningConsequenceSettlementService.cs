using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Paradigm;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Resolves authoritative consequences after the
    /// every-turn shared Happening interaction window.
    ///
    /// Entry 19 must already have:
    ///
    /// - materialized factual successful Behaviors/Hails;
    /// - recorded Activation Attempts;
    /// - classified successful activation praxis;
    /// - opened every required Allegiance Crisis.
    ///
    /// Any opened Crisis must be resolved before this
    /// service is allowed to mutate semantic state.
    ///
    /// This service then:
    ///
    /// - applies already-Covered activation;
    /// - settles resolved Crises;
    /// - records Accepted Transgression precedent;
    /// - applies Crisis-legitimized activation;
    /// - stores Society-rejected Pending activation;
    /// - redeems Pending now covered by precedent;
    /// - freshly evaluates public release legitimacy;
    /// - resolves Fringe fettering/grace/failure;
    /// - settles prepared Happenings.
    ///
    /// It does not perform movement or Canonization.
    /// </summary>
    public sealed class
        KvltHappeningConsequenceSettlementService
    {
        private readonly
            ActivationCrisisSettlementService
            crisisSettlement =
                new();

        private readonly
            SceneReleaseActivationStateService
            activationState =
                new();

        private readonly
            SceneReleaseLegitimacyEvaluator
            legitimacyEvaluator =
                new();

        private readonly
            SceneReleaseFetteringService
            fettering =
                new();

        private readonly KvltParadigmContestSettlementService
            paradigmSettlement = new();

        public KvltHappeningConsequenceSettlementResult
            Settle(
                int globalTurn,
                KvltHappeningPreparationResult
                    preparation,
                IReadOnlyList<
                    SceneReleaseActivationSource>
                    publicSources,
                AcceptedTransgressionState
                    acceptedTransgressions,
                AllegianceCrisisRegistry
                    crisisRegistry,
                TrackEvaluationEnvironment
                    currentEnvironment)
        {
            return Settle(
                globalTurn,
                preparation,
                publicSources,
                acceptedTransgressions,
                crisisRegistry,
                currentEnvironment,
                new KvltParadigmState(),
                Array.Empty<KvltParadigmOpposition>(),
                string.Empty);
        }

        public KvltHappeningConsequenceSettlementResult
            Settle(
                int globalTurn,
                KvltHappeningPreparationResult
                    preparation,
                IReadOnlyList<SceneReleaseActivationSource>
                    publicSources,
                AcceptedTransgressionState
                    acceptedTransgressions,
                AllegianceCrisisRegistry
                    crisisRegistry,
                TrackEvaluationEnvironment
                    currentEnvironment,
                KvltParadigmState paradigmState,
                IReadOnlyList<KvltParadigmOpposition>
                    paradigmOppositions,
                string activeKeeperEntityId)
        {
            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            if (preparation == null)
            {
                throw new ArgumentNullException(
                    nameof(preparation)
                );
            }

            if (publicSources == null)
            {
                throw new ArgumentNullException(
                    nameof(publicSources)
                );
            }

            if (acceptedTransgressions == null)
            {
                throw new ArgumentNullException(
                    nameof(acceptedTransgressions)
                );
            }

            if (crisisRegistry == null)
            {
                throw new ArgumentNullException(
                    nameof(crisisRegistry)
                );
            }

            if (currentEnvironment == null)
            {
                throw new ArgumentNullException(
                    nameof(currentEnvironment)
                );
            }

            if (paradigmState == null)
                throw new ArgumentNullException(
                    nameof(paradigmState));

            if (paradigmOppositions == null)
                throw new ArgumentNullException(
                    nameof(paradigmOppositions));

            if (preparation.GlobalTurn !=
                globalTurn)
            {
                throw new ArgumentException(
                    "Happening preparation belongs to " +
                    "another turn.",
                    nameof(preparation)
                );
            }

            if (currentEnvironment.SettledTurn !=
                globalTurn)
            {
                throw new ArgumentException(
                    "Happening consequence environment " +
                    "belongs to another turn.",
                    nameof(currentEnvironment)
                );
            }

            Dictionary<
                    string,
                    SceneReleaseActivationSource>
                sourcesByRelease =
                    BuildSourceLookup(
                        publicSources
                    );

            /*
             * VALIDATION PASS.
             *
             * Do this before any activation mutation so
             * an unresolved shared-window Crisis cannot
             * partially settle the turn.
             */
            ValidatePreparedHappenings(
                preparation,
                globalTurn
            );

            ValidateAssessmentSources(
                preparation.LegitimacyAssessments,
                sourcesByRelease
            );

            ValidateCrisisSources(
                preparation.CrisisGroups,
                sourcesByRelease
            );

            Dictionary<
                    string,
                    AllegianceCrisis>
                resolvedCrisesByQuestion =
                    ResolveRequiredCrises(
                        preparation.CrisisGroups,
                        crisisRegistry
                    );

            int coveredApplications =
                0;

            int crisisLegitimizedApplications =
                0;

            int pendingStoredCount =
                0;

            int acceptedPrecedentsRaised =
                0;

            List<ActivationCrisisSettlement>
                crisisSettlements =
                    new();

            /*
             * PASS 1 — already-covered praxis.
             *
             * These needed no institutional vote.
             */
            foreach (
                ActivationLegitimacyAssessment assessment
                in preparation.LegitimacyAssessments)
            {
                if (assessment.Disposition !=
                    ActivationLegitimacyDisposition
                        .Covered)
                {
                    continue;
                }

                SceneReleaseActivationSource source =
                    RequireSource(
                        assessment.Candidate,
                        sourcesByRelease
                    );

                if (!activationState.ApplyCovered(
                        source.Release,
                        assessment,
                        globalTurn
                    ))
                {
                    throw new InvalidOperationException(
                        "Covered Happening activation " +
                        "could not be applied | " +
                        $"release=" +
                        $"{assessment.Candidate.SceneReleaseId} | " +
                        $"track=" +
                        $"{assessment.Candidate.SourceTrackId} | " +
                        $"idea=" +
                        $"{assessment.Candidate.SourceIdeaId}"
                    );
                }

                coveredApplications++;
            }

            /*
             * PASS 2 — institutional Crisis meaning.
             *
             * Every group corresponds to one factual
             * praxis question. Its already-resolved vote
             * now yields either legitimate activation or
             * Pending semantic consequence.
             */
            foreach (
                ActivationLegitimacyCrisisGroup group
                in preparation.CrisisGroups)
            {
                AllegianceCrisis crisis =
                    resolvedCrisesByQuestion[
                        group.Question.QuestionId
                    ];

                ActivationCrisisSettlement
                    settlement =
                        crisisSettlement.Settle(
                            group,
                            crisis,
                            acceptedTransgressions
                        );

                crisisSettlements.Add(
                    settlement
                );

                if (settlement
                    .RaisedAcceptedPrecedent)
                {
                    acceptedPrecedentsRaised++;
                }

                foreach (
                    LegitimizedActivationCandidate
                        legitimate
                    in settlement
                        .LegitimizedCandidates)
                {
                    SceneReleaseActivationSource source =
                        RequireSource(
                            legitimate.Candidate,
                            sourcesByRelease
                        );

                    if (!activationState
                            .ApplyCrisisLegitimized(
                                source.Release,
                                legitimate
                            ))
                    {
                        throw new
                            InvalidOperationException(
                                "Crisis-legitimized " +
                                "Happening activation could " +
                                "not be applied | " +
                                $"release=" +
                                $"{legitimate.Candidate.SceneReleaseId} | " +
                                $"question=" +
                                $"{legitimate.AllegianceCrisisQuestionId}"
                            );
                    }

                    crisisLegitimizedApplications++;
                }

                foreach (
                    PendingActivationCandidate pending
                    in settlement.PendingCandidates)
                {
                    SceneReleaseActivationSource source =
                        RequireSource(
                            pending.Candidate,
                            sourcesByRelease
                        );

                    if (!activationState.StorePending(
                            source.Release,
                            pending,
                            out _
                        ))
                    {
                        throw new
                            InvalidOperationException(
                                "Society-rejected activation " +
                                "could not be stored as " +
                                "Pending | " +
                                $"release=" +
                                $"{pending.Candidate.SceneReleaseId} | " +
                                $"question=" +
                                $"{pending.AllegianceCrisisQuestionId}"
                            );
                    }

                    pendingStoredCount++;
                }
            }

            /*
             * PASS 3 — prospective redemption.
             *
             * A precedent established by any settled
             * Crisis may immediately legitimate matching
             * Pending material from an earlier turn.
             *
             * Previous turns are not rescored.
             */
            int pendingRedeemedCount =
                0;

            foreach (
                KeyValuePair<
                    string,
                    SceneReleaseActivationSource>
                    pair
                in sourcesByRelease)
            {
                SceneRelease release =
                    pair.Value.Release;

                if (release.IsActivationFrozen)
                {
                    continue;
                }

                pendingRedeemedCount +=
                    activationState
                        .RedeemEligiblePending(
                            release,
                            acceptedTransgressions,
                            globalTurn
                        );
            }

            /*
             * PASS 4 — fresh semantic state.
             *
             * This is the authoritative state AFTER all
             * same-turn Happening activation/Pending
             * consequences have resolved.
             *
             * The result is also the state that Winter's
             * later post-Happening Canon screening may
             * observe.
             */
            List<
                SceneReleaseLegitimacyEvaluation>
                legitimacyEvaluations =
                    new();

            Dictionary<
                    string,
                    SceneReleaseLegitimacyEvaluation>
                legitimacyByRelease =
                    new(
                        StringComparer.Ordinal
                    );

            Dictionary<
                    string,
                    SceneReleaseFetteringResult>
                fetteringByRelease =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                KeyValuePair<
                    string,
                    SceneReleaseActivationSource>
                    pair
                in sourcesByRelease)
            {
                SceneReleaseActivationSource source =
                    pair.Value;

                SceneReleaseLegitimacyEvaluation
                    evaluation =
                        legitimacyEvaluator.Evaluate(
                            source.Release,
                            source.DemoTape,
                            currentEnvironment
                        );

                legitimacyEvaluations.Add(
                    evaluation
                );

                legitimacyByRelease.Add(
                    source.Release.ReleaseId,
                    evaluation
                );

                /*
                 * Existing Field releases have already
                 * completed their movement pass earlier
                 * this turn.
                 *
                 * Only Fringe lifecycle is resolved here.
                 */
                if (source.Release.LifecycleState !=
                    SceneReleaseLifecycleState.Fringe)
                {
                    continue;
                }

                SceneReleaseFetteringResult
                    fetteringResult =
                        fettering.Resolve(
                            source.Release,
                            evaluation.HasTrve,
                            globalTurn
                        );

                fetteringByRelease.Add(
                    source.Release.ReleaseId,
                    fetteringResult
                );
            }

            /*
             * PASS 5 — Paradigm verdicts.
             *
             * This consumes the factual Hails while the
             * Happening is still Resolving, reinforces Grip
             * for both sides, and writes Beef/Poserdom into
             * the authoritative world-owned state.
             */
            IReadOnlyList<KvltParadigmContestResult>
                paradigmResults =
                    paradigmSettlement.SettleAll(
                        globalTurn,
                        preparation.PreparedHappenings,
                        paradigmOppositions,
                        activeKeeperEntityId ?? string.Empty,
                        paradigmState);

            /*
             * PASS 6 — Happening lifecycle closure.
             *
             * Only now, after semantic and institutional
             * consequences exist, can the prepared
             * public episode become Settled.
             */
            List<Happening>
                settledHappenings =
                    new();

            foreach (
                Happening happening
                in preparation.PreparedHappenings)
            {
                if (!happening.TrySettle(
                        globalTurn))
                {
                    throw new InvalidOperationException(
                        "Prepared Happening could not be " +
                        "settled after consequences | " +
                        $"happening={happening.HappeningId}"
                    );
                }

                settledHappenings.Add(
                    happening
                );
            }

            return new
                KvltHappeningConsequenceSettlementResult(
                    globalTurn,
                    settledHappenings,
                    crisisSettlements,
                    legitimacyEvaluations,
                    legitimacyByRelease,
                    fetteringByRelease,
                    coveredApplications,
                    crisisLegitimizedApplications,
                    pendingStoredCount,
                    pendingRedeemedCount,
                    acceptedPrecedentsRaised,
                    paradigmResults
                );
        }

        private static
            Dictionary<
                string,
                SceneReleaseActivationSource>
            BuildSourceLookup(
                IReadOnlyList<
                    SceneReleaseActivationSource>
                    publicSources)
        {
            Dictionary<
                    string,
                    SceneReleaseActivationSource>
                result =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                SceneReleaseActivationSource source
                in publicSources)
            {
                if (source == null)
                {
                    throw new ArgumentException(
                        "Happening settlement source " +
                        "population cannot contain null.",
                        nameof(publicSources)
                    );
                }

                if (!result.TryAdd(
                        source.Release.ReleaseId,
                        source))
                {
                    throw new ArgumentException(
                        "Happening settlement source " +
                        "population contains duplicate " +
                        "SceneRelease identity.",
                        nameof(publicSources)
                    );
                }
            }

            return result;
        }

        private static void
            ValidatePreparedHappenings(
                KvltHappeningPreparationResult
                    preparation,
                int globalTurn)
        {
            foreach (
                Happening happening
                in preparation.PreparedHappenings)
            {
                if (happening.CommittedTurn !=
                        globalTurn ||
                    happening.LifecycleState !=
                        HappeningLifecycleState
                            .Resolving)
                {
                    throw new InvalidOperationException(
                        "Happening consequence settlement " +
                        "received stale lifecycle state | " +
                        $"happening={happening.HappeningId} | " +
                        $"committed={happening.CommittedTurn} | " +
                        $"state={happening.LifecycleState}"
                    );
                }
            }
        }

        private static void
            ValidateAssessmentSources(
                IReadOnlyList<
                    ActivationLegitimacyAssessment>
                    assessments,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseActivationSource>
                    sourcesByRelease)
        {
            foreach (
                ActivationLegitimacyAssessment assessment
                in assessments)
            {
                RequireSource(
                    assessment.Candidate,
                    sourcesByRelease
                );
            }
        }

        private static void
            ValidateCrisisSources(
                IReadOnlyList<
                    ActivationLegitimacyCrisisGroup>
                    groups,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseActivationSource>
                    sourcesByRelease)
        {
            foreach (
                ActivationLegitimacyCrisisGroup group
                in groups)
            {
                foreach (
                    ActivationLegitimacyAssessment
                        assessment
                    in group.Assessments)
                {
                    RequireSource(
                        assessment.Candidate,
                        sourcesByRelease
                    );
                }
            }
        }

        private static
            Dictionary<string, AllegianceCrisis>
            ResolveRequiredCrises(
                IReadOnlyList<
                    ActivationLegitimacyCrisisGroup>
                    groups,
                AllegianceCrisisRegistry registry)
        {
            Dictionary<string, AllegianceCrisis>
                result =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                ActivationLegitimacyCrisisGroup group
                in groups)
            {
                string questionId =
                    group.Question.QuestionId;

                if (!registry.TryGet(
                        questionId,
                        out AllegianceCrisis crisis))
                {
                    throw new InvalidOperationException(
                        "Happening settlement is missing " +
                        "required Allegiance Crisis | " +
                        $"question={questionId}"
                    );
                }

                if (!crisis.IsResolved)
                {
                    throw new InvalidOperationException(
                        "Happening settlement cannot " +
                        "continue while Allegiance Crisis " +
                        "voting remains open | " +
                        $"question={questionId}"
                    );
                }

                if (crisis.Question.QuestionId !=
                        group.Question.QuestionId ||
                    crisis.Question.HappeningId !=
                        group.Question.HappeningId ||
                    crisis.Question.SourceIntentId !=
                        group.Question.SourceIntentId ||
                    crisis.Question
                            .TriggeringActorEntityId !=
                        group.Question
                            .TriggeringActorEntityId)
                {
                    throw new InvalidOperationException(
                        "Resolved Allegiance Crisis " +
                        "provenance does not match the " +
                        "prepared semantic question."
                    );
                }

                result.Add(
                    questionId,
                    crisis
                );
            }

            return result;
        }

        private static
            SceneReleaseActivationSource
            RequireSource(
                ActivationLegitimacyCandidate candidate,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseActivationSource>
                    sourcesByRelease)
        {
            if (!sourcesByRelease.TryGetValue(
                    candidate.SceneReleaseId,
                    out SceneReleaseActivationSource
                        source))
            {
                throw new InvalidOperationException(
                    "Happening semantic candidate has no " +
                    "authoritative public release source | " +
                    $"release={candidate.SceneReleaseId}"
                );
            }

            if (source.DemoTape.DemoTapeId !=
                candidate.SourceDemoTapeId)
            {
                throw new InvalidOperationException(
                    "Happening semantic candidate and " +
                    "public DemoTape provenance disagree | " +
                    $"release={candidate.SceneReleaseId}"
                );
            }

            return source;
        }
    }
}
