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

        public ReleaseCirculationState CirculationState { get; }

        public SceneRelease(
            string displayName,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string hostedSceneNodeId,
            int releasedTurn,
            float sourceConveyance)
        {
            ReleaseId = Guid.NewGuid().ToString();
            DisplayName = displayName;

            SourceDemoTapeId = sourceDemoTapeId;
            SourceOwnerEntityId = sourceOwnerEntityId;
            HostedSceneNodeId = hostedSceneNodeId;

            CirculationState = new ReleaseCirculationState(
                sourceDemoTapeId,
                sourceOwnerEntityId,
                releasedTurn,
                sourceConveyance
            );
        }
    }
}