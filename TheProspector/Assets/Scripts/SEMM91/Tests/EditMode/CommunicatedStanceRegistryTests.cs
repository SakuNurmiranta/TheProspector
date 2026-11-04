
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class CommunicatedStanceRegistryTests
{
    private sealed class Fixture
    {
        public Network Network;
        public CulturalProduct D1;
        public CulturalProduct D2;
        public NetworkConnection CB;
        public NetworkConnection DB;
        public NetworkConnection CA;

        public MessagePackage Feedback(
            string id,
            NetworkConnection connection,
            CulturalProduct product,
            int thumb)
        {
            return new MessagePackage(
                id,
                connection,
                feedbackProduct: product,
                thumb: thumb);
        }
    }

    private static Fixture CreateFixture()
    {
        var network = new Network();

        foreach (string id in new[] { "A", "B", "C", "D" })
            network.AddNode(new Node(id));

        foreach (string id in new[] { "A", "B", "C", "D" })
        {
            network.AddAddress("ADDR-" + id, "FI");
            network.PlaceNode(id, "ADDR-" + id);
        }

        network.LearnAddress("C", "B", "ADDR-B");

        network.AddConnection(
            "CONN-CB",
            "C", "B",
            CommunicationLayer.Interpersonal,
            "personal");

        network.AddConnection(
            "CONN-DB",
            "D", "B",
            CommunicationLayer.Interpersonal,
            "personal");

        network.AddConnection(
            "CONN-CA",
            "C", "A",
            CommunicationLayer.Interpersonal,
            "personal");

        NetworkConnection Connection(
            string source, string id)
        {
            return network.GetOutgoingConnections(
                source,
                CommunicationLayer.Interpersonal)
                .Single(c => c.Id == id);
        }

        var field = new Field("F1", network);
        field.RegisterLocalNode("C");

        field.SpawnArtist(
            "ART1",
            new Dictionary<string, int>
            {
                ["C"] = 10
            },
            new Random(123));

        return new Fixture
        {
            Network = network,
            D1 = field.CreateDemo("D1", "ART1", true),
            D2 = field.CreateDemo("D2", "ART1", false),
            CB = Connection("C", "CONN-CB"),
            DB = Connection("D", "CONN-DB"),
            CA = Connection("C", "CONN-CA")
        };
    }

    [Test]
    public void Stances_UnknownBeforeCommunication()
    {
        var registry = new CommunicatedStanceRegistry();

        Assert.IsNull(
            registry.GetLastThumb("B", "C", "D1"));

        Assert.IsFalse(
            registry.HasCommunicatedDislike(
                "B", "C", "D1"));

        Assert.AreEqual(0, registry.Count);
    }

    [Test]
    public void Stances_NegativeFeedbackBecomesKnownToRecipient()
    {
        var f = CreateFixture();
        var registry = new CommunicatedStanceRegistry();

        registry.RecordOpened(
            f.Feedback("NEG", f.CB, f.D1, -1));

        Assert.AreEqual(
            -1,
            registry.GetLastThumb("B", "C", "D1"));

        Assert.IsTrue(
            registry.HasCommunicatedDislike(
                "B", "C", "D1"));

        Assert.AreEqual(1, registry.Count);
    }

    [Test]
    public void Stances_PositiveFeedbackIsNotDislike()
    {
        var f = CreateFixture();
        var registry = new CommunicatedStanceRegistry();

        registry.RecordOpened(
            f.Feedback("POS", f.CB, f.D1, +1));

        Assert.AreEqual(
            +1,
            registry.GetLastThumb("B", "C", "D1"));

        Assert.IsFalse(
            registry.HasCommunicatedDislike(
                "B", "C", "D1"));
    }

    [Test]
    public void Stances_LaterPositiveFeedbackSupersedesNegative()
    {
        var f = CreateFixture();
        var registry = new CommunicatedStanceRegistry();

        registry.RecordOpened(
            f.Feedback("NEG", f.CB, f.D1, -1));

        registry.RecordOpened(
            f.Feedback("POS", f.CB, f.D1, +1));

        Assert.AreEqual(
            +1,
            registry.GetLastThumb("B", "C", "D1"));

        Assert.IsFalse(
            registry.HasCommunicatedDislike(
                "B", "C", "D1"));

        Assert.AreEqual(1, registry.Count);
    }

    [Test]
    public void Stances_LaterNegativeFeedbackSupersedesPositive()
    {
        var f = CreateFixture();
        var registry = new CommunicatedStanceRegistry();

        registry.RecordOpened(
            f.Feedback("POS", f.CB, f.D1, +1));

        registry.RecordOpened(
            f.Feedback("NEG", f.CB, f.D1, -1));

        Assert.IsTrue(
            registry.HasCommunicatedDislike(
                "B", "C", "D1"));
    }

    [Test]
    public void Stances_ProductHistoriesAreIndependent()
    {
        var f = CreateFixture();
        var registry = new CommunicatedStanceRegistry();

        registry.RecordOpened(
            f.Feedback("NEG", f.CB, f.D1, -1));

        registry.RecordOpened(
            f.Feedback("POS", f.CB, f.D2, +1));

        Assert.AreEqual(
            -1,
            registry.GetLastThumb("B", "C", "D1"));

        Assert.AreEqual(
            +1,
            registry.GetLastThumb("B", "C", "D2"));

        Assert.AreEqual(2, registry.Count);
    }

    [Test]
    public void Stances_DifferentSpeakersAreIndependent()
    {
        var f = CreateFixture();
        var registry = new CommunicatedStanceRegistry();

        registry.RecordOpened(
            f.Feedback("C-NEG", f.CB, f.D1, -1));

        registry.RecordOpened(
            f.Feedback("D-POS", f.DB, f.D1, +1));

        Assert.IsTrue(
            registry.HasCommunicatedDislike(
                "B", "C", "D1"));

        Assert.IsFalse(
            registry.HasCommunicatedDislike(
                "B", "D", "D1"));
    }

    [Test]
    public void Stances_OnlyAddresseeLearnsTheFeedback()
    {
        var f = CreateFixture();
        var registry = new CommunicatedStanceRegistry();

        registry.RecordOpened(
            f.Feedback("C-TO-B", f.CB, f.D1, -1));

        Assert.IsTrue(
            registry.HasCommunicatedDislike(
                "B", "C", "D1"));

        Assert.IsNull(
            registry.GetLastThumb("A", "C", "D1"));

        registry.RecordOpened(
            f.Feedback("C-TO-A", f.CA, f.D1, +1));

        // C voi viestiä eri kannan eri henkilöille.
        Assert.AreEqual(
            -1,
            registry.GetLastThumb("B", "C", "D1"));

        Assert.AreEqual(
            +1,
            registry.GetLastThumb("A", "C", "D1"));
    }

    [Test]
    public void Stances_RejectsNullMessage()
    {
        var registry = new CommunicatedStanceRegistry();

        Assert.Throws<ArgumentNullException>(() =>
            registry.RecordOpened(null));

        Assert.AreEqual(0, registry.Count);
    }

    [Test]
    public void Stances_PostalDeliveryAloneDoesNotRevealDislike()
    {
        var f = CreateFixture();

        var clock = new SimulationClock();
        var inventory = new CassetteInventory(f.Network);
        var backlog = new RecipientBacklog();
        var known = new KnownCollectionRegistry(f.Network);

        var capital = new CulturalCapitalLedger(f.Network);

        foreach (string id in new[] { "A", "B", "C", "D" })
            capital.InitializeNode(id, 10);

        var reception =
            new MessageReceptionRegistry(known);

        var stances = new CommunicatedStanceRegistry();

        var exchange = new TimedPostalExchange(
            clock,
            f.Network,
            inventory,
            backlog,
            new ReplyWindowRegistry(),
            new CopyDeliveryRewardService(
                f.Network, inventory, capital),
            new ReciprocalExchangeRewardService(
                reception, capital),
            stances);

        var package = f.Feedback(
            "MSG-CB-NEG",
            f.CB,
            f.D1,
            -1);

        exchange.Send(
            "SHIP-CB-NEG",
            package,
            "FI",
            "FI",
            5);

        Assert.IsFalse(
            stances.HasCommunicatedDislike(
                "B", "C", "D1"));

        clock.AdvanceTo(1);

        Assert.IsFalse(
            stances.HasCommunicatedDislike(
                "B", "C", "D1"));

        clock.AdvanceTo(2);

        // Kirje on perillä, mutta sitä ei
        // ole vielä avattu.
        Assert.AreEqual(
            1,
            backlog.GetPendingCount("B"));

        Assert.IsFalse(
            stances.HasCommunicatedDislike(
                "B", "C", "D1"));

        Assert.IsTrue(
            exchange.TryOpenNext("B", out int reward));

        Assert.AreEqual(0, reward);

        // Vasta avaamisen jälkeen B tietää,
        // että C on viestinyt kielteisen kannan.
        Assert.IsTrue(
            stances.HasCommunicatedDislike(
                "B", "C", "D1"));

        Assert.AreEqual(-1,
            stances.GetLastThumb("B", "C", "D1"));

        Assert.AreEqual(0,
            backlog.GetPendingCount("B"));
    }
}
