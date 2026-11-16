
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible92202MultiIssueContactTests
{
    private const string Target = "B070";
    private const string AdvertisedDemo = "D001";

    private static DemoSession Create()
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 0,
            enableRecurringZineIssues: true,
            enableMultiIssueZinePostalInserts: true,
            enableAutonomousMultiIssueContacts: true);
    }

    private static string Reader(DemoSession session)
    {
        return session.InitialZine.DistributorNodeIds
            .Single(id => id.StartsWith(
                "A", StringComparison.Ordinal));
    }

    private static void PublishAddress(
        DemoSession session)
    {
        Assert.IsTrue(
            session.Scenario.Network.TryGetNodeAddress(
                Target, out string address));

        session.RecurringZines.Editorial.RecordAddress(
            Target, address);
    }

    private static void AdvertiseDemoForReader(
        DemoSession session,
        string reader)
    {
        var preference =
            session.AestheticProfiles.GetProfile(
                SixAxisProfileRole.PersonalPreference,
                reader);

        // The test explicitly supplies editorial
        // content. Production code must not infer
        // published claims from private profiles.
        session.RecurringZines.Editorial
            .RecordDemoAdvertisement(
                Target,
                AdvertisedDemo,
                new SixAxisProfile(preference.Mask));
    }

    private static MultiIssueZineContactSelection Selector(
        DemoSession session)
    {
        return new MultiIssueZineContactSelection(
            session.Scenario,
            new MultiIssueZineContactDirectory(
                session.Scenario.Network,
                session.InitialZine.Knowledge),
            session.InitialZine.Knowledge,
            session.AestheticProfiles);
    }

    [Test]
    public void IssueTwo_IsNotAvailableBeforePublication()
    {
        var session = Create();
        string reader = Reader(session);

        PublishAddress(session);
        AdvertiseDemoForReader(session, reader);

        session.Scenario.Clock.AdvanceDays(89);

        Assert.AreEqual(0,
            session.RecurringZines.Issues.Count);

        Assert.IsFalse(Selector(session)
            .GetOpportunities(reader)
            .Any(c => c.TargetNodeId == Target));
    }

    [Test]
    public void AddressOnly_DoesNotInventAnAestheticMotive()
    {
        var session = Create();
        string reader = Reader(session);

        PublishAddress(session);

        session.Scenario.Clock.AdvanceDays(90);

        var issue = session.RecurringZines.Issues.Single();

        Assert.IsTrue(
            session.InitialZine.Knowledge.HasReceived(
                reader, issue.ProductId));

        Assert.IsFalse(Selector(session)
            .GetOpportunities(reader)
            .Any(c => c.TargetNodeId == Target));
    }

    [Test]
    public void PublishedDemoProfile_CreatesRequestOpportunity()
    {
        var session = Create();
        string reader = Reader(session);

        PublishAddress(session);
        AdvertiseDemoForReader(session, reader);

        session.Scenario.Clock.AdvanceDays(90);

        var candidate = Selector(session)
            .GetOpportunities(reader)
            .Single(c => c.TargetNodeId == Target);

        Assert.AreEqual(
            MultiIssueContactIntent.RequestDemo,
            candidate.Intent);

        Assert.AreEqual(
            AdvertisedDemo, candidate.DemoId);

        Assert.GreaterOrEqual(
            candidate.MatchingAxes, 4);

        Assert.AreEqual(
            session.RecurringZines.Issues[0].ProductId,
            candidate.IssueProductId);
    }

    [Test]
    public void MismatchingPublishedDescription_BlocksRequest()
    {
        var session = Create();
        string reader = Reader(session);

        PublishAddress(session);

        var personal =
            session.AestheticProfiles.GetProfile(
                SixAxisProfileRole.PersonalPreference,
                reader);

        // Exact opposite: 0/6 matching axes.
        var published = new SixAxisProfile(
            personal.Mask ^ 63);

        session.RecurringZines.Editorial
            .RecordDemoAdvertisement(
                Target, AdvertisedDemo, published);

        session.Scenario.Clock.AdvanceDays(90);

        Assert.IsFalse(Selector(session)
            .GetOpportunities(reader)
            .Any(c => c.TargetNodeId == Target));
    }

    [Test]
    public void AutonomousCycle_SendsRequestFromLaterIssue()
    {
        var session = Create();
        string reader = Reader(session);

        PublishAddress(session);
        AdvertiseDemoForReader(session, reader);

        session.Scenario.Clock.AdvanceDays(90);

        Assert.IsTrue(
            session.FirstContactCycle.TryInitiate(reader));

        var requests = session.Scenario.Exchange
            .GetShipments()
            .Where(s =>
                s.Package.SourceNodeId == reader &&
                s.Package.TargetNodeId == Target &&
                s.Package.RequestedDemoId ==
                    AdvertisedDemo)
            .ToArray();

        Assert.AreEqual(1, requests.Length);
        Assert.IsNull(
            requests[0].Package.CassetteCopyId);

        Assert.AreEqual(
            PostalDistance.Long,
            requests[0].Distance);
    }
}
