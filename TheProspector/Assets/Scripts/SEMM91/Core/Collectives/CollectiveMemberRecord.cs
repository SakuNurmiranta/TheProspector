using System;
using UnityEngine;

namespace SEMM91.Core.Collectives
{
    [Serializable]
    public class CollectiveMemberRecord
    {
        [SerializeField] private string membershipId;
        [SerializeField] private string collectiveId;
        [SerializeField] private string memberId;
        [SerializeField] private CollectiveMembershipMode membershipMode;
        [SerializeField] private bool isActive;
        [SerializeField] private int joinedTurn;

        public string MembershipId => membershipId;
        public string CollectiveId => collectiveId;
        public string MemberId => memberId;
        public CollectiveMembershipMode MembershipMode => membershipMode;
        public bool IsActive => isActive;
        public int JoinedTurn => joinedTurn;

        public CollectiveMemberRecord(
            string collectiveId,
            string memberId,
            CollectiveMembershipMode membershipMode,
            bool isActive = true,
            int joinedTurn = -1
        )
        {
            membershipId = Guid.NewGuid().ToString();
            this.collectiveId = collectiveId;
            this.memberId = memberId;
            this.membershipMode = membershipMode;
            this.isActive = isActive;
            this.joinedTurn = joinedTurn;
        }

        public void SetMembershipMode(
            CollectiveMembershipMode newMode)
        {
            membershipMode = newMode;
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
        }

        public bool IsActiveMembership()
        {
            return isActive && membershipMode == CollectiveMembershipMode.Active;
        }
    }
}