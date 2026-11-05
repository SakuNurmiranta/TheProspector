
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class FieldZineDiscoveryTests
{
    private sealed class FixedRandom : Random
    {
        private readonly double _sample;

        public FixedRandom(double sample)
        {
            _sample = sample;
        }

        public override double NextDouble()
        {
            return _sample;
        }
    }

    private sealed class Fixture : IDisposable
    {
        public readonly Network Network;
        public readonly Field Field;
        public readonly SimulationClock Clock;
        public readonly ZineEditorialCycle Editorial;
        public readonly ZineIssueKnowledgeRegistry Knowledge;
        public readonly FieldZineDiscovery Discovery;

        private readonly ZinePublicationDistribution _publication;

        public readonly Dictionary<string, int> Capital =
            new Dictionary<string, int>
            {
                ["B"] = 10,
                ["C"] = 30,
                ["D"] = 60
            };

        public Fixture()
        {
            Network = new Network();

            foreach (string id in
                     new[] { "E", "A", "B", "C", "D", "F" })
            {
                Network.AddNode(new Node(id));
                Network.AddAddress("ADDR-" + id, "FI");
                Network.PlaceNode(id, "ADDR-" + id);
            }

            Network.AddAddress("ADDR-A-NEW", "SE");

            // E toimittaa zinen B:lle.
            // B:n ja C:n välillä ei ole kaarta.
            Network.AddConnection(
                "EB", "E", "B",
                CommunicationLayer.Insert, "zine");

            Field = new Field("F1", Network);

            Field.RegisterLocalNode("B");
            Field.RegisterLocalNode("C");
            Field.RegisterLocalNode("D");

            Clock = new SimulationClock();

            Editorial = new ZineEditorialCycle(
                "Z1", "E", true, Clock);

            Knowledge = new ZineIssueKnowledgeRegistry(
                Network);

            var delivery = new ZineIssueInsertDelivery(
                new InsertDistributionRegistry(),
                Knowledge);

            _publication = new ZinePublicationDistribution(
                Editorial,
                delivery,
                () => Network.GetOutgoingConnections(
                    "E", CommunicationLayer.Insert),
                recipient => recipient == "B");

            Discovery = new FieldZineDiscovery(
                Field, Knowledge);

            Editorial.RecordSelfDeclaredContact(
                "A", "ADDR-A", true);

            Clock.AdvanceDays(30);

            Assert.IsTrue(
                Knowledge.HasReceived(
                    "B", FirstIssue.ProductId));
        }

        public ZineEditorialIssue FirstIssue =>
            Editorial.Issues[0];

        public void Dispose()
        {
            _publication.Dispose();
        }
    }

    [Test]
    public void FieldZine_RequiresActualSourceAndLocalMembership()
    {
        using (var f = new Fixture())
        {
            // C ei vielä omista numeroa.
            Assert.IsFalse(f.Discovery.Introduce(
                "C", f.FirstIssue, 0.5));

            // B sai sen oikean Layer 3 -jakelun kautta.
            Assert.IsTrue(f.Discovery.Introduce(
                "B", f.FirstIssue, 0.5));

            // Sama numero tuodaan kenttään vain kerran.
            Assert.IsFalse(f.Discovery.Introduce(
                "B", f.FirstIssue, 0.5));

            Assert.AreEqual(1,
                f.Discovery.AvailableIssueCount);
        }
    }

    [Test]
    public void FieldZine_CanReachNodeWithoutAnyDirectConnection()
    {
        using (var f = new Fixture())
        {
            f.Discovery.Introduce(
                "B", f.FirstIssue, 0.5);

            // C:n kenttäosuus = 30/100 = 0.3.
            // P(löytö) = 0.5 * 0.3 = 0.15.
            Assert.IsTrue(f.Discovery.TryDiscover(
                "C",
                f.FirstIssue.ProductId,
                true,
                f.Capital,
                new FixedRandom(0.14)));

            Assert.IsTrue(
                f.Knowledge.HasReceived(
                    "C", f.FirstIssue.ProductId));

            Assert.IsTrue(
                f.Network.TryGetKnownAddress(
                    "C", "A", out var address));

            Assert.AreEqual("ADDR-A", address);

            Assert.AreEqual(0,
                f.Network.GetOutgoingConnections(
                    "B",
                    CommunicationLayer.Interpersonal).Count);

            Assert.AreEqual(0,
                f.Network.GetOutgoingConnections(
                    "B",
                    CommunicationLayer.Insert).Count);

            Assert.AreEqual(0,
                f.Network.GetOutgoingConnections(
                    "C",
                    CommunicationLayer.Interpersonal).Count);
        }
    }

    [Test]
    public void FieldZine_RespectsProbabilityAndReaderProfile()
    {
        using (var f = new Fixture())
        {
            f.Discovery.Introduce(
                "B", f.FirstIssue, 0.5);

            // False-lukija ei valitse true-zineä.
            Assert.IsFalse(f.Discovery.TryDiscover(
                "C", f.FirstIssue.ProductId,
                false, f.Capital,
                new FixedRandom(0.0)));

            // P = 0.15, satunnaisotos 0.16.
            Assert.IsFalse(f.Discovery.TryDiscover(
                "C", f.FirstIssue.ProductId,
                true, f.Capital,
                new FixedRandom(0.16)));

            Assert.IsFalse(
                f.Knowledge.HasReceived(
                    "C", f.FirstIssue.ProductId));

            // Myöhempi onnistunut kohtaaminen.
            Assert.IsTrue(f.Discovery.TryDiscover(
                "C", f.FirstIssue.ProductId,
                true, f.Capital,
                new FixedRandom(0.14)));
        }
    }

    [Test]
    public void FieldZine_ZeroReachAndNonLocalNodesCannotDiscover()
    {
        using (var f = new Fixture())
        {
            Assert.IsTrue(f.Discovery.Introduce(
                "B", f.FirstIssue, 0.0));

            Assert.IsFalse(f.Discovery.TryDiscover(
                "C", f.FirstIssue.ProductId,
                true, f.Capital,
                new FixedRandom(0.0)));

            // F on eksplisiittinen solmu,
            // mutta ei kuulu tähän kenttään.
            Assert.IsFalse(f.Discovery.TryDiscover(
                "F", f.FirstIssue.ProductId,
                true, f.Capital,
                new FixedRandom(0.0)));

            Assert.IsFalse(
                f.Knowledge.HasReceived(
                    "F", f.FirstIssue.ProductId));
        }
    }

    [Test]
    public void FieldZine_UnknownOrUnavailableIssueIsNotDiscovered()
    {
        using (var f = new Fixture())
        {
            // Numero on B:n tiedossa, mutta sitä
            // ei ole tuotu kentän saataville.
            Assert.IsFalse(f.Discovery.TryDiscover(
                "C", f.FirstIssue.ProductId,
                true, f.Capital,
                new FixedRandom(0.0)));

            Assert.IsFalse(f.Discovery.TryDiscover(
                "C", "UNKNOWN-ISSUE",
                true, f.Capital,
                new FixedRandom(0.0)));
        }
    }

    [Test]
    public void FieldZine_RepeatDiscoveryDoesNotDuplicateInformation()
    {
        using (var f = new Fixture())
        {
            f.Discovery.Introduce(
                "B", f.FirstIssue, 1.0);

            Assert.IsTrue(f.Discovery.TryDiscover(
                "C", f.FirstIssue.ProductId,
                true, f.Capital,
                new FixedRandom(0.0)));

            Assert.IsFalse(f.Discovery.TryDiscover(
                "C", f.FirstIssue.ProductId,
                true, f.Capital,
                new FixedRandom(0.0)));

            Assert.AreEqual(1,
                f.Knowledge.ReceivedIssueCount("C"));

            Assert.AreEqual(1,
                f.Knowledge.GetClaims("C").Count);
        }
    }

    [Test]
    public void FieldZine_NewerIssueCanCorrectOldAddress()
    {
        using (var f = new Fixture())
        {
            f.Discovery.Introduce(
                "B", f.FirstIssue, 1.0);

            Assert.IsTrue(f.Discovery.TryDiscover(
                "C", f.FirstIssue.ProductId,
                true, f.Capital,
                new FixedRandom(0.0)));

            // A muuttaa. Seuraavaan numeroon
            // tulee itse ilmoitettu uusi osoite.
            f.Network.MoveNode("A", "ADDR-A-NEW");

            f.Editorial.RecordSelfDeclaredContact(
                "A", "ADDR-A-NEW", true);

            f.Clock.AdvanceDays(30);

            var second = f.Editorial.Issues[1];

            Assert.IsTrue(
                f.Knowledge.HasReceived(
                    "B", second.ProductId));

            Assert.IsTrue(f.Discovery.Introduce(
                "B", second, 1.0));

            Assert.IsTrue(f.Discovery.TryDiscover(
                "C", second.ProductId,
                true, f.Capital,
                new FixedRandom(0.0)));

            Assert.IsTrue(
                f.Network.TryGetKnownAddress(
                    "C", "A", out var known));

            Assert.AreEqual("ADDR-A-NEW", known);

            Assert.AreEqual(2,
                f.Knowledge.ReceivedIssueCount("C"));

            Assert.AreEqual("ADDR-A",
                f.FirstIssue.Entries[0].AddressId);
        }
    }
}
