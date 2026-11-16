
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible9220RecurringZineTests
{
    private static DemoSession Create(bool enabled = true)
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 0,
            enableRecurringZineIssues: enabled);
    }

    [Test]
    public void RecurringIssues_AreOptional()
    {
        var session = Create(false);

        Assert.IsNull(session.RecurringZines);

        Assert.AreEqual(1,
            session.InitialZine.Issue.Number);

        Assert.AreEqual(0,
            session.InitialZine.Issue.PublishedDay);
    }

    [Test]
    public void SecondIssue_IsPublishedOnDay90()
    {
        var session = Create();

        Assert.IsNotNull(session.RecurringZines);
        Assert.AreEqual(0,
            session.RecurringZines.Issues.Count);

        session.Scenario.Clock.AdvanceDays(89);

        Assert.AreEqual(0,
            session.RecurringZines.Issues.Count);

        session.Scenario.Clock.AdvanceDays(1);

        Assert.AreEqual(1,
            session.RecurringZines.Issues.Count);

        var issue = session.RecurringZines.Issues[0];

        Assert.AreEqual(2, issue.Number);
        Assert.AreEqual(90, issue.PublishedDay);

        Assert.AreEqual(
            session.InitialZine.Issue.ZineId,
            issue.ZineId);

        Assert.AreNotEqual(
            session.InitialZine.Issue.ProductId,
            issue.ProductId);
    }

    [Test]
    public void ThirdIssue_UsesNewNumberAndNewPrintRun()
    {
        var session = Create();

        session.Scenario.Clock.AdvanceDays(180);

        Assert.AreEqual(2,
            session.RecurringZines.Issues.Count);

        var second = session.RecurringZines.Issues[0];
        var third = session.RecurringZines.Issues[1];

        Assert.AreEqual(2, second.Number);
        Assert.AreEqual(3, third.Number);

        Assert.AreEqual(90, second.PublishedDay);
        Assert.AreEqual(180, third.PublishedDay);

        Assert.AreNotEqual(
            second.ProductId,
            third.ProductId);

        Assert.AreEqual(
            25,
            session.InitialZine.Bulk
                .GetPrintedCopies(second));

        Assert.AreEqual(
            25,
            session.InitialZine.Bulk
                .GetPrintedCopies(third));

        // Each issue has its own conserved stock.
        foreach (var issue in new[] { second, third })
        {
            int held = session.InitialZine.Bulk
                .GetTotalCopiesHeld(issue);

            int transit = session.InitialZine.Bulk
                .GetPostalInTransitCopies(issue);

            Assert.AreEqual(25, held + transit);
        }
    }

[Test]
public void Distributors_LearnSecondIssueOnlyWhenPublished()
{
    var session = Create();

    string reader =
        session.InitialZine.DistributorNodeIds[0];

    string editor =
        session.InitialZine.Issue.EditorNodeId;

    string newSubject = "A002";

    Assert.IsTrue(
        session.Scenario.Network.TryGetNodeAddress(
            newSubject, out string address));

    // Issue 1 is initially known only by the editor.
    // Distributors receive it on day 1.
    Assert.IsFalse(
        session.InitialZine.Knowledge.HasReceived(
            reader,
            session.InitialZine.Issue.ProductId));

    // Supply explicit editorial information for issue 2.
    session.RecurringZines.Editorial.RecordAddress(
        newSubject, address);

    session.Scenario.Clock.AdvanceDays(89);

    // Issue 1 has now reached the distributor.
    Assert.IsTrue(
        session.InitialZine.Knowledge.HasReceived(
            reader,
            session.InitialZine.Issue.ProductId));

    // Issue 2 has not yet been published.
    Assert.AreEqual(
        0,
        session.RecurringZines.Issues.Count);

    int knownBeforePublication =
        session.InitialZine.Knowledge
            .ReceivedIssueCount(reader);

    session.Scenario.Clock.AdvanceDays(1);

    // Issue 2 is published on day 90.
    Assert.AreEqual(
        1,
        session.RecurringZines.Issues.Count);

    var issue = session.RecurringZines.Issues[0];

    Assert.AreEqual(2, issue.Number);
    Assert.AreEqual(90, issue.PublishedDay);

    // The distributor receives issue 2.
    Assert.IsTrue(
        session.InitialZine.Knowledge.HasReceived(
            reader, issue.ProductId));

    Assert.AreEqual(
        knownBeforePublication + 1,
        session.InitialZine.Knowledge
            .ReceivedIssueCount(reader));

    // The editor knows their own publication.
    Assert.IsTrue(
        session.InitialZine.Knowledge.HasReceived(
            editor, issue.ProductId));

    // Issue 2 contains the editorially collected address.
    Assert.IsTrue(
        session.InitialZine.Knowledge.GetClaims(reader)
            .Any(c =>
                c.IssueProductId == issue.ProductId &&
                c.Entry.Kind ==
                    ZineEditorialEntryKind.Address &&
                c.Entry.SubjectNodeId == newSubject &&
                c.Entry.AddressId == address));
}

    [Test]
    public void OpenedEditorMail_FeedsNextIssue()
    {
        var session = Create();

        var scenario = session.Scenario;
        var network = scenario.Network;

        const string sender = "A002";

        string editor =
            session.InitialZine.Issue.EditorNodeId;

        Assert.IsTrue(
            network.TryGetNodeAddress(
                sender, out string senderAddress));

        Assert.IsTrue(
            network.TryGetNodeAddress(
                editor, out string editorAddress));

        // Establish an explicit test correspondence.
        // Knowing an editor's address is not evidence
        // of a pre-existing reciprocal relationship.
        network.LearnAddress(
            sender, editor, editorAddress);

        const string routeId = "EDITOR-MAIL-ROUTE";

        network.AddConnection(
            routeId,
            sender,
            editor,
            CommunicationLayer.Interpersonal,
            "editorial-submission");

        var route = network
            .GetOutgoingConnections(
                sender,
                CommunicationLayer.Interpersonal)
            .Single(c => c.Id == routeId);

        var package = new MessagePackage(
            "EDITOR-MAIL-92120",
            route,
            addressNoticeAddressId: senderAddress);

        var shipment = scenario.Exchange.Send(
            "EDITOR-SHIP-92120",
            package,
            "A",
            "B",
            replyAllowanceDays: 5);

        scenario.Clock.AdvanceDays(5);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);

        Assert.IsTrue(
            scenario.Exchange.TryOpenNext(
                editor,
                out _,
                out var opened));

        Assert.AreEqual(
            package.Id, opened.Package.Id);

        Assert.AreEqual(
            1,
            session.RecurringZines
                .MailCollector.CapturedPackages);

        // The manuscript remains pending until day 90.
        Assert.AreEqual(
            1,
            session.RecurringZines
                .Editorial.PendingCount);

        scenario.Clock.AdvanceDays(85);

        var issue = session.RecurringZines.Issues[0];

        Assert.IsTrue(issue.Entries.Any(e =>
            e.Kind == ZineEditorialEntryKind.Address &&
            e.SubjectNodeId == sender &&
            e.AddressId == senderAddress));
    }

    [Test]
    public void PublishedContent_DoesNotLeakIntoLaterIssue()
    {
        var session = Create();

        session.RecurringZines.Editorial.RecordAddress(
            "A002",
            GetAddress(session, "A002"));

        session.Scenario.Clock.AdvanceDays(180);

        var second = session.RecurringZines.Issues[0];
        var third = session.RecurringZines.Issues[1];

        Assert.AreEqual(
            1, second.Entries.Count);

        Assert.AreEqual(
            0, third.Entries.Count);

        Assert.AreEqual(
            1, second.Entries.Count,
            "Publishing issue 3 must not mutate issue 2.");
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
