using UnityEngine;

namespace SEMM91.GamePlay.Entities
{
    [System.Serializable]
    public class CollectiveMembership
    {
        [SerializeField] private string collectiveEntityId;
        [SerializeField] private bool isActiveMembership;
        
        public string CollectiveEntityId => collectiveEntityId;
        public bool IsActiveMembership => isActiveMembership;

        public CollectiveMembership(string collectiveEntityId, bool isActiveMembership)
        {
            this.collectiveEntityId = collectiveEntityId;
            this.isActiveMembership = isActiveMembership;
        }

        public void SetActiveMembership(bool active)
        {
            isActiveMembership = active;
        }

        public bool Matches(string targetCollectiveEntityId)
        {
            return collectiveEntityId == targetCollectiveEntityId;
        }
    }
}