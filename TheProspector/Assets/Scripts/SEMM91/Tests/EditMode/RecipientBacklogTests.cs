
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class RecipientBacklogTests
{
    private static (
        Network network,
        CassetteInventory inventory,
        NetworkConnection ab,
        NetworkConnection ac,
        CulturalProduct demo,
        CassetteCopy cassette,
        Node b
    ) CreateFixture()
    {
        var network = new Network();

        network.AddNode(new Node("A"));

        var b = new Node("B");
        network.AddNode(b);

        network.AddNode(new Node("C"));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "FI");
        network.AddAddress("ADDR-B-NEW", "SE");
        network.AddAddress("ADDR-C", "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");
        network.PlaceNode("C", "ADDR-C");

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

        var ab = connections.Single(
            c => c.Id == "CONN-AB");

        var ac = connections.Single(
            c => c.Id == "CONN-AC");

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

        return (
            network, inventory, ab, ac,
            demo, cassette, b);
    }

    [Test]
    public void Backlog_DeliveredCassetteRemainsUnprocessed()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            cassetteCopy: f.cassette);

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network,
            f.inventory, package);

        shipment.ProcessArrival();

        var backlog = new RecipientBacklog();
        backlog.ReceiveDelivered(shipment);

        Assert.AreEqual(1, backlog.GetPendingCount("B"));
        Assert.AreEqual("B", f.cassette.HolderNodeId);

        // Fyysinen toimitus ei ole kuuntelu.
        Assert.IsNull(f.b.Preference);

        var sir = new SIRStateRegistry();

        Assert.AreEqual(
            SIRState.S,
            sir.GetNodeState(f.b, f.demo));
    }

    [Test]
    public void Backlog_RejectsUndeliveredShipments()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            feedbackProduct: f.demo,
            thumb: -1);

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network,
            f.inventory, package);

        var backlog = new RecipientBacklog();

        Assert.Throws<InvalidOperationException>(() =>
            backlog.ReceiveDelivered(shipment));

        f.network.MoveNode("B", "ADDR-B-NEW");

        shipment.ProcessArrival();

        Assert.AreEqual(
            PostalShipmentStatus.Returning,
            shipment.Status);

        Assert.Throws<InvalidOperationException>(() =>
            backlog.ReceiveDelivered(shipment));

        shipment.ProcessReturn();

        Assert.Throws<InvalidOperationException>(() =>
            backlog.ReceiveDelivered(shipment));

        Assert.AreEqual(0, backlog.GetPendingCount("B"));
    }

    [Test]
    public void Backlog_UsesArrivalOrderWithinRecipient()
    {
        var f = CreateFixture();

        var firstPackage = new MessagePackage(
            "MSG1", f.ab,
            feedbackProduct: f.demo,
            thumb: +1);

        var secondPackage = new MessagePackage(
            "MSG2", f.ab,
            feedbackProduct: f.demo,
            thumb: -1);

        var first = PostalShipment.Dispatch(
            "SHIP1", f.network,
            f.inventory, firstPackage);

        var second = PostalShipment.Dispatch(
            "SHIP2", f.network,
            f.inventory, secondPackage);

        first.ProcessArrival();
        second.ProcessArrival();

        var backlog = new RecipientBacklog();

        backlog.ReceiveDelivered(first);
        backlog.ReceiveDelivered(second);

        Assert.AreEqual(2, backlog.GetPendingCount("B"));

        Assert.IsTrue(
            backlog.TryTakeNext("B", out var item1));

        Assert.AreEqual("SHIP1", item1.ShipmentId);
        Assert.AreEqual("MSG1", item1.Package.Id);

        Assert.IsTrue(
            backlog.TryTakeNext("B", out var item2));

        Assert.AreEqual("SHIP2", item2.ShipmentId);
        Assert.AreEqual("MSG2", item2.Package.Id);

        Assert.IsFalse(
            backlog.TryTakeNext("B", out var empty));

        Assert.IsNull(empty);
        Assert.AreEqual(0, backlog.GetPendingCount("B"));
    }

    [Test]
    public void Backlog_RecipientsHaveIndependentQueues()
    {
        var f = CreateFixture();

        var packageB = new MessagePackage(
            "MSG-B", f.ab,
            feedbackProduct: f.demo,
            thumb: +1);

        var packageC = new MessagePackage(
            "MSG-C", f.ac,
            feedbackProduct: f.demo,
            thumb: -1);

        var shipmentB = PostalShipment.Dispatch(
            "SHIP-B", f.network,
            f.inventory, packageB);

        var shipmentC = PostalShipment.Dispatch(
            "SHIP-C", f.network,
            f.inventory, packageC);

        shipmentB.ProcessArrival();
        shipmentC.ProcessArrival();

        var backlog = new RecipientBacklog();

        backlog.ReceiveDelivered(shipmentC);
        backlog.ReceiveDelivered(shipmentB);

        Assert.AreEqual(1, backlog.GetPendingCount("B"));
        Assert.AreEqual(1, backlog.GetPendingCount("C"));

        Assert.IsTrue(
            backlog.TryTakeNext("C", out var itemC));

        Assert.AreEqual("MSG-C", itemC.Package.Id);
        Assert.AreEqual("C", itemC.RecipientNodeId);

        Assert.AreEqual(1, backlog.GetPendingCount("B"));
    }

    [Test]
    public void Backlog_CannotRegisterSameShipmentTwice()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            feedbackProduct: f.demo,
            thumb: +1);

        var shipment = PostalShipment.Dispatch(
            "SHIP1", f.network,
            f.inventory, package);

        shipment.ProcessArrival();

        var backlog = new RecipientBacklog();

        backlog.ReceiveDelivered(shipment);

        Assert.Throws<InvalidOperationException>(() =>
            backlog.ReceiveDelivered(shipment));

        Assert.AreEqual(1, backlog.GetPendingCount("B"));

        backlog.TryTakeNext("B", out _);

        // Käsittelystä poistaminen ei salli
        // saman lähetyksen rekisteröimistä uudelleen.
        Assert.Throws<InvalidOperationException>(() =>
            backlog.ReceiveDelivered(shipment));
    }

    [Test]
    public void Backlog_ResentPackageUsesNewShipment()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1", f.ab,
            cassetteCopy: f.cassette);

        f.network.MoveNode("B", "ADDR-B-NEW");

        var first = PostalShipment.Dispatch(
            "SHIP1", f.network,
            f.inventory, package);

        first.ProcessArrival();
        first.ProcessReturn();

        Assert.AreEqual("A", f.cassette.HolderNodeId);

        // A saa myöhemmin B:n uuden osoitteen.
        f.network.LearnAddress(
            "A", "B", "ADDR-B-NEW");

        var second = PostalShipment.Dispatch(
            "SHIP2", f.network,
            f.inventory, package);

        second.ProcessArrival();

        var backlog = new RecipientBacklog();

        backlog.ReceiveDelivered(second);

        Assert.AreEqual(1, backlog.GetPendingCount("B"));

        Assert.IsTrue(
            backlog.TryTakeNext("B", out var item));

        Assert.AreEqual("SHIP2", item.ShipmentId);
        Assert.AreEqual("MSG1", item.Package.Id);
        Assert.AreEqual("B", f.cassette.HolderNodeId);
    }
}
