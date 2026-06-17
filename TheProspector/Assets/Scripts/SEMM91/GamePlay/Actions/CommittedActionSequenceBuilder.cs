using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Actions
{
    public sealed class CommittedActionSequenceBuilder
    {
        public const int BaseActionCount = 2;
        public const int MaximumActionCount = 3;

        public IReadOnlyList<CommittedActionSlot> Build(
            IReadOnlyList<DraftedActionPayload> committedPayloads)
        {
            int explicitActionCount = 
                committedPayloads?.Count ?? 0;

            if (explicitActionCount > MaximumActionCount)
            {
                throw new InvalidOperationException(
                    $"Cannot commit more than {MaximumActionCount} actions"
                );

            }

            List<CommittedActionSlot> slots =
                new List<CommittedActionSlot>(
                    Math.Max(
                        BaseActionCount,
                        explicitActionCount
                    )
                );

            for (int index = 0;
                 index < explicitActionCount;
                 index++)
            {
                DraftedActionPayload payload =
                    committedPayloads[index];

                if (payload == null)
                {
                    throw new ArgumentException(
                        $"Cannot commit a null action at index {index}",
                        nameof(committedPayloads)
                        );
                }

                slots.Add(new CommittedActionSlot(
                    actionPosition: index + 1,
                    actionType: payload.ActionType,
                    isImplicit: false,
                    sourcePayload: payload
                ));
            }

            while (slots.Count < BaseActionCount)
            {
                slots.Add(new CommittedActionSlot(
                    actionPosition: slots.Count + 1,
                    actionType: DraftedActionType.Rest,
                    isImplicit: true,
                    sourcePayload: null));
            }
            
            return slots.ToArray();
            
        }
        
    }
}