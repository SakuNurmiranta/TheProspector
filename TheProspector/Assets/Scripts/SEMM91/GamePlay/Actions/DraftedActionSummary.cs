using System;
using SEMM91.Core.Tags;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.GamePlay.Actions
{
    public struct DraftedActionSummary :
        INetworkSerializable,
        IEquatable<DraftedActionSummary>
    {
        public bool IsOccupied;
        public DraftedActionType ActionType;

        public bool HasIdeaSource;
        public TagContainerType IdeaSourceContainerType;

        public bool HasPhysicalEventLocation;

        public FixedString32Bytes PhysicalEventNodeId;

        public static DraftedActionSummary Empty =>
            default;

        public static DraftedActionSummary FromPayload(
            DraftedActionPayload payload)
        {
            if (payload == null)
                return Empty;

            bool hasIdeaSource =
                payload.IdeaSourceContainerType
                    .HasValue;

            bool hasPhysicalEventLocation =
                payload.HasPhysicalEventLocation;

            return new DraftedActionSummary
            {
                IsOccupied = true,
                ActionType = payload.ActionType,

                HasIdeaSource =
                    hasIdeaSource,

                IdeaSourceContainerType =
                    hasIdeaSource
                        ? payload
                            .IdeaSourceContainerType
                            .Value
                        : default,

                HasPhysicalEventLocation =
                    hasPhysicalEventLocation,

                PhysicalEventNodeId =
                    hasPhysicalEventLocation
                        ? new FixedString32Bytes(
                            payload.PhysicalEventNodeId
                        )
                        : default
            };
        }
        
        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(
                ref HasPhysicalEventLocation
            );

            serializer.SerializeValue(
                ref PhysicalEventNodeId
            );
            
            serializer.SerializeValue(
                ref IsOccupied
            );

            serializer.SerializeValue(
                ref ActionType
            );

            serializer.SerializeValue(
                ref HasIdeaSource
            );

            serializer.SerializeValue(
                ref IdeaSourceContainerType
            );
        }

        public bool Equals(
            DraftedActionSummary other)
        {
            return IsOccupied == other.IsOccupied &&
                   ActionType == other.ActionType &&
                   HasIdeaSource == other.HasIdeaSource &&
                   HasPhysicalEventLocation == other.HasPhysicalEventLocation &&
                   PhysicalEventNodeId.Equals(other.PhysicalEventNodeId) &&
                   IdeaSourceContainerType ==
                   other.IdeaSourceContainerType;
        }

        public override bool Equals(object obj)
        {
            return obj is DraftedActionSummary other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash =
                    hash * 31 +
                    IsOccupied.GetHashCode();

                hash =
                    hash * 31 +
                    ActionType.GetHashCode();

                hash =
                    hash * 31 +
                    HasIdeaSource.GetHashCode();

                hash =
                    hash * 31 +
                    IdeaSourceContainerType.GetHashCode();
                
                hash =
                    hash * 31 +
                    HasPhysicalEventLocation
                        .GetHashCode();

                hash =
                    hash * 31 +
                    PhysicalEventNodeId
                        .GetHashCode();

                return hash;
            }
        }
    }
}