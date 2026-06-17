using System.Collections.Generic;
using NUnit.Framework;

namespace SEMM91.GamePlay.Actions.Tests
{
    public class CommittedActionSequenceBuilderTests
    {
        private CommittedActionSequenceBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new CommittedActionSequenceBuilder();
        }

        [Test]
        public void Build_NoPayloads_CreatesTwoImplicitRests()
        {
            IReadOnlyList<CommittedActionSlot> slots =
                builder.Build(
                    new List<DraftedActionPayload>()
                );

            Assert.That(slots.Count, Is.EqualTo(2));

            Assert.That(
                slots[0].ActionPosition,
                Is.EqualTo(1)
            );

            Assert.That(
                slots[1].ActionPosition,
                Is.EqualTo(2)
            );

            Assert.That(
                slots[0].ActionType,
                Is.EqualTo(DraftedActionType.Rest)
            );

            Assert.That(
                slots[1].ActionType,
                Is.EqualTo(DraftedActionType.Rest)
            );

            Assert.That(slots[0].IsImplicit, Is.True);
            Assert.That(slots[1].IsImplicit, Is.True);

            Assert.That(slots[0].SourcePayload, Is.Null);
            Assert.That(slots[1].SourcePayload, Is.Null);
        }

        [Test]
        public void Build_OnePayload_AppendsImplicitRest()
        {
            DraftedActionPayload createIdea =
                new DraftedActionPayload(
                    DraftedActionType.CreateIdea,
                    createdTurn: 2
                );

            IReadOnlyList<CommittedActionSlot> slots =
                builder.Build(
                    new List<DraftedActionPayload>
                    {
                        createIdea
                    }
                );

            Assert.That(slots.Count, Is.EqualTo(2));

            Assert.That(
                slots[0].ActionType,
                Is.EqualTo(DraftedActionType.CreateIdea)
            );

            Assert.That(slots[0].IsImplicit, Is.False);

            Assert.That(
                slots[0].SourcePayload,
                Is.SameAs(createIdea)
            );

            Assert.That(
                slots[1].ActionType,
                Is.EqualTo(DraftedActionType.Rest)
            );

            Assert.That(slots[1].IsImplicit, Is.True);
        }

        [Test]
        public void Build_ExplicitRest_RemainsExplicit()
        {
            DraftedActionPayload explicitRest =
                new DraftedActionPayload(
                    DraftedActionType.Rest,
                    createdTurn: 2
                );

            IReadOnlyList<CommittedActionSlot> slots =
                builder.Build(
                    new List<DraftedActionPayload>
                    {
                        explicitRest
                    }
                );

            Assert.That(
                slots[0].ActionType,
                Is.EqualTo(DraftedActionType.Rest)
            );

            Assert.That(slots[0].IsImplicit, Is.False);

            Assert.That(
                slots[0].SourcePayload,
                Is.SameAs(explicitRest)
            );

            Assert.That(slots[1].IsImplicit, Is.True);
        }

        [Test]
        public void Build_ThreePayloads_PreservesOrderAndPositions()
        {
            DraftedActionPayload rehearse =
                new DraftedActionPayload(
                    DraftedActionType.RehearseActiveSet,
                    createdTurn: 3
                );

            DraftedActionPayload recordOne =
                new DraftedActionPayload(
                    DraftedActionType.RecordActiveSetToDemo,
                    createdTurn: 3
                );

            DraftedActionPayload recordTwo =
                new DraftedActionPayload(
                    DraftedActionType.RecordActiveSetToDemo,
                    createdTurn: 3
                );

            IReadOnlyList<CommittedActionSlot> slots =
                builder.Build(
                    new List<DraftedActionPayload>
                    {
                        rehearse,
                        recordOne,
                        recordTwo
                    }
                );

            Assert.That(slots.Count, Is.EqualTo(3));

            Assert.That(
                slots[0].ActionPosition,
                Is.EqualTo(1)
            );

            Assert.That(
                slots[1].ActionPosition,
                Is.EqualTo(2)
            );

            Assert.That(
                slots[2].ActionPosition,
                Is.EqualTo(3)
            );

            Assert.That(
                slots[0].ActionType,
                Is.EqualTo(
                    DraftedActionType.RehearseActiveSet
                )
            );

            Assert.That(
                slots[1].ActionType,
                Is.EqualTo(
                    DraftedActionType.RecordActiveSetToDemo
                )
            );

            Assert.That(
                slots[2].ActionType,
                Is.EqualTo(
                    DraftedActionType.RecordActiveSetToDemo
                )
            );

            Assert.That(
                slots,
                Has.All.Property(
                    nameof(CommittedActionSlot.IsImplicit)
                ).False
            );
        }

        [Test]
        public void Build_MoreThanThreePayloads_Throws()
        {
            List<DraftedActionPayload> payloads = new()
            {
                CreatePayload(),
                CreatePayload(),
                CreatePayload(),
                CreatePayload()
            };

            Assert.Throws<System.InvalidOperationException>(
                () => builder.Build(payloads)
            );
        }

        private static DraftedActionPayload CreatePayload()
        {
            return new DraftedActionPayload(
                DraftedActionType.CreateIdea,
                createdTurn: 1
            );
        }
    }
}