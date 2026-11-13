using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible92183DemoRequestsTests
{
    private static DemoSession Create()
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 0,
            enableInitialZineForwarding: true);
    }

    private static string CountryAReader(DemoSession session)
    {
        return session.InitialZine.DistributorNodeIds
            .Single(id => id.StartsWith(
                "A", StringComparison.Ordinal));
    }

    [Test]
    public void FirstIssue_PublishesSevenAdvertisedDemos()
    {
        var session = Create();
        var zine = session.InitialZine;

        var advertisements = zine.Issue.Entries
            .Where(e => e.Kind ==
                        ZineEditorialEntryKind.DemoAdvertisement)
            .ToArray();

        Assert.AreEqual(7, advertisements.Length);

        foreach (var advertisement in advertisements)
        {
            Assert.AreEqual(
                zine.AdvertisedDemoIds[
                    advertisement.SubjectNodeId],
                advertisement.ProductId);

            var published =
                session.AestheticProfiles.GetProfile(
                    SixAxisProfileRole.PublishedDemoDescription,
                    advertisement.ProductId,
                    zine.Issue.ProductId);

            var actual =
                session.AestheticProfiles.GetProfile(
                    SixAxisProfileRole.DemoAtmosphere,
                    advertisement.ProductId);

            Assert.IsNotNull(published);
            Assert.IsNotNull(actual);
            Assert.AreNotSame(actual, published);
        }
    }

    [Test]
    public void Reader_LearnsAdvertisementOnlyAfterReceivingIssue()
    {
        var session = Create();
        var reader = CountryAReader(session);

        Assert.AreEqual(0,
            session.InitialZine.Knowledge.GetClaims(reader)
                .Count(c => c.Entry.Kind ==
                            ZineEditorialEntryKind.DemoAdvertisement));

        session.AdvanceDay();

        Assert.AreEqual(7,
            session.InitialZine.Knowledge.GetClaims(reader)
                .Count(c => c.Entry.Kind ==
                            ZineEditorialEntryKind.DemoAdvertisement));
    }

    [Test]
    public void DirectoryIncludesContactsRegardlessOfPersonalMatch()
    {
        var session = Create();
        session.AdvanceDay();

        var reader = CountryAReader(session);

        var directory =
            session.SixAxisContacts.GetDirectoryContacts(reader);

        var personalMatches =
            session.SixAxisContacts.GetCandidates(reader);

        Assert.AreEqual(7, directory.Count);
        Assert.LessOrEqual(
            personalMatches.Count, directory.Count);

        Assert.IsTrue(personalMatches.All(candidate =>
            candidate.MatchingAxes >= 4));
    }

    [Test]
    public void CassettelessRequest_UsesExistingInternationalPost()
    {
        var session = Create();
        session.AdvanceDay();

        var sender = CountryAReader(session);

        var target =
            session.SixAxisContacts
                .GetDirectoryContacts(sender)[0];

        var demoId =
            session.InitialZine.AdvertisedDemoIds[
                target.TargetNodeId];

        Assert.IsTrue(
            session.FirstContactPostal.TrySend(
                sender,
                false,
                target.TargetNodeId,
                null,
                "92183-REQUEST",
                out var shipment,
                demoId));

        Assert.IsNull(shipment.Package.CassetteCopyId);
        Assert.IsNull(shipment.Package.PayloadProductId);

        Assert.AreEqual(
            demoId,
            shipment.Package.RequestedDemoId);

        Assert.AreEqual(
            PostalDistance.Long,
            shipment.Distance);

        session.Scenario.Clock.AdvanceDays(4);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            shipment.Status);

        session.Scenario.Clock.AdvanceDays(1);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);
    }

    [Test]
    public void Request_CanBeAnsweredUsingExistingCassette()
    {
        var session = Create();
        session.AdvanceDay();

        var sender = CountryAReader(session);
        var target =
            session.SixAxisContacts
                .GetDirectoryContacts(sender)[0];

        var demoId =
            session.InitialZine.AdvertisedDemoIds[
                target.TargetNodeId];

        Assert.IsTrue(
            session.FirstContactPostal.TrySend(
                sender,
                false,
                target.TargetNodeId,
                null,
                "92183-REQUEST-REPLY",
                out _,
                demoId));

        session.Scenario.Clock.AdvanceDays(5);

        BacklogItem opened = null;

        while (session.Scenario.Exchange.TryOpenNext(
                   target.TargetNodeId,
                   out _,
                   out var item))
            if (item.Package.Id == "92183-REQUEST-REPLY")
                opened = item;

        Assert.IsNotNull(opened);

        var source = session.Scenario.Inventory
            .GetHeldCopies(target.TargetNodeId)
            .First(c => c.ProductId == demoId);

        var replyCopy = session.Scenario.Inventory.RecordCopy(
            source.Id,
            "92183-REPLY-COPY",
            target.TargetNodeId,
            target.TargetNodeId);

        var reply = session.FirstContactPostal.ReplyWithAddress(
            opened,
            "92183-REPLY",
            replyCopy);

        Assert.AreEqual(
            demoId,
            reply.Package.PayloadProductId);

        Assert.AreEqual(
            PostalDistance.Long,
            reply.Distance);

        session.Scenario.Clock.AdvanceDays(5);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            reply.Status);

        Assert.IsTrue(session.Scenario.Network
            .GetOutgoingConnections(
                sender,
                CommunicationLayer.Interpersonal)
            .Any(c => c.TargetNodeId ==
                      target.TargetNodeId));

        Assert.IsTrue(session.Scenario.Network
            .GetOutgoingConnections(
                target.TargetNodeId,
                CommunicationLayer.Interpersonal)
            .Any(c => c.TargetNodeId == sender));
    }

    [Test]
    public void MessagePackage_CannotBothOfferAndRequestDemo()
    {
        var session = Create();
        var original = session.Scenario.GetOriginal("D001");

        var network = session.Scenario.Network;

        string sender = original.HolderNodeId;

        string recipient = session.Scenario.NodeIds
            .First(id => id != sender);

        const string routeId = "92183-CONFLICT-ROUTE";

        network.AddConnection(
            routeId,
            sender,
            recipient,
            CommunicationLayer.Interpersonal,
            "test");

        var route = network
            .GetOutgoingConnections(
                sender,
                CommunicationLayer.Interpersonal)
            .Single(c => c.Id == routeId);

        Assert.Throws<ArgumentException>(() =>
            new MessagePackage(
                "92183-CONFLICT",
                route,
                cassetteCopy: original,
                requestedDemoId: "D002"));
    }

    [Test]
    [Category("FullSimulation")]
    public void Autonomous180Days_ReportsOfferAndRequestActivity()
    {
        var session = DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 14,
            enableInitialZineForwarding: true);

        session.AdvanceDays(180);

        var cycle = session.FirstContactCycle;
        var routing = PostalCountryRoutingReport.Capture(
            session);

        TestContext.WriteLine(
            "Day=" + session.Day +
            " Offers=" + cycle.OfferSentCount +
            " Requests=" + cycle.RequestSentCount +
            " Fulfilled=" + cycle.RequestFulfilledCount +
            " Declined=" + cycle.RequestDeclinedCount +
            " International=" +
            routing.InternationalShipments);

        foreach (var route in routing.Routes)
            TestContext.WriteLine(route.ToString());

        Assert.AreEqual(
            cycle.SentCount,
            cycle.OfferSentCount + cycle.RequestSentCount);

        Assert.LessOrEqual(
            cycle.RequestFulfilledCount +
            cycle.RequestDeclinedCount,
            cycle.RequestSentCount);
    }

    [Test]
    public void AutonomousReaderWithoutCassette_CanRequestDemo()
    {
        var session = Create();
        session.AdvanceDay();

        var zine = session.InitialZine;
        var contacts = session.SixAxisContacts;
        var scenario = session.Scenario;

        // The test supplies the same published issue to
        // several country A nodes. It does not fabricate
        // an interpersonal connection or a cassette.
        var possibleReaders = scenario.NodeIds
            .Where(id => id.StartsWith(
                "A", StringComparison.Ordinal))
            .Where(id =>
                scenario.Inventory.GetHeldCopies(id).Count == 0)
            .ToArray();

        foreach (var reader in possibleReaders)
            zine.Knowledge.ReceiveIssue(reader, zine.Issue);

        var selectedReader = possibleReaders.First(id =>
        {
            var personal = contacts.GetPersonalProfile(id);

            return contacts.GetDirectoryContacts(id).Any(c =>
                contacts.TryGetAdvertisedDemo(
                    id, c.TargetNodeId, out var demoId) &&
                personal.MatchingAxes(
                    contacts.GetPublishedDemoDescription(
                        demoId)) >= 4);
        });

        Assert.AreEqual(
            0,
            scenario.Inventory.GetHeldCopies(
                selectedReader).Count);

        Assert.IsTrue(
            session.FirstContactCycle.TryInitiate(
                selectedReader));

        var request = scenario.Exchange.GetShipments()
            .Single(s =>
                s.Package.SourceNodeId == selectedReader &&
                s.Package.RequestedDemoId != null);

        Assert.IsNull(request.Package.CassetteCopyId);
        Assert.AreEqual(
            PostalDistance.Long,
            request.Distance);

        Assert.AreEqual(
            1,
            session.FirstContactCycle.RequestSentCount);
    }

    [Test]
    public void AutonomousRequest_CanProduceRequestedCassetteReply()
    {
        var session = Create();
        session.AdvanceDay();

        // Replace only the autonomous decision cycle,
        // not the postal, inventory or listening services.
        session.FirstContactCycle.Dispose();

        var cycle = new ZineAutonomousSixAxisExchangeCycle(
            session.Scenario,
            session.SixAxisContacts,
            session.AestheticProfiles,
            session.FirstContactPostal,
            new Random(92183),
            positiveReplyProbability: 1.0,
            negativeReplyProbability: 1.0);

        try
        {
            var contacts = session.SixAxisContacts;
            var scenario = session.Scenario;

            foreach (var node in scenario.NodeIds.Where(
                         id => id.StartsWith(
                             "A", StringComparison.Ordinal)))
                session.InitialZine.Knowledge.ReceiveIssue(
                    node,
                    session.InitialZine.Issue);

            var reader = scenario.NodeIds
                .Where(id => id.StartsWith(
                    "A", StringComparison.Ordinal))
                .Where(id =>
                    scenario.Inventory.GetHeldCopies(id).Count == 0)
                .First(id =>
                {
                    var preference =
                        contacts.GetPersonalProfile(id);

                    return contacts.GetDirectoryContacts(id).Any(c =>
                        contacts.TryGetAdvertisedDemo(
                            id,
                            c.TargetNodeId,
                            out var demoId) &&
                        preference.MatchingAxes(
                            contacts.GetPublishedDemoDescription(
                                demoId)) >= 4);
                });

            Assert.IsTrue(cycle.TryInitiate(reader));

            var request = scenario.Exchange.GetShipments()
                .Single(s =>
                    s.Package.SourceNodeId == reader &&
                    s.Package.RequestedDemoId != null);

            var responder = request.Package.TargetNodeId;

            scenario.Clock.AdvanceDays(
                PostalTimingRules.InternationalDays);

            while (scenario.Exchange.TryOpenNext(
                       responder, out _, out _))
            {
                // The opened-letter event is handled
                // by the autonomous contact cycle.
            }

            session.AdvanceDay();

            var reply = scenario.Exchange.GetShipments()
                .Single(s =>
                    s.Package.ReplyToPackageId ==
                    request.Package.Id);

            Assert.AreEqual(
                request.Package.RequestedDemoId,
                reply.Package.PayloadProductId);

            Assert.IsNotNull(
                reply.Package.CassetteCopyId);

            Assert.AreEqual(1, cycle.RequestFulfilledCount);

            scenario.Clock.AdvanceDays(
                PostalTimingRules.InternationalDays);

            Assert.AreEqual(
                PostalShipmentStatus.Delivered,
                reply.Status);
        }
        finally
        {
            cycle.Dispose();
        }
    }
}