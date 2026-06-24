using System;

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

        public int ReleasedTurn =>
            CirculationState?.ReleasedTurn ?? -1;

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

            LegacyState =
                SceneLegacyState.Active;

            CanonizationSubjectSinceRound = -1;
            CanonizationSubjectYears = 0;

            CanonizedRound = -1;

            CanonizedAtTransitionToKeeperClientId =
                ulong.MaxValue;
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
    }
}