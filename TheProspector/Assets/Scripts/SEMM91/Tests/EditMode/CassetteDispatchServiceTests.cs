
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class CassetteDispatchServiceTests
{
    private static (
        Network network,
        CassetteInventory inventory,
        KnownCollectionRegistry known,
        CassetteDispatchService dispatch,
        NetworkConnection ab,
        NetworkConnection cb,
        CulturalProduct demo,
        CassetteCopy tapeA1,
        CassetteCopy tapeA2,
        CassetteCopy tapeC
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
        network.LearnAddress("C", "B", "ADDR-B");

        network.AddConnection(
            "CONN-AB", "A", "B",
            CommunicationLayer.Interpersonal,
            "personal");

        network.AddConnection(
            "CONN-CB", "C", "B",
            CommunicationLayer.Interpersonal,
            "personal");

        var ab = network.GetOutgoingConnections(
            "A", CommunicationLayer.Interpersonal)[0];

        var cb = network.GetOutgoingConnections(
            "C", CommunicationLayer.Interpersonal)[0];

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        field.SpawnArtist(
            "ART1",
            new Dictionary<string, int>
            {
                ["A"] = 10
            },
            new Random(123));

        var demo = field.CreateDemo("D1", "ART1", true);

        var inventory = new CassetteInventory(network);

        var tapeA1 = inventory.RegisterExistingCopy(
            "TAPE-A1", demo, "A");

        var tapeA2 = inventory.RegisterExistingCopy(
            "TAPE-A2", demo, "A");

        var tapeC = inventory.RegisterExistingCopy(
            "TAPE-C", demo, "C");

        var known = new KnownCollectionRegistry(network);

        var dispatch = new CassetteDispatchService(
            network, inventory, known);

        return (
            network, inventory, known, dispatch,
            ab, cb, demo, tapeA1, tapeA2, tapeC);
    }

    [Test]
    public void Dispatch_UnknownCollectionDoesNotBlockOffer()
    {
        var f = CreateFixture();

        // Simulaattori tietää, että B on omistanut D1:n.
        f.inventory.RegisterExistingCopy(
            "TAPE-B", f.demo, "B");

        // A ei ole saanut siitä tietoa.
        Assert.IsNull(
            f.known.IsKnownToHold("A", "B", "D1"));

        Assert.IsTrue(
            f.dispatch.CanOfferDemo("A", "B", "D1"));
    }

    [Test]
    public void Dispatch_KnownHistoricalOwnershipBlocksOffer()
    {
        var f = CreateFixture();

        var tapeB = f.inventory.RegisterExistingCopy(
            "TAPE-B", f.demo, "B");

        // B on sittemmin luovuttanut kasettinsa C:lle.
        f.inventory.RecordTransfer(
            tapeB.Id, "B", "C");

        // A saa B:n kokoelmahistorian.
        f.known.RecordSnapshot(
            "A", "B",
            f.inventory.GetEverHeldProductIds("B"));

        Assert.IsFalse(
            f.dispatch.CanOfferDemo("A", "B", "D1"));

        var package = new MessagePackage(
            "MSG1", f.ab,
            cassetteCopy: f.tapeA1);

        Assert.Throws<InvalidOperationException>(() =>
            f.dispatch.Dispatch("SHIP1", package));

        // Estetty lähetys ei muuta fyysistä haltijuutta.
        Assert.AreEqual("A", f.tapeA1.HolderNodeId);
        Assert.IsNull(f.tapeA1.InTransitShipmentId);
    }

    [Test]
    public void Dispatch_PendingAndReturningPreventSecondOffer()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            cassetteCopy: f.tapeA1);

        var first = f.dispatch.Dispatch(
            "SHIP1", package);

        Assert.IsFalse(
            f.dispatch.CanOfferDemo("A", "B", "D1"));

        // B ehtii muuttaa ennen toimitusyritystä.
        f.network.MoveNode("B", "ADDR-B-NEW");

        first.ProcessArrival();

        Assert.AreEqual(
            PostalShipmentStatus.Returning,
            first.Status);

        // Kasetti on yhä postissa.
        Assert.IsFalse(
            f.dispatch.CanOfferDemo("A", "B", "D1"));

        // A:lla on toinen D1-kopio, mutta
        // tämä ei oikeuta uuteen lähetykseen.
        Assert.AreEqual("A", f.tapeA2.HolderNodeId);
    }

    [Test]
    public void Dispatch_ReturnedPackageCanBeRetried()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            cassetteCopy: f.tapeA1);

        var first = f.dispatch.Dispatch(
            "SHIP1", package);

        f.network.MoveNode("B", "ADDR-B-NEW");

        first.ProcessArrival();
        first.ProcessReturn();

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            first.Status);

        Assert.IsTrue(
            f.dispatch.CanOfferDemo("A", "B", "D1"));

        // A saa myöhemmin B:n uuden osoitteen.
        f.network.LearnAddress(
            "A", "B", "ADDR-B-NEW");

        var second = f.dispatch.Dispatch(
            "SHIP2", package);

        Assert.AreEqual(
            "ADDR-B-NEW",
            second.DestinationAddressId);

        second.ProcessArrival();

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            second.Status);

        Assert.AreEqual("B", f.tapeA1.HolderNodeId);
    }

    [Test]
    public void Dispatch_DeliveredDemoBlocksFutureOffer()
    {
        var f = CreateFixture();

        var firstPackage = new MessagePackage(
            "MSG1", f.ab,
            cassetteCopy: f.tapeA1);

        var shipment = f.dispatch.Dispatch(
            "SHIP1", firstPackage);

        shipment.ProcessArrival();

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);

        // A ei tarvitse B:n vastausta muistaakseen
        // oman onnistuneen lähetyksensä.
        Assert.IsFalse(
            f.dispatch.CanOfferDemo("A", "B", "D1"));

        var secondPackage = new MessagePackage(
            "MSG2", f.ab,
            cassetteCopy: f.tapeA2);

        Assert.Throws<InvalidOperationException>(() =>
            f.dispatch.Dispatch("SHIP2", secondPackage));

        Assert.AreEqual("A", f.tapeA2.HolderNodeId);
    }

    [Test]
    public void Dispatch_IndependentSendersCanCreateDuplicates()
    {
        var f = CreateFixture();

        // A ja C eivät tiedä B:n jo saaneen D1:tä.
        var fromA = new MessagePackage(
            "MSG-A", f.ab,
            cassetteCopy: f.tapeA1);

        var fromC = new MessagePackage(
            "MSG-C", f.cb,
            cassetteCopy: f.tapeC);

        var shipmentA = f.dispatch.Dispatch(
            "SHIP-A", fromA);

        var shipmentC = f.dispatch.Dispatch(
            "SHIP-C", fromC);

        shipmentA.ProcessArrival();
        shipmentC.ProcessArrival();

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipmentA.Status);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipmentC.Status);

        // Kaksi erillistä fyysistä kopiota
        // päätyy samalle vastaanottajalle.
        Assert.AreEqual("B", f.tapeA1.HolderNodeId);
        Assert.AreEqual("B", f.tapeC.HolderNodeId);

        Assert.AreNotEqual(
            f.tapeA1.Id, f.tapeC.Id);
    }

    [Test]
    public void Dispatch_StaleSnapshotCannotEraseKnownHistory()
    {
        var f = CreateFixture();

        f.known.RecordSnapshot(
            "A", "B", new[] { "D1" });

        // Vanha kokoelmalista saapuu myöhemmin.
        f.known.RecordSnapshot(
            "A", "B", Array.Empty<string>());

        Assert.AreEqual(
            true,
            f.known.IsKnownToHold("A", "B", "D1"));

        Assert.IsFalse(
            f.dispatch.CanOfferDemo("A", "B", "D1"));
    }
}
