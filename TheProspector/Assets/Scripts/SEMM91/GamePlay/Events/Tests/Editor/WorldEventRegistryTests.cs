using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions.History;

namespace SEMM91.GamePlay.Events.Tests
{
    public class WorldEventRegistryTests
    {
        private WorldEventRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new WorldEventRegistry();
        }

        [Test]
        public void Record_GetRecent_PreservesInsertionOrder()
        {
            WorldEventRecord first =
                CreateEvent("EVENT_1", "CHARACTER_A", 1);

            WorldEventRecord second =
                CreateEvent("EVENT_2", "CHARACTER_A", 1);

            _registry.Record(first);
            _registry.Record(second);

            var records =
                _registry.GetRecent("CHARACTER_A");

            Assert.That(records.Count, Is.EqualTo(2));
            Assert.That(records[0], Is.SameAs(first));
            Assert.That(records[1], Is.SameAs(second));
        }

        [Test]
        public void GetRecent_DifferentEntitiesRemainSeparate()
        {
            _registry.Record(
                CreateEvent("EVENT_A", "CHARACTER_A", 1)
            );

            _registry.Record(
                CreateEvent("EVENT_B", "CHARACTER_B", 1)
            );

            var records =
                _registry.GetRecent("CHARACTER_A");

            Assert.That(records.Count, Is.EqualTo(1));

            Assert.That(
                records[0].EventId,
                Is.EqualTo("EVENT_A")
            );
        }

        [Test]
        public void Record_DuplicateEventId_Throws()
        {
            _registry.Record(
                CreateEvent("EVENT_DUPLICATE", "CHARACTER_A", 1)
            );

            Assert.Throws<InvalidOperationException>(
                () => _registry.Record(
                    CreateEvent(
                        "EVENT_DUPLICATE",
                        "CHARACTER_B",
                        1
                    )
                )
            );
        }

        [Test]
        public void Record_PrunesEventsOlderThanFourTurns()
        {
            for (int turn = 0; turn <= 4; turn++)
            {
                _registry.Record(
                    CreateEvent(
                        $"EVENT_{turn}",
                        "CHARACTER_A",
                        turn
                    )
                );
            }

            var records =
                _registry.GetRecent("CHARACTER_A");

            Assert.That(records.Count, Is.EqualTo(4));

            Assert.That(
                records[0].GlobalTurn,
                Is.EqualTo(1)
            );

            Assert.That(
                _registry.TryGet("EVENT_0", out _),
                Is.False
            );
        }

        [Test]
        public void Record_RetainsSourceActionAndAxisSignals()
        {
            CharacterActionKey actionKey =
                new CharacterActionKey(
                    characterEntityId: "CHARACTER_A",
                    globalTurn: 2,
                    actionPosition: 1
                );

            WorldEventAxisSignal signal =
                new WorldEventAxisSignal(
                    axis: TagAxis.Symbolic,
                    signedIntensity: -1.25f,
                    description: "Symbolic backlash"
                );

            WorldEventRecord record =
                new WorldEventRecord(
                    eventId: "EVENT_BACKLASH",
                    globalTurn: 2,
                    affectedEntityId: "CHARACTER_A",
                    sourceActionKey: actionKey,
                    axisSignals: new[] { signal }
                );

            _registry.Record(record);

            bool found = _registry.TryGet(
                "EVENT_BACKLASH",
                out WorldEventRecord stored
            );

            Assert.That(found, Is.True);
            Assert.That(stored.SourceActionKey.HasValue, Is.True);

            Assert.That(
                stored.SourceActionKey.Value,
                Is.EqualTo(actionKey)
            );

            Assert.That(
                stored.AxisSignals.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                stored.AxisSignals[0].Axis,
                Is.EqualTo(TagAxis.Symbolic)
            );

            Assert.That(
                stored.AxisSignals[0].SignedIntensity,
                Is.EqualTo(-1.25f)
            );
        }

        [Test]
        public void AxisSignal_NonFiniteIntensity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new WorldEventAxisSignal(
                    axis: TagAxis.Symbolic,
                    signedIntensity: float.NaN,
                    description: "Invalid"
                )
            );
        }

        private static WorldEventRecord CreateEvent(
            string eventId,
            string affectedEntityId,
            int globalTurn)
        {
            return new WorldEventRecord(
                eventId: eventId,
                globalTurn: globalTurn,
                affectedEntityId: affectedEntityId,
                sourceActionKey: null,
                axisSignals: Array.Empty<WorldEventAxisSignal>()
            );
        }
    }
}