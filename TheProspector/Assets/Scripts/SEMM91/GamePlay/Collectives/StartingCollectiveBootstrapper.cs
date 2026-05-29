using System;
using SEMM91.Core.Collectives;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Entities;
using UnityEngine;

namespace SEMM91.GamePlay.Collectives
{
    public class StartingCollectiveBootstrapper
    {
        private readonly Action<string> log;

        public StartingCollectiveBootstrapper(Action<string> log = null)
        {
            this.log = log ?? Debug.Log;
        }

        public StartingCollectiveBootstrapResult BootstrapForPlayerLeader(GameEntity playerLeader)
        {
            if (playerLeader == null)
            {
                Debug.LogWarning("[StartingCollectiveBootstrapper] Cannot bootstrap collectives without player leader");
                return null;
            }

            CollectiveRegistry registry = new CollectiveRegistry();

            Collective society = CreateSociety();
            Collective kvlt = CreateKvlt();
            Collective playerBand = CreatePlayerBand(playerLeader);

            registry.AddCollective(society);
            registry.AddCollective(kvlt);
            registry.AddCollective(playerBand);

            ConnectSceneHierarchy(society, kvlt, playerBand);
            ConnectLeader(playerLeader, playerBand, kvlt);

            registry.DebugPrintSummary();

            return new StartingCollectiveBootstrapResult(
                registry,
                playerBand,
                kvlt,
                society
            );
        }

        private Collective CreateSociety()
        {
            Collective society = new Collective(
                "The Society",
                CollectiveType.Society,
                CollectiveAgencyMode.Passive
            );

            society.SetActive(false);

            society.SetAlignment(TagAxis.Symbolic, 3f);
            society.SetAlignment(TagAxis.Interpretive, 2f);
            society.SetAlignment(TagAxis.Physical, 1f);

            log?.Invoke("[COLLECTIVE SEED] Created passive Society scaffold");
            return society;
        }

        private Collective CreateKvlt()
        {
            Collective kvlt = new Collective(
                "KVLT",
                CollectiveType.Scene,
                CollectiveAgencyMode.Active
            );

            kvlt.SetActive(true);

            kvlt.SetAlignment(TagAxis.Symbolic, -3f);
            kvlt.SetAlignment(TagAxis.Expressive, -2f);
            kvlt.SetAlignment(TagAxis.Existential, -2f);

            log?.Invoke("[COLLECTIVE SEED] Created active KVLT scene scaffold");
            return kvlt;
        }

        private Collective CreatePlayerBand(GameEntity playerLeader)
        {
            string bandName = "Not_Saved.SNO";

            Collective band = new Collective(
                bandName,
                CollectiveType.Band,
                CollectiveAgencyMode.Active
            );

            band.SetActive(true);
            band.SetLeaderEntity(playerLeader.EntityId);

            band.SetAlignment(TagAxis.Symbolic, -2f);
            band.SetAlignment(TagAxis.Expressive, -1f);
            band.SetAlignment(TagAxis.Existential, -1f);

            log?.Invoke($"[COLLECTIVE SEED] Created player band: {bandName}");
            return band;
        }

        private void ConnectSceneHierarchy(
            Collective society,
            Collective kvlt,
            Collective playerBand
        )
        {
            kvlt.AddCollectiveMember(
                playerBand.CollectiveId,
                CollectiveMembershipMode.Passive
            );

            society.AddCollectiveMember(
                kvlt.CollectiveId,
                CollectiveMembershipMode.Passive
            );

            log?.Invoke("[COLLECTIVE SEED] Connected band → KVLT → Society");
        }

        private void ConnectLeader(
            GameEntity playerLeader,
            Collective playerBand,
            Collective kvlt
        )
        {
            playerBand.AddEntityMember(
                playerLeader.EntityId,
                CollectiveMembershipMode.Active
            );

            kvlt.AddEntityMember(
                playerLeader.EntityId,
                CollectiveMembershipMode.Passive
            );

            playerLeader.AddCollectiveMembership(playerBand.CollectiveId, true);
            playerLeader.AddCollectiveMembership(kvlt.CollectiveId, false);

            log?.Invoke(
                $"[COLLECTIVE SEED] Connected leader {playerLeader.DisplayName} " +
                $"to band={playerBand.DisplayName} and scene={kvlt.DisplayName}"
            );
        }
    }
}