
using NUnit.Framework;
using SEMM91.Domain;

public class ZineContactSelectionTests
{
    private sealed class Fixture
    {
        public readonly Network Network;
        public readonly SimulationClock Clock;
        public readonly ZineEditorialCycle Zine;
        public readonly ZineIssueKnowledgeRegistry Knowledge;
        public readonly ZineIssueInsertDelivery Delivery;
        public readonly ZineContactOutreachPolicy Outreach;

        public Fixture()
        {
            Network = new Network();
            Clock = new SimulationClock();

            foreach (var id in new[] { "E", "A", "B", "C", "D" })
            {
                Network.AddNode(new Node(id));
                Network.AddAddress("ADDR-" + id, "FI");
                Network.PlaceNode(id, "ADDR-" + id);
            }

            Network.AddAddress("ADDR-A-NEW", "SE");

            Network.AddConnection(
                "EB", "E", "B",
                CommunicationLayer.Insert, "zine");

            Zine = new ZineEditorialCycle(
                "Z1", "E", true, Clock);

            Knowledge = new ZineIssueKnowledgeRegistry(
                Network);

            Delivery = new ZineIssueInsertDelivery(
                new InsertDistributionRegistry(),
                Knowledge);

            Outreach = new ZineContactOutreachPolicy(
                Network, Knowledge);
        }

        public ZineEditorialIssue Publish()
        {
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
    public void Contact_SelfDeclaredProfileAppearsInIssue()
    {
        var f = new Fixture();

        f.Zine.RecordSelfDeclaredContact(
            "A", "ADDR-A", true);

        var issue = f.Publish();

        Assert.AreEqual(true,
            issue.Entries[0].SelfDeclaredPublicProfile);

        Assert.AreEqual("A",
            issue.Entries[0].SubjectNodeId);

        Assert.AreEqual("ADDR-A",
            issue.Entries[0].AddressId);
    }

    [Test]
    public void Contact_SelectsMatchingPublicProfiles()
    {
        var f = new Fixture();

        f.Zine.RecordSelfDeclaredContact(
            "A", "ADDR-A", true);

        f.Zine.RecordSelfDeclaredContact(
            "C", "ADDR-C", false);

        // Osoite ilman ilmoitettua profiilia.
        f.Zine.RecordAddress("D", "ADDR-D");

        f.DeliverToB(f.Publish());

        var trueCandidates =
            f.Outreach.GetCandidates("B", true);

        var falseCandidates =
            f.Outreach.GetCandidates("B", false);

        Assert.AreEqual(1, trueCandidates.Count);
        Assert.AreEqual("A",
            trueCandidates[0].TargetNodeId);

        Assert.AreEqual(1, falseCandidates.Count);
        Assert.AreEqual("C",
            falseCandidates[0].TargetNodeId);
    }

    [Test]
    public void Contact_ReadingZineDoesNotStartCommunication()
    {
        var f = new Fixture();

        f.Zine.RecordSelfDeclaredContact(
            "A", "ADDR-A", true);

        f.DeliverToB(f.Publish());

        Assert.IsFalse(
            f.Outreach.HasPendingOutreach("B"));

        Assert.AreEqual(0,
            f.Network.GetOutgoingConnections(
                "B",
                CommunicationLayer.Interpersonal).Count);

        Assert.AreEqual(1,
            f.Outreach.GetCandidates("B", true).Count);
    }

    [Test]
    public void Contact_OnlyOneUnresolvedOutreachPerSender()
    {
        var f = new Fixture();

        f.Zine.RecordSelfDeclaredContact(
            "A", "ADDR-A", true);

        f.Zine.RecordSelfDeclaredContact(
            "D", "ADDR-D", true);

        f.DeliverToB(f.Publish());

        Assert.IsTrue(
            f.Outreach.TryStartOutreach("B", true, "A"));

        Assert.IsFalse(
            f.Outreach.TryStartOutreach("B", true, "D"));

        Assert.IsFalse(
            f.Outreach.CompleteOutreach("B", "D"));

        Assert.IsTrue(
            f.Outreach.HasPendingOutreach("B"));

        Assert.IsTrue(
            f.Outreach.CompleteOutreach("B", "A"));

        Assert.IsTrue(
            f.Outreach.TryStartOutreach("B", true, "D"));
    }

    [Test]
    public void Contact_ExistingOutgoingRelationshipIsExcluded()
    {
        var f = new Fixture();

        f.Network.AddConnection(
            "BA", "B", "A",
            CommunicationLayer.Interpersonal,
            "personal");

        f.Zine.RecordSelfDeclaredContact(
            "A", "ADDR-A", true);

        f.DeliverToB(f.Publish());

        Assert.AreEqual(0,
            f.Outreach.GetCandidates("B", true).Count);

        Assert.IsFalse(
            f.Outreach.TryStartOutreach("B", true, "A"));
    }

    [Test]
    public void Contact_StaleAddressCanStillAttractOutreach()
    {
        var f = new Fixture();

        f.Zine.RecordSelfDeclaredContact(
            "A", "ADDR-A", true);

        var issue = f.Publish();

        // Osoite vanhentuu vasta julkaisemisen
        // jälkeen, koska A muuttaa.
        f.Network.MoveNode("A", "ADDR-A-NEW");

        f.DeliverToB(issue);

        var candidates =
            f.Outreach.GetCandidates("B", true);

        Assert.AreEqual(1, candidates.Count);

        Assert.AreEqual("ADDR-A",
            candidates[0].AddressId);

        f.Network.TryGetNodeAddress(
            "A", out var actual);

        Assert.AreEqual("ADDR-A-NEW", actual);
    }

    [Test]
    public void Contact_LateOldIssueDoesNotRestoreOldProfile()
    {
        var f = new Fixture();

        f.Zine.RecordSelfDeclaredContact(
            "A", "ADDR-A", true);

        var oldIssue = f.Publish();

        f.Network.MoveNode("A", "ADDR-A-NEW");

        f.Zine.RecordSelfDeclaredContact(
            "A", "ADDR-A-NEW", false);

        var newIssue = f.Publish();

        // Numero 2 saapuu ennen numeroa 1.
        f.Knowledge.ReceiveIssue("B", newIssue);
        f.Knowledge.ReceiveIssue("B", oldIssue);

        Assert.AreEqual(0,
            f.Outreach.GetCandidates("B", true).Count);

        var candidates =
            f.Outreach.GetCandidates("B", false);

        Assert.AreEqual(1, candidates.Count);

        Assert.AreEqual("ADDR-A-NEW",
            candidates[0].AddressId);
    }
}
