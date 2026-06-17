using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Gestation.Questing.Tests
{
    public class QuestingTurnUsageRegistryTests
    {
        private QuestingTurnUsageRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new QuestingTurnUsageRegistry();
        }

        [Test]
        public void HasUnusedDreamForTurn_NewClient_ReturnsTrue()
        {
            bool available =
                _registry.HasUnusedDreamForTurn(
                    clientId: 1,
                    globalTurn: 0
                );

            Assert.That(available, Is.True);
        }

        [Test]
        public void TryConsumeDreamForTurn_SameTurnCanOnlyBeConsumedOnce()
        {
            bool first =
                _registry.TryConsumeDreamForTurn(
                    clientId: 1,
                    globalTurn: 3
                );

            bool second =
                _registry.TryConsumeDreamForTurn(
                    clientId: 1,
                    globalTurn: 3
                );

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);

            Assert.That(
                _registry.HasUnusedDreamForTurn(1, 3),
                Is.False
            );
        }

        [Test]
        public void HasUnusedDreamForTurn_LaterTurnReturnsTrue()
        {
            _registry.TryConsumeDreamForTurn(
                clientId: 1,
                globalTurn: 3
            );

            Assert.That(
                _registry.HasUnusedDreamForTurn(1, 4),
                Is.True
            );

            Assert.That(
                _registry.TryConsumeDreamForTurn(1, 4),
                Is.True
            );
        }

        [Test]
        public void Usage_DifferentClientsRemainIndependent()
        {
            _registry.TryConsumeDreamForTurn(
                clientId: 1,
                globalTurn: 2
            );

            Assert.That(
                _registry.HasUnusedDreamForTurn(1, 2),
                Is.False
            );

            Assert.That(
                _registry.HasUnusedDreamForTurn(2, 2),
                Is.True
            );
        }

        [Test]
        public void HasUnusedDreamForTurn_OlderTurnReturnsFalse()
        {
            _registry.TryConsumeDreamForTurn(
                clientId: 1,
                globalTurn: 4
            );

            Assert.That(
                _registry.HasUnusedDreamForTurn(1, 3),
                Is.False
            );

            Assert.That(
                _registry.TryConsumeDreamForTurn(1, 3),
                Is.False
            );
        }

        [Test]
        public void ClearClient_RemovesUsageState()
        {
            _registry.TryConsumeDreamForTurn(
                clientId: 1,
                globalTurn: 2
            );

            _registry.ClearClient(1);

            Assert.That(
                _registry.HasUnusedDreamForTurn(1, 2),
                Is.True
            );

            Assert.That(
                _registry.TryGetLastDreamTurn(1, out _),
                Is.False
            );
        }

        [Test]
        public void NegativeGlobalTurn_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _registry.HasUnusedDreamForTurn(
                    clientId: 1,
                    globalTurn: -1
                )
            );

            Assert.Throws<ArgumentOutOfRangeException>(
                () => _registry.TryConsumeDreamForTurn(
                    clientId: 1,
                    globalTurn: -1
                )
            );
        }
    }
}