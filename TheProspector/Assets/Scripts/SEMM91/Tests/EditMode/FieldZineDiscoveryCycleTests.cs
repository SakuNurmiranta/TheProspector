
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class FieldZineDiscoveryCycleTests
{
    private sealed class CountingRandom : Random
    {
        public double Sample { get; set; }
        public int Calls { get; private set; }

        public CountingRandom(double sample)
        {
            Sample = sample;
        }

        public override double NextDouble()
        {
            Calls++;
            return Sample;
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
        public readonly FieldZineDiscoveryCycle Cycle;
        public readonly CountingRandom Random;

        private readonly ZinePublicationDistribution _publication;

        public readonly Dictionary<string, int> Capital =
            new Dictionary<string, int>
            {
                ["B"] = 10,
                ["C"] = 30,
                ["D"] = 60
            };

        public bool ReaderPrefersTrue = true;

        public Fixture(
            double sample = 0.14,
            bool distributeToB = true)
        {
            Network = new Network();

            foreach (string id in
                     new[] { "E", "A", "B", "C", "D" })
            {
                Network.AddNode(new Node(id));
                Network.AddAddress("ADDR-" + id, "FI");
                Network.PlaceNode(id, "ADDR-" + id);
            }

            Network.AddAddress("ADDR-A-NEW", "SE");

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
                id => distributeToB && id == "B");

            Discovery = new FieldZineDiscovery(
                Field, Knowledge);

            Random = new CountingRandom(sample);

            Cycle = new FieldZineDiscoveryCycle(
                Clock,
                Editorial,
                Discovery,
                Knowledge,
                "B",
                new[] { "C" },
                id => ReaderPrefersTrue,
                () => Capital,
                Random,
                0.5);

            Editorial.RecordSelfDeclaredContact(
                "A", "ADDR-A", true);
        }

        public void Dispose()
        {
            Cycle.Dispose();
            _publication.Dispose();
        }
    }

    [Test]
    public void Cycle_DiscoversAutomaticallyAfterPublication()
    {
        using (var f = new Fixture())
        {
            f.Clock.AdvanceDays(29);

            Assert.AreEqual(0, f.Random.Calls);
            Assert.AreEqual(0, f.Editorial.Issues.Count);

            f.Clock.AdvanceDays(2);

            var issue = f.Editorial.Issues[0];

            Assert.IsTrue(
                f.Knowledge.HasReceived(
                    "B", issue.ProductId));

            Assert.IsTrue(
                f.Knowledge.HasReceived(
                    "C", issue.ProductId));

            Assert.AreEqual(1, f.Random.Calls);

            Assert.IsTrue(
                f.Network.TryGetKnownAddress(
                    "C", "A", out var known));

            Assert.AreEqual("ADDR-A", known);

            // Kenttälöytö ei luo verkkokaarta.
            Assert.AreEqual(0,
                f.Network.GetOutgoingConnections(
                    "C",
                    CommunicationLayer.Interpersonal).Count);

            Assert.AreEqual(0,
                f.Network.GetOutgoingConnections(
                    "C",
                    CommunicationLayer.Insert).Count);
        }
    }

    [Test]
    public void Cycle_FailedSampleIsNotRepeatedDaily()
    {
        using (var f = new Fixture(sample: 0.16))
        {
            f.Clock.AdvanceDays(31);

            var issue = f.Editorial.Issues[0];

            Assert.IsFalse(
                f.Knowledge.HasReceived(
                    "C", issue.ProductId));

            Assert.AreEqual(1, f.Random.Calls);

            // Sama epäonnistunut löytö ei muutu
            // varmaksi toistamalla arvonta joka päivä.
            f.Clock.AdvanceDays(25);

            Assert.AreEqual(1, f.Random.Calls);

            Assert.IsFalse(
                f.Knowledge.HasReceived(
                    "C", issue.ProductId));
        }
    }

    [Test]
    public void Cycle_WaitsUntilSourceActuallyReceivesIssue()
    {
        using (var f = new Fixture(
                   distributeToB: false))
        {
            f.Clock.AdvanceDays(31);

            var issue = f.Editorial.Issues[0];

            Assert.IsFalse(
                f.Knowledge.HasReceived(
                    "B", issue.ProductId));

            Assert.IsFalse(
                f.Knowledge.HasReceived(
                    "C", issue.ProductId));

            Assert.AreEqual(0, f.Random.Calls);

            // B saa numeron myöhemmin toisesta
            // jakelukanavasta.
            f.Knowledge.ReceiveIssue("B", issue);

            f.Clock.AdvanceDays(1);

            Assert.IsTrue(
                f.Knowledge.HasReceived(
                    "C", issue.ProductId));

            Assert.AreEqual(1, f.Random.Calls);
        }
    }

    [Test]
    public void Cycle_ProfileMismatchDoesNotConsumeSample()
    {
        using (var f = new Fixture(sample: 0.0))
        {
            f.ReaderPrefersTrue = false;

            f.Clock.AdvanceDays(31);

            Assert.AreEqual(0, f.Random.Calls);

            Assert.IsFalse(
                f.Knowledge.HasReceived(
                    "C", f.Editorial.Issues[0].ProductId));

            // Myöhemmin lukijan julkinen
            // kiinnostus muuttuu.
            f.ReaderPrefersTrue = true;

            f.Clock.AdvanceDays(1);

            Assert.AreEqual(1, f.Random.Calls);

            Assert.IsTrue(
                f.Knowledge.HasReceived(
                    "C", f.Editorial.Issues[0].ProductId));
        }
    }

    [Test]
    public void Cycle_NewIssueCanUpdatePreviouslyLearnedAddress()
    {
        using (var f = new Fixture())
        {
            f.Clock.AdvanceDays(31);

            Assert.AreEqual("ADDR-A",
                f.Knowledge.GetLatestPublishedAddress(
                    "C", "A"));

            f.Network.MoveNode("A", "ADDR-A-NEW");

            f.Editorial.RecordSelfDeclaredContact(
                "A", "ADDR-A-NEW", true);

            f.Clock.AdvanceDays(30);

            Assert.AreEqual(2, f.Editorial.Issues.Count);

            Assert.AreEqual(2,
                f.Knowledge.ReceivedIssueCount("C"));

            Assert.AreEqual(2, f.Random.Calls);

            Assert.AreEqual("ADDR-A-NEW",
                f.Knowledge.GetLatestPublishedAddress(
                    "C", "A"));

            Assert.AreEqual("ADDR-A",
                f.Editorial.Issues[0]
                    .Entries[0].AddressId);
        }
    }

    [Test]
    public void Cycle_DisposeStopsFurtherFieldSampling()
    {
        using (var f = new Fixture())
        {
            f.Cycle.Dispose();

            f.Clock.AdvanceDays(31);

            var issue = f.Editorial.Issues[0];

            // Zine julkaistaan ja toimitetaan
            // edelleen normaalisti B:lle.
            Assert.IsTrue(
                f.Knowledge.HasReceived(
                    "B", issue.ProductId));

            // Kentän automaattinen otanta
            // on kuitenkin pysäytetty.
            Assert.IsFalse(
                f.Knowledge.HasReceived(
                    "C", issue.ProductId));

            Assert.AreEqual(0, f.Random.Calls);
        }
    }
}
