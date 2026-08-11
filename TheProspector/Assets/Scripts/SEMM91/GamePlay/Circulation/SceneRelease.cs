using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation
{
    public class SceneRelease
    {
        public string ReleaseId { get; }
        public string DisplayName { get; }

        public string SourceDemoTapeId { get; }
        public string SourceOwnerEntityId { get; }
        public string HostedSceneNodeId { get; }

        public SceneReleaseRejectionState
            RejectionState { get; private set; }

        public bool IsRejected =>
            LifecycleState ==
            SceneReleaseLifecycleState.Rejected;

        public bool HasRejectionState =>
            RejectionState != null;

        public ReleaseCirculationState
            CirculationState { get; }

        private readonly
            List<SceneReleaseActivationAttempt>
            activationAttempts =
                new();

        public IReadOnlyList<
                SceneReleaseActivationAttempt>
            ActivationAttempts =>
            activationAttempts;

        private readonly
            List<SceneReleaseLifecycleTransition>
            lifecycleTransitions =
                new();

        public SceneReleaseLifecycleState
            LifecycleState { get; private set; }

        public IReadOnlyList<
                SceneReleaseLifecycleTransition>
            LifecycleTransitions =>
            lifecycleTransitions;

        public SceneReleaseFieldPositionState
            FieldPositionState { get; private set; }

        public bool HasFieldPosition =>
            FieldPositionState != null;
        
        public int ReleasedTurn =>
            CirculationState?.ReleasedTurn ?? -1;

        private const float VisibilityTolerance =
            0.0001f;

        private float
            pendingVisibilityAdjustment;

        public bool HasPendingVisibilityAdjustment { get; private set; }

        private readonly
            List<SceneReleasePairActivationState>
            pairActivationStates =
                new();

        private readonly
            Dictionary<
                SceneReleasePairActivationKey,
                SceneReleasePairActivationState>
            pairActivationStatesByKey =
                new();

        private readonly
            List<SceneReleaseActivationHistoryRecord>
            activationHistory =
                new();

        private readonly
            List<SceneReleasePendingActivation>
            pendingActivations =
                new();

        private readonly
            Dictionary<
                string,
                SceneReleasePendingActivation>
            pendingActivationsById =
                new(
                    StringComparer.Ordinal
                );

        public IReadOnlyList<
                SceneReleasePairActivationState>
            PairActivationStates =>
            pairActivationStates;

        public IReadOnlyList<
                SceneReleaseActivationHistoryRecord>
            ActivationHistory =>
            activationHistory;

        public IReadOnlyList<
                SceneReleasePendingActivation>
            PendingActivations =>
            pendingActivations;
        
        public float PendingVisibilityAdjustment =>
            pendingVisibilityAdjustment;

        public float EffectiveVisibility
        {
            get
            {
                float organicReach =
                    CirculationState?.Reach ?? 0.0f;

                return Clamp01(
                    organicReach +
                    pendingVisibilityAdjustment
                );
            }
        }

        public SceneLegacyState LegacyState { get; private set; }

        public int CanonizationSubjectSinceRound { get; private set; }

        public int CanonizationSubjectYears { get; private set; }

        public int CanonizedRound { get; private set; }

        /// <summary>
        /// The incoming Keeper whose valid year-end transition
        /// caused this former subject to become canonized.
        ///
        /// This is not necessarily the owner of the release.
        /// </summary>
        public ulong
            CanonizedAtTransitionToKeeperClientId { get; private set; }

        public bool IsEligiblePrimaryWorkProxy =>
            LegacyState ==
            SceneLegacyState.Active;

        public bool TryRecordActivationAttempt(
            SceneReleaseActivationAttempt attempt)
        {
            if (attempt == null)
            {
                return false;
            }

            if (attempt.SceneReleaseId !=
                ReleaseId)
            {
                return false;
            }

            if (attempt.SourceDemoTapeId !=
                SourceDemoTapeId)
            {
                return false;
            }

            if (attempt.DeclaredTurn <
                ReleasedTurn)
            {
                return false;
            }

            foreach (
                SceneReleaseActivationAttempt existing
                in activationAttempts)
            {
                if (existing.HappeningId ==
                    attempt.HappeningId &&
                    existing.SourceIntentId ==
                    attempt.SourceIntentId)
                {
                    return false;
                }
            }

            activationAttempts.Add(
                attempt
            );

            return true;
        }

        public bool TryEstablishFieldPosition(
            float initialPosition,
            int globalTurn)
        {
            if (LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                return false;
            }

            if (FieldPositionState != null)
            {
                return false;
            }

            if (float.IsNaN(initialPosition) ||
                float.IsInfinity(initialPosition))
            {
                return false;
            }

            /*
             * Do not permit retroactive Field placement
             * before the release actually entered Field.
             */
            int fetteredTurn =
                -1;

            foreach (
                SceneReleaseLifecycleTransition transition
                in lifecycleTransitions)
            {
                if (transition.ToState ==
                    SceneReleaseLifecycleState.Field)
                {
                    fetteredTurn =
                        transition.GlobalTurn;

                    break;
                }
            }

            if (fetteredTurn < 0 ||
                globalTurn <
                fetteredTurn)
            {
                return false;
            }

            FieldPositionState =
                new SceneReleaseFieldPositionState(
                    initialPosition,
                    globalTurn
                );

            return true;
        }
        
        public bool TryApplyFieldMovement(
            float delta,
            int globalTurn,
            out SceneReleaseFieldPositionTransition
                transition)
        {
            transition =
                null;

            if (LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                return false;
            }

            if (FieldPositionState == null)
            {
                return false;
            }

            return FieldPositionState
                .TryApplySettlementMovement(
                    delta,
                    globalTurn,
                    out transition
                );
        }
        
        public SceneRelease(
            string displayName,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string hostedSceneNodeId,
            int releasedTurn,
            float sourceConveyance)
        {
            ReleaseId =
                Guid.NewGuid().ToString();

            DisplayName =
                displayName ?? string.Empty;

            SourceDemoTapeId =
                sourceDemoTapeId ?? string.Empty;

            SourceOwnerEntityId =
                sourceOwnerEntityId ?? string.Empty;

            HostedSceneNodeId =
                hostedSceneNodeId ?? string.Empty;

            CirculationState =
                new ReleaseCirculationState(
                    SourceDemoTapeId,
                    SourceOwnerEntityId,
                    releasedTurn,
                    sourceConveyance
                );

            LifecycleState =
                SceneReleaseLifecycleState.Fringe;

            LegacyState =
                SceneLegacyState.Active;

            CanonizationSubjectSinceRound = -1;
            CanonizationSubjectYears = 0;

            CanonizedRound = -1;

            CanonizedAtTransitionToKeeperClientId =
                ulong.MaxValue;

            pendingVisibilityAdjustment =
                0.0f;

            HasPendingVisibilityAdjustment =
                false;
        }

        public bool TryBeginCanonizationSubject(
            int startedRound)
        {
            if (LegacyState !=
                SceneLegacyState.Active)
            {
                return false;
            }

            LegacyState =
                SceneLegacyState
                    .CanonizationSubject;

            CanonizationSubjectSinceRound =
                Math.Max(0, startedRound);

            CanonizationSubjectYears = 0;

            return true;
        }

        public bool
            TryAdvanceCanonizationSubjectYear()
        {
            if (LegacyState !=
                SceneLegacyState
                    .CanonizationSubject)
            {
                return false;
            }

            CanonizationSubjectYears++;

            return true;
        }

        public bool TryCanonize(
            int canonizedRound,
            ulong incomingKeeperClientId)
        {
            if (LegacyState !=
                SceneLegacyState
                    .CanonizationSubject)
            {
                return false;
            }

            LegacyState =
                SceneLegacyState.Canonized;

            CanonizedRound =
                Math.Max(0, canonizedRound);

            CanonizedAtTransitionToKeeperClientId =
                incomingKeeperClientId;

            return true;
        }

        public bool TryPreviewVisibilityAdjustment(
            float requestedDelta,
            out float appliedDelta)
        {
            appliedDelta = 0.0f;

            if (float.IsNaN(requestedDelta) ||
                float.IsInfinity(requestedDelta) ||
                Math.Abs(requestedDelta) <=
                VisibilityTolerance)
            {
                return false;
            }

            float normalizedDelta =
                ClampSigned01(
                    requestedDelta
                );

            float visibilityBefore =
                EffectiveVisibility;

            float visibilityAfter =
                Clamp01(
                    visibilityBefore +
                    normalizedDelta
                );

            appliedDelta =
                visibilityAfter -
                visibilityBefore;

            if (Math.Abs(appliedDelta) <=
                VisibilityTolerance)
            {
                appliedDelta = 0.0f;
                return false;
            }

            return true;
        }

        public bool TryStageVisibilityAdjustment(
            float requestedDelta,
            out float appliedDelta)
        {
            if (!TryPreviewVisibilityAdjustment(
                    requestedDelta,
                    out appliedDelta
                ))
            {
                return false;
            }

            pendingVisibilityAdjustment +=
                appliedDelta;

            pendingVisibilityAdjustment =
                ClampSigned01(
                    pendingVisibilityAdjustment
                );

            HasPendingVisibilityAdjustment =
                true;

            return true;
        }

        public bool
            ConsumePendingVisibilityAdjustment()
        {
            if (!HasPendingVisibilityAdjustment)
                return false;

            pendingVisibilityAdjustment =
                0.0f;

            HasPendingVisibilityAdjustment =
                false;

            return true;
        }

        private static float Clamp01(
            float value)
        {
            if (value < 0.0f)
                return 0.0f;

            if (value > 1.0f)
                return 1.0f;

            return value;
        }

        private static float ClampSigned01(
            float value)
        {
            if (value < -1.0f)
                return -1.0f;

            if (value > 1.0f)
                return 1.0f;

            return value;
        }

        public bool TryFetter(
            int globalTurn)
        {
            if (LifecycleState !=
                SceneReleaseLifecycleState.Fringe)
            {
                return false;
            }

            if (globalTurn < ReleasedTurn)
            {
                return false;
            }

            SceneReleaseLifecycleState previous =
                LifecycleState;

            LifecycleState =
                SceneReleaseLifecycleState.Field;

            lifecycleTransitions.Add(
                new SceneReleaseLifecycleTransition(
                    previous,
                    LifecycleState,
                    globalTurn
                )
            );

            return true;
        }

        public bool TryFailToFetter(
            int globalTurn)
        {
            if (LifecycleState !=
                SceneReleaseLifecycleState.Fringe)
            {
                return false;
            }

            if (globalTurn < ReleasedTurn)
            {
                return false;
            }

            SceneReleaseLifecycleState previous =
                LifecycleState;

            LifecycleState =
                SceneReleaseLifecycleState
                    .FailedToFetter;

            lifecycleTransitions.Add(
                new SceneReleaseLifecycleTransition(
                    previous,
                    LifecycleState,
                    globalTurn
                )
            );

            return true;
        }

        public bool TryReject(
            int globalTurn,
            float outerBoundary)
        {
            if (LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                return false;
            }

            if (FieldPositionState == null)
            {
                return false;
            }

            if (RejectionState != null)
            {
                return false;
            }

            if (globalTurn < 0)
            {
                return false;
            }

            if (float.IsNaN(outerBoundary) ||
                float.IsInfinity(outerBoundary))
            {
                return false;
            }

            if (globalTurn <
                FieldPositionState.EstablishedTurn)
            {
                return false;
            }

            if (FieldPositionState.LastMovementTurn >
                globalTurn)
            {
                return false;
            }

            float rejectedPosition =
                FieldPositionState.CurrentPosition;

            /*
             * Reaching the boundary is not rejection.
             * The release must have crossed outward.
             */
            if (rejectedPosition >=
                outerBoundary)
            {
                return false;
            }

            RejectionState =
                new SceneReleaseRejectionState(
                    globalTurn,
                    outerBoundary,
                    rejectedPosition,
                    FieldPositionState.PeakInwardPosition
                );

            SceneReleaseLifecycleState previous =
                LifecycleState;

            LifecycleState =
                SceneReleaseLifecycleState.Rejected;

            lifecycleTransitions.Add(
                new SceneReleaseLifecycleTransition(
                    previous,
                    LifecycleState,
                    globalTurn
                )
            );

            return true;
        }
        
        public bool TryGetPairActivationState(
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex,
            out SceneReleasePairActivationState state)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceTrackId) ||
                string.IsNullOrWhiteSpace(
                    sourceIdeaId) ||
                ideaIndex < 0)
            {
                state = null;
                return false;
            }

            SceneReleasePairActivationKey key =
                new(
                    sourceTrackId,
                    sourceIdeaId,
                    ideaIndex
                );

            return pairActivationStatesByKey.TryGetValue(
                key,
                out state
            );
        }

        internal bool TryApplyLegitimateActivation(
            ActivationLegitimacyCandidate candidate,
            AcceptedTransgressionRecord precedent,
            SceneReleaseActivationHistoryKind historyKind,
            int occurredTurn,
            string allegianceCrisisQuestionId = null,
            string pendingActivationId = null)
        {
            if (candidate == null ||
                precedent == null)
            {
                return false;
            }

            if (candidate.SceneReleaseId !=
                ReleaseId ||
                candidate.SourceDemoTapeId !=
                SourceDemoTapeId)
            {
                return false;
            }

            if (historyKind ==
                SceneReleaseActivationHistoryKind
                    .PendingStored)
            {
                return false;
            }

            if (occurredTurn <
                ReleasedTurn)
            {
                return false;
            }

            if (precedent.AcceptedTurn >
                occurredTurn)
            {
                return false;
            }

            if (precedent.Key.BehaviorTypeId !=
                candidate.BehaviorTypeId ||
                precedent.Key.Axis !=
                candidate.Axis ||
                precedent.Key.Pole !=
                candidate.Pole ||
                (int)precedent.AcceptedDegree <
                (int)candidate.PraxisDegree)
            {
                return false;
            }

            if ((int)candidate.AppliedActivationDegree >
                (int)candidate.RecordedDominantDegree)
            {
                return false;
            }

            SceneReleasePairActivationKey key =
                new(
                    candidate.SourceTrackId,
                    candidate.SourceIdeaId,
                    candidate.IdeaIndex
                );

            if (!pairActivationStatesByKey.TryGetValue(
                    key,
                    out SceneReleasePairActivationState
                        state))
            {
                state =
                    new SceneReleasePairActivationState(
                        key,
                        candidate.RecordedDominantDegree
                    );

                pairActivationStates.Add(
                    state
                );

                pairActivationStatesByKey.Add(
                    key,
                    state
                );
            }
            else if (state.RecordedDominantDegree !=
                     candidate.RecordedDominantDegree)
            {
                return false;
            }

            if (!state.TryApply(
                    candidate.AppliedActivationDegree,
                    out var previous,
                    out var current))
            {
                return false;
            }

            activationHistory.Add(
                new SceneReleaseActivationHistoryRecord(
                    historyKind,
                    candidate,
                    previous,
                    current,
                    occurredTurn,
                    precedent,
                    allegianceCrisisQuestionId,
                    pendingActivationId
                )
            );

            return true;
        }
        
        internal bool TryStorePendingActivation(
            PendingActivationCandidate candidate,
            out SceneReleasePendingActivation pending)
        {
            pending = null;

            if (candidate == null)
            {
                return false;
            }

            ActivationLegitimacyCandidate activation =
                candidate.Candidate;

            if (activation.SceneReleaseId !=
                ReleaseId ||
                activation.SourceDemoTapeId !=
                SourceDemoTapeId)
            {
                return false;
            }

            if (candidate.PendingTurn <
                ReleasedTurn)
            {
                return false;
            }

            if ((int)activation.AppliedActivationDegree >
                (int)activation.RecordedDominantDegree)
            {
                return false;
            }

            SceneReleasePendingActivation proposed =
                new(
                    candidate
                );

            if (pendingActivationsById.ContainsKey(
                    proposed.PendingActivationId))
            {
                return false;
            }

            TagDegree currentDegree =
                TagDegree.Neutral;

            SceneReleasePairActivationKey key =
                new(
                    activation.SourceTrackId,
                    activation.SourceIdeaId,
                    activation.IdeaIndex
                );

            if (pairActivationStatesByKey.TryGetValue(
                    key,
                    out SceneReleasePairActivationState
                        existingState))
            {
                currentDegree =
                    existingState.CurrentActivationDegree;
            }

            pendingActivations.Add(
                proposed
            );

            pendingActivationsById.Add(
                proposed.PendingActivationId,
                proposed
            );

            activationHistory.Add(
                new SceneReleaseActivationHistoryRecord(
                    SceneReleaseActivationHistoryKind
                        .PendingStored,
                    activation,
                    currentDegree,
                    currentDegree,
                    candidate.PendingTurn,
                    null,
                    candidate.AllegianceCrisisQuestionId,
                    proposed.PendingActivationId
                )
            );

            pending =
                proposed;

            return true;
        }
        
        internal bool TryRedeemPendingActivation(
            SceneReleasePendingActivation pending,
            AcceptedTransgressionRecord precedent,
            int redeemedTurn)
        {
            if (pending == null ||
                precedent == null ||
                pending.IsRedeemed)
            {
                return false;
            }

            if (!pendingActivationsById.TryGetValue(
                    pending.PendingActivationId,
                    out SceneReleasePendingActivation stored))
            {
                return false;
            }

            if (!ReferenceEquals(
                    stored,
                    pending))
            {
                return false;
            }

            if (redeemedTurn <
                pending.PendingTurn)
            {
                return false;
            }

            ActivationLegitimacyCandidate candidate =
                pending.Candidate;

            if (precedent.Key.BehaviorTypeId !=
                candidate.BehaviorTypeId ||
                precedent.Key.Axis !=
                candidate.Axis ||
                precedent.Key.Pole !=
                candidate.Pole ||
                (int)precedent.AcceptedDegree <
                (int)candidate.PraxisDegree)
            {
                return false;
            }

            if (precedent.AcceptedTurn >
                redeemedTurn)
            {
                return false;
            }

            if (!TryApplyLegitimateActivation(
                    candidate,
                    precedent,
                    SceneReleaseActivationHistoryKind
                        .PendingRedeemed,
                    redeemedTurn,
                    pending.AllegianceCrisisQuestionId,
                    pending.PendingActivationId
                ))
            {
                return false;
            }

            pending.MarkRedeemed(
                precedent,
                redeemedTurn
            );

            return true;
        }
    }
}