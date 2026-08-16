using System;
using NUnit.Framework;
using SEMM91.GamePlay.Actions.History;

namespace SEMM91.GamePlay.Events.Tests.Editor
{
    public class HappeningRegistryTests
    {
        [Test]
        public void Registry_PreservesDurableInsertionOrderAndLookup()
        {
            HappeningRegistry registry =
                new HappeningRegistry();

            Happening first =
                Create(
                    "HAPPENING_A",
                    "COLLECTIVE_KVLT",
                    1
                );

            Happening second =
                Create(
                    "HAPPENING_B",
                    "COLLECTIVE_KVLT",
                    100
                );

            registry.Record(first);
            registry.Record(second);

            Assert.That(
                registry.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                registry.TryGet(
                    "HAPPENING_A",
                    out Happening stored
                ),
                Is.True
            );

            Assert.That(
                stored,
                Is.SameAs(first)
            );

            Assert.That(
                registry.GetAll(),
                Is.EqualTo(
                    new[]
                    {
                        first,
                        second
                    }
                )
            );

            // Unlike WorldEventRegistry,
            // old Happenings are not pruned.

            Assert.That(
                registry.TryGet(
                    "HAPPENING_A",
                    out _
                ),
                Is.True
            );
        }

        [Test]
        public void Registry_RejectsDuplicateHappeningIdentity()
        {
            HappeningRegistry registry =
                new HappeningRegistry();

            registry.Record(
                Create(
                    "DUPLICATE",
                    "COLLECTIVE_A",
                    1
                )
            );

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    registry.Record(
                        Create(
                            "DUPLICATE",
                            "COLLECTIVE_B",
                            2
                        )
                    )
            );
        }

        [Test]
        public void Registry_CanQueryCollectiveOwnedEpisodes()
        {
            HappeningRegistry registry =
                new HappeningRegistry();

            Happening kvltFirst =
                Create(
                    "KVLT_A",
                    "COLLECTIVE_KVLT",
                    1
                );

            Happening society =
                Create(
                    "SOCIETY_A",
                    "COLLECTIVE_SOCIETY",
                    2
                );

            Happening kvltSecond =
                Create(
                    "KVLT_B",
                    "COLLECTIVE_KVLT",
                    3
                );

            registry.Record(kvltFirst);
            registry.Record(society);
            registry.Record(kvltSecond);

            Assert.That(
                registry.GetOwnedByCollective(
                    "COLLECTIVE_KVLT"
                ),
                Is.EqualTo(
                    new[]
                    {
                        kvltFirst,
                        kvltSecond
                    }
                )
            );
        }

        private static Happening Create(
            string id,
            string collectiveId,
            int turn)
        {
            return new Happening(
                id,
                collectiveId,
                "KEEPER",
                "CAUSE",
                "CRUX",
                "NODE",
                turn,
                new CharacterActionKey(
                    "KEEPER",
                    turn,
                    1
                )
            );
        }
    }
}