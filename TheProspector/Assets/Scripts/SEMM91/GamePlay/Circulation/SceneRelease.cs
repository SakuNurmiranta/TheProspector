using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Circulation
{
    public class SceneRelease
    {
        public string ReleaseId { get; }
        public string DisplayName { get; }

        public string SourceDemoTapeId { get; }
        public string SourceOwnerEntityId { get; }
        public string HostedSceneNodeId { get; }

        
        
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
        
        public int ReleasedTurn =>
            CirculationState?.ReleasedTurn ?? -1;

        private const float VisibilityTolerance =
            0.0001f;

        private float
            pendingVisibilityAdjustment;
        
        public bool HasPendingVisibilityAdjustment
        {
            get;
            private set;
        }

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
        
        public SceneLegacyState LegacyState
        {
            get;
            private set;
        }

        public int CanonizationSubjectSinceRound
        {
            get;
            private set;
        }

        public int CanonizationSubjectYears
        {
            get;
            private set;
        }

        public int CanonizedRound
        {
            get;
            private set;
        }

        /// <summary>
        /// The incoming Keeper whose valid year-end transition
        /// caused this former subject to become canonized.
        ///
        /// This is not necessarily the owner of the release.
        /// </summary>
        public ulong
            CanonizedAtTransitionToKeeperClientId
        {
            get;
            private set;
        }

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
    }
}