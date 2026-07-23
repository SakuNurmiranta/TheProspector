using System;
using SEMM91.GamePlay.Actions;
using Unity.Collections;
using Unity.Netcode;

namespace SEMM91.GamePlay.Events
{
    public enum ObservedPhysicalEventPlanState : byte
    {
        Drafted = 1,
        Committed = 2
    }

    /// <summary>
    /// One server-authored physical-event projection visible to
    /// a particular observing player.
    ///
    /// This is presentation/query data. It does not execute the event
    /// or mutate the physical world.
    /// </summary>
    public struct ObservedPhysicalEventSummary :
        INetworkSerializable,
        IEquatable<ObservedPhysicalEventSummary>
    {
        public FixedString64Bytes EventId;

        public ulong OwnerClientId;

        public DraftedActionType ActionType;

        public FixedString32Bytes PhysicalNodeId;

        public int CreatedTurn;

        public byte ActionPosition;

        public ObservedPhysicalEventPlanState PlanState;

        public static ObservedPhysicalEventSummary Create(
            DraftedActionPayload payload,
            ulong ownerClientId,
            int actionPosition,
            ObservedPhysicalEventPlanState planState)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(
                    nameof(payload)
                );
            }

            if (!payload.HasPhysicalEventLocation)
            {
                throw new ArgumentException(
                    "An observed physical event requires " +
                    "a physical event location.",
                    nameof(payload)
                );
            }

            if (actionPosition < 1 ||
                actionPosition >
                TurnActionRules.MaximumProductiveActions)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionPosition)
                );
            }

            FixedString64Bytes eventId =
                default;

            eventId.CopyFromTruncated(
                payload.PayloadId ??
                string.Empty
            );

            FixedString32Bytes physicalNodeId =
                default;

            physicalNodeId.CopyFromTruncated(
                payload.PhysicalEventNodeId ??
                string.Empty
            );

            return new ObservedPhysicalEventSummary
            {
                EventId = eventId,
                OwnerClientId = ownerClientId,
                ActionType = payload.ActionType,
                PhysicalNodeId = physicalNodeId,
                CreatedTurn = payload.CreatedTurn,
                ActionPosition =
                    (byte)actionPosition,
                PlanState = planState
            };
        }

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(
                ref EventId
            );

            serializer.SerializeValue(
                ref OwnerClientId
            );

            serializer.SerializeValue(
                ref ActionType
            );

            serializer.SerializeValue(
                ref PhysicalNodeId
            );

            serializer.SerializeValue(
                ref CreatedTurn
            );

            serializer.SerializeValue(
                ref ActionPosition
            );

            serializer.SerializeValue(
                ref PlanState
            );
        }

        public bool Equals(
            ObservedPhysicalEventSummary other)
        {
            return
                EventId.Equals(other.EventId) &&
                OwnerClientId ==
                other.OwnerClientId &&
                ActionType ==
                other.ActionType &&
                PhysicalNodeId.Equals(
                    other.PhysicalNodeId
                ) &&
                CreatedTurn ==
                other.CreatedTurn &&
                ActionPosition ==
                other.ActionPosition &&
                PlanState ==
                other.PlanState;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is
                    ObservedPhysicalEventSummary other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                EventId,
                OwnerClientId,
                ActionType,
                PhysicalNodeId,
                CreatedTurn,
                ActionPosition,
                PlanState
            );
        }
    }
}