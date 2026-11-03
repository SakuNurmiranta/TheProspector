
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class AddressNoticeTests
{
    private static (
        Network network,
        CassetteInventory inventory,
        NetworkConnection ba
    ) CreateFixture()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-A-NEW", "FI");
        network.AddAddress("ADDR-B-OLD", "FI");
        network.AddAddress("ADDR-B-NEW", "SE");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B-OLD");

        // A tuntee B:n vanhan osoitteen.
        network.LearnAddress(
            "A", "B", "ADDR-B-OLD");

        // B tuntee A:n osoitteen.
        network.LearnAddress(
            "B", "A", "ADDR-A");

        network.MoveNode("B", "ADDR-B-NEW");

        network.AddConnection(
            "CONN-BA",
            "B",
            "A",
            CommunicationLayer.Interpersonal,
            "personal");

        var ba = network.GetOutgoingConnections(
            "B", CommunicationLayer.Interpersonal)[0];

        return (
            network,
            new CassetteInventory(network),
            ba);
    }

    private static (
        PostalShipment shipment,
        BacklogItem item
    ) DeliverNotice(
        Network network,
        CassetteInventory inventory,
        MessagePackage package)
    {
        var shipment = PostalShipment.Dispatch(
            "SHIP1", network, inventory, package);

        shipment.ProcessArrival();

        var backlog = new RecipientBacklog();
        backlog.ReceiveDelivered(shipment);

        Assert.IsTrue(
            backlog.TryTakeNext("A", out var item));

        return (shipment, item);
    }

    [Test]
    public void Notice_CanBeOnlyMessageContent()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1",
            f.ba,
            addressNoticeAddressId: "ADDR-B-NEW");

        Assert.AreEqual(
            "ADDR-B-NEW",
            package.AddressNoticeAddressId);

        Assert.AreEqual("B", package.SourceNodeId);
        Assert.AreEqual("A", package.TargetNodeId);

        Assert.IsNull(package.CassetteCopyId);
        Assert.IsNull(package.FeedbackProductId);
        Assert.IsFalse(package.Thumb.HasValue);
    }

    [Test]
    public void Notice_UpdatesAddressOnlyWhenOpened()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1",
            f.ba,
            addressNoticeAddressId: "ADDR-B-NEW");

        var (shipment, item) = DeliverNotice(
            f.network, f.inventory, package);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);

        // Toimitus ei päivitä osoitekirjaa.
        f.network.TryGetKnownAddress(
            "A", "B", out var before);

        Assert.AreEqual("ADDR-B-OLD", before);

        var reception = new MessageReceptionRegistry();
        reception.Open(item, f.network);

        f.network.TryGetKnownAddress(
            "A", "B", out var after);

        Assert.AreEqual("ADDR-B-NEW", after);

        // B:n todellista sijaintia ei muuteta.
        f.network.TryGetNodeAddress(
            "B", out var actual);

        Assert.AreEqual("ADDR-B-NEW", actual);
    }

    [Test]
    public void Notice_ReturnedMailDoesNotUpdateAddress()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1",
            f.ba,
            addressNoticeAddressId: "ADDR-B-NEW");

        var shipment = PostalShipment.Dispatch(
            "SHIP1",
            f.network,
            f.inventory,
            package);

        // A muuttaa ennen toimitusyritystä.
        f.network.MoveNode("A", "ADDR-A-NEW");

        shipment.ProcessArrival();

        Assert.AreEqual(
            PostalShipmentStatus.Returning,
            shipment.Status);

        shipment.ProcessReturn();

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            shipment.Status);

        var backlog = new RecipientBacklog();

        Assert.Throws<InvalidOperationException>(() =>
            backlog.ReceiveDelivered(shipment));

        f.network.TryGetKnownAddress(
            "A", "B", out var known);

        Assert.AreEqual("ADDR-B-OLD", known);
    }

    [Test]
    public void Notice_RequiresNetworkWhenOpened()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1",
            f.ba,
            addressNoticeAddressId: "ADDR-B-NEW");

        var (_, item) = DeliverNotice(
            f.network, f.inventory, package);

        var reception = new MessageReceptionRegistry();

        Assert.Throws<InvalidOperationException>(() =>
            reception.Open(item));

        // Virheellinen yritys ei kuluta viestiä.
        reception.Open(item, f.network);

        f.network.TryGetKnownAddress(
            "A", "B", out var known);

        Assert.AreEqual("ADDR-B-NEW", known);
    }

    [Test]
    public void Notice_UnknownAddressDoesNotChangeKnowledge()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1",
            f.ba,
            addressNoticeAddressId: "ADDR-UNKNOWN");

        var (_, item) = DeliverNotice(
            f.network, f.inventory, package);

        var reception = new MessageReceptionRegistry();

        Assert.Throws<ArgumentException>(() =>
            reception.Open(item, f.network));

        f.network.TryGetKnownAddress(
            "A", "B", out var known);

        Assert.AreEqual("ADDR-B-OLD", known);
    }

    [Test]
    public void Notice_RejectsWhitespaceAddress()
    {
        var f = CreateFixture();

        Assert.Throws<ArgumentException>(() =>
            new MessagePackage(
                "MSG1",
                f.ba,
                addressNoticeAddressId: "  "));

        // Tyhjä paketti on edelleen kielletty.
        Assert.Throws<ArgumentException>(() =>
            new MessagePackage("MSG2", f.ba));
    }
}
