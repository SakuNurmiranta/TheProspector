using System;
using SEMM91.Core.Collectives;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Entities;
using UnityEngine;

namespace SEMM91.GamePlay.Collectives
{
    public class StartingCollectiveBootstrapper
    {
        public const string SocietyId = "COLLECTIVE_SOCIETY";
        public const string KvltId = "COLLECTIVE_KVLT";
        public const string PlayerBandId = "COLLECTIVE_NOT_SAVED_THE_BAND";

        public const string TheHolePremisesId = "ENTITY_THE_HOLE_PREMISES";
        public const string TheHoleId = "ENTITY_THE_HOLE";

        public const string StartingNodeId = "NODE_SNORDENMARK_CAPITAL";

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

            GameEntity theHolePremises = CreateTheHolePremises();
            GameEntity theHole = CreateTheHoleProxy();

            registry.AddCollective(society);
            registry.AddCollective(kvlt);
            registry.AddCollective(playerBand);

            ConnectSceneHierarchy(society, kvlt, playerBand, theHole);
            ConnectLeader(playerLeader, playerBand, kvlt);

            registry.DebugPrintSummary();

            log?.Invoke(
                "[COLLECTIVE SEED] Hole scaffold | " +
                $"premises={theHolePremises.DisplayName} ({theHolePremises.EntityId}), " +
                $"proxy={theHole.DisplayName} ({theHole.EntityId})"
            );

            return new StartingCollectiveBootstrapResult(
                registry,
                playerBand,
                kvlt,
                society,
                theHolePremises,
                theHole
            );
        }

        private Collective CreateSociety()
        {
            Collective society = new Collective(
                "The Society",
                CollectiveType.Society,
                CollectiveAgencyMode.Passive,
                SocietyId
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
                CollectiveAgencyMode.Active,
                KvltId
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
            Collective band = new Collective(
                "Not_Saved.SNO",
                CollectiveType.Band,
                CollectiveAgencyMode.Active,
                PlayerBandId
            );

            band.SetActive(true);
            band.SetLeaderEntity(playerLeader.EntityId);

            band.SetAlignment(TagAxis.Symbolic, -2f);
            band.SetAlignment(TagAxis.Expressive, -1f);
            band.SetAlignment(TagAxis.Existential, -1f);

            log?.Invoke("[COLLECTIVE SEED] Created player band: Not_Saved.SNO");
            return band;
        }

        private GameEntity CreateTheHolePremises()
        {
            GameObject premisesObject = new GameObject("The_Hole_Premises");
            GameEntity premises = premisesObject.AddComponent<GameEntity>();

            premises.InitializeIdentity(
                "The Hole Premises",
                GameEntityType.Structure
            );

            premises.SetNode(StartingNodeId);

            log?.Invoke("[ENTITY SEED] Created neutral-capable structure shell: The Hole Premises");
            return premises;
        }

        private GameEntity CreateTheHoleProxy()
        {
            GameObject holeObject = new GameObject("The_Hole");
            GameEntity theHole = holeObject.AddComponent<GameEntity>();

            theHole.InitializeIdentity(
                "The Hole",
                GameEntityType.CollectiveProxy
            );

            theHole.SetNode(StartingNodeId);

            theHole.AddCollectiveMembership(KvltId, false);

            theHole.AddTagContainer(TagContainerType.Resonance);

            theHole.TrySetTag(
                TagContainerType.Resonance,
                new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak)
            );

            log?.Invoke("[ENTITY SEED] Created KVLT-aligned collective proxy: The Hole");
            return theHole;
        }

        private void ConnectSceneHierarchy(
            Collective society,
            Collective kvlt,
            Collective playerBand,
            GameEntity theHole
        )
        {
            kvlt.AddCollectiveMember(
                playerBand.CollectiveId,
                CollectiveMembershipMode.Passive
            );

            kvlt.AddEntityMember(
                theHole.EntityId,
                CollectiveMembershipMode.Passive
            );

            society.AddCollectiveMember(
                kvlt.CollectiveId,
                CollectiveMembershipMode.Passive
            );

            log?.Invoke("[COLLECTIVE SEED] Connected band → KVLT → Society, and The Hole proxy → KVLT");
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