
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class SimulationWorldTests
{
    private sealed class ProbePolicy : ISimulationNodePolicy
    {
        public readonly List<int> Days = new List<int>();
        public readonly List<string> Opened = new List<string>();

        public Action<string, int> AfterDay;

        public void OnDay(string nodeId, int day)
        {
            Days.Add(day);
            AfterDay?.Invoke(nodeId, day);
        }

        public void OnMessageOpened(
            string nodeId,
            BacklogItem message,
            int capitalAward,
            int day)
        {
            Opened.Add(message.Package.Id);
        }
    }

    // Vain testeissä käytettävä yksinkertainen
    // toimintapolitiikka: B vastaa kerran A:lle.
    //
    // Huomaa, ettei tähän kirjoiteta päivää 2.
    private sealed class ReplyPolicy : ISimulationNodePolicy
    {
        private readonly TimedPostalExchange _exchange;
        private readonly NetworkConnection _replyConnection;
        private readonly CulturalProduct _demo;

        private bool _sent;

        public ReplyPolicy(
            TimedPostalExchange exchange,
            NetworkConnection replyConnection,
            CulturalProduct demo)
        {
            _exchange = exchange;
            _replyConnection = replyConnection;
            _demo = demo;
        }

        public void OnDay(string nodeId, int day)
        {
        }

        public void OnMessageOpened(
            string nodeId,
            BacklogItem message,
            int capitalAward,
            int day)
        {
            if (_sent)
                return;

            _sent = true;

            var reply = new MessagePackage(
                "MSG-REPLY",
                _replyConnection,
                feedbackProduct: _demo,
                thumb: +1,
                replyToPackageId: message.Package.Id);

            _exchange.Send(
                "SHIP-REPLY",
                reply,
                "FI",
                "FI",
                5);
        }
    }

    private sealed class Fixture
    {
        public SimulationClock Clock;
        public SimulationWorld World;
        public TimedPostalExchange Exchange;
        public Network Network;
        public RecipientBacklog Backlog;
        public KnownCollectionRegistry Known;
        public CulturalCapitalLedger Capital;

        public NetworkConnection AB;
        public NetworkConnection AC;
        public NetworkConnection BA;

        public CulturalProduct Demo;

        public MessagePackage Message(
            string id,
            NetworkConnection connection)
        {
            return new MessagePackage(
                id,
                connection,
                feedbackProduct: Demo,
                thumb: +1);
        }

        public void Send(
            string shipmentId,
            string messageId,
            NetworkConnection connection)
        {
            Exchange.Send(
                shipmentId,
                Message(messageId, connection),
                "FI",
                "FI",
                5);
        }
    }

    private static Fixture CreateFixture()
    {
        var network = new Network();

        foreach (string id in new[] { "A", "B", "C" })
        {
            network.AddNode(new Node(id));
            network.AddAddress("ADDR-" + id, "FI");
            network.PlaceNode(id, "ADDR-" + id);
        }

        network.LearnAddress("A", "B", "ADDR-B");
        network.LearnAddress("A", "C", "ADDR-C");
        network.LearnAddress("B", "A", "ADDR-A");

        network.AddConnection(
            "CONN-AB", "A", "B",
            CommunicationLayer.Interpersonal,
            "personal");

        network.AddConnection(
            "CONN-AC", "A", "C",
            CommunicationLayer.Interpersonal,
            "personal");

        network.AddConnection(
            "CONN-BA", "B", "A",
            CommunicationLayer.Interpersonal,
            "personal");

        NetworkConnection GetConnection(
            string sender, string id)
        {
            foreach (var connection in
                network.GetOutgoingConnections(
                    sender,
                    CommunicationLayer.Interpersonal))
            {
                if (connection.Id == id)
                    return connection;
            }

            throw new InvalidOperationException(
                "Missing connection: " + id);
        }

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

        inventory.RegisterExistingCopy(
            "TAPE-ORIGINAL", demo, "A");

        var known = new KnownCollectionRegistry(network);
        var backlog = new RecipientBacklog();

        var capital = new CulturalCapitalLedger(network);

        foreach (string id in new[] { "A", "B", "C" })
            capital.InitializeNode(id, 10);

        var reception = new MessageReceptionRegistry(known);

        var clock = new SimulationClock();

        var exchange = new TimedPostalExchange(
            clock,
            network,
            inventory,
            backlog,
            new ReplyWindowRegistry(),
            new CopyDeliveryRewardService(
                network, inventory, capital),
            new ReciprocalExchangeRewardService(
                reception, capital));

        return new Fixture
        {
            Clock = clock,
            World = new SimulationWorld(
                clock, network, exchange),
            Exchange = exchange,
            Network = network,
            Backlog = backlog,
            Known = known,
            Capital = capital,
            AB = GetConnection("A", "CONN-AB"),
            AC = GetConnection("A", "CONN-AC"),
            BA = GetConnection("B", "CONN-BA"),
            Demo = demo
        };
    }

    [Test]
    public void World_AdvancesRegisteredNodesEveryDay()
    {
        var f = CreateFixture();
        var policy = new ProbePolicy();

        f.World.RegisterNode("A", policy);

        f.World.AdvanceDays(3);

        Assert.AreEqual(3, f.World.CurrentDay);
        Assert.AreEqual(3, f.Clock.CurrentDay);

        CollectionAssert.AreEqual(
            new[] { 1, 2, 3 },
            policy.Days);

        Assert.AreEqual(0, f.World.OpenedMessages.Count);
    }

    [Test]
    public void World_OpensArrivedMailAutomatically()
    {
        var f = CreateFixture();
        var policy = new ProbePolicy();

        f.World.RegisterNode("B", policy);

        f.Send("SHIP1", "MSG1", f.AB);

        f.World.AdvanceDay();

        Assert.AreEqual(1, f.World.CurrentDay);
        Assert.AreEqual(0, policy.Opened.Count);

        f.World.AdvanceDay();

        Assert.AreEqual(2, f.World.CurrentDay);

        CollectionAssert.AreEqual(
            new[] { "MSG1" },
            policy.Opened);

        Assert.AreEqual(
            0, f.Backlog.GetPendingCount("B"));

        Assert.AreEqual(
            true,
            f.Known.IsKnownToHold(
                "B", "A", "D1"));

        Assert.AreEqual(1, f.World.OpenedMessages.Count);

        Assert.AreEqual(
            2, f.World.OpenedMessages[0].Day);

        Assert.AreEqual(
            "B", f.World.OpenedMessages[0].NodeId);
    }

    [Test]
    public void World_ProcessesAtMostOneLetterPerNodePerDay()
    {
        var f = CreateFixture();

        f.World.RegisterNode("B");

        f.Send("SHIP1", "MSG1", f.AB);
        f.Send("SHIP2", "MSG2", f.AB);

        f.World.AdvanceDays(2);

        Assert.AreEqual(
            1, f.Backlog.GetPendingCount("B"));

        Assert.AreEqual(1, f.World.OpenedMessages.Count);

        Assert.AreEqual(
            "MSG1",
            f.World.OpenedMessages[0].PackageId);

        f.World.AdvanceDay();

        Assert.AreEqual(
            0, f.Backlog.GetPendingCount("B"));

        Assert.AreEqual(2, f.World.OpenedMessages.Count);

        Assert.AreEqual(
            3, f.World.OpenedMessages[1].Day);

        Assert.AreEqual(
            "MSG2",
            f.World.OpenedMessages[1].PackageId);
    }

    [Test]
    public void World_NodeProcessingOrderIsDeterministic()
    {
        var f = CreateFixture();

        // Rekisteröidään tarkoituksella C ensin.
        f.World.RegisterNode("C");
        f.World.RegisterNode("B");

        f.Send("SHIP-B", "MSG-B", f.AB);
        f.Send("SHIP-C", "MSG-C", f.AC);

        f.World.AdvanceDays(2);

        Assert.AreEqual(2, f.World.OpenedMessages.Count);

        Assert.AreEqual(
            "C", f.World.OpenedMessages[0].NodeId);

        Assert.AreEqual(
            "B", f.World.OpenedMessages[1].NodeId);
    }

    [Test]
    public void World_PolicyCanReplyWithoutScriptedDays()
    {
        var f = CreateFixture();

        f.World.RegisterNode("A");

        f.World.RegisterNode(
            "B",
            new ReplyPolicy(
                f.Exchange,
                f.BA,
                f.Demo));

        f.Send("SHIP-ORIGINAL", "MSG-ORIGINAL", f.AB);

        f.World.AdvanceDays(2);

        // B avasi viestin ja lähetti vastauksen
        // automaattisesti samana simulaatiopäivänä.
        Assert.AreEqual(2, f.World.CurrentDay);
        Assert.AreEqual(1, f.World.OpenedMessages.Count);

        Assert.AreEqual(10, f.Capital.GetCapital("A"));
        Assert.AreEqual(10, f.Capital.GetCapital("B"));

        f.World.AdvanceDays(2);

        // A avasi vastauksen automaattisesti.
        Assert.AreEqual(4, f.World.CurrentDay);

        Assert.AreEqual(2, f.World.OpenedMessages.Count);

        Assert.AreEqual(
            "A", f.World.OpenedMessages[1].NodeId);

        Assert.AreEqual(
            "MSG-REPLY",
            f.World.OpenedMessages[1].PackageId);

        Assert.AreEqual(2,
            f.World.OpenedMessages[1].CapitalAward);

        Assert.AreEqual(11, f.Capital.GetCapital("A"));
        Assert.AreEqual(11, f.Capital.GetCapital("B"));
    }

    [Test]
    public void World_RejectsUnknownAndDuplicateNodes()
    {
        var f = CreateFixture();

        Assert.Throws<ArgumentException>(() =>
            f.World.RegisterNode(""));

        Assert.Throws<ArgumentException>(() =>
            f.World.RegisterNode("UNKNOWN"));

        f.World.RegisterNode("A");

        Assert.Throws<ArgumentException>(() =>
            f.World.RegisterNode("A"));

        Assert.AreEqual(1, f.World.RegisteredNodeCount);
    }

    [Test]
    public void World_PreventsReentrantAdvancement()
    {
        var f = CreateFixture();
        var policy = new ProbePolicy();

        policy.AfterDay = (id, day) =>
        {
            Assert.Throws<InvalidOperationException>(() =>
                f.World.AdvanceDay());
        };

        f.World.RegisterNode("A", policy);

        f.World.AdvanceDay();

        Assert.AreEqual(1, f.World.CurrentDay);
        Assert.AreEqual(1, policy.Days.Count);
    }

    [Test]
    public void World_BatchedAndSteppedRunsMatch()
    {
        var first = CreateFixture();
        var second = CreateFixture();

        first.World.RegisterNode("B");
        second.World.RegisterNode("B");

        first.Send("SHIP1", "MSG1", first.AB);
        second.Send("SHIP1", "MSG1", second.AB);

        first.World.AdvanceDays(5);

        for (int i = 0; i < 5; i++)
            second.World.AdvanceDay();

        Assert.AreEqual(
            first.World.CurrentDay,
            second.World.CurrentDay);

        Assert.AreEqual(
            first.World.OpenedMessages.Count,
            second.World.OpenedMessages.Count);

        Assert.AreEqual(
            first.World.OpenedMessages[0].Day,
            second.World.OpenedMessages[0].Day);

        Assert.AreEqual(
            first.World.OpenedMessages[0].PackageId,
            second.World.OpenedMessages[0].PackageId);

        Assert.AreEqual(
            first.Capital.GetCapital("A"),
            second.Capital.GetCapital("A"));

        Assert.AreEqual(
            first.Backlog.GetPendingCount("B"),
            second.Backlog.GetPendingCount("B"));
    }
}
