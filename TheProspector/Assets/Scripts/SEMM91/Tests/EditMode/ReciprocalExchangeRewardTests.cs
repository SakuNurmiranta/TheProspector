
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class ReciprocalExchangeRewardTests
{
    private sealed class Fixture
    {
        public Network Network;
        public CassetteInventory Inventory;
        public CulturalCapitalLedger Capital;
        public ReciprocalExchangeRewardService Exchanges;

        public NetworkConnection AB;
        public NetworkConnection BA;
        public NetworkConnection BC;

        public CulturalProduct Demo;

        public BacklogItem Deliver(
            string shipmentId,
            MessagePackage package)
        {
            var shipment = PostalShipment.Dispatch(
                shipmentId, Network, Inventory, package);

            shipment.ProcessArrival();

            Assert.AreEqual(
                PostalShipmentStatus.Delivered,
                shipment.Status);

            var backlog = new RecipientBacklog();
            backlog.ReceiveDelivered(shipment);

            Assert.IsTrue(backlog.TryTakeNext(
                package.TargetNodeId, out var item));

            return item;
        }

        public MessagePackage Message(
            string id,
            NetworkConnection connection,
            string replyTo = null)
        {
            return new MessagePackage(
                id,
                connection,
                feedbackProduct: Demo,
                thumb: +1,
                replyToPackageId: replyTo);
        }
    }

    private static Fixture CreateFixture(
        int capitalA = 10,
        int capitalB = 10)
    {
        var network = new Network();

        foreach (var id in new[] { "A", "B", "C" })
        {
            network.AddNode(new Node(id));
            network.AddAddress("ADDR-" + id, "FI");
            network.PlaceNode(id, "ADDR-" + id);
        }

        network.LearnAddress("A", "B", "ADDR-B");
        network.LearnAddress("B", "A", "ADDR-A");
        network.LearnAddress("B", "C", "ADDR-C");

        network.AddConnection(
            "CONN-AB", "A", "B",
            CommunicationLayer.Interpersonal,
            "personal");

        network.AddConnection(
            "CONN-BA", "B", "A",
            CommunicationLayer.Interpersonal,
            "personal");

        network.AddConnection(
            "CONN-BC", "B", "C",
            CommunicationLayer.Interpersonal,
            "personal");

        var ab = network.GetOutgoingConnections(
            "A", CommunicationLayer.Interpersonal)
            .Single(c => c.Id == "CONN-AB");

        var ba = network.GetOutgoingConnections(
            "B", CommunicationLayer.Interpersonal)
            .Single(c => c.Id == "CONN-BA");

        var bc = network.GetOutgoingConnections(
            "B", CommunicationLayer.Interpersonal)
            .Single(c => c.Id == "CONN-BC");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        field.SpawnArtist(
            "ART1",
            new Dictionary<string, int>
            {
                ["A"] = 10
            },
            new Random(123));

        var demo = field.CreateDemo(
            "D1", "ART1", true);

        var inventory = new CassetteInventory(network);

        var capital = new CulturalCapitalLedger(network);
        capital.InitializeNode("A", capitalA);
        capital.InitializeNode("B", capitalB);
        capital.InitializeNode("C", 10);

        var reception = new MessageReceptionRegistry();

        return new Fixture
        {
            Network = network,
            Inventory = inventory,
            Capital = capital,
            Exchanges =
                new ReciprocalExchangeRewardService(
                    reception, capital),
            AB = ab,
            BA = ba,
            BC = bc,
            Demo = demo
        };
    }

    [Test]
    public void Exchange_OnTimeReplyAwardsBoth()
    {
        var f = CreateFixture();

        var original = f.Deliver(
            "SHIP1", f.Message("MSG1", f.AB));

        Assert.AreEqual(0, f.Exchanges.Open(original));

        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));

        var reply = f.Deliver(
            "SHIP2",
            f.Message("MSG2", f.BA, "MSG1"));

        Assert.AreEqual(
            2,
            f.Exchanges.Open(reply, ReplyTiming.OnTime));

        Assert.AreEqual(11, f.Capital.GetCapital("A"));
        Assert.AreEqual(11, f.Capital.GetCapital("B"));
    }

    [Test]
    public void Exchange_DeliveryAloneDoesNotAward()
    {
        var f = CreateFixture();

        // B saa viestin ja se poistetaan backlogista,
        // mutta viestiä ei ole vielä avattu.
        f.Deliver(
            "SHIP1", f.Message("MSG1", f.AB));

        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));

        // B:n vastaukseksi merkitty viesti saapuu A:lle.
        // Alkuperäistä viestiä ei kuitenkaan ole avattu.
        var reply = f.Deliver(
            "SHIP2",
            f.Message("MSG2", f.BA, "MSG1"));

        Assert.AreEqual(
            0,
            f.Exchanges.Open(reply, ReplyTiming.OnTime));

        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));
    }

    [Test]
    public void Exchange_UnrelatedReverseMessageDoesNotAward()
    {
        var f = CreateFixture();

        var first = f.Deliver(
            "SHIP1", f.Message("MSG1", f.AB));

        var second = f.Deliver(
            "SHIP2", f.Message("MSG2", f.BA));

        f.Exchanges.Open(first);
        f.Exchanges.Open(second);

        // Kaksi vastakkaissuuntaista kirjettä eivät
        // yksin muodosta vahvistettua vastaussykliä.
        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));
    }

    [Test]
    public void Exchange_LateReplyAwardsOnlyInitiator()
    {
        var f = CreateFixture();

        f.Exchanges.Open(f.Deliver(
            "SHIP1", f.Message("MSG1", f.AB)));

        var reply = f.Deliver(
            "SHIP2",
            f.Message("MSG2", f.BA, "MSG1"));

        Assert.AreEqual(
            1,
            f.Exchanges.Open(reply, ReplyTiming.Late));

        Assert.AreEqual(11, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));
    }

    [Test]
    public void Exchange_WrongDirectionDoesNotAward()
    {
        var f = CreateFixture();

        f.Exchanges.Open(f.Deliver(
            "SHIP1", f.Message("MSG1", f.AB)));

        // B lähettää C:lle viestin, joka väittää
        // vastaavansa A:n alkuperäiseen kirjeeseen.
        var unrelated = f.Deliver(
            "SHIP2",
            f.Message("MSG2", f.BC, "MSG1"));

        Assert.AreEqual(
            0,
            f.Exchanges.Open(
                unrelated, ReplyTiming.OnTime));

        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));
        Assert.AreEqual(10, f.Capital.GetCapital("C"));
    }

    [Test]
    public void Exchange_SecondReplyDoesNotRewardSameRequest()
    {
        var f = CreateFixture();

        f.Exchanges.Open(f.Deliver(
            "SHIP1", f.Message("MSG1", f.AB)));

        var reply1 = f.Deliver(
            "SHIP2",
            f.Message("MSG2", f.BA, "MSG1"));

        var reply2 = f.Deliver(
            "SHIP3",
            f.Message("MSG3", f.BA, "MSG1"));

        Assert.AreEqual(
            2,
            f.Exchanges.Open(reply1, ReplyTiming.OnTime));

        Assert.AreEqual(
            0,
            f.Exchanges.Open(reply2, ReplyTiming.OnTime));

        Assert.AreEqual(11, f.Capital.GetCapital("A"));
        Assert.AreEqual(11, f.Capital.GetCapital("B"));
    }

    [Test]
    public void Exchange_NewReplyCycleCanAwardAgain()
    {
        var f = CreateFixture();

        // A -> B
        f.Exchanges.Open(f.Deliver(
            "SHIP1", f.Message("MSG1", f.AB)));

        // B -> A, vastaus MSG1:een.
        f.Exchanges.Open(
            f.Deliver(
                "SHIP2",
                f.Message("MSG2", f.BA, "MSG1")),
            ReplyTiming.OnTime);

        // A -> B, vastaus MSG2:een.
        f.Exchanges.Open(
            f.Deliver(
                "SHIP3",
                f.Message("MSG3", f.AB, "MSG2")),
            ReplyTiming.OnTime);

        Assert.AreEqual(12, f.Capital.GetCapital("A"));
        Assert.AreEqual(12, f.Capital.GetCapital("B"));
    }

    [Test]
    public void Exchange_UnknownTimingRejectsEligibleReply()
    {
        var f = CreateFixture();

        f.Exchanges.Open(f.Deliver(
            "SHIP1", f.Message("MSG1", f.AB)));

        var reply = f.Deliver(
            "SHIP2",
            f.Message("MSG2", f.BA, "MSG1"));

        // Palkkiota ei arvata määräaikaiseksi,
        // jos viiveen tilaa ei ole ratkaistu.
        Assert.Throws<InvalidOperationException>(() =>
            f.Exchanges.Open(reply));

        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));

        // Sama viesti voidaan käsitellä, kun ajoitus
        // on selvitetty.
        Assert.AreEqual(
            2,
            f.Exchanges.Open(reply, ReplyTiming.OnTime));
    }

    [Test]
    public void Exchange_OverflowDoesNotAwardEitherNode()
    {
        var f = CreateFixture(
            capitalA: int.MaxValue,
            capitalB: 10);

        f.Exchanges.Open(f.Deliver(
            "SHIP1", f.Message("MSG1", f.AB)));

        var reply = f.Deliver(
            "SHIP2",
            f.Message("MSG2", f.BA, "MSG1"));

        Assert.Throws<OverflowException>(() =>
            f.Exchanges.Open(reply, ReplyTiming.OnTime));

        Assert.AreEqual(
            int.MaxValue,
            f.Capital.GetCapital("A"));

        Assert.AreEqual(10, f.Capital.GetCapital("B"));
    }

    [Test]
    public void Exchange_OpenedMessageCannotBeProcessedTwice()
    {
        var f = CreateFixture();

        var item = f.Deliver(
            "SHIP1", f.Message("MSG1", f.AB));

        Assert.AreEqual(0, f.Exchanges.Open(item));

        Assert.Throws<InvalidOperationException>(() =>
            f.Exchanges.Open(item));
    }
}
