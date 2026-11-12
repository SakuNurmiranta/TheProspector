using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible9217InternationalRoutingTests
{
    private static DemoSession Create()
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 0);
    }

    private sealed class InternationalFixture
    {
        public readonly DemoSession Session;
        public readonly string Sender;
        public readonly string Recipient;
        public readonly string ProductId;
        public readonly ZineFirstContactPostalService Postal;

        public InternationalFixture()
        {
            Session = Create();

            Sender = Session.InitialWorld.UnlistedDemoOwnerIds
                .First(id => id.StartsWith(
                    "A", StringComparison.Ordinal));

            Recipient = Session.InitialWorld.ZineListedNodeIds
                .First(id => id.StartsWith(
                    "B", StringComparison.Ordinal));

            ProductId = Session.InitialWorld.Definition.Seeds
                .Single(s => s.SourceNodeId == Sender)
                .DemoId;

            // The reader learns the published address.
            // This creates no interpersonal connection.
            Session.InitialZine.Knowledge.ReceiveIssue(
                Sender,
                Session.InitialZine.Issue);

            Postal = new ZineFirstContactPostalService(
                Session.Scenario.Network,
                Session.Scenario.Clock,
                new ZineContactOutreachPolicy(
                    Session.Scenario.Network,
                    Session.InitialZine.Knowledge),
                Session.Scenario.Exchange,
                (sender, target) =>
                    sender == Sender && target == Recipient);
        }

        public PostalShipment Send()
        {
            var original =
                Session.Scenario.GetOriginal(ProductId);

            var copy = Session.Scenario.Inventory.RecordCopy(
                original.Id,
                "9217-FIRST-COPY",
                Sender,
                Sender);

            Assert.IsTrue(Postal.TrySend(
                Sender,
                false,
                Recipient,
                copy,
                "9217-FIRST",
                out var shipment));

            return shipment;
        }

        public BacklogItem OpenRequest()
        {
            Session.Scenario.Clock.AdvanceDays(
                PostalTimingRules.InternationalDays);

            BacklogItem found = null;

            while (Session.Scenario.Exchange.TryOpenNext(
                       Recipient, out _, out var item))
            {
                if (item.Package.Id == "9217-FIRST")
                    found = item;
            }

            Assert.IsNotNull(found);
            return found;
        }

        public bool HasPersonalArc(string from, string to)
        {
            return Session.Scenario.Network
                .GetOutgoingConnections(
                    from,
                    CommunicationLayer.Interpersonal)
                .Any(c => c.TargetNodeId == to);
        }
    }

    [Test]
    public void ConfiguredWorld_HasTwoCountriesButNoInitialCrossBorderFriends()
    {
        var session = Create();

        Assert.AreEqual(30,
            session.InitialWorld.Definition.Nodes.Count(
                n => n.CountryCode == "A"));

        Assert.AreEqual(70,
            session.InitialWorld.Definition.Nodes.Count(
                n => n.CountryCode == "B"));

        foreach (var connection in
                 session.InitialWorld.Definition.Connections)
        {
            bool sameCountry =
                connection.SenderId[0] ==
                connection.RecipientId[0];

            Assert.IsTrue(sameCountry);
        }

        var report =
            PostalCountryRoutingReport.Capture(session);

        Assert.AreEqual(0, report.InternationalShipments);
        Assert.AreEqual(0, report.NonPhysicalShipments);
        int initialShipments =
            session.InitialWorld.Definition.Seeds.Sum(seed =>
                session.Scenario.GetSeedWave(seed.DemoId).Count);

        Assert.AreEqual(initialShipments, report.TotalShipments);
    }

    [Test]
    public void InternationalRequestAndReply_UseFiveDaysEachWay()
    {
        var fixture = new InternationalFixture();
        var shipment = fixture.Send();

        Assert.AreEqual(
            PostalDistance.Long,
            shipment.Distance);

        Assert.IsFalse(fixture.HasPersonalArc(
            fixture.Sender, fixture.Recipient));

        Assert.IsFalse(fixture.HasPersonalArc(
            fixture.Recipient, fixture.Sender));

        fixture.Session.Scenario.Clock.AdvanceDays(4);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            shipment.Status);

        fixture.Session.Scenario.Clock.AdvanceDays(1);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);

        BacklogItem request = null;

        while (fixture.Session.Scenario.Exchange.TryOpenNext(
                   fixture.Recipient, out _, out var item))
        {
            if (item.Package.Id == "9217-FIRST")
                request = item;
        }

        Assert.IsNotNull(request);

        var reply = fixture.Postal.ReplyWithAddress(
            request,
            "9217-REPLY");

        Assert.AreEqual(
            PostalDistance.Long,
            reply.Distance);

        fixture.Session.Scenario.Clock.AdvanceDays(4);

        Assert.IsFalse(fixture.HasPersonalArc(
            fixture.Sender, fixture.Recipient));

        fixture.Session.Scenario.Clock.AdvanceDays(1);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            reply.Status);

        Assert.IsTrue(fixture.HasPersonalArc(
            fixture.Sender, fixture.Recipient));

        Assert.IsTrue(fixture.HasPersonalArc(
            fixture.Recipient, fixture.Sender));

        Assert.AreEqual(
            10,
            fixture.Session.Scenario.Clock.CurrentDay);
    }

    [Test]
    public void RoutingReport_RecordsBothDirectionsAndAddressOnlyReply()
    {
        var fixture = new InternationalFixture();

        fixture.Send();

        var request = fixture.OpenRequest();

        fixture.Postal.ReplyWithAddress(
            request,
            "9217-REPLY");

        fixture.Session.Scenario.Clock.AdvanceDays(5);

        var report = PostalCountryRoutingReport.Capture(
            fixture.Session);

        Assert.AreEqual(1, report.CountRoute("A", "B"));
        Assert.AreEqual(1, report.CountRoute("B", "A"));

        Assert.AreEqual(2, report.InternationalShipments);

        var sent = report.Shipments.Single(s =>
            s.PackageId == "9217-FIRST");

        Assert.IsTrue(sent.IsPhysical);
        Assert.IsTrue(sent.IsFirstContactRequest);
        Assert.AreEqual("A", sent.SourceCountry);
        Assert.AreEqual("B", sent.DestinationCountry);

        var reply = report.Shipments.Single(s =>
            s.PackageId == "9217-REPLY");

        Assert.IsFalse(reply.IsPhysical);
        Assert.IsTrue(reply.IsFirstContactReply);
        Assert.AreEqual("B", reply.SourceCountry);
        Assert.AreEqual("A", reply.DestinationCountry);
        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            reply.Status);

        Assert.AreEqual(
            report.TotalShipments,
            report.PhysicalShipments +
            report.NonPhysicalShipments);
    }

    [Test]
    public void RoutingSnapshot_DoesNotChangeAfterDelivery()
    {
        var fixture = new InternationalFixture();
        fixture.Send();

        var before = PostalCountryRoutingReport.Capture(
            fixture.Session);

        var initial = before.Shipments.Single(s =>
            s.PackageId == "9217-FIRST");

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            initial.Status);

        fixture.Session.Scenario.Clock.AdvanceDays(5);

        var after = PostalCountryRoutingReport.Capture(
            fixture.Session);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            initial.Status);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            after.Shipments.Single(s =>
                s.PackageId == "9217-FIRST").Status);
    }

    [Test]
    public void RoutingCountry_IsHistoricalAddressNotCurrentResidence()
    {
        var fixture = new InternationalFixture();
        fixture.Send();

        var network = fixture.Session.Scenario.Network;

        network.AddAddress("9217-NEW-ADDRESS", "C");
        network.MoveNode(
            fixture.Sender,
            "9217-NEW-ADDRESS");

        var report = PostalCountryRoutingReport.Capture(
            fixture.Session);

        var observation = report.Shipments.Single(s =>
            s.PackageId == "9217-FIRST");

        Assert.AreEqual("A", observation.SourceCountry);
        Assert.AreEqual("B", observation.DestinationCountry);
        Assert.AreEqual(PostalDistance.Long,
            observation.Distance);
    }

    [Test]
    public void Capture_IsReadOnlyAndDeterministic()
    {
        var session = Create();

        int originalDay = session.Day;
        int originalOpened = session.OpenedMessageCount;

        var first =
            PostalCountryRoutingReport.Capture(session);

        var second =
            PostalCountryRoutingReport.Capture(session);

        Assert.AreEqual(first.ToString(), second.ToString());

        CollectionAssert.AreEqual(
            first.Shipments.Select(s => s.ToString()).ToArray(),
            second.Shipments.Select(s => s.ToString()).ToArray());

        Assert.AreEqual(originalDay, session.Day);
        Assert.AreEqual(originalOpened,
            session.OpenedMessageCount);

        Assert.AreEqual(
            first.TotalShipments,
            first.CountRoute("A", "A") +
            first.CountRoute("A", "B") +
            first.CountRoute("B", "A") +
            first.CountRoute("B", "B"));
    }

    [Test]
    [Category("FullSimulation")]
    public void Autonomous180Days_ReportsCountryRoutes()
    {
        var session = DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 14);

        session.AdvanceDays(180);

        var report =
            PostalCountryRoutingReport.Capture(session);

        TestContext.WriteLine(report.ToString());

        foreach (var route in report.Routes)
            TestContext.WriteLine(route.ToString());

        Assert.AreEqual(180, report.Day);
        Assert.AreEqual(100, session.NodeCount);

        Assert.AreEqual(
            report.TotalShipments,
            report.Routes.Sum(r => r.ShipmentCount));

        Assert.AreEqual(
            report.InternationalShipments,
            report.CountRoute("A", "B") +
            report.CountRoute("B", "A"));
    }
}