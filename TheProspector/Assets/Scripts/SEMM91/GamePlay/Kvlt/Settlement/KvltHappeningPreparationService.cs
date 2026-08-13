using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Prepares all current-turn Happenings for the
    /// institutional reaction portion of the shared
    /// Happening window.
    ///
    /// The caller is responsible for resolving the
    /// ordinary participant Intent outcomes first.
    ///
    /// This service then:
    ///
    /// - materializes successful factual Behavior/Hail
    ///   occurrences;
    /// - records release-specific Activation Attempts,
    ///   including attempts whose Intent was blocked or
    ///   failed;
    /// - classifies successful activation praxis;
    /// - groups uncovered praxis into one Crisis per
    ///   factual occurrence;
    /// - registers those Crises for voting.
    ///
    /// It does not:
    ///
    /// - resolve Allegiance Crisis votes;
    /// - apply activation;
    /// - store Pending activation;
    /// - redeem Pending activation;
    /// - fetter releases;
    /// - settle Happenings.
    ///
    /// Those belong to the later HappeningSettlement
    /// phase.
    /// </summary>
    public sealed class
        KvltHappeningPreparationService
    {
        private readonly
            ActivationAttemptDiscoveryService
            attemptDiscovery =
                new();

        private readonly
            ActivationLegitimacyClassificationService
            legitimacyClassifier =
                new();

        private readonly
            ActivationLegitimacyCrisisGroupingService
            crisisGrouping =
                new();

        public KvltHappeningPreparationResult
            Prepare(
                int globalTurn,
                IReadOnlyList<Happening> happenings,
                IReadOnlyList<
                    SceneReleaseActivationSource>
                    publicSources,
                AcceptedTransgressionState
                    acceptedTransgressions,
                AllegianceCrisisRegistry
                    crisisRegistry,
                IReadOnlyList<string>
                    kvltParticipantEntityIds,
                string keeperEntityId)
        {
            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            if (happenings == null)
            {
                throw new ArgumentNullException(
                    nameof(happenings)
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

            if (kvltParticipantEntityIds == null)
            {
                throw new ArgumentNullException(
                    nameof(kvltParticipantEntityIds)
                );
            }

            keeperEntityId =
                RequireText(
                    keeperEntityId,
                    nameof(keeperEntityId)
                );

            ValidateSources(
                publicSources
            );

            string[] normalizedParticipants =
                NormalizeParticipants(
                    kvltParticipantEntityIds
                );

            List<Happening>
                preparedHappenings =
                    new();

            List<
                SceneReleaseActivationAttempt>
                allAttempts =
                    new();

            List<
                ActivationLegitimacyAssessment>
                allAssessments =
                    new();

            int behaviorCount =
                0;

            int hailCount =
                0;

            /*
             * Preparation is deterministic by Happening
             * identity rather than caller collection
             * order.
             */
            List<Happening>
                orderedHappenings =
                    GetOrderedCurrentTurnHappenings(
                        happenings,
                        globalTurn
                    );

            foreach (
                Happening happening
                in orderedHappenings)
            {
                ValidateHappeningReadyForPreparation(
                    happening,
                    globalTurn
                );

                /*
                 * PASS 1:
                 *
                 * Materialize successful factual
                 * EnactBehavior outcomes before Hail
                 * legitimacy classification reads them.
                 */
                foreach (
                    HappeningParticipantIntent intent
                    in happening.ParticipantIntents)
                {
                    if (intent is not
                        HappeningEnactBehaviorIntent
                            enact)
                    {
                        continue;
                    }

                    HappeningIntentResolution
                        resolution =
                            RequireResolution(
                                happening,
                                intent
                            );

                    if (resolution.Outcome !=
                        HappeningIntentOutcome.Succeeded)
                    {
                        continue;
                    }

                    if (HasBehaviorForIntent(
                            happening,
                            intent.IntentId))
                    {
                        /*
                         * Preparation is not intended to
                         * duplicate factual records.
                         *
                         * Existing matching materialization
                         * is accepted so this operation
                         * remains safe if presentation
                         * re-enters preparation before
                         * settlement.
                         */
                        continue;
                    }

                    string behaviorId =
                        BuildOccurrenceId(
                            "BEHAVIOR",
                            happening.HappeningId,
                            intent.IntentId
                        );

                    string hailId =
                        enact.HasIntendedHail
                            ? BuildOccurrenceId(
                                "HAIL",
                                happening.HappeningId,
                                intent.IntentId
                            )
                            : null;

                    if (!happening
                            .TryMaterializeSuccessfulEnactBehavior(
                                intent.IntentId,
                                behaviorId,
                                hailId
                            ))
                    {
                        throw new InvalidOperationException(
                            "Successful EnactBehavior Intent " +
                            "could not materialize factual " +
                            "Happening provenance | " +
                            $"happening={happening.HappeningId} | " +
                            $"intent={intent.IntentId}"
                        );
                    }

                    behaviorCount++;

                    if (enact.HasIntendedHail)
                    {
                        hailCount++;
                    }
                }

                /*
                 * PASS 2:
                 *
                 * Discover release-specific Activation
                 * Attempts from every activation-capable
                 * Intent.
                 *
                 * Discovery is independent of success.
                 * Therefore blocked/failed attempts remain
                 * visible to Fringe grace.
                 */
                foreach (
                    HappeningParticipantIntent intent
                    in happening.ParticipantIntents)
                {
                    RequireResolution(
                        happening,
                        intent
                    );

                    IReadOnlyList<
                            SceneReleaseActivationAttempt>
                        discovered =
                            attemptDiscovery.Discover(
                                happening,
                                intent.IntentId,
                                publicSources
                            );

                    foreach (
                        SceneReleaseActivationAttempt
                            attempt
                        in discovered)
                    {
                        SceneReleaseActivationSource
                            source =
                                RequireSourceForAttempt(
                                    attempt,
                                    publicSources
                                );

                        SceneReleaseActivationAttempt
                            authoritativeAttempt =
                                FindExistingAttempt(
                                    source.Release,
                                    attempt
                                );

                        if (authoritativeAttempt == null)
                        {
                            if (!source.Release
                                    .TryRecordActivationAttempt(
                                        attempt))
                            {
                                throw new
                                    InvalidOperationException(
                                        "Discovered Activation " +
                                        "Attempt could not be " +
                                        "recorded | " +
                                        $"release=" +
                                        $"{attempt.SceneReleaseId} | " +
                                        $"happening=" +
                                        $"{attempt.HappeningId} | " +
                                        $"intent=" +
                                        $"{attempt.SourceIntentId}"
                                    );
                            }

                            authoritativeAttempt =
                                attempt;
                        }

                        allAttempts.Add(
                            authoritativeAttempt
                        );

                        /*
                         * Classification itself checks the
                         * Intent resolution. Blocked/failed
                         * attempts consequently produce no
                         * legitimacy assessments.
                         */
                        IReadOnlyList<
                                ActivationLegitimacyAssessment>
                            assessments =
                                legitimacyClassifier.Classify(
                                    happening,
                                    authoritativeAttempt,
                                    source,
                                    acceptedTransgressions
                                );

                        foreach (
                            ActivationLegitimacyAssessment
                                assessment
                            in assessments)
                        {
                            allAssessments.Add(
                                assessment
                            );
                        }
                    }
                }

                preparedHappenings.Add(
                    happening
                );
            }

            /*
             * PASS 3:
             *
             * One factual praxis occurrence produces one
             * institutional question even when several
             * releases/pairs would gain semantic
             * consequences from the answer.
             */
            IReadOnlyList<
                    ActivationLegitimacyCrisisGroup>
                groups =
                    crisisGrouping.Group(
                        allAssessments,
                        globalTurn
                    );

            List<AllegianceCrisis>
                openedCrises =
                    new();

            foreach (
                ActivationLegitimacyCrisisGroup group
                in groups)
            {
                AllegianceCrisisQuestion question =
                    group.Question;

                if (crisisRegistry.TryGet(
                        question.QuestionId,
                        out AllegianceCrisis existing))
                {
                    ValidateExistingCrisis(
                        existing,
                        question
                    );

                    openedCrises.Add(
                        existing
                    );

                    continue;
                }

                string[] eligibleVoters =
                    BuildEligibleVoters(
                        normalizedParticipants,
                        question.TriggeringActorEntityId
                    );

                AllegianceCrisis crisis =
                    new(
                        question,
                        eligibleVoters,
                        keeperEntityId
                    );

                crisisRegistry.Record(
                    crisis
                );

                openedCrises.Add(
                    crisis
                );
            }

            return new
                KvltHappeningPreparationResult(
                    globalTurn,
                    preparedHappenings,
                    allAttempts,
                    allAssessments,
                    groups,
                    openedCrises,
                    behaviorCount,
                    hailCount
                );
        }

        private static void
            ValidateHappeningReadyForPreparation(
                Happening happening,
                int globalTurn)
        {
            if (happening == null)
            {
                throw new ArgumentException(
                    "Happening preparation population " +
                    "cannot contain null.",
                    nameof(happening)
                );
            }

            if (happening.CommittedTurn !=
                globalTurn)
            {
                throw new InvalidOperationException(
                    "Happening preparation may only " +
                    "consume the current turn | " +
                    $"happening={happening.HappeningId} | " +
                    $"committed={happening.CommittedTurn} | " +
                    $"current={globalTurn}"
                );
            }

            if (happening.LifecycleState !=
                HappeningLifecycleState.Resolving)
            {
                throw new InvalidOperationException(
                    "Happening must already be Resolving " +
                    "before institutional preparation | " +
                    $"happening={happening.HappeningId} | " +
                    $"state={happening.LifecycleState}"
                );
            }

            foreach (
                HappeningParticipantIntent intent
                in happening.ParticipantIntents)
            {
                RequireResolution(
                    happening,
                    intent
                );
            }
        }

        private static
            HappeningIntentResolution
            RequireResolution(
                Happening happening,
                HappeningParticipantIntent intent)
        {
            if (!happening.TryGetIntentResolution(
                    intent.IntentId,
                    out HappeningIntentResolution
                        resolution))
            {
                throw new InvalidOperationException(
                    "Happening institutional preparation " +
                    "requires every participant Intent " +
                    "to have an authoritative outcome | " +
                    $"happening={happening.HappeningId} | " +
                    $"intent={intent.IntentId}"
                );
            }

            return resolution;
        }

        private static bool
            HasBehaviorForIntent(
                Happening happening,
                string intentId)
        {
            foreach (
                BehaviorOccurrence behavior
                in happening.BehaviorOccurrences)
            {
                if (behavior.SourceIntentId ==
                    intentId)
                {
                    return true;
                }
            }

            return false;
        }

        private static
            SceneReleaseActivationAttempt
            FindExistingAttempt(
                SceneRelease release,
                SceneReleaseActivationAttempt
                    candidate)
        {
            foreach (
                SceneReleaseActivationAttempt existing
                in release.ActivationAttempts)
            {
                if (existing.HappeningId ==
                        candidate.HappeningId &&
                    existing.SourceIntentId ==
                        candidate.SourceIntentId)
                {
                    if (existing.SceneReleaseId !=
                            candidate.SceneReleaseId ||
                        existing.SourceDemoTapeId !=
                            candidate.SourceDemoTapeId ||
                        existing.Route !=
                            candidate.Route)
                    {
                        throw new
                            InvalidOperationException(
                                "Existing Activation Attempt " +
                                "collides with preparation " +
                                "provenance."
                            );
                    }

                    return existing;
                }
            }

            return null;
        }

        private static
            SceneReleaseActivationSource
            RequireSourceForAttempt(
                SceneReleaseActivationAttempt attempt,
                IReadOnlyList<
                    SceneReleaseActivationSource>
                    publicSources)
        {
            SceneReleaseActivationSource found =
                null;

            foreach (
                SceneReleaseActivationSource source
                in publicSources)
            {
                if (source.Release.ReleaseId !=
                        attempt.SceneReleaseId ||
                    source.DemoTape.DemoTapeId !=
                        attempt.SourceDemoTapeId)
                {
                    continue;
                }

                if (found != null)
                {
                    throw new InvalidOperationException(
                        "Activation source population " +
                        "contains duplicate release/demo " +
                        "identity."
                    );
                }

                found =
                    source;
            }

            return found ??
                throw new InvalidOperationException(
                    "Discovered Activation Attempt has no " +
                    "matching public source | " +
                    $"release={attempt.SceneReleaseId} | " +
                    $"demo={attempt.SourceDemoTapeId}"
                );
        }

        private static void ValidateSources(
            IReadOnlyList<
                SceneReleaseActivationSource>
                publicSources)
        {
            HashSet<string> releaseIds =
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
                        "Public activation source " +
                        "population cannot contain null.",
                        nameof(publicSources)
                    );
                }

                if (!releaseIds.Add(
                        source.Release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Public activation source " +
                        "population contains duplicate " +
                        "SceneRelease identity.",
                        nameof(publicSources)
                    );
                }
            }
        }

        private static List<Happening>
            GetOrderedCurrentTurnHappenings(
                IReadOnlyList<Happening> happenings,
                int globalTurn)
        {
            List<Happening> result =
                new();

            HashSet<string> ids =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                Happening happening
                in happenings)
            {
                if (happening == null)
                {
                    throw new ArgumentException(
                        "Happening preparation population " +
                        "cannot contain null.",
                        nameof(happenings)
                    );
                }

                if (!ids.Add(
                        happening.HappeningId))
                {
                    throw new ArgumentException(
                        "Happening preparation population " +
                        "contains duplicate identity.",
                        nameof(happenings)
                    );
                }

                if (happening.CommittedTurn ==
                    globalTurn)
                {
                    result.Add(
                        happening
                    );
                }
            }

            result.Sort(
                (
                    left,
                    right
                ) =>
                    string.CompareOrdinal(
                        left.HappeningId,
                        right.HappeningId
                    )
            );

            return result;
        }

        private static string[]
            NormalizeParticipants(
                IReadOnlyList<string>
                    participantEntityIds)
        {
            List<string> result =
                new();

            HashSet<string> seen =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                string entityId
                in participantEntityIds)
            {
                string normalized =
                    RequireText(
                        entityId,
                        nameof(participantEntityIds)
                    );

                if (!seen.Add(
                        normalized))
                {
                    throw new ArgumentException(
                        "KVLT participant population " +
                        "contains duplicate identity.",
                        nameof(participantEntityIds)
                    );
                }

                result.Add(
                    normalized
                );
            }

            if (result.Count == 0)
            {
                throw new ArgumentException(
                    "Happening preparation requires at " +
                    "least one KVLT participant.",
                    nameof(participantEntityIds)
                );
            }

            result.Sort(
                StringComparer.Ordinal
            );

            return result.ToArray();
        }

        private static string[]
            BuildEligibleVoters(
                IReadOnlyList<string>
                    kvltParticipants,
                string triggeringActorEntityId)
        {
            List<string> voters =
                new();

            foreach (
                string entityId
                in kvltParticipants)
            {
                if (entityId ==
                    triggeringActorEntityId)
                {
                    continue;
                }

                voters.Add(
                    entityId
                );
            }

            if (voters.Count == 0)
            {
                throw new InvalidOperationException(
                    "Allegiance Crisis has no eligible " +
                    "KVLT voters after excluding the " +
                    "triggering actor."
                );
            }

            return voters.ToArray();
        }

        private static void
            ValidateExistingCrisis(
                AllegianceCrisis crisis,
                AllegianceCrisisQuestion question)
        {
            if (crisis.Question.QuestionId !=
                    question.QuestionId ||
                crisis.Question.HappeningId !=
                    question.HappeningId ||
                crisis.Question.SourceIntentId !=
                    question.SourceIntentId ||
                crisis.Question.TriggeringActorEntityId !=
                    question.TriggeringActorEntityId ||
                crisis.Question.BehaviorTypeId !=
                    question.BehaviorTypeId ||
                crisis.Question.Axis !=
                    question.Axis ||
                crisis.Question.Pole !=
                    question.Pole ||
                crisis.Question.PraxisDegree !=
                    question.PraxisDegree ||
                crisis.Question.BehaviorOccurrenceId !=
                    question.BehaviorOccurrenceId)
            {
                throw new InvalidOperationException(
                    "Existing Allegiance Crisis identity " +
                    "collides with different semantic " +
                    "provenance."
                );
            }
        }

        private static string BuildOccurrenceId(
            string prefix,
            string happeningId,
            string intentId)
        {
            return
                prefix +
                "|" +
                happeningId.Length +
                ":" +
                happeningId +
                "|" +
                intentId.Length +
                ":" +
                intentId;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Happening preparation identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}