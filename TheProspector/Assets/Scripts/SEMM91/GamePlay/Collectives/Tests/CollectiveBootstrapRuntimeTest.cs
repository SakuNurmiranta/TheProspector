using SEMM91.GamePlay.Agency;
using SEMM91.GamePlay.Entities;
using UnityEngine;

namespace SEMM91.GamePlay.Collectives
{
    public class CollectiveBootstrapRuntimeTest : MonoBehaviour
    {
        [ContextMenu("Debug/Run Collective Bootstrap Test")]
        private void DebugRunCollectiveBootstrapTest()
        {
            PlayerEntityBootstrapper playerEntityBootstrapper = new PlayerEntityBootstrapper(Debug.Log);
            GameEntity playerLeader = playerEntityBootstrapper.CreateStartingPlayerEntity(0);

            StartingCollectiveBootstrapper collectiveBootstrapper = new StartingCollectiveBootstrapper(Debug.Log);
            StartingCollectiveBootstrapResult result = collectiveBootstrapper.BootstrapForPlayerLeader(playerLeader);

            if (result == null)
            {
                Debug.LogError("[CollectiveBootstrapRuntimeTest] Bootstrap failed");
                return;
            }

            Debug.Log(
                "[CollectiveBootstrapRuntimeTest] Bootstrap complete | " +
                $"band={result.PlayerBand.DisplayName}, " +
                $"kvlt={result.Kvlt.DisplayName}, " +
                $"society={result.Society.DisplayName}, " +
                $"registryCount={result.Registry.Collectives.Count}, " +
                $"leaderMemberships={playerLeader.CollectiveMemberships.Count}"
            );
        }
    }
}