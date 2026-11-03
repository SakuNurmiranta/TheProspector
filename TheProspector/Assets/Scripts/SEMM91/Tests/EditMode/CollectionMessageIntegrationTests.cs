
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class CollectionMessageIntegrationTests
{
    private static (
        Network network,
        CassetteInventory inventory,
        NetworkConnection ab,
        NetworkConnection ac,
        CulturalProduct d1,
        CulturalProduct d2,
        CassetteCopy tape
    ) CreateFixture()
    {
        var network = new Network();

        foreach (var id in new[] { "A", "B", "C" })
        {
            network.AddNode(new Node(id));
            network.AddAddress("ADDR-" + id, "FI");
            network.PlaceNode(id, "ADDR-" + id);
        }

        network.AddAddress("ADDR-B-NEW", "SE");

        network.LearnAddress("A", "B", "ADDR-B");
        network.LearnAddress("A", "C", "ADDR-C");

        network.AddConnection(
            "CONN-AB", "A", "B",
            CommunicationLayer.Interpersonal,
            "personal");

        network.AddConnection(
            "CONN-AC", "A", "C",
            CommunicationLayer.Interpersonal,
            "personal");

        var connections = network.GetOutgoingConnections(
            "A", CommunicationLayer.Interpersonal);

        var ab = connections.Single(c => c.Id == "CONN-AB");
        var ac = connections.Single(c => c.Id == "CONN-AC");

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

        return (network, inventory, ab, ac, d1, d2, tape);
    }

    private static BacklogItem DeliverAndTake(
        PostalShipment shipment)
    {
        shipment.ProcessArrival();

        var backlog = new RecipientBacklog();
        backlog.ReceiveDelivered(shipment);

        Assert.IsTrue(backlog.TryTakeNext(
            shipment.Package.TargetNodeId, out var item));

        return item;
    }

    [Test]
    public void Catalog_KnowledgeUpdatesOnlyWhenOpened()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            feedbackProduct: f.d1,
            thumb: +1);

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, package);

        Assert.IsTrue(
            shipment.SenderCollectionProductIds.Contains("D1"));

        var known = new KnownCollectionRegistry(f.network);
        var reception = new MessageReceptionRegistry(known);

        Assert.IsNull(
            known.IsKnownToHold("B", "A", "D1"));

        var item = DeliverAndTake(shipment);

        // Saapuminen ei vielä päivitä tietoa.
        Assert.IsNull(
            known.IsKnownToHold("B", "A", "D1"));

        reception.Open(item);

        Assert.AreEqual(
            true,
            known.IsKnownToHold("B", "A", "D1"));
    }

    [Test]
    public void Catalog_ForwardedDemoRemainsInHistory()
    {
        var f = CreateFixture();

        // A luovuttaa ainoan D1-kasettinsa C:lle.
        f.inventory.RecordTransfer(
            "TAPE1", "A", "C");

        Assert.AreEqual("C", f.tape.HolderNodeId);

        Assert.IsTrue(
            f.inventory.GetEverHeldProductIds("A")
                .Contains("D1"));

        var package = new MessagePackage(
            "MSG1", f.ab,
            feedbackProduct: f.d1,
            thumb: -1);

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, package);

        // A ilmoittaa edelleen vastaanottaneensa D1:n.
        Assert.IsTrue(
            shipment.SenderCollectionProductIds.Contains("D1"));

        var known = new KnownCollectionRegistry(f.network);
        var reception = new MessageReceptionRegistry(known);

        reception.Open(DeliverAndTake(shipment));

        Assert.AreEqual(
            true,
            known.IsKnownToHold("B", "A", "D1"));
    }

    [Test]
    public void Catalog_HeldDemoRemainsListedWithoutCopying()
    {
        var f = CreateFixture();

        // A pitää D1:n hallussaan, mutta ei levitä sitä.
        // Varsinainen hyllytyspäätös toteutetaan myöhemmin.
        var package = new MessagePackage(
            "MSG1", f.ab,
            feedbackProduct: f.d1,
            thumb: -1);

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, package);

        Assert.AreEqual("A", f.tape.HolderNodeId);

        Assert.AreEqual(
            1, shipment.SenderCollectionProductIds.Count);

        Assert.IsTrue(
            shipment.SenderCollectionProductIds.Contains("D1"));
    }

    [Test]
    public void Catalog_DeliveryAndLaterTransferPreserveHistory()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            cassetteCopy: f.tape);

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, package);

        // Lähettäjän historia säilyy,
        // vaikka kasetti poistuu fyysisestä hallusta.
        Assert.IsTrue(
            shipment.SenderCollectionProductIds.Contains("D1"));

        shipment.ProcessArrival();

        Assert.AreEqual("B", f.tape.HolderNodeId);

        Assert.IsTrue(
            f.inventory.GetEverHeldProductIds("B")
                .Contains("D1"));

        // B luovuttaa kasetin edelleen C:lle.
        f.inventory.RecordTransfer(
            "TAPE1", "B", "C");

        Assert.AreEqual("C", f.tape.HolderNodeId);

        Assert.IsTrue(
            f.inventory.GetEverHeldProductIds("B")
                .Contains("D1"));
    }

    [Test]
    public void Catalog_ResendCapturesNewHistory()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            feedbackProduct: f.d1,
            thumb: +1);

        var first = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, package);

        // B on muuttanut pois A:n tuntemasta osoitteesta.
        f.network.MoveNode("B", "ADDR-B-NEW");

        first.ProcessArrival();
        first.ProcessReturn();

        // A vastaanottaa uuden demon palautuksen jälkeen.
        f.inventory.RegisterExistingCopy(
            "TAPE2", f.d2, "A");

        f.network.LearnAddress(
            "A", "B", "ADDR-B-NEW");

        var second = PostalShipment.Dispatch(
            "SHIP2", f.network, f.inventory, package);

        Assert.AreEqual(
            1, first.SenderCollectionProductIds.Count);

        Assert.AreEqual(
            2, second.SenderCollectionProductIds.Count);

        // Ensimmäisen lähetyksen tilannekuva ei muutu.
        Assert.IsFalse(
            first.SenderCollectionProductIds.Contains("D2"));

        Assert.IsTrue(
            second.SenderCollectionProductIds.Contains("D2"));

        var known = new KnownCollectionRegistry(f.network);
        var reception = new MessageReceptionRegistry(known);

        reception.Open(DeliverAndTake(second));

        Assert.AreEqual(
            true,
            known.IsKnownToHold("B", "A", "D2"));
    }

    [Test]
    public void Catalog_DeduplicatesProductsAndSeparatesObservers()
    {
        var f = CreateFixture();

        // Kaksi fyysistä kopiota samasta demosta.
        f.inventory.RegisterExistingCopy(
            "TAPE2", f.d1, "A");

        var packageB = new MessagePackage(
            "MSG-B", f.ab,
            feedbackProduct: f.d1,
            thumb: +1);

        var packageC = new MessagePackage(
            "MSG-C", f.ac,
            feedbackProduct: f.d1,
            thumb: +1);

        var shipmentB = PostalShipment.Dispatch(
            "SHIP-B", f.network, f.inventory, packageB);

        var shipmentC = PostalShipment.Dispatch(
            "SHIP-C", f.network, f.inventory, packageC);

        Assert.AreEqual(
            1, shipmentB.SenderCollectionProductIds.Count);

        Assert.Throws<NotSupportedException>(() =>
            ((ICollection<string>)
                shipmentB.SenderCollectionProductIds)
                .Add("D2"));

        var itemB = DeliverAndTake(shipmentB);
        var itemC = DeliverAndTake(shipmentC);

        var known = new KnownCollectionRegistry(f.network);
        var reception = new MessageReceptionRegistry(known);

        reception.Open(itemB);

        Assert.AreEqual(
            true,
            known.IsKnownToHold("B", "A", "D1"));

        Assert.IsNull(
            known.IsKnownToHold("C", "A", "D1"));

        reception.Open(itemC);

        Assert.AreEqual(
            true,
            known.IsKnownToHold("C", "A", "D1"));
    }
}
