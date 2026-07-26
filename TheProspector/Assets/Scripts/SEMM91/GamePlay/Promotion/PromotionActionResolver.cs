using SEMM91.Core.Entities;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.SceneSpace;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Promotion
{
    public class PromotionActionResolver
    {
        public bool TryReleaseLatestDemoToKvlt(
            ulong clientId,
            GameEntity playerEntity,
            SeededWorldState worldState,
            out string message)
        {
            message = string.Empty;

            if (playerEntity == null)
            {
                message =
                    $"Client {clientId}: missing player entity.";

                return false;
            }

            DemoTape demo =
                playerEntity
                    .GetLatestUnreleasedDemoTape();

            if (demo == null)
            {
                message =
                    $"Client {clientId}: no unreleased " +
                    "demo tape available.";

                return false;
            }

            return TryReleaseResolvedDemoToKvlt(
                clientId,
                playerEntity,
                worldState,
                demo,
                out message
            );
        }

        public bool TryReleaseDemoToKvlt(
            ulong clientId,
            GameEntity playerEntity,
            SeededWorldState worldState,
            string demoTapeId,
            out string message)
        {
            message = string.Empty;

            if (playerEntity == null)
            {
                message =
                    $"Client {clientId}: missing player entity.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(demoTapeId))
            {
                message =
                    $"Client {clientId}: selected demo tape ID " +
                    "is empty.";

                return false;
            }

            if (!playerEntity.TryGetDemoTapeById(
                    demoTapeId,
                    out DemoTape demo
                ))
            {
                message =
                    $"Client {clientId}: selected demo tape " +
                    $"'{demoTapeId}' is not owned by the " +
                    "acting entity.";

                return false;
            }

            if (demo.SceneState !=
                DemoTapeSceneState.Unreleased)
            {
                message =
                    $"Client {clientId}: demo " +
                    $"'{demo.DisplayName}' is no longer " +
                    $"unreleased. state={demo.SceneState}.";

                return false;
            }

            return TryReleaseResolvedDemoToKvlt(
                clientId,
                playerEntity,
                worldState,
                demo,
                out message
            );
        }

        private static bool TryReleaseResolvedDemoToKvlt(
            ulong clientId,
            GameEntity playerEntity,
            SeededWorldState worldState,
            DemoTape demo,
            out string message)
        {
            message = string.Empty;

            if (playerEntity == null)
            {
                message =
                    $"Client {clientId}: missing player entity.";

                return false;
            }

            if (worldState == null)
            {
                message =
                    $"Client {clientId}: missing seeded " +
                    "world state.";

                return false;
            }

            if (demo == null)
            {
                message =
                    $"Client {clientId}: missing demo tape.";

                return false;
            }

            if (demo.SceneState !=
                DemoTapeSceneState.Unreleased)
            {
                message =
                    $"Client {clientId}: demo " +
                    $"'{demo.DisplayName}' cannot be released " +
                    $"from state {demo.SceneState}.";

                return false;
            }

            SceneSpaceGraph graph =
                worldState.SceneSpaceGraph;

            if (graph == null)
            {
                message =
                    $"Client {clientId}: missing scene-space " +
                    "graph.";

                return false;
            }

            if (!graph.TryGetNode(
                    StartingCollectiveBootstrapper.NodeKvltScene,
                    out SceneSpaceNode kvltNode
                ))
            {
                message =
                    $"Client {clientId}: KVLT scene node " +
                    "not found.";

                return false;
            }

            if (kvltNode.Status !=
                SceneSpaceNodeStatus.Active)
            {
                message =
                    $"Client {clientId}: KVLT scene node is " +
                    $"not active. status={kvltNode.Status}.";

                return false;
            }

            if (kvltNode.ConstructMode !=
                SceneConstructMode.Or)
            {
                message =
                    $"Client {clientId}: KVLT scene node is " +
                    $"not OR. construct={kvltNode.ConstructMode}.";

                return false;
            }

            demo.MarkHosted();

            float sourceConveyance = 1.0f;

            SceneRelease release =
                new SceneRelease(
                    displayName:
                    $"{demo.DisplayName} - KVLT Release",

                    sourceDemoTapeId:
                    demo.DemoTapeId,

                    sourceOwnerEntityId:
                    playerEntity.EntityId,

                    hostedSceneNodeId:
                    kvltNode.NodeId,

                    releasedTurn:
                    demo.RecordedTurn,

                    sourceConveyance:
                    sourceConveyance
                );

            worldState.AddSceneRelease(release);

            message =
                $"Client {clientId}: released demo " +
                $"'{demo.DisplayName}' into " +
                $"{kvltNode.DisplayName}. " +
                $"demoId={demo.DemoTapeId}, " +
                $"demoState={demo.SceneState}, " +
                $"sceneRelease={release.DisplayName}, " +
                $"releaseId={release.ReleaseId}, " +
                $"gen={release.CirculationState.Generation}, " +
                $"reach={release.CirculationState.Reach}.";

            return true;
        }
    }
}