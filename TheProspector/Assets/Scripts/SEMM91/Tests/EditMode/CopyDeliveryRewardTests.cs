
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class CopyDeliveryRewardTests
{
    private sealed class Fixture
    {
        public Network Network;
        public CassetteInventory Inventory;
        public CulturalCapitalLedger Capital;
        public CopyDeliveryRewardService Rewards;

        public NetworkConnection AB;
        public NetworkConnection CB;

        public CulturalProduct Demo;

        public CassetteCopy OriginalA;
        public CassetteCopy OriginalC;

        public PostalShipment Send(
            string shipmentId,
            string messageId,
            NetworkConnection connection,
            CassetteCopy cassette)
        {
            var package = new MessagePackage(
                messageId,
                connection,
                cassetteCopy: cassette);

            return PostalShipment.Dispatch(
                shipmentId,
                Network,
                Inventory,
                package);
        }
    }

    private static Fixture CreateFixture(
        int startingCapitalA = 10)
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

        var originalA = inventory.RegisterExistingCopy(
            "TAPE-A-ORIGINAL", demo, "A");

        var originalC = inventory.RegisterExistingCopy(
            "TAPE-C-ORIGINAL", demo, "C");

        var capital = new CulturalCapitalLedger(network);

        capital.InitializeNode("A", startingCapitalA);
        capital.InitializeNode("B", 10);
        capital.InitializeNode("C", 10);

        return new Fixture
        {
            Network = network,
            Inventory = inventory,
            Capital = capital,
            Rewards = new CopyDeliveryRewardService(
                network, inventory, capital),
            AB = ab,
            CB = cb,
            Demo = demo,
            OriginalA = originalA,
            OriginalC = originalC
        };
    }

    [Test]
    public void Reward_FirstDeliveredCopyAwardsFour()
    {
        var f = CreateFixture();

        var copy = f.Inventory.RecordCopy(
            f.OriginalA.Id, "TAPE-A2", "A", "A");

        Assert.AreEqual("A", copy.CopiedByNodeId);

        var shipment = f.Send(
            "SHIP1", "MSG1", f.AB, copy);

        // Kopion valmistaminen ja lähettäminen
        // eivät vielä tuota palkkiota.
        Assert.AreEqual(10, f.Capital.GetCapital("A"));

        int awarded = f.Rewards.ProcessArrivalAndAward(
            shipment);

        Assert.AreEqual(4, awarded);
        Assert.AreEqual(14, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));

        Assert.AreEqual("B", copy.HolderNodeId);
        Assert.AreEqual(
            "A", f.OriginalA.HolderNodeId);
    }

    [Test]
    public void Reward_OriginalCassetteTransferAwardsNothing()
    {
        var f = CreateFixture();

        Assert.IsNull(f.OriginalA.CopiedByNodeId);

        var shipment = f.Send(
            "SHIP1", "MSG1", f.AB, f.OriginalA);

        int awarded = f.Rewards.ProcessArrivalAndAward(
            shipment);

        Assert.AreEqual(0, awarded);
        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual("B", f.OriginalA.HolderNodeId);
    }

    [Test]
    public void Reward_IndependentSendersOnlyFirstGetsAward()
    {
        var f = CreateFixture();

        var copyA = f.Inventory.RecordCopy(
            f.OriginalA.Id, "TAPE-A2", "A", "A");

        var copyC = f.Inventory.RecordCopy(
            f.OriginalC.Id, "TAPE-C2", "C", "C");

        var shipmentA = f.Send(
            "SHIP-A", "MSG-A", f.AB, copyA);

        var shipmentC = f.Send(
            "SHIP-C", "MSG-C", f.CB, copyC);

        // C:n lähetys saapuu ensin.
        Assert.AreEqual(
            4,
            f.Rewards.ProcessArrivalAndAward(shipmentC));

        // B on nyt jo vastaanottanut D1:n.
        Assert.AreEqual(
            0,
            f.Rewards.ProcessArrivalAndAward(shipmentA));

        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual(14, f.Capital.GetCapital("C"));

        // Molemmat fyysiset kopiot jäävät B:lle.
        Assert.AreEqual("B", copyA.HolderNodeId);
        Assert.AreEqual("B", copyC.HolderNodeId);
    }

    [Test]
    public void Reward_ReturnedShipmentCanBeRetried()
    {
        var f = CreateFixture();

        var copy = f.Inventory.RecordCopy(
            f.OriginalA.Id, "TAPE-A2", "A", "A");

        var first = f.Send(
            "SHIP1", "MSG1", f.AB, copy);

        // B muuttaa ennen toimitusyritystä.
        f.Network.MoveNode("B", "ADDR-B-NEW");

        Assert.AreEqual(
            0,
            f.Rewards.ProcessArrivalAndAward(first));

        Assert.AreEqual(
            PostalShipmentStatus.Returning,
            first.Status);

        Assert.AreEqual(10, f.Capital.GetCapital("A"));

        first.ProcessReturn();

        Assert.AreEqual("A", copy.HolderNodeId);

        // A oppii uuden osoitteen.
        f.Network.LearnAddress(
            "A", "B", "ADDR-B-NEW");

        var second = f.Send(
            "SHIP2", "MSG2", f.AB, copy);

        Assert.AreEqual(
            4,
            f.Rewards.ProcessArrivalAndAward(second));

        Assert.AreEqual(14, f.Capital.GetCapital("A"));
        Assert.AreEqual("B", copy.HolderNodeId);
    }

    [Test]
    public void Reward_PreviousOwnershipPreventsAward()
    {
        var f = CreateFixture();

        // A valmistaa kopion myöhempää lähetystä varten.
        var copy = f.Inventory.RecordCopy(
            f.OriginalA.Id, "TAPE-A2", "A", "A");

        // B on jo kerran saanut alkuperäisen D1:n.
        f.Inventory.RecordTransfer(
            f.OriginalA.Id, "A", "B");

        // B luovuttaa kasetin edelleen C:lle.
        f.Inventory.RecordTransfer(
            f.OriginalA.Id, "B", "C");

        Assert.IsTrue(
            f.Inventory.HasEverHeldProduct("B", "D1"));

        var shipment = f.Send(
            "SHIP1", "MSG1", f.AB, copy);

        Assert.AreEqual(
            0,
            f.Rewards.ProcessArrivalAndAward(shipment));

        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual("B", copy.HolderNodeId);
    }

    [Test]
    public void Reward_ForwardingAnotherNodesCopyAwardsNothing()
    {
        var f = CreateFixture();

        // A tekee fyysisen kopion.
        var copy = f.Inventory.RecordCopy(
            f.OriginalA.Id, "TAPE-A2", "A", "A");

        // A luovuttaa sen C:lle.
        f.Inventory.RecordTransfer(
            copy.Id, "A", "C");

        Assert.AreEqual("A", copy.CopiedByNodeId);
        Assert.AreEqual("C", copy.HolderNodeId);

        // C vain välittää A:n tekemän kopion B:lle.
        var shipment = f.Send(
            "SHIP1", "MSG1", f.CB, copy);

        Assert.AreEqual(
            0,
            f.Rewards.ProcessArrivalAndAward(shipment));

        Assert.AreEqual(10, f.Capital.GetCapital("C"));
        Assert.AreEqual("B", copy.HolderNodeId);
    }

    [Test]
    public void Reward_FeedbackOnlyDeliveryAwardsNothing()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1",
            f.AB,
            feedbackProduct: f.Demo,
            thumb: +1);

        var shipment = PostalShipment.Dispatch(
            "SHIP1",
            f.Network,
            f.Inventory,
            package);

        Assert.AreEqual(
            0,
            f.Rewards.ProcessArrivalAndAward(shipment));

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            shipment.Status);

        Assert.AreEqual(10, f.Capital.GetCapital("A"));
    }

    [Test]
    public void Reward_OverflowDoesNotCompleteDelivery()
    {
        var f = CreateFixture(int.MaxValue - 2);

        var copy = f.Inventory.RecordCopy(
            f.OriginalA.Id, "TAPE-A2", "A", "A");

        var shipment = f.Send(
            "SHIP1", "MSG1", f.AB, copy);

        Assert.Throws<OverflowException>(() =>
            f.Rewards.ProcessArrivalAndAward(shipment));

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            shipment.Status);

        Assert.IsNull(copy.HolderNodeId);

        Assert.AreEqual(
            "SHIP1", copy.InTransitShipmentId);

        Assert.AreEqual(
            int.MaxValue - 2,
            f.Capital.GetCapital("A"));

        Assert.IsFalse(
            f.Inventory.HasEverHeldProduct("B", "D1"));
    }

    [Test]
    public void Reward_DeliveryCannotBeAwardedTwice()
    {
        var f = CreateFixture();

        var copy = f.Inventory.RecordCopy(
            f.OriginalA.Id, "TAPE-A2", "A", "A");

        var shipment = f.Send(
            "SHIP1", "MSG1", f.AB, copy);

        Assert.AreEqual(
            4,
            f.Rewards.ProcessArrivalAndAward(shipment));

        Assert.Throws<InvalidOperationException>(() =>
            f.Rewards.ProcessArrivalAndAward(shipment));

        Assert.AreEqual(14, f.Capital.GetCapital("A"));
    }
}
