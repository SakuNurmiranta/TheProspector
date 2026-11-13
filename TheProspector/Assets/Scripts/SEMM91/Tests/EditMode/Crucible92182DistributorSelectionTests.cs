using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible92182DistributorSelectionTests
{
    private static DemoSession Create(
        int seed = 921,
        int fieldInterval = 0)
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = seed
            },
            fieldDemoIntervalDays: fieldInterval,
            enableInitialZineForwarding: true);
    }

    private static string CountryADistributor(
        DemoSession session)
    {
        return session.InitialZine.DistributorNodeIds
            .Single(id => id.StartsWith(
                "A", StringComparison.Ordinal));
    }

    private static int PersonalContactCount(
        DemoSession session,
        string nodeId)
    {
        return session.Scenario.Network
            .GetOutgoingConnections(
                nodeId,
                CommunicationLayer.Interpersonal)
            .Select(c => c.TargetNodeId)
            .Distinct(StringComparer.Ordinal)
            .Count();
    }

    private static bool HasInitialDemo(
        DemoSession session,
        string nodeId)
    {
        return session.InitialWorld.Definition.Seeds
            .Any(s => s.SourceNodeId == nodeId);
    }

    private static string ExpectedCountryADistributor(
        DemoSession session)
    {
        return session.InitialWorld.Definition.Nodes
            .Where(n => n.CountryCode == "A")
            .Select(n => n.Id)
            .OrderByDescending(id =>
                HasInitialDemo(session, id) &&
                PersonalContactCount(session, id) > 0)
            .ThenByDescending(id =>
                HasInitialDemo(session, id))
            .ThenByDescending(id =>
                PersonalContactCount(session, id))
            .ThenBy(id => id, StringComparer.Ordinal)
            .First();
    }

    [Test]
    public void CountryA_UsesStructuralDistributorRanking()
    {
        var session = Create();

        string selected = CountryADistributor(session);
        string expected =
            ExpectedCountryADistributor(session);

        Assert.AreEqual(expected, selected);

        TestContext.WriteLine(
            "Selected=" + selected +
            " InitialDemo=" +
            HasInitialDemo(session, selected) +
            " PersonalContacts=" +
            PersonalContactCount(session, selected));
    }

    [Test]
    public void SelectionRule_WorksAcrossDifferentSeeds()
    {
        foreach (int seed in new[] { 921, 2468, 7713 })
        {
            var session = Create(seed);

            Assert.AreEqual(
                ExpectedCountryADistributor(session),
                CountryADistributor(session));
        }
    }

    [Test]
    public void DistributorChange_DoesNotModifyNetworkOrPrintRun()
    {
        var session = Create();

        var zine = session.InitialZine;

        int personalBefore = session.Scenario.NodeIds.Sum(
            id => PersonalContactCount(session, id));

        Assert.AreEqual(5,
            zine.DistributorNodeIds.Count);

        Assert.AreEqual(1,
            zine.DistributorNodeIds.Count(id =>
                id.StartsWith("A", StringComparison.Ordinal)));

        Assert.AreEqual(4,
            zine.DistributorNodeIds.Count(id =>
                id.StartsWith("B", StringComparison.Ordinal)));

        session.AdvanceDay();

        int personalAfter = session.Scenario.NodeIds.Sum(
            id => PersonalContactCount(session, id));

        Assert.AreEqual(personalBefore, personalAfter);

        Assert.AreEqual(25, zine.PrintedCopies);

        foreach (string distributor in
                 zine.DistributorNodeIds)
        {
            Assert.AreEqual(5,
                zine.Bulk.GetCopies(
                    zine.Issue, distributor));
        }

        Assert.AreEqual(0,
            zine.ForwardedBulkShipments.Count);

        Assert.AreEqual(
            25,
            zine.Bulk.GetTotalCopiesHeld(zine.Issue) +
            zine.Bulk.GetPostalInTransitCopies(zine.Issue));
    }

    [Test]
    public void SameSeed_ProducesSameDistributorSelection()
    {
        var first = Create(921);
        var second = Create(921);

        CollectionAssert.AreEqual(
            first.InitialZine.DistributorNodeIds,
            second.InitialZine.DistributorNodeIds);

        Assert.AreEqual(
            CountryADistributor(first),
            CountryADistributor(second));
    }

    [Test]
    [Category("FullSimulation")]
    public void Autonomous180Days_ReportsDistributorActivity()
    {
        var session = Create(
            seed: 921,
            fieldInterval: 14);

        string selectedA =
            CountryADistributor(session);

        bool initialDemo =
            HasInitialDemo(session, selectedA);

        int initialContacts =
            PersonalContactCount(session, selectedA);

        session.AdvanceDays(180);

        var routing =
            PostalCountryRoutingReport.Capture(session);

        var readiness =
            ZineFirstContactReadinessReport.Capture(
                session);

        var inserts = session.ZinePostalInserts;

        TestContext.WriteLine(
            "--- Selected country A distributor ---");

        TestContext.WriteLine(
            "Node=" + selectedA +
            " InitialDemo=" + initialDemo +
            " InitialPersonalContacts=" +
                initialContacts);

        TestContext.WriteLine(
            "--- Distributor postal activity ---");

        foreach (string distributor in
                 session.InitialZine.DistributorNodeIds)
        {
            int outbound = routing.Shipments.Count(s =>
                s.SenderNodeId == distributor);

            int international = routing.Shipments.Count(s =>
                s.SenderNodeId == distributor &&
                s.IsInternational);

            int firstContacts = routing.Shipments.Count(s =>
                s.SenderNodeId == distributor &&
                s.IsFirstContactRequest);

            int attachments =
                inserts.GetAttachedCountFrom(distributor);

            TestContext.WriteLine(
                "Distributor=" + distributor +
                " Outbound=" + outbound +
                " International=" + international +
                " FirstContactRequests=" +
                    firstContacts +
                " ZineAttachments=" + attachments);
        }

        TestContext.WriteLine(
            "--- Postal country routes ---");

        TestContext.WriteLine(routing.ToString());

        foreach (var route in routing.Routes)
            TestContext.WriteLine(route.ToString());

        TestContext.WriteLine(
            "--- Country A readiness ---");

        foreach (var node in readiness.Nodes.Where(n =>
                     n.CountryId == "A" && n.HasIssue))
        {
            TestContext.WriteLine(node.ToString());
        }

        int attachmentSum = session.Scenario.NodeIds.Sum(
            id => inserts.GetAttachedCountFrom(id));

        Assert.AreEqual(180, session.Day);

        Assert.AreEqual(
            inserts.AttachedCount,
            attachmentSum);

        Assert.AreEqual(
            routing.TotalShipments,
            routing.DomesticShipments +
            routing.InternationalShipments);

        Assert.AreEqual(
            session.InitialZine.PrintedCopies,
            session.InitialZine.Bulk.GetTotalCopiesHeld(
                session.InitialZine.Issue) +
            session.InitialZine.Bulk.GetPostalInTransitCopies(
                session.InitialZine.Issue));
    }
}