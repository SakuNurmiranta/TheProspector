using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots.Tests.Editor
{
    public sealed class
        NetworkListSnapshotSynchronizerTests
    {
        [Test]
        public void IdenticalSnapshotProducesNoMutations()
        {
            using NetworkList<
                    KvltCanonPrecedentDebugRow>
                destination =
                    new();

            destination.Add(Row(0, 1));
            destination.Add(Row(1, 2));

            int mutations =
                NetworkListSnapshotSynchronizer
                    .Synchronize(
                        destination,
                        new[]
                        {
                            Row(0, 1),
                            Row(1, 2)
                        }
                    );

            Assert.That(
                mutations,
                Is.EqualTo(0)
            );

            Assert.That(
                destination.Count,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void ChangedAndAppendedRowsProduceOnlyTwoMutations()
        {
            using NetworkList<
                    KvltCanonPrecedentDebugRow>
                destination =
                    new();

            destination.Add(Row(0, 1));
            destination.Add(Row(1, 2));

            int mutations =
                NetworkListSnapshotSynchronizer
                    .Synchronize(
                        destination,
                        new[]
                        {
                            Row(0, 1),
                            Row(1, 3),
                            Row(2, 1)
                        }
                    );

            Assert.That(
                mutations,
                Is.EqualTo(2)
            );

            Assert.That(
                destination.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                destination[1].DegreeValue,
                Is.EqualTo(3)
            );

            Assert.That(
                destination[2].EstablishedTurn,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void ShorterSnapshotRemovesOnlyTrailingRows()
        {
            using NetworkList<
                    KvltCanonPrecedentDebugRow>
                destination =
                    new();

            destination.Add(Row(0, 1));
            destination.Add(Row(1, 2));
            destination.Add(Row(2, 3));

            IReadOnlyList<
                    KvltCanonPrecedentDebugRow>
                source =
                    new[]
                    {
                        Row(0, 1)
                    };

            int mutations =
                NetworkListSnapshotSynchronizer
                    .Synchronize(
                        destination,
                        source
                    );

            Assert.That(
                mutations,
                Is.EqualTo(2)
            );

            Assert.That(
                destination.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                destination[0].EstablishedTurn,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void EmptySnapshotUsesSingleClearMutation()
        {
            using NetworkList<
                    KvltCanonPrecedentDebugRow>
                destination =
                    new();

            destination.Add(Row(0, 1));
            destination.Add(Row(1, 2));
            destination.Add(Row(2, 3));

            int mutations =
                NetworkListSnapshotSynchronizer
                    .Synchronize(
                        destination,
                        new KvltCanonPrecedentDebugRow[0]
                    );

            Assert.That(
                mutations,
                Is.EqualTo(1)
            );

            Assert.That(
                destination.Count,
                Is.EqualTo(0)
            );
        }

        private static KvltCanonPrecedentDebugRow
            Row(
                int establishedTurn,
                byte degree)
        {
            return new KvltCanonPrecedentDebugRow
            {
                AxisValue = 0,
                PoleValue = 0,
                DegreeValue = degree,
                ProvenanceKindValue = 0,
                EstablishedTurn = establishedTurn
            };
        }
    }
}
