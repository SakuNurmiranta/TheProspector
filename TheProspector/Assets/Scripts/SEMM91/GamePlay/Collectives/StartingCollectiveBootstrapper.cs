using System;
using SEMM91.Core.Collectives;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Entities;
using SEMM91.GamePlay.SceneSpace;
using SEMM91.GamePlay.World;
using UnityEngine;

namespace SEMM91.GamePlay.Collectives
{
    public class StartingCollectiveBootstrapper
    {
        public const string SocietyId = "COLLECTIVE_SOCIETY";
        public const string KvltId = "COLLECTIVE_KVLT";
        public const string NodeWilderness = "SCENE_NODE_WILDERNESS";
        public const string NodeSociety = "SCENE_NODE_SOCIETY";
        public const string NodeBlackMetalBreach = "SCENE_NODE_BLACK_METAL_BREACH";
        public const string NodeKvltScene = "SCENE_NODE_KVLT";
        public const string NodeDeathMetalScene = "SCENE_NODE_DEATH_METAL";
        public const string NodeBadOrInsideSociety = "SCENE_NODE_BAD_OR_INSIDE_SOCIETY";

        public const string PlayerBandIdPrefix = "COLLECTIVE_PLAYER_BAND_";

        public const string TheHolePremisesId = "ENTITY_THE_HOLE_PREMISES";
        public const string TheHoleId = "ENTITY_THE_HOLE";

        public const string StartingNodeId = "NODE_SNORDENMARK_CAPITAL";

        private readonly Action<string> log;

        public StartingCollectiveBootstrapper(Action<string> log = null)
        {
            this.log = log ?? Debug.Log;
        }

        public StartingCollectiveBootstrapResult BootstrapSharedWorld()
        {
            CollectiveRegistry registry = new CollectiveRegistry();

            Collective society = CreateSociety();
            Collective kvlt = CreateKvlt();

            GameEntity theHolePremises = CreateTheHolePremises();
            GameEntity theHole = CreateTheHoleProxy();

            EntityHostingRecord theHoleHosting = CreateTheHoleHosting(
                theHole,
                theHolePremises
            );

            registry.AddCollective(society);
            registry.AddCollective(kvlt);

            ConnectSharedSceneHierarchy(society, kvlt, theHole);

            SeededWorldState worldState = new SeededWorldState(registry);

            worldState.AddEntity(theHolePremises);
            worldState.AddEntity(theHole);
            worldState.AddHostingRecord(theHoleHosting);

            SeedSceneSpaceGraph(worldState, society, kvlt, theHole);

            registry.DebugPrintSummary();
            worldState.DebugPrintSummary();

            log?.Invoke(
                "[COLLECTIVE SEED] Shared world bootstrapped | " +
                $"society={society.DisplayName}, " +
                $"kvlt={kvlt.DisplayName}, " +
                $"premises={theHolePremises.DisplayName}, " +
                $"proxy={theHole.DisplayName}"
            );

            return new StartingCollectiveBootstrapResult(
                worldState,
                registry,
                kvlt,
                society,
                theHolePremises,
                theHole,
                theHoleHosting
            );
        }

        public Collective AddPlayerLeaderToWorld(
            SeededWorldState worldState,
            GameEntity playerLeader,
            ulong playerId
        )
        {
            if (worldState == null)
            {
                Debug.LogWarning("[StartingCollectiveBootstrapper] Cannot add player to null world state");
                return null;
            }

            if (playerLeader == null)
            {
                Debug.LogWarning("[StartingCollectiveBootstrapper] Cannot add null player leader to world");
                return null;
            }

            CollectiveRegistry registry = worldState.CollectiveRegistry;

            if (registry == null)
            {
                Debug.LogWarning("[StartingCollectiveBootstrapper] World state has no CollectiveRegistry");
                return null;
            }

            Collective kvlt = registry.FindCollective(KvltId);

            if (kvlt == null)
            {
                Debug.LogWarning("[StartingCollectiveBootstrapper] Cannot add player because KVLT is missing");
                return null;
            }

            Collective playerBand = CreatePlayerBand(playerLeader, playerId);

            registry.AddCollective(playerBand);
            worldState.AddEntity(playerLeader);

            ConnectPlayerToWorld(playerLeader, playerBand, kvlt);

            registry.DebugPrintSummary();
            worldState.DebugPrintSummary();

            log?.Invoke(
                "[COLLECTIVE SEED] Player inserted into shared world | " +
                $"player={playerLeader.DisplayName}, " +
                $"band={playerBand.DisplayName}, " +
                $"playerId={playerId}"
            );

            return playerBand;
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

        private Collective CreatePlayerBand(GameEntity playerLeader, ulong playerId)
        {
            string bandId = $"{PlayerBandIdPrefix}{playerId}";
            string bandName = playerId == 0
                ? "Not_Saved.SNO"
                : $"Player {playerId} Band";

            Collective band = new Collective(
                bandName,
                CollectiveType.Band,
                CollectiveAgencyMode.Active,
                bandId
            );

            band.SetActive(true);
            band.SetLeaderEntity(playerLeader.EntityId);

            band.SetAlignment(TagAxis.Symbolic, -2f);
            band.SetAlignment(TagAxis.Expressive, -1f);
            band.SetAlignment(TagAxis.Existential, -1f);

            log?.Invoke($"[COLLECTIVE SEED] Created player band: {bandName}");
            return band;
        }

        private GameEntity CreateTheHolePremises()
        {
            GameObject premisesObject = new GameObject("The_Hole_Premises");
            GameEntity premises = premisesObject.AddComponent<GameEntity>();

            premises.InitializeIdentity(
                TheHolePremisesId,
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
                TheHoleId,
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

        private EntityHostingRecord CreateTheHoleHosting(
            GameEntity theHole,
            GameEntity theHolePremises
        )
        {
            EntityHostingRecord hostingRecord = new EntityHostingRecord(
                theHole.EntityId,
                theHolePremises.EntityId,
                true
            );

            log?.Invoke(
                "[ENTITY SEED] Created hosting relation | " +
                $"hosted={theHole.DisplayName} ({theHole.EntityId}), " +
                $"host={theHolePremises.DisplayName} ({theHolePremises.EntityId})"
            );

            return hostingRecord;
        }

        private void ConnectSharedSceneHierarchy(
            Collective society,
            Collective kvlt,
            GameEntity theHole
        )
        {
            kvlt.AddEntityMember(
                theHole.EntityId,
                CollectiveMembershipMode.Passive
            );

            society.AddCollectiveMember(
                kvlt.CollectiveId,
                CollectiveMembershipMode.Passive
            );

            log?.Invoke("[COLLECTIVE SEED] Connected The Hole proxy → KVLT → Society");
        }

        private void ConnectPlayerToWorld(
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

            kvlt.AddCollectiveMember(
                playerBand.CollectiveId,
                CollectiveMembershipMode.Passive
            );

            playerLeader.AddCollectiveMembership(playerBand.CollectiveId, true);
            playerLeader.AddCollectiveMembership(kvlt.CollectiveId, false);

            log?.Invoke(
                "[COLLECTIVE SEED] Connected player leader to shared world | " +
                $"leader={playerLeader.DisplayName}, " +
                $"band={playerBand.DisplayName}, " +
                $"scene={kvlt.DisplayName}"
            );
        }
        
        private void SeedSceneSpaceGraph(
            SeededWorldState worldState,
            Collective society,
            Collective kvlt,
            GameEntity theHole)
        {
            if (worldState == null)
            {
                log?.Invoke("[SCENE SPACE SEED] Blocked: missing world state.");
                return;
            }

            SceneSpaceGraph graph = worldState.SceneSpaceGraph;

            graph.AddNode(new SceneSpaceNode(
                NodeWilderness,
                "Wilderness Board",
                SceneSpaceNodeType.Wilderness
            ));

            graph.AddNode(new SceneSpaceNode(
                NodeSociety,
                "The Society",
                SceneSpaceNodeType.Society,
                SceneConstructMode.And,
                NodeWilderness,
                society?.CollectiveId
            ));

            graph.AddNode(new SceneSpaceNode(
                NodeBlackMetalBreach,
                "Black Metal Breach",
                SceneSpaceNodeType.Breach,
                SceneConstructMode.Or,
                NodeSociety
            ));

            graph.AddNode(new SceneSpaceNode(
                NodeKvltScene,
                "KVLT Scene",
                SceneSpaceNodeType.Scene,
                SceneConstructMode.Or,
                NodeBlackMetalBreach,
                kvlt?.CollectiveId,
                theHole?.EntityId
            ));

            graph.AddNode(new SceneSpaceNode(
                NodeDeathMetalScene,
                "Death Metal Scene",
                SceneSpaceNodeType.Scene,
                SceneConstructMode.And,
                NodeSociety
            ));

            graph.AddNode(new SceneSpaceNode(
                NodeBadOrInsideSociety,
                "Badly Placed OR Construct",
                SceneSpaceNodeType.Scene,
                SceneConstructMode.Or,
                NodeSociety
            ));

            graph.ApplyConstructHostingRules();
            
            foreach (SceneSpaceNode node in graph.Nodes)
            {
                log?.Invoke(
                    "[SCENE SPACE NODE] " +
                    $"{node.DisplayName} | " +
                    $"id={node.NodeId}, " +
                    $"type={node.NodeType}, " +
                    $"construct={node.ConstructMode}, " +
                    $"status={node.Status}, " +
                    $"parent={node.ParentNodeId}, " +
                    $"collective={node.BoundCollectiveId}, " +
                    $"entity={node.BoundEntityId}"
                );
            }

            log?.Invoke(
                "[SCENE SPACE SEED] Seeded graph | " +
                $"nodes={graph.Nodes.Count}"
            );
        }
    }
}