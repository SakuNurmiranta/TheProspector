using System.Collections.Generic;
using SEMM91.Core.Collectives;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Entities;
using UnityEngine;

namespace SEMM91.GamePlay.World
{
    public class SeededWorldState
    {
        private readonly List<GameEntity> entities = new();
        private readonly List<EntityHostingRecord> hostingRecords = new();

        public CollectiveRegistry CollectiveRegistry { get; }

        public IReadOnlyList<GameEntity> Entities => entities;
        public IReadOnlyList<EntityHostingRecord> HostingRecords => hostingRecords;

        public SeededWorldState(CollectiveRegistry collectiveRegistry)
        {
            CollectiveRegistry = collectiveRegistry;
        }

        public bool AddEntity(GameEntity entity)
        {
            if (entity == null)
            {
                Debug.LogWarning("[SeededWorldState] Cannot add null entity");
                return false;
            }

            if (FindEntity(entity.EntityId) != null)
            {
                Debug.LogWarning($"[SeededWorldState] Entity already registered: {entity.EntityId}");
                return false;
            }

            entities.Add(entity);
            Debug.Log($"[SeededWorldState] Registered entity: {entity.DisplayName} ({entity.EntityType})");
            return true;
        }

        public bool AddHostingRecord(EntityHostingRecord hostingRecord)
        {
            if (hostingRecord == null)
            {
                Debug.LogWarning("[SeededWorldState] Cannot add null hosting record");
                return false;
            }

            if (FindHostingRecord(hostingRecord.HostedEntityId, hostingRecord.HostEntityId) != null)
            {
                Debug.LogWarning(
                    "[SeededWorldState] Hosting record already registered | " +
                    $"hosted={hostingRecord.HostedEntityId}, host={hostingRecord.HostEntityId}"
                );

                return false;
            }

            hostingRecords.Add(hostingRecord);

            Debug.Log(
                "[SeededWorldState] Registered hosting record | " +
                $"hosted={hostingRecord.HostedEntityId}, host={hostingRecord.HostEntityId}"
            );

            return true;
        }

        public GameEntity FindEntity(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return null;

            foreach (GameEntity entity in entities)
            {
                if (entity != null && entity.EntityId == entityId)
                    return entity;
            }

            return null;
        }

        public GameEntity FindFirstEntityByType(GameEntityType entityType)
        {
            foreach (GameEntity entity in entities)
            {
                if (entity != null && entity.EntityType == entityType)
                    return entity;
            }

            return null;
        }

        public GameEntity FindEntityByDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            foreach (GameEntity entity in entities)
            {
                if (entity != null && entity.DisplayName == displayName)
                    return entity;
            }

            return null;
        }

        public Collective FindCollective(string collectiveId)
        {
            return CollectiveRegistry?.FindCollective(collectiveId);
        }

        public Collective FindKvlt()
        {
            return FindCollective(StartingCollectiveBootstrapper.KvltId);
        }

        public Collective FindSociety()
        {
            return FindCollective(StartingCollectiveBootstrapper.SocietyId);
        }

        public GameEntity FindTheHole()
        {
            return FindEntityByDisplayName("The Hole");
        }

        public GameEntity FindTheHolePremises()
        {
            return FindEntityByDisplayName("The Hole Premises");
        }

        public EntityHostingRecord FindTheHoleHosting()
        {
            GameEntity theHole = FindTheHole();

            if (theHole == null)
                return null;

            return FindActiveHostFor(theHole.EntityId);
        }

        public List<Collective> FindPlayerBands()
        {
            List<Collective> playerBands = new();

            if (CollectiveRegistry == null)
                return playerBands;

            foreach (Collective collective in CollectiveRegistry.Collectives)
            {
                if (collective != null && collective.CollectiveType == CollectiveType.Band)
                    playerBands.Add(collective);
            }

            return playerBands;
        }

        public EntityHostingRecord FindHostingRecord(string hostedEntityId, string hostEntityId)
        {
            if (string.IsNullOrWhiteSpace(hostedEntityId) || string.IsNullOrWhiteSpace(hostEntityId))
                return null;

            foreach (EntityHostingRecord record in hostingRecords)
            {
                if (record != null && record.Matches(hostedEntityId, hostEntityId))
                    return record;
            }

            return null;
        }

        public EntityHostingRecord FindActiveHostFor(string hostedEntityId)
        {
            if (string.IsNullOrWhiteSpace(hostedEntityId))
                return null;

            foreach (EntityHostingRecord record in hostingRecords)
            {
                if (record != null &&
                    record.HostedEntityId == hostedEntityId &&
                    record.IsActive)
                {
                    return record;
                }
            }

            return null;
        }

        public void DebugPrintLookupSummary()
        {
            Collective kvlt = FindKvlt();
            Collective society = FindSociety();
            GameEntity theHole = FindTheHole();
            GameEntity theHolePremises = FindTheHolePremises();
            EntityHostingRecord theHoleHosting = FindTheHoleHosting();
            List<Collective> playerBands = FindPlayerBands();

            Debug.Log(
                "[SeededWorldState] Lookup summary | " +
                $"kvltFound={kvlt != null}, " +
                $"societyFound={society != null}, " +
                $"theHoleFound={theHole != null}, " +
                $"premisesFound={theHolePremises != null}, " +
                $"theHoleHostingFound={theHoleHosting != null}, " +
                $"playerBands={playerBands.Count}"
            );
        }

        public void DebugPrintSummary()
        {
            Debug.Log(
                "[SeededWorldState] Summary | " +
                $"entities={entities.Count}, " +
                $"hostingRecords={hostingRecords.Count}, " +
                $"collectives={CollectiveRegistry?.Collectives.Count ?? 0}"
            );

            foreach (GameEntity entity in entities)
            {
                if (entity == null)
                    continue;

                Debug.Log(
                    "[SeededWorldState] Entity | " +
                    $"{entity.DisplayName}, " +
                    $"id={entity.EntityId}, " +
                    $"type={entity.EntityType}, " +
                    $"collectiveMemberships={entity.CollectiveMemberships.Count}"
                );
            }
        }
    }
}