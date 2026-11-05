
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class ZineIssueKnowledgeTests
{
    private static Network CreateNetwork()
    {
        var network = new Network();

        foreach (var id in new[] { "E", "B", "C", "D" })
        {
            network.AddNode(new Node(id));
            network.AddAddress("ADDR-" + id, "FI");
            network.PlaceNode(id, "ADDR-" + id);
        }

        network.AddConnection(
            "EB", "E", "B",
            CommunicationLayer.Insert, "zine");

        network.AddConnection(
            "EC", "E", "C",
            CommunicationLayer.Insert, "zine");

        network.AddConnection(
            "BD", "B", "D",
            CommunicationLayer.Insert, "zine");

        network.AddConnection(
            "ED", "E", "D",
            CommunicationLayer.Insert, "zine");

        return network;
    }

    private static ZineEditorialIssue CreateIssue(
        bool profile = true)
    {
        var clock = new SimulationClock();

        var zine = new ZineEditorialCycle(
            "Z1", "E", profile, clock);

        zine.RecordAddress("X", "ADDR-OLD-X");
        zine.RecordProductOpinion("A", "D4", false);
        zine.RecordSkepticism("R", "X");

        clock.AdvanceDays(30);

        return zine.Issues[0];
    }

    [Test]
    public void ZineKnowledge_DeliveryUpdatesOnlyRecipients()
    {
        var network = CreateNetwork();
        var knowledge = new ZineIssueKnowledgeRegistry();

        var delivery = new ZineIssueInsertDelivery(
            new InsertDistributionRegistry(),
            knowledge);

        var issue = CreateIssue();

        var result = delivery.Distribute(
            issue,
            network.GetOutgoingConnections(
                "E", CommunicationLayer.Insert),
            nodeId => nodeId == "B");

        Assert.AreEqual(1, result.Count);

        Assert.AreEqual(3,
            knowledge.GetClaims("B").Count);

        Assert.AreEqual(0,
            knowledge.GetClaims("C").Count);

        Assert.AreEqual("ADDR-OLD-X",
            knowledge.GetLatestPublishedAddress("B", "X"));

        Assert.IsTrue(
            knowledge.HasReceived("B", issue.ProductId));

        Assert.IsFalse(
            knowledge.HasReceived("C", issue.ProductId));
    }

    [Test]
    public void ZineKnowledge_ClaimsPreserveTheirSources()
    {
        var knowledge = new ZineIssueKnowledgeRegistry();
        var issue = CreateIssue();

        Assert.IsTrue(
            knowledge.ReceiveIssue("B", issue));

        var claims = knowledge.GetClaims("B");

        var opinion = claims.Single(c =>
            c.Entry.Kind ==
                ZineEditorialEntryKind.ProductOpinion);

        Assert.AreEqual("A",
            opinion.Entry.SubjectNodeId);

        Assert.AreEqual("D4",
            opinion.Entry.ProductId);

        Assert.AreEqual(false,
            opinion.Entry.Opinion);

        var skepticism = claims.Single(c =>
            c.Entry.Kind ==
                ZineEditorialEntryKind.Skepticism);

        Assert.AreEqual("R",
            skepticism.Entry.SubjectNodeId);

        Assert.AreEqual("X",
            skepticism.Entry.TargetNodeId);

        Assert.AreEqual("E",
            skepticism.EditorNodeId);

        Assert.AreEqual(issue.ProductId,
            skepticism.IssueProductId);

        Assert.AreEqual(30,
            skepticism.PublishedDay);

        // Väite R:n skeptisyydestä ei ole
        // B:n henkilökohtainen skeptisyys.
        var personal = new DirectionalSkepticismLedger();

        Assert.AreEqual(0,
            personal.Get("B", "X").Total);
    }

    [Test]
    public void ZineKnowledge_ForwardingDoesNotDuplicateClaims()
    {
        var network = CreateNetwork();
        var knowledge = new ZineIssueKnowledgeRegistry();

        var delivery = new ZineIssueInsertDelivery(
            new InsertDistributionRegistry(),
            knowledge);

        var issue = CreateIssue();

        delivery.Distribute(
            issue,
            network.GetOutgoingConnections(
                "E", CommunicationLayer.Insert),
            nodeId => true);

        // B on saanut numeron ja välittää sen D:lle.
        delivery.Distribute(
            issue,
            network.GetOutgoingConnections(
                "B", CommunicationLayer.Insert),
            nodeId => true);

        // D sai saman numeron myös E:ltä.
        Assert.AreEqual(3,
            knowledge.GetClaims("D").Count);

        Assert.AreEqual(1,
            knowledge.ReceivedIssueCount("D"));

        Assert.AreEqual("E",
            knowledge.GetClaims("D")[0].EditorNodeId);
    }

    [Test]
    public void ZineKnowledge_FieldRecipientNeedsNoDirectEdge()
    {
        var knowledge = new ZineIssueKnowledgeRegistry();
        var issue = CreateIssue();

        // Kentästä löytynyt numero voidaan
        // toimittaa tietorekisteriin ilman
        // suoraa jakelijayhteyttä.
        Assert.IsTrue(
            knowledge.ReceiveIssue("OUTSIDER", issue));

        Assert.AreEqual("ADDR-OLD-X",
            knowledge.GetLatestPublishedAddress(
                "OUTSIDER", "X"));

        Assert.AreEqual(3,
            knowledge.GetClaims("OUTSIDER").Count);
    }

    [Test]
    public void ZineKnowledge_OldIssueDoesNotOverrideNewCorrection()
    {
        var clock = new SimulationClock();

        var zine = new ZineEditorialCycle(
            "Z1", "E", true, clock);

        zine.RecordAddress("X", "ADDR-OLD-X");

        clock.AdvanceDays(30);

        var first = zine.Issues[0];

        zine.RecordAddress("X", "ADDR-NEW-X");

        clock.AdvanceDays(30);

        var second = zine.Issues[1];

        var knowledge = new ZineIssueKnowledgeRegistry();

        // Lukija saa ensin uudemman numeron.
        knowledge.ReceiveIssue("B", second);

        // Vanhempi numero löytyy myöhemmin.
        knowledge.ReceiveIssue("B", first);

        Assert.AreEqual("ADDR-NEW-X",
            knowledge.GetLatestPublishedAddress("B", "X"));

        // Historiallista vanhaa tietoa ei
        // kuitenkaan poisteta.
        Assert.AreEqual(2,
            knowledge.GetClaims("B").Count);
    }

    [Test]
    public void ZineKnowledge_EmptyIssueAndSnapshotsAreStable()
    {
        var clock = new SimulationClock();

        var zine = new ZineEditorialCycle(
            "Z1", "E", false, clock);

        clock.AdvanceDays(30);

        var emptyIssue = zine.Issues[0];

        var knowledge = new ZineIssueKnowledgeRegistry();

        Assert.IsTrue(
            knowledge.ReceiveIssue("B", emptyIssue));

        Assert.IsFalse(
            knowledge.ReceiveIssue("B", emptyIssue));

        Assert.IsTrue(
            knowledge.HasReceived("B", emptyIssue.ProductId));

        Assert.AreEqual(0,
            knowledge.GetClaims("B").Count);

        zine.RecordAddress("X", "ADDR-X");
        clock.AdvanceDays(30);

        var before = knowledge.GetClaims("B");

        knowledge.ReceiveIssue("B", zine.Issues[1]);

        Assert.AreEqual(0, before.Count);

        Assert.AreEqual(1,
            knowledge.GetClaims("B").Count);

        Assert.IsNull(
            knowledge.GetLatestPublishedAddress("C", "X"));
    }
}
