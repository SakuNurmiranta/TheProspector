using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;
using UnityEngine;

namespace SEMM91.Core.Collectives
{
    [Serializable]
    public class Collective
    {
        [Header("Identity")] [SerializeField] private string collectiveId;
        [SerializeField] private string displayName;
        [SerializeField] private CollectiveType collectiveType;

        [Header("Agency")] [SerializeField] private CollectiveAgencyMode agencyMode;
        [SerializeField] private bool isActive;
        [SerializeField] private string leaderEntityId;

        [Header("Memberships")] [SerializeField]
        private List<CollectiveMemberRecord> entityMemberships = new(); //for game entity objects

        [SerializeField] private List<CollectiveMemberRecord> collectiveMembers = new(); //for other collectives

        [Header("Tag Axis Alignment")] [SerializeField]
        private List<TagAxisAlignment> alignments = new();

        public string CollectiveId => collectiveId;
        public string DisplayName => displayName;
        public CollectiveType CollectiveType => collectiveType;
        public CollectiveAgencyMode AgencyMode => agencyMode;
        public bool IsActive => isActive;
        public string LeaderEntityId => leaderEntityId;

        public IReadOnlyList<CollectiveMemberRecord> EntityMemberships => entityMemberships;
        public IReadOnlyList<CollectiveMemberRecord> CollectiveMembers => collectiveMembers;
        public IReadOnlyList<TagAxisAlignment> Alignments => alignments;

        public Collective(
            string displayName,
            CollectiveType collectiveType,
            CollectiveAgencyMode agencyMode
        )
        {
            collectiveId = Guid.NewGuid().ToString();
            this.displayName = displayName;
            this.collectiveType = collectiveType;
            this.agencyMode = agencyMode;

            isActive = agencyMode == CollectiveAgencyMode.Active;
            InitializeDefaultAlignments();
        }

        public void SetLeaderEntity(string entityId)
        {
            leaderEntityId = entityId;
        }

        public void SetActive(bool active)
        {
            isActive = active;
        }
        
        public CollectiveMemberRecord AddEntityMember(
            string entityId,
            CollectiveMembershipMode membershipMode,
            int joinedTurn = -1)
        {
            if (HasEntityMember(entityId)) return null;

            CollectiveMemberRecord membership = new CollectiveMemberRecord(
                collectiveId,
                entityId,
                membershipMode,
                isActive: true,
                joinedTurn: joinedTurn
            );

            entityMemberships.Add(membership);
            return membership;
        }

        public CollectiveMemberRecord AddCollectiveMember(
            string memberCollectiveId,
            CollectiveMembershipMode membershipMode,
            int joinedTurn = -1
        )
        {
            if (HasCollectiveMember(memberCollectiveId))
                return null;

            CollectiveMemberRecord membership = new CollectiveMemberRecord(
                collectiveId,
                memberCollectiveId,
                membershipMode,
                true,
                joinedTurn
            );

            collectiveMembers.Add(membership);
            return membership;
        }

        public bool HasEntityMember(string entityId)
        {
            foreach (CollectiveMemberRecord membership in entityMemberships)
            {
                if (membership.MemberId == entityId && membership.IsActive)
                    return true;
            }

            return false;
        }

        public bool HasCollectiveMember(string memberCollectiveId)
        {
            foreach (CollectiveMemberRecord membership in collectiveMembers)
            {
                if (membership.MemberId == memberCollectiveId && membership.IsActive)
                    return true;
            }

            return false;
        }

    public bool RemoveEntityMember(string entityId)
    {
        CollectiveMemberRecord membership = FindEntityMembership(entityId);

        if (membership == null)
            return false;

        membership.SetActive(false);
        return true;
    }

    public bool RemoveCollectiveMember(string memberCollectiveId)
    {
        CollectiveMemberRecord membership = FindCollectiveMembership(memberCollectiveId);

        if (membership == null)
            return false;

        membership.SetActive(false);
        return true;
    }

    public void SetAlignment(TagAxis axis, float value)
    {
        value = Mathf.Clamp(value, -5f, 5f);

        foreach (TagAxisAlignment alignment in alignments)
        {
            if (alignment.Axis == axis)
            {
                alignment.SetValue(value);
                return;
            }
        }

        alignments.Add(new TagAxisAlignment(axis, value));
    }

    public float GetAlignment(TagAxis axis)
    {
        foreach (TagAxisAlignment alignment in alignments)
        {
            if (alignment.Axis == axis)
                return alignment.Value;
        }

        return 0f;
    }

    private CollectiveMemberRecord FindEntityMembership(string entityId)
    {
        foreach (CollectiveMemberRecord membership in entityMemberships)
        {
            if (membership.MemberId == entityId)
                return membership;
        }

        return null;
    }

    private CollectiveMemberRecord FindCollectiveMembership(string memberCollectiveId)
    {
        foreach (CollectiveMemberRecord membership in collectiveMembers)
        {
            if (membership.MemberId == memberCollectiveId)
                return membership;
        }

        return null;
    }

    private void InitializeDefaultAlignments()
    {
        alignments.Clear();

        foreach (TagAxis axis in Enum.GetValues(typeof(TagAxis)))
        {
            alignments.Add(new TagAxisAlignment(axis, 0f));
        }
    }


[Serializable]
        public class TagAxisAlignment
        {
            [SerializeField] private TagAxis axis;
            [SerializeField] private float value;

            public TagAxis Axis => axis;
            public float Value => value;

            public TagAxisAlignment(TagAxis axis, float value)
            {
                this.axis = axis;
                this.value = Mathf.Clamp(value, -5f, 5f);
            }

            public void SetValue(float newValue)
            {
                value = Mathf.Clamp(newValue, -5f, 5f);
            }
        }
    }
}
    
    
