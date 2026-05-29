using System.Collections.Generic;
using SEMM91.Core.Collectives;
using UnityEngine;

namespace SEMM91.GamePlay.Collectives
{
    public class CollectiveRegistry
    {
        private readonly List<Collective> collectives = new();
        public IReadOnlyList<Collective> Collectives => collectives;

        public bool AddCollective(Collective collective)
        {
            if (collective == null)
            {
                Debug.LogWarning("Collective is null");
                return false;
            }

            if (HasCollective(collective.CollectiveId))
            {
                Debug.LogWarning($"Collective with id {collective.CollectiveId} already exists");
                return false;
            }

            collectives.Add(collective);
            Debug.Log(
                $"[CollectiveRegistry] Registered collective: {collective.DisplayName} ({collective.CollectiveType})");
            return true;
        }

        public bool HasCollective(string collectiveId)
        {
            return FindCollective(collectiveId) != null;
        }

        public Collective FindCollective(string collectiveId)
        {
            if (string.IsNullOrWhiteSpace(collectiveId))
                return null;

            foreach (Collective collective in collectives)
            {
                if (collective != null && collective.CollectiveId == collectiveId)
                    return collective;
            }

            return null;
        }
        
        public Collective FindFirstByType(CollectiveType collectiveType)
        {
            foreach (Collective collective in collectives)
            {
                if (collective != null && collective.CollectiveType == collectiveType)
                    return collective;
            }

            return null;
        }

        public void DebugPrintSummary()
        {
            Debug.Log($"[CollectiveRegistry] Summary: collectives={collectives.Count}");

            foreach (Collective collective in collectives)
            {
                if (collective == null)
                    continue;

                Debug.Log(
                    $"[CollectiveRegistry] {collective.DisplayName} | " +
                    $"id={collective.CollectiveId}, " +
                    $"type={collective.CollectiveType}, " +
                    $"agency={collective.AgencyMode}, " +
                    $"active={collective.IsActive}, " +
                    $"leader={collective.LeaderEntityId}, " +
                    $"entityMembers={collective.EntityMemberships.Count}, " +
                    $"collectiveMembers={collective.CollectiveMembers.Count}"
                );
            }
        }
    }
}
        