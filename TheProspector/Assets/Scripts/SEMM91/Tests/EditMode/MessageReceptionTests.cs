
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class MessageReceptionTests
{
    private static (
        Network network,
        CassetteInventory inventory,
        NetworkConnection ab,
        CassetteCopy tape,
        CulturalProduct d1,
        CulturalProduct d2,
        Node b
    ) CreateFixture()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        var b = new Node("B");
        network.AddNode(b);

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");

        network.LearnAddress("A", "B", "ADDR-B");

        network.AddConnection(
            "CONN-AB",
            "A",
            "B",
            CommunicationLayer.Interpersonal,
            "personal");

        var ab = network.GetOutgoingConnections(
            "A", CommunicationLayer.Interpersonal)[0];

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
        var tape = inventory.RegisterExistingCopy(
            "TAPE1", d1, "A");

        return (network, inventory, ab, tape, d1, d2, b);
    }

    private static BacklogItem DeliverAndTake(
        Network network,
        CassetteInventory inventory,
        MessagePackage package,
        string shipmentId)
    {
        var shipment = PostalShipment.Dispatch(
            shipmentId, network, inventory, package);

        shipment.ProcessArrival();

        var backlog = new RecipientBacklog();
        backlog.ReceiveDelivered(shipment);

        Assert.IsTrue(backlog.TryTakeNext(
            package.TargetNodeId, out var item));

        return item;
    }

    [Test]
    public void Reception_OpeningCassetteCreatesPendingReview()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab, cassetteCopy: f.tape);

        var item = DeliverAndTake(
            f.network, f.inventory, package, "SHIP1");

        var reception = new MessageReceptionRegistry();
        reception.Open(item);

        Assert.AreEqual(
            ProductReviewState.Pending,
            reception.GetReviewState("B", "D1"));

        Assert.IsNull(
            reception.GetPersonalOpinion("B", "D1"));

        var sir = new SIRStateRegistry();

        Assert.AreEqual(
            SIRState.S,
            sir.GetNodeState(f.b, f.d1));
    }

    [Test]
    public void Reception_FeedbackOnlyDoesNotImplyListening()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            feedbackProduct: f.d1,
            thumb: -1);

        var item = DeliverAndTake(
            f.network, f.inventory, package, "SHIP1");

        var reception = new MessageReceptionRegistry();
        reception.Open(item);

        Assert.AreEqual(
            ProductReviewState.NotReceived,
            reception.GetReviewState("B", "D1"));

        var claims = reception.GetFeedback("B");

        Assert.AreEqual(1, claims.Count);
        Assert.AreEqual("A", claims[0].SourceNodeId);
        Assert.AreEqual("D1", claims[0].ProductId);
        Assert.AreEqual(-1, claims[0].Thumb);

        Assert.IsNull(
            reception.GetPersonalOpinion("B", "D1"));
    }

    [Test]
    public void Reception_PayloadAndFeedbackMayConcernDifferentDemos()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            cassetteCopy: f.tape,
            feedbackProduct: f.d2,
            thumb: +1);

        var item = DeliverAndTake(
            f.network, f.inventory, package, "SHIP1");

        var reception = new MessageReceptionRegistry();
        reception.Open(item);

        Assert.AreEqual(
            ProductReviewState.Pending,
            reception.GetReviewState("B", "D1"));

        Assert.AreEqual(
            ProductReviewState.NotReceived,
            reception.GetReviewState("B", "D2"));

        var claims = reception.GetFeedback("B");

        Assert.AreEqual("D2", claims[0].ProductId);
        Assert.AreEqual(+1, claims[0].Thumb);
    }

    [Test]
    public void Reception_PersonalOpinionRequiresExplicitReview()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab, cassetteCopy: f.tape);

        var item = DeliverAndTake(
            f.network, f.inventory, package, "SHIP1");

        var reception = new MessageReceptionRegistry();
        reception.Open(item);

        // Mieltymystä ei ole vielä alustettu.
        Assert.Throws<InvalidOperationException>(() =>
            reception.Review(f.b, f.d1));

        Assert.AreEqual(
            ProductReviewState.Pending,
            reception.GetReviewState("B", "D1"));

        f.b.SetPreference(false);

        // D1:n ominaisuus on true.
        bool opinion = reception.Review(f.b, f.d1);

        Assert.IsFalse(opinion);

        Assert.AreEqual(
            ProductReviewState.Reviewed,
            reception.GetReviewState("B", "D1"));

        Assert.AreEqual(
            false,
            reception.GetPersonalOpinion("B", "D1"));

        // D2:ta ei ole vastaanotettu kasettina.
        Assert.Throws<InvalidOperationException>(() =>
            reception.Review(f.b, f.d2));
    }

    [Test]
    public void Reception_DuplicateShipmentCannotBeOpenedTwice()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            feedbackProduct: f.d1,
            thumb: +1);

        var first = DeliverAndTake(
            f.network, f.inventory, package, "SHIP1");

        var reception = new MessageReceptionRegistry();

        reception.Open(first);

        Assert.Throws<InvalidOperationException>(() =>
            reception.Open(first));

        // Saman paketin uusi postilähetys on eri tapahtuma.
        var second = DeliverAndTake(
            f.network, f.inventory, package, "SHIP2");

        reception.Open(second);

        Assert.AreEqual(2, reception.GetFeedback("B").Count);
    }

    [Test]
    public void Reception_FeedbackPreservesOrderAndSource()
    {
        var f = CreateFixture();

        var positive = new MessagePackage(
            "MSG1", f.ab,
            feedbackProduct: f.d1,
            thumb: +1);

        var negative = new MessagePackage(
            "MSG2", f.ab,
            feedbackProduct: f.d2,
            thumb: -1);

        var first = DeliverAndTake(
            f.network, f.inventory, positive, "SHIP1");

        var second = DeliverAndTake(
            f.network, f.inventory, negative, "SHIP2");

        var reception = new MessageReceptionRegistry();

        reception.Open(first);
        reception.Open(second);

        var claims = reception.GetFeedback("B");

        Assert.AreEqual(2, claims.Count);

        Assert.AreEqual("SHIP1", claims[0].ShipmentId);
        Assert.AreEqual("MSG1", claims[0].MessageId);
        Assert.AreEqual("D1", claims[0].ProductId);
        Assert.AreEqual(+1, claims[0].Thumb);

        Assert.AreEqual("SHIP2", claims[1].ShipmentId);
        Assert.AreEqual("D2", claims[1].ProductId);
        Assert.AreEqual(-1, claims[1].Thumb);

        Assert.AreEqual("A", claims[1].SourceNodeId);
    }
}
