
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class PostalShipmentTests
{
    private static (
        Network network,
        CassetteInventory inventory,
        MessagePackage package,
        CassetteCopy cassette,
        CulturalProduct demo
    ) CreateFixture()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));
        network.AddNode(new Node("C"));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-A-NEW", "FI");
        network.AddAddress("ADDR-B", "FI");
        network.AddAddress("ADDR-B-NEW", "SE");
        network.AddAddress("ADDR-C", "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");
        network.PlaceNode("C", "ADDR-C");

        network.AddConnection(
            "CONN-AB",
            "A",
            "B",
            CommunicationLayer.Interpersonal,
            "personal");

        var connection = network.GetOutgoingConnections(
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

        var demo = field.CreateDemo(
            "D1", "ART1", true);

        var inventory = new CassetteInventory(network);

        var cassette = inventory.RegisterExistingCopy(
            "TAPE1", demo, "A");

        var package = new MessagePackage(
            "MSG1",
            connection,
            cassetteCopy: cassette);

        return (network, inventory, package, cassette, demo);
    }

    [Test]
    public void Postal_DispatchUsesKnownAddressNotActualLocation()
    {
        var f = CreateFixture();

        f.network.LearnAddress("A", "B", "ADDR-B");
        f.network.MoveNode("B", "ADDR-B-NEW");

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, f.package);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            shipment.Status);

        Assert.AreEqual(
            "ADDR-B", shipment.DestinationAddressId);

        Assert.AreEqual(
            "ADDR-A", shipment.ReturnAddressId);

        Assert.AreEqual("B", shipment.Package.TargetNodeId);

        // Kasetti ei ole enää A:n hallussa.
        Assert.IsNull(f.cassette.HolderNodeId);
        Assert.AreEqual(
            "SHIP1", f.cassette.InTransitShipmentId);
    }

    [Test]
    public void Postal_WrongAddressStartsReturn()
    {
        var f = CreateFixture();

        f.network.LearnAddress("A", "B", "ADDR-B");

        f.network.MoveNode("B", "ADDR-B-NEW");

        // C muuttaa B:n vanhaan osoitteeseen.
        f.network.MoveNode("C", "ADDR-B");

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, f.package);

        shipment.ProcessArrival();

        Assert.AreEqual(
            PostalShipmentStatus.Returning,
            shipment.Status);

        // Pakettia ei anneta C:lle.
        Assert.IsNull(f.cassette.HolderNodeId);
        Assert.AreEqual(
            "B", shipment.Package.TargetNodeId);
    }

    [Test]
    public void Postal_ReturnRestoresCassetteToSender()
    {
        var f = CreateFixture();

        f.network.LearnAddress("A", "B", "ADDR-B");
        f.network.MoveNode("B", "ADDR-B-NEW");

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, f.package);

        shipment.ProcessArrival();

        Assert.AreEqual(
            PostalShipmentStatus.Returning,
            shipment.Status);

        shipment.ProcessReturn();

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            shipment.Status);

        Assert.AreEqual("A", f.cassette.HolderNodeId);
        Assert.IsNull(f.cassette.InTransitShipmentId);

        // Palautus ei päivitä osoitekirjaa.
        Assert.IsTrue(f.network.TryGetKnownAddress(
            "A", "B", out var known));

        Assert.AreEqual("ADDR-B", known);
        Assert.AreEqual("B", shipment.Package.TargetNodeId);
    }

    [Test]
    public void Postal_ReturnedPackageCanBeSentToNewAddress()
    {
        var f = CreateFixture();

        f.network.LearnAddress("A", "B", "ADDR-B");
        f.network.MoveNode("B", "ADDR-B-NEW");

        var first = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, f.package);

        first.ProcessArrival();
        first.ProcessReturn();

        // B ilmoittaa myöhemmin uuden osoitteensa.
        f.network.LearnAddress(
            "A", "B", "ADDR-B-NEW");

        // Sama paketti voidaan lähettää uudelleen,
        // mutta siitä muodostetaan uusi lähetys.
        var second = PostalShipment.Dispatch(
            "SHIP2", f.network, f.inventory, f.package);

        Assert.AreEqual(
            "ADDR-B", first.DestinationAddressId);

        Assert.AreEqual(
            "ADDR-B-NEW", second.DestinationAddressId);

        second.ProcessArrival();

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            second.Status);

        Assert.AreEqual("B", f.cassette.HolderNodeId);
    }

    [Test]
    public void Postal_CorrectAddressDeliversCassette()
    {
        var f = CreateFixture();

        f.network.LearnAddress("A", "B", "ADDR-B");

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, f.package);

        shipment.ProcessArrival();

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);

        Assert.AreEqual("B", f.cassette.HolderNodeId);
        Assert.IsNull(f.cassette.InTransitShipmentId);

        Assert.Throws<InvalidOperationException>(() =>
            shipment.ProcessArrival());
    }

    [Test]
    public void Postal_UnknownAddressPreventsDispatch()
    {
        var f = CreateFixture();

        Assert.Throws<InvalidOperationException>(() =>
            PostalShipment.Dispatch(
                "SHIP1", f.network, f.inventory, f.package));

        Assert.AreEqual("A", f.cassette.HolderNodeId);
        Assert.IsNull(f.cassette.InTransitShipmentId);
    }

    [Test]
    public void Postal_SameCassetteCannotBeDispatchedTwice()
    {
        var f = CreateFixture();

        f.network.LearnAddress("A", "B", "ADDR-B");

        var first = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, f.package);

        Assert.Throws<InvalidOperationException>(() =>
            PostalShipment.Dispatch(
                "SHIP2", f.network, f.inventory, f.package));

        Assert.Throws<InvalidOperationException>(() =>
            f.inventory.RecordTransfer("TAPE1", "A", "C"));

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            first.Status);

        Assert.AreEqual(
            "SHIP1", f.cassette.InTransitShipmentId);
    }

    [Test]
    public void Postal_FeedbackOnlyCanReturnWithoutCassette()
    {
        var f = CreateFixture();

        var connection = f.network.GetOutgoingConnections(
            "A", CommunicationLayer.Interpersonal)[0];

        var feedback = new MessagePackage(
            "MSG-FEEDBACK",
            connection,
            feedbackProduct: f.demo,
            thumb: -1);

        f.network.LearnAddress("A", "B", "ADDR-B");
        f.network.MoveNode("B", "ADDR-B-NEW");

        var shipment = PostalShipment.Dispatch(
            "SHIP-FEEDBACK",
            f.network,
            f.inventory,
            feedback);

        shipment.ProcessArrival();

        Assert.AreEqual(
            PostalShipmentStatus.Returning,
            shipment.Status);

        shipment.ProcessReturn();

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            shipment.Status);

        Assert.IsNull(feedback.CassetteCopyId);
        Assert.AreEqual("A", f.cassette.HolderNodeId);
    }

    [Test]
    public void Postal_ReturnRequiresSenderAtReturnAddress()
    {
        var f = CreateFixture();

        f.network.LearnAddress("A", "B", "ADDR-B");
        f.network.MoveNode("B", "ADDR-B-NEW");

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network, f.inventory, f.package);

        shipment.ProcessArrival();

        // A muuttaa paluumatkan aikana.
        f.network.MoveNode("A", "ADDR-A-NEW");

        Assert.Throws<InvalidOperationException>(() =>
            shipment.ProcessReturn());

        Assert.AreEqual(
            PostalShipmentStatus.Returning,
            shipment.Status);

        Assert.IsNull(f.cassette.HolderNodeId);
    }
}
