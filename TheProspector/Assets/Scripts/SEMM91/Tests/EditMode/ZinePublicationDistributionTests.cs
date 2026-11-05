
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class ZinePublicationDistributionTests
{
    private sealed class Fixture
    {
        public readonly SimulationClock Clock;
        public readonly Network Network;
        public readonly ZineEditorialCycle Zine;
        public readonly ZineIssueKnowledgeRegistry Knowledge;
        public readonly ZineIssueInsertDelivery Delivery;

        public Fixture(bool profile = true)
        {
            Clock = new SimulationClock();
            Network = new Network();

            foreach (var id in new[] { "E", "B", "C", "D" })
            {
                Network.AddNode(new Node(id));
                Network.AddAddress("ADDR-" + id, "FI");
                Network.PlaceNode(id, "ADDR-" + id);
            }

            Network.AddConnection(
                "EB", "E", "B",
                CommunicationLayer.Insert, "zine");

            Network.AddConnection(
                "EC", "E", "C",
                CommunicationLayer.Insert, "zine");

            Network.AddConnection(
                "BD", "B", "D",
                CommunicationLayer.Insert, "zine");

            Zine = new ZineEditorialCycle(
                "Z1", "E", profile, Clock);

            Knowledge = new ZineIssueKnowledgeRegistry();

            Delivery = new ZineIssueInsertDelivery(
                new InsertDistributionRegistry(),
                Knowledge);
        }

        public ZinePublicationDistribution Bind()
        {
            return new ZinePublicationDistribution(
                Zine,
                Delivery,
                () => Network.GetOutgoingConnections(
                    "E", CommunicationLayer.Insert),
                nodeId => nodeId == "B" || nodeId == "D");
        }
    }

    [Test]
    public void Publication_Day30AutomaticallyDistributesIssue()
    {
        var f = new Fixture();

        using (f.Bind())
        {
            f.Zine.RecordAddress("X", "ADDR-X");
            f.Zine.RecordProductOpinion(
                "A", "D4", false);

            f.Clock.AdvanceDays(29);

            Assert.AreEqual(0, f.Zine.Issues.Count);
            Assert.AreEqual(0,
                f.Knowledge.ReceivedIssueCount("B"));

            f.Clock.AdvanceDays(1);

            Assert.AreEqual(1, f.Zine.Issues.Count);
            Assert.AreEqual(0, f.Zine.PendingCount);

            Assert.AreEqual(1,
                f.Knowledge.ReceivedIssueCount("B"));

            Assert.AreEqual(0,
                f.Knowledge.ReceivedIssueCount("C"));

            Assert.AreEqual(2,
                f.Knowledge.GetClaims("B").Count);

            Assert.AreEqual("ADDR-X",
                f.Knowledge.GetLatestPublishedAddress(
                    "B", "X"));
        }
    }

    [Test]
    public void Publication_Day60DistributesOnlyNewIssue()
    {
        var f = new Fixture();

        using (f.Bind())
        {
            f.Zine.RecordAddress("X", "ADDR-OLD");

            f.Clock.AdvanceDays(30);

            Assert.AreEqual(1,
                f.Knowledge.ReceivedIssueCount("B"));

            f.Zine.RecordAddress("X", "ADDR-NEW");

            f.Clock.AdvanceDays(30);

            Assert.AreEqual(2, f.Zine.Issues.Count);
            Assert.AreEqual(2,
                f.Knowledge.ReceivedIssueCount("B"));

            Assert.AreEqual("ADDR-NEW",
                f.Knowledge.GetLatestPublishedAddress(
                    "B", "X"));

            Assert.AreEqual("ADDR-OLD",
                f.Zine.Issues[0].Entries[0].AddressId);

            Assert.AreEqual(0, f.Zine.PendingCount);
        }
    }

    [Test]
    public void Publication_FalseZineSelectsFalseReaders()
    {
        var f = new Fixture(profile: false);

        using (f.Bind())
        {
            f.Zine.RecordProductOpinion(
                "A", "D1", true);

            f.Clock.AdvanceDays(30);

            // B = true, C = false.
            Assert.AreEqual(0,
                f.Knowledge.ReceivedIssueCount("B"));

            Assert.AreEqual(1,
                f.Knowledge.ReceivedIssueCount("C"));

            Assert.IsFalse(f.Zine.Issues[0].Profile);
        }
    }

    [Test]
    public void Publication_DistributorCanForwardPublishedIssue()
    {
        var f = new Fixture();

        using (f.Bind())
        {
            f.Zine.RecordAddress("X", "ADDR-X");

            f.Clock.AdvanceDays(30);

            Assert.IsTrue(
                f.Knowledge.HasReceived(
                    "B", f.Zine.Issues[0].ProductId));

            // B jakaa toimitukselta saamansa
            // itsenäisen kulttuurituotteen D:lle.
            f.Delivery.Distribute(
                f.Zine.Issues[0],
                f.Network.GetOutgoingConnections(
                    "B", CommunicationLayer.Insert),
                nodeId => true);

            Assert.AreEqual(1,
                f.Knowledge.ReceivedIssueCount("D"));

            Assert.AreEqual("ADDR-X",
                f.Knowledge.GetLatestPublishedAddress(
                    "D", "X"));

            Assert.AreEqual("E",
                f.Delivery.GetIssue(
                    f.Zine.Issues[0].ProductId)
                    .EditorNodeId);
        }
    }

    [Test]
    public void Publication_DisposingBindingStopsAutomaticDelivery()
    {
        var f = new Fixture();
        var binding = f.Bind();

        f.Zine.RecordAddress("X", "ADDR-FIRST");

        f.Clock.AdvanceDays(30);

        Assert.AreEqual(1,
            f.Knowledge.ReceivedIssueCount("B"));

        binding.Dispose();

        f.Zine.RecordAddress("X", "ADDR-SECOND");

        f.Clock.AdvanceDays(30);

        // Toimitus jatkaa julkaisemista,
        // mutta irrotettu jakelija ei enää
        // vastaanota julkaisuilmoituksia.
        Assert.AreEqual(2, f.Zine.Issues.Count);

        Assert.AreEqual(1,
            f.Knowledge.ReceivedIssueCount("B"));
    }
}
