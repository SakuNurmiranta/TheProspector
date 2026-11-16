using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible92201MultiIssueTests
{
    private static DemoSession Create()
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 0,
            enableRecurringZineIssues: true,
            enableMultiIssueZinePostalInserts: true);
    }

    private static PostalShipment SendTestLetter(
        DemoSession session,
        string sender,
        string recipient,
        string id)
    {
        var network = session.Scenario.Network;

        Assert.IsTrue(network.TryGetNodeAddress(
            sender, out string senderAddress));

        Assert.IsTrue(network.TryGetNodeAddress(
            recipient, out string recipientAddress));

        network.LearnAddress(
            sender, recipient, recipientAddress);

        network.AddConnection(
            "ROUTE:" + id,
            sender,
            recipient,
            CommunicationLayer.Interpersonal,
            "test-correspondence");

        var route = network.GetOutgoingConnections(
                sender,
                CommunicationLayer.Interpersonal)
            .Single(c => c.Id == "ROUTE:" + id);

        var package = new MessagePackage(
            "PACKAGE:" + id,
            route,
            addressNoticeAddressId: senderAddress);

        Assert.IsTrue(network.TryGetAddress(
            senderAddress, out var from));

        Assert.IsTrue(network.TryGetAddress(
            recipientAddress, out var to));

        return session.Scenario.Exchange.Send(
            "SHIPMENT:" + id,
            package,
            from.CountryId,
            to.CountryId,
            replyAllowanceDays: 5);
    }

    private static string Distributor(DemoSession session)
    {
        var issue = session.RecurringZines.Issues.Last();

        return session.InitialZine.DistributorNodeIds
            .First(id => session.InitialZine.Bulk
                .GetCopies(issue, id) > 0);
    }

    private static string UninformedReader(
        DemoSession session)
    {
        var distributors =
            session.InitialZine.DistributorNodeIds;

        return session.Scenario.NodeIds.First(id =>
            !distributors.Contains(id) &&
            id != session.InitialZine.Issue.EditorNodeId &&
            id != "A002");
    }

    [Test]
    public void SecondIssue_RidesAnExistingLayerTwoLetter()
    {
        var session = Create();

        session.AdvanceDay();
        session.Scenario.Clock.AdvanceDays(89);

        var issue = session.RecurringZines.Issues.Single();

        string sender = Distributor(session);
        string recipient = UninformedReader(session);

        var shipment = SendTestLetter(
            session, sender, recipient, "SECOND");

        Assert.IsTrue(
            session.MultiIssueZinePostalInserts
                .TryGetAttachedIssueId(
                    shipment.Id, out string issueId));

        Assert.AreEqual(issue.ProductId, issueId);

        // The old issue-1 router must not also
        // reserve a second physical insert.
        Assert.IsFalse(
            session.ZinePostalInserts
                .HasAttachment(shipment.Id));

        Assert.AreEqual(
            issue.ProductId,
            session.MultiIssueZinePostalInserts
                .GetAttachedIssueId(shipment.Id));

        Assert.AreEqual(
            25,
            session.InitialZine.Bulk
                .GetTotalCopiesHeld(issue) +
            session.InitialZine.Bulk
                .GetPostalInTransitCopies(issue));
    }

    [Test]
    public void Recipient_LearnsNewIssueOnlyOnOpening()
    {
        var session = Create();

        session.RecurringZines.Editorial.RecordAddress(
            "A002", GetAddress(session, "A002"));

        session.AdvanceDay();
        session.Scenario.Clock.AdvanceDays(89);

        var issue = session.RecurringZines.Issues.Single();

        string sender = Distributor(session);
        string recipient = UninformedReader(session);

        var shipment = SendTestLetter(
            session, sender, recipient, "OPEN");

        Assert.IsFalse(
            session.InitialZine.Knowledge.HasReceived(
                recipient, issue.ProductId));

        int days = shipment.Distance ==
            PostalDistance.Short ? 2 : 5;

        session.Scenario.Clock.AdvanceDays(days);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);

        // Delivery transfers the physical copy,
        // but not its information.
        Assert.IsFalse(
            session.InitialZine.Knowledge.HasReceived(
                recipient, issue.ProductId));

        Assert.IsTrue(
            session.Scenario.Exchange.TryOpenNext(
                recipient, out _, out var opened));

        Assert.AreEqual(
            shipment.Id, opened.ShipmentId);

        Assert.IsTrue(
            session.InitialZine.Knowledge.HasReceived(
                recipient, issue.ProductId));

        Assert.AreEqual(
            25,
            session.InitialZine.Bulk
                .GetTotalCopiesHeld(issue) +
            session.InitialZine.Bulk
                .GetPostalInTransitCopies(issue));
    }

    [Test]
    public void ReadSecondIssue_RevealsNewPublishedAddress()
    {
        var session = Create();

        string subject = "A002";
        string address = GetAddress(session, subject);

        session.RecurringZines.Editorial.RecordAddress(
            subject, address);

        session.AdvanceDay();
        session.Scenario.Clock.AdvanceDays(89);

        var issue = session.RecurringZines.Issues.Single();

        string sender = Distributor(session);
        string reader = UninformedReader(session);

        var directory = new MultiIssueZineContactDirectory(
            session.Scenario.Network,
            session.InitialZine.Knowledge);

        Assert.IsFalse(directory.GetContacts(reader)
            .Any(c => c.SubjectNodeId == subject &&
                      c.IssueProductId == issue.ProductId));

        var shipment = SendTestLetter(
            session, sender, reader, "DISCOVERY");

        session.Scenario.Clock.AdvanceDays(
            shipment.Distance == PostalDistance.Short
                ? 2 : 5);

        Assert.IsFalse(directory.GetContacts(reader)
            .Any(c => c.IssueProductId == issue.ProductId));

        Assert.IsTrue(
            session.Scenario.Exchange.TryOpenNext(
                reader, out _, out _));

        var contact = directory.GetContacts(reader)
            .Single(c =>
                c.SubjectNodeId == subject &&
                c.IssueProductId == issue.ProductId);

        Assert.AreEqual(address, contact.AddressId);

        Assert.IsTrue(
            session.Scenario.Network.TryGetKnownAddress(
                reader, subject, out string known));

        Assert.AreEqual(address, known);
    }

    [Test]
    public void ThirdIssue_IsPreferredOverSecondIssue()
    {
        var session = Create();

        session.AdvanceDay();
        session.Scenario.Clock.AdvanceDays(179);

        Assert.AreEqual(2,
            session.RecurringZines.Issues.Count);

        var second = session.RecurringZines.Issues[0];
        var third = session.RecurringZines.Issues[1];

        string sender = Distributor(session);
        string recipient = UninformedReader(session);

        var shipment = SendTestLetter(
            session, sender, recipient, "THIRD");

        Assert.AreEqual(
            third.ProductId,
            session.MultiIssueZinePostalInserts
                .GetAttachedIssueId(shipment.Id));

        Assert.AreEqual(
            25,
            session.InitialZine.Bulk
                .GetTotalCopiesHeld(second) +
            session.InitialZine.Bulk
                .GetPostalInTransitCopies(second));

        Assert.AreEqual(
            25,
            session.InitialZine.Bulk
                .GetTotalCopiesHeld(third) +
            session.InitialZine.Bulk
                .GetPostalInTransitCopies(third));
    }

    [Test]
    public void SameIssue_IsNotSentTwiceAlongSameDirection()
    {
        var session = Create();

        session.AdvanceDay();
        session.Scenario.Clock.AdvanceDays(89);

        var issue = session.RecurringZines.Issues.Single();

        string sender = Distributor(session);
        string recipient = UninformedReader(session);

        var first = SendTestLetter(
            session, sender, recipient, "FIRST-SEND");

        Assert.AreEqual(
            issue.ProductId,
            session.MultiIssueZinePostalInserts
                .GetAttachedIssueId(first.Id));

        // Another ordinary letter may still be sent.
        // It must not carry the same zine issue again
        // along the same sender -> recipient direction.
        var second = SendTestLetter(
            session, sender, recipient, "SECOND-SEND");

        Assert.IsFalse(
            session.MultiIssueZinePostalInserts
                .TryGetAttachedIssueId(second.Id, out _));
    }

    [Test]
    public void Directory_DoesNotInventUnpublishedProfiles()
    {
        var session = Create();

        session.RecurringZines.Editorial.RecordAddress(
            "A002", GetAddress(session, "A002"));

        session.AdvanceDay();
        session.Scenario.Clock.AdvanceDays(89);

        var issue = session.RecurringZines.Issues.Single();

        string reader = Distributor(session);

        var directory = new MultiIssueZineContactDirectory(
            session.Scenario.Network,
            session.InitialZine.Knowledge);

        Assert.IsTrue(directory.GetContacts(reader).Any(
            c => c.SubjectNodeId == "A002" &&
                 c.IssueProductId == issue.ProductId));

        // Reading an address must not fabricate
        // an issue-scoped aesthetic description.
        Assert.IsFalse(
            session.AestheticProfiles.TryGetProfile(
                SixAxisProfileRole.PublishedContactDescription,
                "A002",
                out _,
                issue.ProductId));
    }

    private static string GetAddress(
        DemoSession session,
        string nodeId)
    {
        Assert.IsTrue(
            session.Scenario.Network.TryGetNodeAddress(
                nodeId, out string address));

        return address;
    }
}