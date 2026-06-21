using System;
using UnityEngine;

namespace SEMM91.Core.Entities
{
    [Serializable]
    public class EntityHostingRecord
    {
        [SerializeField] private string hostedEntityId;
        [SerializeField] private string hostEntityId;
        [SerializeField] private bool isActive;
        
        public string HostedEntityId => hostedEntityId;
        public string HostEntityId => hostEntityId;
        public bool IsActive => isActive;

        public EntityHostingRecord(
            string hostedEntityId,
            string hostEntityId,
            bool isActive = true
        )
        {
            this.hostedEntityId = hostedEntityId;
            this.hostEntityId = hostEntityId;
            this.isActive = isActive;
        }

        public bool Matches(string targetHostedEntityId, string targetHostEntityId)
        {
            return hostedEntityId == targetHostedEntityId &&
                   hostEntityId == targetHostEntityId;
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
        }
    }
}