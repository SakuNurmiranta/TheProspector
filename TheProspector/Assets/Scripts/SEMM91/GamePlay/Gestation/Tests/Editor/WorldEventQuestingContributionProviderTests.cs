using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Events;

namespace SEMM91.GamePlay.Gestation.Questing.Tests
{
    public class
        WorldEventQuestingContributionProviderTests
    {
        private WorldEventRegistry _registry;

        private WorldEventQuestingContributionProvider
            _provider;

        [SetUp]
        public void SetUp()
        {
            _registry = new WorldEventRegistry();

            _provider =
                new WorldEventQuestingContributionProvider(
                    _registry
                );
        }

        [Test]
        public void BuildContributions_PreservesEventAndSignalOrder()
        {
            _registry.Record(
                CreateEvent(
                    eventId: "EVENT_1",
                    characterId: "CHARACTER_A",
                    globalTurn: 3,
                    new WorldEventAxisSignal(
                        TagAxis.Symbolic,
                        -0.50f,
                        "First symbolic signal"
                    ),
                    new WorldEventAxisSignal(
                        TagAxis.Emotional,
                        1.00f,
                        "Ignored emotional signal"
                    ),
                    new WorldEventAxisSignal(
                        TagAxis.Symbolic,
                        -0.25f,
                        "Second symbolic signal"
                    )
                )
            );

            _registry.Record(
                CreateEvent(
                    eventId: "EVENT_2",
                    characterId: "CHARACTER_A",
                    globalTurn: 4,
                    new WorldEventAxisSignal(
                        TagAxis.Symbolic,
                        0.75f,
                        "Third symbolic signal"
                    )
                )
            );

            var contributions =
                _provider.BuildContributions(
                    CreateContext(
                        characterId: "CHARACTER_A",
                        activeAxis: TagAxis.Symbolic,
                        currentTurn: 4
                    )
                );

            Assert.That(
                contributions.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                contributions[0].Value,
                Is.EqualTo(-0.50f)
            );

            Assert.That(
                contributions[1].Value,
                Is.EqualTo(-0.25f)
            );

            Assert.That(
                contributions[2].Value,
                Is.EqualTo(0.75f)
            );

            Assert.That(
                contributions[0].SourceId,
                Is.EqualTo(
                    "world-event:EVENT_1:signal:0"
                )
            );

            Assert.That(
                contributions[1].SourceId,
                Is.EqualTo(
                    "world-event:EVENT_1:signal:2"
                )
            );

            Assert.That(
                contributions[2].SourceId,
                Is.EqualTo(
                    "world-event:EVENT_2:signal:0"
                )
            );
        }

        [Test]
        public void BuildContributions_DifferentAxis_IsIgnored()
        {
            _registry.Record(
                CreateEvent(
                    eventId: "EVENT_EMOTIONAL",
                    characterId: "CHARACTER_A",
                    globalTurn: 2,
                    new WorldEventAxisSignal(
                        TagAxis.Emotional,
                        2.0f,
                        "Emotional event"
                    )
                )
            );

            var contributions =
                _provider.BuildContributions(
                    CreateContext(
                        characterId: "CHARACTER_A",
                        activeAxis: TagAxis.Symbolic,
                        currentTurn: 2
                    )
                );

            Assert.That(contributions, Is.Empty);
        }

        [Test]
        public void BuildContributions_EventOlderThanFourTurns_IsIgnored()
        {
            _registry.Record(
                CreateEvent(
                    eventId: "EVENT_OLD",
                    characterId: "CHARACTER_A",
                    globalTurn: 0,
                    new WorldEventAxisSignal(
                        TagAxis.Symbolic,
                        -2.0f,
                        "Old event"
                    )
                )
            );

            var contributions =
                _provider.BuildContributions(
                    CreateContext(
                        characterId: "CHARACTER_A",
                        activeAxis: TagAxis.Symbolic,
                        currentTurn: 4
                    )
                );

            Assert.That(contributions, Is.Empty);
        }

        [Test]
        public void BuildContributions_FutureEvent_IsIgnored()
        {
            _registry.Record(
                CreateEvent(
                    eventId: "EVENT_FUTURE",
                    characterId: "CHARACTER_A",
                    globalTurn: 5,
                    new WorldEventAxisSignal(
                        TagAxis.Symbolic,
                        1.0f,
                        "Future event"
                    )
                )
            );

            var contributions =
                _provider.BuildContributions(
                    CreateContext(
                        characterId: "CHARACTER_A",
                        activeAxis: TagAxis.Symbolic,
                        currentTurn: 4
                    )
                );

            Assert.That(contributions, Is.Empty);
        }

        [Test]
        public void BuildContributions_DifferentCharacter_IsIgnored()
        {
            _registry.Record(
                CreateEvent(
                    eventId: "EVENT_B",
                    characterId: "CHARACTER_B",
                    globalTurn: 3,
                    new WorldEventAxisSignal(
                        TagAxis.Symbolic,
                        -1.0f,
                        "Other character event"
                    )
                )
            );

            var contributions =
                _provider.BuildContributions(
                    CreateContext(
                        characterId: "CHARACTER_A",
                        activeAxis: TagAxis.Symbolic,
                        currentTurn: 3
                    )
                );

            Assert.That(contributions, Is.Empty);
        }

        [Test]
        public void BuildContributions_NullContext_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => _provider.BuildContributions(null)
            );
        }

        private static QuestingCompositeContext
            CreateContext(
                string characterId,
                TagAxis activeAxis,
                int currentTurn)
        {
            return new QuestingCompositeContext(
                characterId,
                activeAxis,
                currentTurn
            );
        }

        private static WorldEventRecord CreateEvent(
            string eventId,
            string characterId,
            int globalTurn,
            params WorldEventAxisSignal[] signals)
        {
            return new WorldEventRecord(
                eventId: eventId,
                globalTurn: globalTurn,
                affectedEntityId: characterId,
                sourceActionKey: null,
                axisSignals:
                    signals ??
                    Array.Empty<WorldEventAxisSignal>()
            );
        }
    }
}