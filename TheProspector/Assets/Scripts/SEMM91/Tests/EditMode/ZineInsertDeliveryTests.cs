
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class ZineInsertDeliveryTests
{
    private static Network CreateNetwork()
    {
        var network = new Network();

        foreach (string id in new[] { "E", "B", "C", "D" })
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
            "EB2", "E", "B",
            CommunicationLayer.Insert, "zine");

        network.AddConnection(
            "BD", "B", "D",
            CommunicationLayer.Insert, "zine");

        return network;
    }

    private static ZineEditorialIssue CreateIssue(
        bool profile,
        string zineId = "Z1")
    {
        var clock = new SimulationClock();

        var editor = new ZineEditorialCycle(
            zineId, "E", profile, clock);

        editor.RecordProductOpinion(
            "X", "D4", false);

        editor.RecordAddress(
            "X", "ADDR-X");

        clock.AdvanceDays(30);

        return editor.Issues[0];
    }

    [Test]
    public void Insert_TrueIssueReachesOnlyTrueRecipients()
    {
        var network = CreateNetwork();
        var registry = new InsertDistributionRegistry();

        var delivery = new ZineIssueInsertDelivery(registry);
        var issue = CreateIssue(true);

        // B ja D ovat true; C on false.
        bool PrefersTrue(string id) =>
            id == "B" || id == "D";

        var result = delivery.Distribute(
            issue,
            network.GetOutgoingConnections(
                "E", CommunicationLayer.Insert),
            PrefersTrue);

        Assert.AreEqual(1, result.Count);

        Assert.AreEqual("B", result[0].TargetNodeId);
        Assert.AreEqual(issue.ProductId, result[0].ProductId);

        Assert.AreEqual(1,
            registry.GetForRecipient("B").Count);

        Assert.AreEqual(0,
            registry.GetForRecipient("C").Count);

        Assert.AreSame(
            issue,
            delivery.GetIssue(issue.ProductId));
    }

    [Test]
    public void Insert_FalseIssueReachesFalseRecipients()
    {
        var network = CreateNetwork();
        var registry = new InsertDistributionRegistry();

        var delivery = new ZineIssueInsertDelivery(registry);
        var issue = CreateIssue(false);

        bool PrefersTrue(string id) => id == "B";

        var result = delivery.Distribute(
            issue,
            network.GetOutgoingConnections(
                "E", CommunicationLayer.Insert),
            PrefersTrue);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("C", result[0].TargetNodeId);

        Assert.AreEqual(0,
            registry.GetForRecipient("B").Count);

        Assert.AreEqual(1,
            registry.GetForRecipient("C").Count);
    }

    [Test]
    public void Insert_ParallelArcsAndRetriesDoNotDuplicateIssue()
    {
        var network = CreateNetwork();
        var registry = new InsertDistributionRegistry();

        var delivery = new ZineIssueInsertDelivery(registry);
        var issue = CreateIssue(true);

        bool PrefersTrue(string id) => true;

        var first = delivery.Distribute(
            issue,
            network.GetOutgoingConnections(
                "E", CommunicationLayer.Insert),
            PrefersTrue);

        var second = delivery.Distribute(
            issue,
            network.GetOutgoingConnections(
                "E", CommunicationLayer.Insert),
            PrefersTrue);

        // E->B ja E->B rinnakkaisista kaarista
        // kirjataan vain yksi toimitus.
        Assert.AreEqual(2, first.Count);
        Assert.AreEqual(0, second.Count);

        Assert.AreEqual(1,
            registry.GetForRecipient("B").Count);

        Assert.AreEqual(1,
            registry.GetForRecipient("C").Count);
    }

    [Test]
    public void Insert_DistributorCanForwardIndependentIssue()
    {
        var network = CreateNetwork();
        var registry = new InsertDistributionRegistry();

        var delivery = new ZineIssueInsertDelivery(registry);
        var issue = CreateIssue(true);

        bool PrefersTrue(string id) => true;

        delivery.Distribute(
            issue,
            network.GetOutgoingConnections(
                "E", CommunicationLayer.Insert),
            PrefersTrue);

        var forwarded = delivery.Distribute(
            issue,
            network.GetOutgoingConnections(
                "B", CommunicationLayer.Insert),
            PrefersTrue);

        Assert.AreEqual(1, forwarded.Count);

        Assert.AreEqual("B", forwarded[0].SourceNodeId);
        Assert.AreEqual("D", forwarded[0].TargetNodeId);
        Assert.AreEqual(issue.ProductId, forwarded[0].ProductId);

        // B:n edelleenlähetys ei tee zinen
        // toimituksellisesta sisällöstä B:n omaa.
        var recovered = delivery.GetIssue(
            forwarded[0].ProductId);

        Assert.AreEqual("E", recovered.EditorNodeId);
        Assert.AreEqual(2, recovered.Entries.Count);
        Assert.AreEqual(30, recovered.PublishedDay);
    }
}
