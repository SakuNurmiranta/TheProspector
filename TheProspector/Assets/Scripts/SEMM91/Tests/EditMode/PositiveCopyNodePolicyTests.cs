
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class PositiveCopyNodePolicyTests
{
    private sealed class Fixture
    {
        public SimulationWorld World;
        public SimulationClock Clock;
        public TimedPostalExchange Exchange;
        public PositiveCopyNodePolicy Policy;

        public CassetteInventory Inventory;
        public KnownCollectionRegistry Known;
        public CommunicatedStanceRegistry Stances;
        public CulturalCapitalLedger Capital;

        public CulturalProduct Demo;
        public CassetteCopy IncomingCopy;

        public NetworkConnection AB;
        public NetworkConnection CB;

        public List<CopyDistributionRoute> Routes;
    }

    private static Fixture Create(
        bool knowsC = true,
        bool knowsD = true,
        bool approves = true,
        bool sendCassette = true)
    {
        var network = new Network();

        foreach (string id in new[] { "A", "B", "C", "D" })
        {
            network.AddNode(new Node(id));
            network.AddAddress("ADDR-" + id, "FI");
            network.PlaceNode(id, "ADDR-" + id);
        }

        network.LearnAddress("A", "B", "ADDR-B");
        network.LearnAddress("B", "A", "ADDR-A");
        network.LearnAddress("B", "C", "ADDR-C");
        network.LearnAddress("B", "D", "ADDR-D");
        network.LearnAddress("C", "B", "ADDR-B");

        void Connect(string id, string source, string target)
        {
            network.AddConnection(
                id, source, target,
                CommunicationLayer.Interpersonal,
                "personal");
        }

        Connect("AB", "A", "B");
        Connect("BA", "B", "A");
        Connect("BC", "B", "C");
        Connect("BD", "B", "D");
        Connect("CB", "C", "B");

        NetworkConnection Connection(string source, string id)
        {
            return network.GetOutgoingConnections(
                    source,
                    CommunicationLayer.Interpersonal)
                .Single(c => c.Id == id);
        }

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        field.SpawnArtist(
            "ART1",
            new Dictionary<string, int> { ["A"] = 10 },
            new Random(123));

        var demo = field.CreateDemo("D1", "ART1", true);
        var inventory = new CassetteInventory(network);

        var original = inventory.RegisterExistingCopy(
            "ORIGINAL", demo, "A");

        var known = new KnownCollectionRegistry(network);

        if (knowsC)
            known.RecordSnapshot(
                "B", "C", Array.Empty<string>());

        if (knowsD)
            known.RecordSnapshot(
                "B", "D", Array.Empty<string>());

        var capital = new CulturalCapitalLedger(network);

        foreach (string id in new[] { "A", "B", "C", "D" })
            capital.InitializeNode(id, 10);

        var clock = new SimulationClock();
        var backlog = new RecipientBacklog();
        var stances = new CommunicatedStanceRegistry();

        var exchange = new TimedPostalExchange(
            clock,
            network,
            inventory,
            backlog,
            new ReplyWindowRegistry(),
            new CopyDeliveryRewardService(
                network, inventory, capital),
            new ReciprocalExchangeRewardService(
                new MessageReceptionRegistry(known),
                capital),
            stances);

        var routes = new List<CopyDistributionRoute>
        {
            new CopyDistributionRoute(
                "A", Connection("B", "BA"), "FI"),
            new CopyDistributionRoute(
                "C", Connection("B", "BC"), "FI"),
            new CopyDistributionRoute(
                "D", Connection("B", "BD"), "FI")
        };

        CassetteCopy incomingCopy = null;

        if (sendCassette)
        {
            incomingCopy = inventory.RecordCopy(
                original.Id,
                "COPY-AB",
                "A",
                "A");
        }

        var package = sendCassette
            ? new MessagePackage(
                "MSG-AB",
                Connection("A", "AB"),
                cassetteCopy: incomingCopy)
            : new MessagePackage(
                "MSG-AB",
                Connection("A", "AB"),
                feedbackProduct: demo,
                thumb: +1);

        exchange.Send(
            "SHIP-AB", package, "FI", "FI", 5);

        var distribution =
            new PositiveCopyDistributionService(
                inventory, known, exchange);

        // Päätössääntö on tässä testin lähtöehto.
        // Se ei vielä edusta esteettistä arviointia.
        var policy = new PositiveCopyNodePolicy(
            "B",
            distribution,
            stances,
            item => item.Package.Id == "MSG-AB"
                ? incomingCopy
                : null,
            () => routes,
            nodeId => "FI",
            (nodeId, productId) => approves,
            (nodeId, recipientId) => true);

        var world = new SimulationWorld(
            clock, network, exchange);

        foreach (string id in new[] { "A", "B", "C", "D" })
            world.RegisterNode(
                id, id == "B" ? policy : null);

        return new Fixture
        {
            World = world,
            Clock = clock,
            Exchange = exchange,
            Policy = policy,
            Inventory = inventory,
            Known = known,
            Stances = stances,
            Capital = capital,
            Demo = demo,
            IncomingCopy = incomingCopy,
            AB = Connection("A", "AB"),
            CB = Connection("C", "CB"),
            Routes = routes
        };
    }

    [Test]
    public void Policy_DoesNothingBeforeMessageOpens()
    {
        var f = Create();

        f.World.AdvanceDay();

        Assert.AreEqual(1, f.World.CurrentDay);
        Assert.AreEqual(0, f.Policy.Dispatched.Count);
        Assert.AreEqual(0, f.Policy.CopyDecisions);
    }

    [Test]
    public void Policy_ForwardsToAllKnownMissingContacts()
    {
        var f = Create();

        f.World.AdvanceDays(2);

        Assert.AreEqual(2, f.World.CurrentDay);
        Assert.AreEqual(1, f.Policy.CopyDecisions);

        CollectionAssert.AreEquivalent(
            new[] { "C", "D" },
            f.Policy.Dispatched
                .Select(x => x.RecipientNodeId)
                .ToArray());

        Assert.AreEqual(2, f.Policy.Dispatched.Count);

        foreach (var shipment in f.Policy.Dispatched)
        {
            Assert.AreEqual(
                PostalShipmentStatus.InTransit,
                shipment.Shipment.Status);
        }
    }

    [Test]
    public void Policy_DoesNotSendDemoBackToOriginalHolder()
    {
        var f = Create();

        f.World.AdvanceDays(2);

        Assert.AreEqual(
            true,
            f.Known.IsKnownToHold("B", "A", "D1"));

        Assert.IsFalse(f.Policy.Dispatched.Any(
            x => x.RecipientNodeId == "A"));
    }

    [Test]
    public void Policy_DoesNotForwardToUnknownCollections()
    {
        var f = Create(
            knowsC: false,
            knowsD: false);

        f.World.AdvanceDays(2);

        Assert.IsNull(
            f.Known.IsKnownToHold("B", "C", "D1"));

        Assert.IsNull(
            f.Known.IsKnownToHold("B", "D", "D1"));

        Assert.AreEqual(1, f.Policy.CopyDecisions);
        Assert.AreEqual(0, f.Policy.Dispatched.Count);
    }

    [Test]
    public void Policy_RespectsCommunicatedDislike()
    {
        var f = Create();

        // B on jo avannut C:n aiemman
        // kielteisen kannanoton.
        f.Stances.RecordOpened(
            new MessagePackage(
                "C-REJECTION",
                f.CB,
                feedbackProduct: f.Demo,
                thumb: -1));

        f.World.AdvanceDays(2);

        CollectionAssert.AreEqual(
            new[] { "D" },
            f.Policy.Dispatched
                .Select(x => x.RecipientNodeId)
                .ToArray());
    }

    [Test]
    public void Policy_RequiresPositiveCopyDecision()
    {
        var f = Create(approves: false);

        f.World.AdvanceDays(2);

        Assert.AreEqual(1, f.Policy.CopyDecisions);
        Assert.AreEqual(0, f.Policy.Dispatched.Count);

        Assert.AreEqual(14,
            f.Capital.GetCapital("A"));

        Assert.AreEqual(10,
            f.Capital.GetCapital("B"));
    }

    [Test]
    public void Policy_UsesContactsAtDecisionTime()
    {
        var f = Create();

        // B menettää D:hen johtavan reitin
        // ennen kasetin vastaanottamista.
        f.World.AdvanceDay();

        f.Routes.RemoveAll(
            r => r.RecipientNodeId == "D");

        f.World.AdvanceDay();

        CollectionAssert.AreEqual(
            new[] { "C" },
            f.Policy.Dispatched
                .Select(x => x.RecipientNodeId)
                .ToArray());
    }

    [Test]
    public void Policy_PhysicalDeliveryAwardsCapital()
    {
        var f = Create();

        f.World.AdvanceDays(4);

        Assert.AreEqual(4, f.World.CurrentDay);

        Assert.AreEqual(14,
            f.Capital.GetCapital("A"));

        Assert.AreEqual(18,
            f.Capital.GetCapital("B"));

        Assert.AreEqual(10,
            f.Capital.GetCapital("C"));

        Assert.AreEqual(10,
            f.Capital.GetCapital("D"));

        Assert.IsTrue(
            f.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.IsTrue(
            f.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(2, f.Policy.Dispatched.Count);
    }

    [Test]
    public void Policy_FeedbackOnlyMessageDoesNotTriggerCopying()
    {
        var f = Create(sendCassette: false);

        f.World.AdvanceDays(2);

        Assert.AreEqual(0, f.Policy.CopyDecisions);
        Assert.AreEqual(0, f.Policy.Dispatched.Count);

        Assert.IsFalse(
            f.Inventory.HasEverHeldProduct("B", "D1"));
    }

    [Test]
    public void Policy_ContinuedSimulationDoesNotResendCopies()
    {
        var f = Create();

        f.World.AdvanceDays(12);

        Assert.AreEqual(12, f.World.CurrentDay);
        Assert.AreEqual(1, f.Policy.CopyDecisions);
        Assert.AreEqual(2, f.Policy.Dispatched.Count);

        Assert.AreEqual(18,
            f.Capital.GetCapital("B"));

        Assert.AreEqual(0, f.Clock.PendingCount);
    }
}
