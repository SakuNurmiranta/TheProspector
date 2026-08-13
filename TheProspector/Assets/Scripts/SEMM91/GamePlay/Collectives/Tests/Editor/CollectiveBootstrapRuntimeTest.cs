using SEMM91.Core.Collectives;
using SEMM91.Core.Entities;
using SEMM91.GamePlay.Agency;
using SEMM91.GamePlay.World;
using UnityEngine;

namespace SEMM91.GamePlay.Collectives
{
    public class CollectiveBootstrapRuntimeTest : MonoBehaviour
    {
        [ContextMenu("Debug/Run Collective Bootstrap Test")]
        private void DebugRunCollectiveBootstrapTest()
        {
            StartingCollectiveBootstrapper collectiveBootstrapper =
                new StartingCollectiveBootstrapper(Debug.Log);

            StartingCollectiveBootstrapResult result =
                collectiveBootstrapper.BootstrapSharedWorld();

            if (result == null)
            {
                Debug.LogError("[CollectiveBootstrapRuntimeTest] Shared world bootstrap failed");
                return;
            }

            SeededWorldStateHolder holder = GetOrCreateWorldStateHolder();
            holder.SetWorldState(result.WorldState);

            PlayerEntityBootstrapper playerEntityBootstrapper =
                new PlayerEntityBootstrapper(Debug.Log);

            GameEntity player0 =
                playerEntityBootstrapper
                    .CreatePlayerEntity(
                        0,
                        "Player 0"
                    );
            Collective band0 = collectiveBootstrapper.AddPlayerLeaderToWorld(
                result.WorldState,
                player0,
                0
            );

            GameEntity player1 =
                playerEntityBootstrapper
                    .CreatePlayerEntity(
                        0,
                        "Player 1"
                    );
            
            Collective band1 = collectiveBootstrapper.AddPlayerLeaderToWorld(
                result.WorldState,
                player1,
                1
            );

            if (band0 == null || band1 == null)
            {
                Debug.LogError("[CollectiveBootstrapRuntimeTest] Player insertion failed");
                return;
            }

            result.WorldState.DebugPrintLookupSummary();
            holder.DebugPrintSummary();

            Debug.Log(
                "[CollectiveBootstrapRuntimeTest] Shared bootstrap complete | " +
                $"holderHasWorld={holder.HasWorldState}, " +
                $"kvlt={result.Kvlt.DisplayName}, " +
                $"society={result.Society.DisplayName}, " +
                $"premises={result.TheHolePremises.DisplayName}, " +
                $"theHole={result.TheHole.DisplayName}, " +
                $"premisesId={result.TheHolePremises.EntityId}, " +
                $"theHoleId={result.TheHole.EntityId}, " +
                $"premisesType={result.TheHolePremises.EntityType}, " +
                $"holeType={result.TheHole.EntityType}, " +
                $"registryCount={result.Registry.Collectives.Count}, " +
                $"worldEntities={result.WorldState.Entities.Count}, " +
                $"worldHostingRecords={result.WorldState.HostingRecords.Count}, " +
                $"player0Memberships={player0.CollectiveMemberships.Count}, " +
                $"player1Memberships={player1.CollectiveMemberships.Count}, " +
                $"premisesMemberships={result.TheHolePremises.CollectiveMemberships.Count}, " +
                $"holeMemberships={result.TheHole.CollectiveMemberships.Count}, " +
                $"hostingActive={result.TheHoleHosting.IsActive}, " +
                $"hostedId={result.TheHoleHosting.HostedEntityId}, " +
                $"hostId={result.TheHoleHosting.HostEntityId}, " +
                $"band0={band0.DisplayName}, " +
                $"band1={band1.DisplayName}"
            );
        }

        private SeededWorldStateHolder GetOrCreateWorldStateHolder()
        {
            if (SeededWorldStateHolder.Instance != null)
                return SeededWorldStateHolder.Instance;

            GameObject holderObject = new GameObject("Seeded_World_State_Holder");
            return holderObject.AddComponent<SeededWorldStateHolder>();
        }
    }
}