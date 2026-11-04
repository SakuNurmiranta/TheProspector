
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class TimedPostalExchangeTests
{
    private sealed class Fixture
    {
        public SimulationClock Clock;
        public Network Network;
        public CassetteInventory Inventory;
        public RecipientBacklog Backlog;
        public CulturalCapitalLedger Capital;
        public KnownCollectionRegistry Known;
        public TimedPostalExchange Exchange;

        public NetworkConnection AB;
        public NetworkConnection BA;
        public CulturalProduct D1;
        public CulturalProduct D2;
        public CassetteCopy Original;

        public MessagePackage Message(
            string id,
            NetworkConnection connection,
            string replyTo = null)
        {
            return new MessagePackage(
                id,
                connection,
                feedbackProduct: D1,
                thumb: +1,
                replyToPackageId: replyTo);
        }

        public PostalShipment Send(
            string shipmentId,
            MessagePackage message,
            string sourceCountry = "FI",
            string targetCountry = "FI")
        {
            return Exchange.Send(
                shipmentId,
                message,
                sourceCountry,
                targetCountry,
                5);
        }
    }

    private static Fixture CreateFixture(
        bool internationalB = false)
    {
        var network = new Network();

        foreach (var id in new[] { "A", "B" })
            network.AddNode(new Node(id));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress(
            "ADDR-B",
            internationalB ? "SE" : "FI");

        network.AddAddress(
            "ADDR-B-NEW",
            internationalB ? "SE" : "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");

        network.LearnAddress("A", "B", "ADDR-B");
        network.LearnAddress("B", "A", "ADDR-A");

        network.AddConnection(
            "CONN-AB", "A", "B",
            CommunicationLayer.Interpersonal,
            "personal");

        network.AddConnection(
            "CONN-BA", "B", "A",
            CommunicationLayer.Interpersonal,
            "personal");

        var ab = network.GetOutgoingConnections(
            "A", CommunicationLayer.Interpersonal)
            .Single(c => c.Id == "CONN-AB");

        var ba = network.GetOutgoingConnections(
            "B", CommunicationLayer.Interpersonal)
            .Single(c => c.Id == "CONN-BA");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        field.SpawnArtist(
            "ART1",
            new Dictionary<string, int>
            {
                ["A"] = 10
            },
            new Random(123));

        var d1 = field.CreateDemo("D1", "ART1", true);
        var d2 = field.CreateDemo("D2", "ART1", false);

        var inventory = new CassetteInventory(network);
        var original = inventory.RegisterExistingCopy(
            "TAPE1", d1, "A");

        var clock = new SimulationClock();
        var backlog = new RecipientBacklog();
        var known = new KnownCollectionRegistry(network);

        var capital = new CulturalCapitalLedger(network);
        capital.InitializeNode("A", 10);
        capital.InitializeNode("B", 10);

        var reception = new MessageReceptionRegistry(known);

        var copyRewards = new CopyDeliveryRewardService(
            network, inventory, capital);

        var reciprocalRewards =
            new ReciprocalExchangeRewardService(
                reception, capital);

        var windows = new ReplyWindowRegistry();

        var exchange = new TimedPostalExchange(
            clock,
            network,
            inventory,
            backlog,
            windows,
            copyRewards,
            reciprocalRewards);

        return new Fixture
        {
            Clock = clock,
            Network = network,
            Inventory = inventory,
            Backlog = backlog,
            Capital = capital,
            Known = known,
            Exchange = exchange,
            AB = ab,
            BA = ba,
            D1 = d1,
            D2 = d2,
            Original = original
        };
    }

    [Test]
    public void Timed_DomesticMailArrivesOnDayTwo()
    {
        var f = CreateFixture();

        var shipment = f.Send(
            "SHIP1",
            f.Message("MSG1", f.AB));

        Assert.AreEqual(0, f.Clock.CurrentDay);
        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            shipment.Status);

        f.Clock.AdvanceTo(1);

        Assert.AreEqual(0, f.Backlog.GetPendingCount("B"));

        f.Clock.AdvanceTo(2);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);

        Assert.AreEqual(1, f.Backlog.GetPendingCount("B"));
    }

    [Test]
    public void Timed_InternationalMailArrivesOnDayFive()
    {
        var f = CreateFixture(internationalB: true);

        var shipment = f.Send(
            "SHIP1",
            f.Message("MSG1", f.AB),
            "FI", "SE");

        f.Clock.AdvanceTo(4);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            shipment.Status);

        f.Clock.AdvanceTo(5);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);

        Assert.AreEqual(1, f.Backlog.GetPendingCount("B"));
    }

    [Test]
    public void Timed_CollectionKnowledgeUpdatesOnlyOnOpening()
    {
        var f = CreateFixture();

        f.Send("SHIP1", f.Message("MSG1", f.AB));

        f.Clock.AdvanceTo(2);

        Assert.IsNull(
            f.Known.IsKnownToHold("B", "A", "D1"));

        Assert.IsTrue(
            f.Exchange.TryOpenNext("B", out int reward));

        Assert.AreEqual(0, reward);

        Assert.AreEqual(
            true,
            f.Known.IsKnownToHold("B", "A", "D1"));

        Assert.AreEqual(0, f.Backlog.GetPendingCount("B"));
    }

    [Test]
    public void Timed_CopyRewardOccursAtArrival()
    {
        var f = CreateFixture();

        var copy = f.Inventory.RecordCopy(
            f.Original.Id, "TAPE2", "A", "A");

        var package = new MessagePackage(
            "MSG1",
            f.AB,
            cassetteCopy: copy);

        f.Send("SHIP1", package);

        Assert.AreEqual(10, f.Capital.GetCapital("A"));

        f.Clock.AdvanceTo(1);

        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.IsNull(copy.HolderNodeId);

        f.Clock.AdvanceTo(2);

        // Kopiointipalkkio syntyy toimituksesta,
        // vaikka viesti odottaa edelleen backlogissa.
        Assert.AreEqual(14, f.Capital.GetCapital("A"));
        Assert.AreEqual("B", copy.HolderNodeId);

        Assert.AreEqual(1, f.Backlog.GetPendingCount("B"));
    }

    [Test]
    public void Timed_WrongAddressCausesReturnWithoutReward()
    {
        var f = CreateFixture();

        var copy = f.Inventory.RecordCopy(
            f.Original.Id, "TAPE2", "A", "A");

        var package = new MessagePackage(
            "MSG1",
            f.AB,
            cassetteCopy: copy);

        var shipment = f.Send("SHIP1", package);

        f.Clock.AdvanceTo(1);

        f.Network.MoveNode("B", "ADDR-B-NEW");

        f.Clock.AdvanceTo(2);

        Assert.AreEqual(
            PostalShipmentStatus.Returning,
            shipment.Status);

        Assert.AreEqual(0, f.Backlog.GetPendingCount("B"));
        Assert.AreEqual(10, f.Capital.GetCapital("A"));

        // Paluuposti käyttää samaa kotimaan
        // kahden päivän kulkuaikaa.
        f.Clock.AdvanceTo(4);

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            shipment.Status);

        Assert.AreEqual("A", copy.HolderNodeId);
        Assert.AreEqual(10, f.Capital.GetCapital("A"));
    }

    [Test]
    public void Timed_OnTimeReplyAwardsBothOnOpening()
    {
        var f = CreateFixture();

        f.Send("SHIP1", f.Message("MSG1", f.AB));

        f.Clock.AdvanceTo(2);

        Assert.IsTrue(
            f.Exchange.TryOpenNext("B", out int firstReward));

        Assert.AreEqual(0, firstReward);

        f.Send(
            "SHIP2",
            f.Message("MSG2", f.BA, "MSG1"));

        f.Clock.AdvanceTo(4);

        // Pelkkä vastauksen toimitus ei palkitse.
        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));

        Assert.IsTrue(
            f.Exchange.TryOpenNext("A", out int reward));

        Assert.AreEqual(2, reward);

        Assert.AreEqual(11, f.Capital.GetCapital("A"));
        Assert.AreEqual(11, f.Capital.GetCapital("B"));
    }

    [Test]
    public void Timed_LateReplyAwardsOnlyInitiator()
    {
        var f = CreateFixture();

        f.Send("SHIP1", f.Message("MSG1", f.AB));

        f.Clock.AdvanceTo(2);
        f.Exchange.TryOpenNext("B", out _);

        // MSG1:n määräaika on päivä 7.
        f.Clock.AdvanceTo(8);

        f.Send(
            "SHIP2",
            f.Message("MSG2", f.BA, "MSG1"));

        f.Clock.AdvanceTo(10);

        Assert.IsTrue(
            f.Exchange.TryOpenNext("A", out int reward));

        Assert.AreEqual(1, reward);

        Assert.AreEqual(11, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));
    }

    [Test]
    public void Timed_ReplyTimingIsFrozenAtArrival()
    {
        var f = CreateFixture();

        f.Send("SHIP1", f.Message("MSG1", f.AB));

        f.Clock.AdvanceTo(2);
        f.Exchange.TryOpenNext("B", out _);

        f.Send(
            "SHIP2",
            f.Message("MSG2", f.BA, "MSG1"));

        // Määräaikainen vastaus saapuu päivänä 4.
        f.Clock.AdvanceTo(4);

        // A avaa kirjeensä vasta päivänä 20.
        f.Clock.AdvanceTo(20);

        Assert.IsTrue(
            f.Exchange.TryOpenNext("A", out int reward));

        Assert.AreEqual(2, reward);

        Assert.AreEqual(11, f.Capital.GetCapital("A"));
        Assert.AreEqual(11, f.Capital.GetCapital("B"));
    }

    [Test]
    public void Timed_UnrelatedReverseLetterDoesNotReward()
    {
        var f = CreateFixture();

        f.Send("SHIP1", f.Message("MSG1", f.AB));

        f.Clock.AdvanceTo(2);
        f.Exchange.TryOpenNext("B", out _);

        // Vastakkaissuuntainen viesti ilman
        // ReplyToPackageId-viittausta.
        f.Send("SHIP2", f.Message("MSG2", f.BA));

        f.Clock.AdvanceTo(4);

        Assert.IsTrue(
            f.Exchange.TryOpenNext("A", out int reward));

        Assert.AreEqual(0, reward);

        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));
    }

    [Test]
    public void Timed_SameDayDeliveriesPreserveBacklogOrder()
    {
        var f = CreateFixture();

        // Ensimmäisen viestin lähetyshetkellä
        // A:n kokoelmahistoriassa on vain D1.
        f.Send("SHIP1", f.Message("MSG1", f.AB));

        // A vastaanottaa toisen demon ennen
        // seuraavaa lähettämistä.
        f.Inventory.RegisterExistingCopy(
            "TAPE-D2", f.D2, "A");

        f.Send("SHIP2", f.Message("MSG2", f.AB));

        f.Clock.AdvanceTo(2);

        Assert.AreEqual(2, f.Backlog.GetPendingCount("B"));

        f.Exchange.TryOpenNext("B", out _);

        // Ensimmäisen viestin kokoelmalista
        // ei vielä sisältänyt D2:ta.
        Assert.AreEqual(
            false,
            f.Known.IsKnownToHold("B", "A", "D2"));

        f.Exchange.TryOpenNext("B", out _);

        Assert.AreEqual(
            true,
            f.Known.IsKnownToHold("B", "A", "D2"));

        Assert.AreEqual(0, f.Backlog.GetPendingCount("B"));
    }
}
