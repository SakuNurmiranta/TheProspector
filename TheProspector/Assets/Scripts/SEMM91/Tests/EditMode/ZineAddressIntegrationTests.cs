
using NUnit.Framework;
using SEMM91.Domain;

public class ZineAddressIntegrationTests
{
    private sealed class Fixture
    {
        public readonly Network Network;
        public readonly SimulationClock Clock;
        public readonly ZineEditorialCycle Zine;
        public readonly ZineIssueKnowledgeRegistry Knowledge;
        public readonly ZineIssueInsertDelivery Delivery;

        public Fixture()
        {
            Network = new Network();
            Clock = new SimulationClock();

            foreach (var id in new[] { "E", "A", "B", "C", "F" })
            {
                Network.AddNode(new Node(id));
                Network.AddAddress("ADDR-" + id, "FI");
                Network.PlaceNode(id, "ADDR-" + id);
            }

            Network.AddAddress("ADDR-A-NEW", "SE");

            Network.AddConnection(
                "EB", "E", "B",
                CommunicationLayer.Insert, "zine");

            Network.AddConnection(
                "EC", "E", "C",
                CommunicationLayer.Insert, "zine");

            Zine = new ZineEditorialCycle(
                "Z1", "E", true, Clock);

            Knowledge = new ZineIssueKnowledgeRegistry(
                Network);

            Delivery = new ZineIssueInsertDelivery(
                new InsertDistributionRegistry(),
                Knowledge);
        }

        public ZineEditorialIssue PublishAddress(
            string addressId)
        {
            Zine.RecordAddress("A", addressId);

            Clock.AdvanceDays(30);

            return Zine.Issues[Zine.Issues.Count - 1];
        }

        public void DeliverToB(ZineEditorialIssue issue)
        {
            Delivery.Distribute(
                issue,
                Network.GetOutgoingConnections(
                    "E", CommunicationLayer.Insert),
                recipient => recipient == "B");
        }
    }

    [Test]
    public void ZineAddress_LearnsAddressWithoutPersonalConnection()
    {
        var f = new Fixture();

        var issue = f.PublishAddress("ADDR-A");

        Assert.IsFalse(
            f.Network.TryGetKnownAddress(
                "B", "A", out _));

        f.DeliverToB(issue);

        Assert.IsTrue(
            f.Network.TryGetKnownAddress(
                "B", "A", out var known));

        Assert.AreEqual("ADDR-A", known);

        Assert.AreEqual(0,
            f.Network.GetOutgoingConnections(
                "B",
                CommunicationLayer.Interpersonal).Count);

        Assert.AreEqual(0,
            f.Network.GetOutgoingConnections(
                "A",
                CommunicationLayer.Interpersonal).Count);
    }

    [Test]
    public void ZineAddress_CanBeStaleBecauseOfMovement()
    {
        var f = new Fixture();

        // Osoite on oikea julkaisun aikaan.
        var issue = f.PublishAddress("ADDR-A");

        // A muuttaa ennen numeron saapumista.
        f.Network.MoveNode("A", "ADDR-A-NEW");

        f.DeliverToB(issue);

        f.Network.TryGetKnownAddress(
            "B", "A", out var known);

        f.Network.TryGetNodeAddress(
            "A", out var actual);

        Assert.AreEqual("ADDR-A", known);
        Assert.AreEqual("ADDR-A-NEW", actual);

        Assert.IsFalse(
            f.Network.TryGetOccupant(
                "ADDR-A", out _));
    }

    [Test]
    public void ZineAddress_NewerIssueCorrectsOldKnowledge()
    {
        var f = new Fixture();

        var first = f.PublishAddress("ADDR-A");
        f.DeliverToB(first);

        f.Network.MoveNode("A", "ADDR-A-NEW");

        var second = f.PublishAddress("ADDR-A-NEW");
        f.DeliverToB(second);

        f.Network.TryGetKnownAddress(
            "B", "A", out var known);

        Assert.AreEqual("ADDR-A-NEW", known);

        // Vanha numero säilyy historiallisena.
        Assert.AreEqual("ADDR-A",
            first.Entries[0].AddressId);

        Assert.AreEqual("ADDR-A-NEW",
            second.Entries[0].AddressId);

        Assert.AreEqual(2,
            f.Knowledge.GetClaims("B").Count);
    }

    [Test]
    public void ZineAddress_LateOldIssueCannotUndoNewCorrection()
    {
        var f = new Fixture();

        var first = f.PublishAddress("ADDR-A");

        f.Network.MoveNode("A", "ADDR-A-NEW");

        var second = f.PublishAddress("ADDR-A-NEW");

        // Numero 2 saapuu ennen numeroa 1.
        f.DeliverToB(second);
        f.DeliverToB(first);

        f.Network.TryGetKnownAddress(
            "B", "A", out var known);

        Assert.AreEqual("ADDR-A-NEW", known);

        Assert.AreEqual("ADDR-A-NEW",
            f.Knowledge.GetLatestPublishedAddress(
                "B", "A"));

        Assert.AreEqual(2,
            f.Knowledge.ReceivedIssueCount("B"));
    }

    [Test]
    public void ZineAddress_NonReaderDoesNotLearnAddress()
    {
        var f = new Fixture();

        var issue = f.PublishAddress("ADDR-A");

        // B haluaa true-zinen, C ei.
        f.Delivery.Distribute(
            issue,
            f.Network.GetOutgoingConnections(
                "E", CommunicationLayer.Insert),
            recipient => recipient == "B");

        Assert.IsTrue(
            f.Network.TryGetKnownAddress(
                "B", "A", out _));

        Assert.IsFalse(
            f.Network.TryGetKnownAddress(
                "C", "A", out _));

        Assert.AreEqual(0,
            f.Knowledge.ReceivedIssueCount("C"));
    }

    [Test]
    public void ZineAddress_FieldDiscoveryDoesNotRequireEdges()
    {
        var f = new Fixture();

        var issue = f.PublishAddress("ADDR-A");

        // F:llä ei ole yhtään verkkoyhteyttä.
        Assert.AreEqual(0,
            f.Network.GetOutgoingConnections(
                "F", CommunicationLayer.Interpersonal).Count);

        Assert.AreEqual(0,
            f.Network.GetOutgoingConnections(
                "F", CommunicationLayer.Insert).Count);

        // Kentän kautta löydetty numero.
        Assert.IsTrue(
            f.Knowledge.ReceiveIssue("F", issue));

        Assert.IsTrue(
            f.Network.TryGetKnownAddress(
                "F", "A", out var known));

        Assert.AreEqual("ADDR-A", known);

        // Osoitetieto ei luo verkkoyhteyttä.
        Assert.AreEqual(0,
            f.Network.GetOutgoingConnections(
                "F", CommunicationLayer.Interpersonal).Count);
    }
}
