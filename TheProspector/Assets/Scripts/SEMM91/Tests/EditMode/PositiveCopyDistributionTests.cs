
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class PositiveCopyDistributionTests
{
    private sealed class Fixture
    {
        public Network Network;
        public SimulationClock Clock;
        public CassetteInventory Inventory;
        public KnownCollectionRegistry Known;
        public CulturalCapitalLedger Capital;
        public RecipientBacklog Backlog;

        public CassetteCopy Original;
        public PositiveCopyDistributionService Service;

        public List<CopyDistributionRoute> Routes;

        public void KnownMissing(params string[] ids)
        {
            foreach (var id in ids)
            {
                Known.RecordSnapshot(
                    "A", id, new[] { "D2" });
            }
        }

        public IReadOnlyList<CopyDistributionShipment> Send(
            string batchId = "BATCH1",
            Func<string, bool> dislikes = null,
            Func<string, bool> canOffer = null,
            IEnumerable<CopyDistributionRoute> routes = null)
        {
            return Service.DispatchAll(
                batchId,
                "A",
                Original,
                routes ?? Routes,
                "FI",
                5,
                dislikes ?? (_ => false),
                canOffer ?? (_ => true));
        }
    }

    private static Fixture CreateFixture()
    {
        var network = new Network();

        foreach (var id in new[] { "A", "B", "C", "D" })
            network.AddNode(new Node(id));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "FI");
        network.AddAddress("ADDR-C", "FI");
        network.AddAddress("ADDR-D", "SE");
        network.AddAddress("ADDR-B-NEW", "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");
        network.PlaceNode("C", "ADDR-C");
        network.PlaceNode("D", "ADDR-D");

        network.LearnAddress("A", "B", "ADDR-B");
        network.LearnAddress("A", "C", "ADDR-C");
        network.LearnAddress("A", "D", "ADDR-D");

        foreach (var id in new[] { "B", "C", "D" })
        {
            network.AddConnection(
                "CONN-A" + id,
                "A", id,
                CommunicationLayer.Interpersonal,
                "personal");
        }

        var connections = network.GetOutgoingConnections(
                "A", CommunicationLayer.Interpersonal)
            .ToDictionary(c => c.Id);

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        field.SpawnArtist(
            "ART1",
            new Dictionary<string, int> { ["A"] = 10 },
            new Random(123));

        var demo = field.CreateDemo("D1", "ART1", true);

        var inventory = new CassetteInventory(network);
        var original = inventory.RegisterExistingCopy(
            "TAPE-ORIGINAL", demo, "A");

        var clock = new SimulationClock();
        var known = new KnownCollectionRegistry(network);
        var backlog = new RecipientBacklog();

        var capital = new CulturalCapitalLedger(network);

        foreach (var id in new[] { "A", "B", "C", "D" })
            capital.InitializeNode(id, 10);

        var reception = new MessageReceptionRegistry(known);

        var copyRewards = new CopyDeliveryRewardService(
            network, inventory, capital);

        var exchange = new TimedPostalExchange(
            clock,
            network,
            inventory,
            backlog,
            new ReplyWindowRegistry(),
            copyRewards,
            new ReciprocalExchangeRewardService(
                reception, capital));

        return new Fixture
        {
            Network = network,
            Clock = clock,
            Inventory = inventory,
            Known = known,
            Capital = capital,
            Backlog = backlog,
            Original = original,
            Service = new PositiveCopyDistributionService(
                inventory, known, exchange),
            Routes = new List<CopyDistributionRoute>
            {
                new CopyDistributionRoute(
                    "B", connections["CONN-AB"], "FI"),
                new CopyDistributionRoute(
                    "C", connections["CONN-AC"], "FI"),
                new CopyDistributionRoute(
                    "D", connections["CONN-AD"], "SE")
            }
        };
    }

    [Test]
    public void Distribution_SendsToAllKnownMissingContacts()
    {
        var f = CreateFixture();
        f.KnownMissing("B", "C", "D");

        var sent = f.Send();

        CollectionAssert.AreEqual(
            new[] { "B", "C", "D" },
            sent.Select(s => s.RecipientNodeId).ToArray());

        Assert.AreEqual(3, sent.Count);
        Assert.AreEqual(3, f.Clock.PendingCount);
    }

    [Test]
    public void Distribution_DoesNotTreatUnknownAsMissing()
    {
        var f = CreateFixture();
        f.KnownMissing("B");

        var sent = f.Send();

        Assert.AreEqual(1, sent.Count);
        Assert.AreEqual("B", sent[0].RecipientNodeId);
    }

    [Test]
    public void Distribution_SkipsKnownPreviousOwners()
    {
        var f = CreateFixture();

        f.Known.RecordSnapshot(
            "A", "B", new[] { "D1" });

        f.KnownMissing("C", "D");

        var sent = f.Send();

        CollectionAssert.AreEqual(
            new[] { "C", "D" },
            sent.Select(s => s.RecipientNodeId).ToArray());
    }

    [Test]
    public void Distribution_SkipsCommunicatedDislike()
    {
        var f = CreateFixture();
        f.KnownMissing("B", "C", "D");

        var sent = f.Send(
            dislikes: id => id == "C");

        CollectionAssert.AreEqual(
            new[] { "B", "D" },
            sent.Select(s => s.RecipientNodeId).ToArray());
    }

    [Test]
    public void Distribution_RespectsDispatchRestrictions()
    {
        var f = CreateFixture();
        f.KnownMissing("B", "C", "D");

        var sent = f.Send(
            canOffer: id => id != "D");

        CollectionAssert.AreEqual(
            new[] { "B", "C" },
            sent.Select(s => s.RecipientNodeId).ToArray());
    }

    [Test]
    public void Distribution_CreatesSeparatePhysicalCopies()
    {
        var f = CreateFixture();
        f.KnownMissing("B", "C");

        var sent = f.Send();

        Assert.AreEqual("A", f.Original.HolderNodeId);

        Assert.AreNotEqual(
            sent[0].Copy.Id,
            sent[1].Copy.Id);

        Assert.IsNull(sent[0].Copy.HolderNodeId);
        Assert.IsNull(sent[1].Copy.HolderNodeId);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            sent[0].Shipment.Status);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            sent[1].Shipment.Status);
    }

    [Test]
    public void Distribution_AwardsFourPerSuccessfulDelivery()
    {
        var f = CreateFixture();
        f.KnownMissing("B", "C");

        var sent = f.Send();

        Assert.AreEqual(10, f.Capital.GetCapital("A"));

        f.Clock.AdvanceTo(1);

        Assert.AreEqual(10, f.Capital.GetCapital("A"));

        f.Clock.AdvanceTo(2);

        Assert.AreEqual(18, f.Capital.GetCapital("A"));

        Assert.AreEqual("B", sent[0].Copy.HolderNodeId);
        Assert.AreEqual("C", sent[1].Copy.HolderNodeId);

        Assert.IsTrue(
            f.Inventory.HasEverHeldProduct("B", "D1"));

        Assert.IsTrue(
            f.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.AreEqual(1, f.Backlog.GetPendingCount("B"));
        Assert.AreEqual(1, f.Backlog.GetPendingCount("C"));
    }

    [Test]
    public void Distribution_UsesDestinationCountryForTransit()
    {
        var f = CreateFixture();
        f.KnownMissing("B", "D");

        var sent = f.Send();

        var domestic = sent.Single(
            s => s.RecipientNodeId == "B");

        var foreign = sent.Single(
            s => s.RecipientNodeId == "D");

        f.Clock.AdvanceTo(2);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            domestic.Shipment.Status);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            foreign.Shipment.Status);

        f.Clock.AdvanceTo(5);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            foreign.Shipment.Status);

        Assert.AreEqual(18, f.Capital.GetCapital("A"));
    }

    [Test]
    public void Distribution_ParallelConnectionsDoNotDuplicate()
    {
        var f = CreateFixture();
        f.KnownMissing("B");

        var routes = new[]
        {
            f.Routes[0],
            f.Routes[0]
        };

        var sent = f.Send(routes: routes);

        Assert.AreEqual(1, sent.Count);
        Assert.AreEqual(1, f.Clock.PendingCount);
    }

    [Test]
    public void Distribution_DoesNotResendWhilePendingOrDelivered()
    {
        var f = CreateFixture();
        f.KnownMissing("B");

        var first = f.Send("BATCH1");

        Assert.AreEqual(1, first.Count);

        var second = f.Send("BATCH2");

        Assert.AreEqual(0, second.Count);

        f.Clock.AdvanceTo(2);

        var third = f.Send("BATCH3");

        Assert.AreEqual(0, third.Count);

        Assert.AreEqual(14, f.Capital.GetCapital("A"));
        Assert.AreEqual(0, f.Clock.PendingCount);
    }

    [Test]
    public void Distribution_ReturnedMailCanBeRetried()
    {
        var f = CreateFixture();
        f.KnownMissing("B");

        var first = f.Send("BATCH1");

        f.Clock.AdvanceTo(1);
        f.Network.MoveNode("B", "ADDR-B-NEW");

        f.Clock.AdvanceTo(4);

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            first[0].Shipment.Status);

        Assert.AreEqual(10, f.Capital.GetCapital("A"));

        f.Network.LearnAddress(
            "A", "B", "ADDR-B-NEW");

        var retry = f.Send("BATCH2");

        Assert.AreEqual(1, retry.Count);

        f.Clock.AdvanceTo(6);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            retry[0].Shipment.Status);

        Assert.AreEqual(14, f.Capital.GetCapital("A"));
    }
}
