
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible926PostalZineInsertTests
{
    private sealed class Fixture
    {
        public readonly DemoSession Session;
        public readonly InitialZineCirculation Zine;
        public readonly ConfigurableCirculationScenario Scenario;

        public readonly string Sender;
        public readonly string Recipient;

        public Fixture()
        {
            Session = DemoSession.CreateConfigured(
                new CirculationInitializationParameters
                {
                    RandomSeed = 921
                });

            // Finish initial bulk distribution.
            Session.AdvanceDay();

            Zine = Session.InitialZine;
            Scenario = Session.Scenario;

            Sender = Zine.DistributorNodeIds
                .Single(id => id.StartsWith("A"));

            // Select an unconnected A-country resident.
            // Existing policies cannot have sent them mail
            // over an initial friendship route.
            Recipient = Scenario.NodeIds
                .Where(id =>
                    id.StartsWith("A") &&
                    id != Sender &&
                    !Zine.DistributorNodeIds.Contains(id) &&
                    !Session.InitialWorld.Definition.Connections
                        .Any(e => e.RecipientId == id))
                .First();

            Assert.AreEqual(
                5,
                Zine.Bulk.GetCopies(Zine.Issue, Sender));
        }

        public PostalShipment SendCassette(
            string from,
            string to,
            string suffix)
        {
            var network = Scenario.Network;

            string connectionId =
                "TEST-L2:" + suffix;

            network.AddConnection(
                connectionId,
                from,
                to,
                CommunicationLayer.Interpersonal,
                "personal");

            Assert.IsTrue(
                network.TryGetNodeAddress(
                    to, out string address));

            network.LearnAddress(from, to, address);

            string productId = Scenario.ProductIds[0];

            var copy = Scenario.Inventory.RegisterExistingCopy(
                "TEST-TAPE:" + suffix,
                Scenario.GetDemo(productId),
                from);

            var connection = network
                .GetOutgoingConnections(
                    from,
                    CommunicationLayer.Interpersonal)
                .Single(c => c.Id == connectionId);

            var package = new MessagePackage(
                "TEST-PACKAGE:" + suffix,
                connection,
                cassetteCopy: copy);

            return Scenario.Exchange.Send(
                "TEST-SHIPMENT:" + suffix,
                package,
                "A",
                "A",
                replyAllowanceDays: 5);
        }
    }

    [Test]
    public void CassetteShipment_AttachesOneExistingZineCopy()
    {
        var f = new Fixture();

        var shipment = f.SendCassette(
            f.Sender, f.Recipient, "ONE");

        Assert.IsTrue(
            f.Session.ZinePostalInserts.HasAttachment(
                shipment.Id));

        Assert.AreEqual(
            4,
            f.Zine.Bulk.GetCopies(
                f.Zine.Issue, f.Sender));

        Assert.AreEqual(
            1,
            f.Zine.Bulk.GetPostalInTransitCopies(
                f.Zine.Issue));

        Assert.AreEqual(
            25,
            f.Zine.Bulk.GetTotalCopiesHeld(
                f.Zine.Issue)
            + f.Zine.Bulk.GetPostalInTransitCopies(
                f.Zine.Issue));
    }

    [Test]
    public void DeliveryTransfersCopy_ButKnowledgeWaitsForOpening()
    {
        var f = new Fixture();
        var shipment = f.SendCassette(
            f.Sender, f.Recipient, "DELIVERY");

        // Advance the clock only: the delivery event runs,
        // but SimulationWorld has not opened the backlog.
        f.Scenario.Clock.AdvanceDays(
            PostalTimingRules.DomesticDays);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);

        Assert.AreEqual(
            1,
            f.Zine.Bulk.GetCopies(
                f.Zine.Issue, f.Recipient));

        Assert.IsFalse(
            f.Zine.Knowledge.HasReceived(
                f.Recipient, f.Zine.Issue.ProductId));

        Assert.IsTrue(
            f.Scenario.Exchange.TryOpenNext(
                f.Recipient, out _));

        Assert.IsTrue(
            f.Zine.Knowledge.HasReceived(
                f.Recipient, f.Zine.Issue.ProductId));

        foreach (var entry in f.Zine.Issue.Entries)
        {
            if (entry.Kind != ZineEditorialEntryKind.Address)
                continue;

            Assert.AreEqual(
                entry.AddressId,
                f.Zine.Knowledge.GetLatestPublishedAddress(
                    f.Recipient,
                    entry.SubjectNodeId));
        }

        Assert.AreEqual(
            25,
            f.Zine.Bulk.GetTotalCopiesHeld(f.Zine.Issue));
    }

    [Test]
    public void FeedbackOnlyShipment_DoesNotCarryZine()
    {
        var f = new Fixture();

        string connectionId = "TEST-FEEDBACK-ROUTE";

        f.Scenario.Network.AddConnection(
            connectionId,
            f.Sender,
            f.Recipient,
            CommunicationLayer.Interpersonal,
            "personal");

        f.Scenario.Network.TryGetNodeAddress(
            f.Recipient, out string address);

        f.Scenario.Network.LearnAddress(
            f.Sender, f.Recipient, address);

        var route = f.Scenario.Network
            .GetOutgoingConnections(
                f.Sender,
                CommunicationLayer.Interpersonal)
            .Single(c => c.Id == connectionId);

        var package = new MessagePackage(
            "TEST-FEEDBACK-PACKAGE",
            route,
            feedbackProduct: f.Scenario.GetDemo(
                f.Scenario.ProductIds[0]),
            thumb: -1);

        var shipment = f.Scenario.Exchange.Send(
            "TEST-FEEDBACK-SHIPMENT",
            package,
            "A", "A",
            replyAllowanceDays: 5);

        Assert.IsFalse(
            f.Session.ZinePostalInserts.HasAttachment(
                shipment.Id));

        Assert.AreEqual(
            5,
            f.Zine.Bulk.GetCopies(
                f.Zine.Issue, f.Sender));

        Assert.AreEqual(
            0,
            f.Zine.Bulk.GetPostalInTransitCopies(
                f.Zine.Issue));
    }

    
    [Test]
    public void ReturnedCassette_ReturnsAttachedZineToSender()
    {
        var f = new Fixture();

        var shipment = f.SendCassette(
            f.Sender, f.Recipient, "RETURN");

        Assert.IsTrue(
            f.Session.ZinePostalInserts.HasAttachment(
                shipment.Id));

        // Recipient moves after the parcel was dispatched.
        // The package still carries the old postal address.
        var vacant = f.Session.InitialWorld.Definition.Addresses
            .First(a =>
                a.CountryCode == "A" &&
                !f.Scenario.Network.TryGetOccupant(
                    a.Id, out _));

        f.Scenario.Network.MoveNode(
            f.Recipient, vacant.Id);

        Assert.AreEqual(
            4,
            f.Zine.Bulk.GetCopies(
                f.Zine.Issue, f.Sender));

        // First journey and return journey.
        f.Scenario.Clock.AdvanceDays(
            PostalTimingRules.DomesticDays * 2);

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            shipment.Status);

        // World day completion reconciles returned inserts.
        f.Session.AdvanceDay();

        Assert.IsFalse(
            f.Session.ZinePostalInserts.HasAttachment(
                shipment.Id));

        Assert.AreEqual(
            5,
            f.Zine.Bulk.GetCopies(
                f.Zine.Issue, f.Sender));

        Assert.AreEqual(
            0,
            f.Zine.Bulk.GetPostalInTransitCopies(
                f.Zine.Issue));

        Assert.IsFalse(
            f.Zine.Knowledge.HasReceived(
                f.Recipient, f.Zine.Issue.ProductId));

        Assert.AreEqual(
            25,
            f.Zine.Bulk.GetTotalCopiesHeld(f.Zine.Issue));
    }


    [Test]
    public void RecipientCanForwardSameCopyInLaterCassetteExchange()
    {
        var f = new Fixture();
        f.SendCassette(f.Sender, f.Recipient, "FIRST");

        f.Scenario.Clock.AdvanceDays(
            PostalTimingRules.DomesticDays);

        Assert.IsTrue(
            f.Scenario.Exchange.TryOpenNext(
                f.Recipient, out _));

        var next = f.Scenario.NodeIds.First(id =>
            id.StartsWith("A") &&
            id != f.Sender &&
            id != f.Recipient &&
            !f.Zine.DistributorNodeIds.Contains(id) &&
            !f.Zine.Knowledge.HasReceived(
                id, f.Zine.Issue.ProductId));

        var forwarded = f.SendCassette(
            f.Recipient, next, "SECOND");

        Assert.IsTrue(
            f.Session.ZinePostalInserts.HasAttachment(
                forwarded.Id));

        Assert.AreEqual(
            0,
            f.Zine.Bulk.GetCopies(
                f.Zine.Issue, f.Recipient));

        Assert.AreEqual(
            1,
            f.Zine.Bulk.GetPostalInTransitCopies(
                f.Zine.Issue));
    }

    [Test]
    public void PostalInsert_DoesNotCreateNewLayerThreeConnections()
    {
        var f = new Fixture();

        int before = f.Scenario.NodeIds.Sum(id =>
            f.Scenario.Network.GetOutgoingConnections(
                id, CommunicationLayer.Insert).Count);

        f.SendCassette(f.Sender, f.Recipient, "NO-L3");

        int after = f.Scenario.NodeIds.Sum(id =>
            f.Scenario.Network.GetOutgoingConnections(
                id, CommunicationLayer.Insert).Count);

        Assert.AreEqual(before, after);
        Assert.AreEqual(5, after);
    }
}
