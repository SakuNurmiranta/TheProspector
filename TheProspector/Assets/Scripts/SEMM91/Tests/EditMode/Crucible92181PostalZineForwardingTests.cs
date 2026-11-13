using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible92181PostalZineForwardingTests
{
    private sealed class Fixture
    {
        public readonly DemoSession Session;
        public readonly ConfigurableCirculationScenario Scenario;
        public readonly InitialZineCirculation Zine;

        private int _serial;

        public Fixture()
        {
            Session = DemoSession.CreateConfigured(
                new CirculationInitializationParameters
                {
                    RandomSeed = 921
                },
                fieldDemoIntervalDays: 0,
                enableInitialZineForwarding: true);

            Session.AdvanceDay();
            Session.FirstContactCycle.Dispose();

            Scenario = Session.Scenario;
            Zine = Session.InitialZine;
        }

        public string Distributor(string country)
        {
            return Zine.DistributorNodeIds
                .Where(id => Country(id) == country)
                .OrderBy(id => id, StringComparer.Ordinal)
                .First();
        }

        public string NewReader(string country,
            params string[] excluded)
        {
            return Scenario.NodeIds.First(id =>
                Country(id) == country &&
                !Zine.DistributorNodeIds.Contains(id) &&
                !excluded.Contains(id));
        }

        private string Country(string nodeId)
        {
            Assert.IsTrue(Scenario.Network.TryGetNodeAddress(
                nodeId, out string addressId));

            Assert.IsTrue(Scenario.Network.TryGetAddress(
                addressId, out var address));

            return address.CountryId;
        }

        public PostalShipment SendLetter(
            string sender,
            string recipient)
        {
            int number = ++_serial;
            string suffix = number.ToString();

            string routeId = "92181-L2:" + suffix;

            Scenario.Network.AddConnection(
                routeId,
                sender,
                recipient,
                CommunicationLayer.Interpersonal,
                "personal");

            Assert.IsTrue(Scenario.Network.TryGetNodeAddress(
                recipient, out string address));

            Scenario.Network.LearnAddress(
                sender, recipient, address);

            var route = Scenario.Network
                .GetOutgoingConnections(
                    sender,
                    CommunicationLayer.Interpersonal)
                .Single(c => c.Id == routeId);

            var package = new MessagePackage(
                "92181-PACKAGE:" + suffix,
                route,
                feedbackProduct: Scenario.GetDemo(
                    Scenario.ProductIds[0]),
                thumb: -1);

            return Scenario.Exchange.Send(
                "92181-SHIPMENT:" + suffix,
                package,
                Country(sender),
                Country(recipient),
                replyAllowanceDays: 5);
        }

        public void DeliverAndOpen(PostalShipment shipment)
        {
            int transitDays =
                shipment.Distance == PostalDistance.Long
                    ? PostalTimingRules.InternationalDays
                    : PostalTimingRules.DomesticDays;

            Scenario.Clock.AdvanceDays(transitDays);

            Assert.AreEqual(
                PostalShipmentStatus.Delivered,
                shipment.Status);

            bool opened = false;

            while (Scenario.Exchange.TryOpenNext(
                       shipment.Package.TargetNodeId,
                       out _,
                       out var item))
            {
                if (item.ShipmentId == shipment.Id)
                    opened = true;
            }

            Assert.IsTrue(opened);
        }

        public void AssertConservation()
        {
            Assert.AreEqual(
                Zine.PrintedCopies,
                Zine.Bulk.GetTotalCopiesHeld(Zine.Issue) +
                Zine.Bulk.GetPostalInTransitCopies(
                    Zine.Issue));
        }
    }

    [Test]
    public void DayTwo_DoesNotUseLayerThreeForIndividualForwarding()
    {
        var f = new Fixture();
        f.Session.AdvanceDay();

        Assert.AreEqual(0,
            f.Zine.ForwardedBulkShipments.Count);

        Assert.AreEqual(5,
            f.Scenario.NodeIds.Sum(id =>
                f.Scenario.Network.GetOutgoingConnections(
                    id, CommunicationLayer.Insert).Count));

        f.AssertConservation();
    }

    [Test]
    public void FeedbackOnlyLetter_CanCarryOneExistingZine()
    {
        var f = new Fixture();
        var sender = f.Distributor("A");
        var recipient = f.NewReader("A");

        var shipment = f.SendLetter(sender, recipient);

        Assert.IsNull(shipment.Package.CassetteCopyId);

        Assert.IsTrue(
            f.Session.ZinePostalInserts.HasAttachment(
                shipment.Id));

        Assert.AreEqual(4,
            f.Zine.Bulk.GetCopies(f.Zine.Issue, sender));

        Assert.AreEqual(1,
            f.Zine.Bulk.GetPostalInTransitCopies(
                f.Zine.Issue));

        f.AssertConservation();
    }

    [Test]
    public void FirstReader_LearnsIssueOnlyWhenLetterIsOpened()
    {
        var f = new Fixture();
        var recipient = f.NewReader("A");

        var shipment = f.SendLetter(
            f.Distributor("A"), recipient);

        f.Scenario.Clock.AdvanceDays(
            PostalTimingRules.DomesticDays);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);

        Assert.IsFalse(f.Zine.Knowledge.HasReceived(
            recipient, f.Zine.Issue.ProductId));

        Assert.AreEqual(1,
            f.Zine.Bulk.GetCopies(
                f.Zine.Issue, recipient));

        Assert.IsTrue(f.Scenario.Exchange.TryOpenNext(
            recipient, out _, out var opened));

        Assert.AreEqual(shipment.Id, opened.ShipmentId);

        Assert.IsTrue(f.Zine.Knowledge.HasReceived(
            recipient, f.Zine.Issue.ProductId));

        Assert.AreEqual(1,
            f.Session.ZinePostalInserts.NewReaderOpenings);

        f.AssertConservation();
    }

    [Test]
    public void PreviouslyInformedReader_CanReceiveAndForwardCopy()
    {
        var f = new Fixture();

        string sender = f.Distributor("B");
        string reader = f.Distributor("A");
        string next = f.NewReader("A");

        // The A distributor has already read this issue
        // through the initial Layer 3 bulk allocation.
        Assert.IsTrue(f.Zine.Knowledge.HasReceived(
            reader, f.Zine.Issue.ProductId));

        var first = f.SendLetter(sender, reader);

        Assert.AreEqual(
            PostalDistance.Long, first.Distance);

        Assert.IsTrue(f.Session.ZinePostalInserts
            .HasAttachment(first.Id));

        f.DeliverAndOpen(first);

        Assert.AreEqual(1,
            f.Session.ZinePostalInserts.RepeatedReaderOpenings);

        // The informed recipient can carry a physical
        // issue onward in an ordinary correspondence.
        var second = f.SendLetter(reader, next);

        Assert.IsTrue(f.Session.ZinePostalInserts
            .HasAttachment(second.Id));

        f.DeliverAndOpen(second);

        Assert.IsTrue(f.Zine.Knowledge.HasReceived(
            next, f.Zine.Issue.ProductId));

        f.AssertConservation();
    }

    [Test]
    public void SamePair_DoesNotBounceSameIssueBackImmediately()
    {
        var f = new Fixture();
        string sender = f.Distributor("A");
        string recipient = f.NewReader("A");

        var outward = f.SendLetter(sender, recipient);
        f.DeliverAndOpen(outward);

        var reverse = f.SendLetter(recipient, sender);

        Assert.IsFalse(
            f.Session.ZinePostalInserts.HasAttachment(
                reverse.Id));

        Assert.AreEqual(1,
            f.Zine.Bulk.GetCopies(
                f.Zine.Issue, recipient));

        f.AssertConservation();
    }

    [Test]
    public void ReturnedLetter_RestoresPhysicalZine()
    {
        var f = new Fixture();
        string sender = f.Distributor("A");
        string recipient = f.NewReader("A");

        var shipment = f.SendLetter(sender, recipient);

        var vacant = f.Session.InitialWorld.Definition
            .Addresses.First(a =>
                a.CountryCode == "A" &&
                !f.Scenario.Network.TryGetOccupant(
                    a.Id, out _));

        f.Scenario.Network.MoveNode(
            recipient, vacant.Id);

        f.Scenario.Clock.AdvanceDays(
            PostalTimingRules.DomesticDays * 2);

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            shipment.Status);

        f.Session.AdvanceDay();

        Assert.IsFalse(
            f.Session.ZinePostalInserts.HasAttachment(
                shipment.Id));

        Assert.IsFalse(f.Zine.Knowledge.HasReceived(
            recipient, f.Zine.Issue.ProductId));

        f.AssertConservation();
    }

    [Test]
    [Category("FullSimulation")]
    public void Autonomous180Days_ReportsLayerTwoZineCirculation()
    {
        var session = DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 14,
            enableInitialZineForwarding: true);

        session.AdvanceDays(180);

        var inserts = session.ZinePostalInserts;
        var zine = session.InitialZine;

        TestContext.WriteLine(
            "Day=" + session.Day +
            " Printed=" + zine.PrintedCopies +
            " Layer2Attachments=" + inserts.AttachedCount +
            " Layer2Delivered=" + inserts.DeliveredCount +
            " NewReaders=" + inserts.NewReaderOpenings +
            " RepeatedReaders=" + inserts.RepeatedReaderOpenings +
            " Returned=" + inserts.ReturnedCount +
            " Pending=" + inserts.PendingCount);

        Assert.AreEqual(180, session.Day);

        Assert.AreEqual(0,
            zine.ForwardedBulkShipments.Count);

        Assert.AreEqual(
            zine.PrintedCopies,
            zine.Bulk.GetTotalCopiesHeld(zine.Issue) +
            zine.Bulk.GetPostalInTransitCopies(zine.Issue));
    }
}