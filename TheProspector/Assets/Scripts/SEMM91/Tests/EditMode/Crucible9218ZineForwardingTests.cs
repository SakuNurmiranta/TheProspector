using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible9218ZineForwardingTests
{
    private static DemoSession Create(
        bool forwarding = true,
        int seed = 921)
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = seed
            },
            fieldDemoIntervalDays: 0,
            enableInitialZineForwarding: forwarding);
    }

    private static string Country(
        DemoSession session,
        string nodeId)
    {
        var network = session.Scenario.Network;

        Assert.IsTrue(network.TryGetNodeAddress(
            nodeId, out string addressId));

        Assert.IsTrue(network.TryGetAddress(
            addressId, out var address));

        return address.CountryId;
    }

    [Test]
    public void DayOne_PreservesOriginalBulkAllocation()
    {
        var session = Create();
        session.AdvanceDay();

        Assert.AreEqual(0,
            session.InitialZine.ForwardedBulkShipments.Count);

        Assert.AreEqual(5,
            session.InitialZine.DistributorNodeIds.Count);

        foreach (string id in
                 session.InitialZine.DistributorNodeIds)
        {
            Assert.AreEqual(5,
                session.InitialZine.Bulk.GetCopies(
                    session.InitialZine.Issue, id));
        }
    }

    [Test]
    public void DayTwo_TransfersFiniteExistingIssues()
    {
        var session = Create();
        session.AdvanceDays(2);

        var zine = session.InitialZine;

        Assert.AreEqual(20,
            zine.ForwardedBulkShipments.Count);

        Assert.AreEqual(25, zine.PrintedCopies);

        Assert.AreEqual(25,
            zine.Bulk.GetTotalCopiesHeld(zine.Issue) +
            zine.Bulk.GetPostalInTransitCopies(zine.Issue));

        foreach (var shipment in zine.ForwardedBulkShipments)
        {
            Assert.AreEqual(1, shipment.CopyCount);

            Assert.AreEqual(
                Country(session, shipment.SenderNodeId),
                Country(session, shipment.RecipientNodeId));

            Assert.IsTrue(zine.Knowledge.HasReceived(
                shipment.RecipientNodeId,
                zine.Issue.ProductId));
        }
    }

    [Test]
    public void CountryA_GainsReadersWithoutPersonalArcs()
    {
        var session = Create();

        int personalBefore = session.Scenario.NodeIds.Sum(
            id => session.Scenario.Network
                .GetOutgoingConnections(
                    id, CommunicationLayer.Interpersonal)
                .Count);

        session.AdvanceDays(2);

        var zine = session.InitialZine;

        var aDistributor = zine.DistributorNodeIds.Single(
            id => Country(session, id) == "A");

        var aRecipients = zine.ForwardedBulkShipments
            .Where(s => s.SenderNodeId == aDistributor)
            .ToList();

        Assert.AreEqual(4, aRecipients.Count);

        Assert.IsTrue(aRecipients.All(s =>
            Country(session, s.RecipientNodeId) == "A"));

        int aReaders = session.Scenario.NodeIds.Count(id =>
            Country(session, id) == "A" &&
            zine.Knowledge.HasReceived(
                id, zine.Issue.ProductId));

        Assert.AreEqual(5, aReaders);

        int personalAfter = session.Scenario.NodeIds.Sum(
            id => session.Scenario.Network
                .GetOutgoingConnections(
                    id, CommunicationLayer.Interpersonal)
                .Count);

        Assert.AreEqual(personalBefore, personalAfter);
    }

    [Test]
    public void LearnedAddresses_StillRequireAestheticEligibility()
    {
        var session = Create();
        session.AdvanceDays(2);

        foreach (string reader in session.Scenario.NodeIds)
        {
            if (!session.InitialZine.Knowledge.HasReceived(
                    reader, session.InitialZine.Issue.ProductId))
                continue;

            foreach (var candidate in
                     session.SixAxisContacts.GetCandidates(reader))
            {
                Assert.GreaterOrEqual(
                    candidate.MatchingAxes, 4);

                Assert.IsTrue(session.Scenario.Network
                    .TryGetKnownAddress(
                        reader,
                        candidate.TargetNodeId,
                        out string known));

                Assert.AreEqual(
                    candidate.PublishedAddressId, known);
            }
        }
    }

    [Test]
    public void Forwarding_IsOptionalAndDoesNotReprint()
    {
        var session = Create(forwarding: false);
        session.AdvanceDays(3);

        Assert.AreEqual(0,
            session.InitialZine.ForwardedBulkShipments.Count);

        Assert.AreEqual(25,
            session.InitialZine.PrintedCopies);

        Assert.AreEqual(25,
            session.InitialZine.Bulk.GetTotalCopiesHeld(
                session.InitialZine.Issue) +
            session.InitialZine.Bulk.GetPostalInTransitCopies(
                session.InitialZine.Issue));
    }

    [Test]
    public void ReadinessReport_IdentifiesCrossBorderBlockers()
    {
        var session = Create();

        var before = ZineFirstContactReadinessReport
            .Capture(session);

        Assert.AreEqual(0, before.CountryAReaders);

        session.AdvanceDays(2);

        var report = ZineFirstContactReadinessReport
            .Capture(session);

        Assert.AreEqual(5, report.CountryAReaders);
        Assert.AreEqual(100, report.Nodes.Count);

        Assert.AreEqual(2, report.Day);

        Assert.IsTrue(report.Nodes.All(n =>
            n.CandidateCount >= n.InternationalCandidates));

        Assert.IsTrue(report.Nodes.All(n =>
            n.SupportedCassetteCount <= n.HeldCassetteCount));

        int day = session.Day;
        int opened = session.OpenedMessageCount;

        var repeated = ZineFirstContactReadinessReport
            .Capture(session);

        Assert.AreEqual(report.ToString(),
            repeated.ToString());

        Assert.AreEqual(day, session.Day);
        Assert.AreEqual(opened, session.OpenedMessageCount);

        TestContext.WriteLine(report.ToString());

        foreach (var node in report.Nodes.Where(n =>
                     n.CountryId == "A" && n.HasIssue))
        {
            TestContext.WriteLine(node.ToString());
        }
    }
    
    [Test]
    [Category("FullSimulation")]
    public void Autonomous180Days_WithForwarding_ReportsRoutes()
    {
        var session = DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 14,
            enableInitialZineForwarding: true);

        session.AdvanceDays(180);

        var routing = PostalCountryRoutingReport.Capture(
            session);

        var readiness = ZineFirstContactReadinessReport.Capture(
            session);

        TestContext.WriteLine("--- Country routing ---");
        TestContext.WriteLine(routing.ToString());

        foreach (var route in routing.Routes)
            TestContext.WriteLine(route.ToString());

        TestContext.WriteLine("--- First-contact readiness ---");
        TestContext.WriteLine(readiness.ToString());

        foreach (var node in readiness.Nodes.Where(n =>
                     n.CountryId == "A" && n.HasIssue))
        {
            TestContext.WriteLine(node.ToString());
        }

        Assert.AreEqual(180, routing.Day);

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

        Assert.GreaterOrEqual(
            readiness.CountryAReaders, 5);
        
        TestContext.WriteLine("--- Country A cassette readiness ---");

        foreach (var node in readiness.Nodes.Where(n =>
                     n.CountryId == "A" &&
                     (n.HeldCassetteCount > 0 ||
                      n.SupportedCassetteCount > 0)))
        {
            var decisions = session.Scenario
                .GetReactionPolicy(node.NodeId).Decisions;

            TestContext.WriteLine(
                $"Node={node.NodeId}" +
                $" Zine={node.HasIssue}" +
                $" Held={node.HeldCassetteCount}" +
                $" SupportedHeld={node.SupportedCassetteCount}" +
                $" SupportDecisions={decisions.Count(d => d.Reaction == ListeningReaction.SupportAndCopy)}" +
                $" TransferDecisions={decisions.Count(d => d.Reaction == ListeningReaction.OpposeAndTransfer)}" +
                $" ShelveDecisions={decisions.Count(d => d.Reaction == ListeningReaction.OpposeAndShelve)}");
        }
    }
}