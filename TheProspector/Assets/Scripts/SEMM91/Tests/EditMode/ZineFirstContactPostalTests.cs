
using NUnit.Framework;
using SEMM91.Domain;

public class ZineFirstContactPostalTests
{
    private sealed class Fixture
    {
        public readonly ConfigurableCirculationScenario Scenario;
        public readonly ZineContactOutreachPolicy Outreach;
        public readonly ZineFirstContactPostalService Postal;

        public Fixture()
        {
            var definition = new CirculationWorldDefinition();

            definition.AddNode("B", "FI");
            definition.AddNode("A", "FI");
            definition.AddNode("C", "FI");
            definition.AddNode("E", "FI");
            definition.SetSeed("B", "D1");

            Scenario = new ConfigurableCirculationScenario(
                definition);

            var network = Scenario.Network;

            network.AddConnection(
                "EB", "E", "B",
                CommunicationLayer.Insert, "zine");

            var editorial = new ZineEditorialCycle(
                "Z1", "E", true, Scenario.Clock);

            var knowledge =
                new ZineIssueKnowledgeRegistry(network);

            var delivery = new ZineIssueInsertDelivery(
                new InsertDistributionRegistry(),
                knowledge);

            // Jakelija tilaa toimituksen julkaisuilmoitukset.
            _ = new ZinePublicationDistribution(
                editorial,
                delivery,
                () => network.GetOutgoingConnections(
                    "E", CommunicationLayer.Insert),
                recipient => recipient == "B");

            Outreach = new ZineContactOutreachPolicy(
                network, knowledge);

            Postal = new ZineFirstContactPostalService(
                network,
                Scenario.Clock,
                Outreach,
                Scenario.Exchange);

            editorial.RecordSelfDeclaredContact(
                "A", "ADDR-A", true);

            editorial.RecordSelfDeclaredContact(
                "C", "ADDR-C", true);

            Scenario.Clock.AdvanceDays(30);

            Assert.AreEqual(2,
                Outreach.GetCandidates("B", true).Count);
        }

        public CassetteCopy Copy(string id)
        {
            return Scenario.Inventory.RecordCopy(
                Scenario.Original.Id,
                id,
                "B",
                "B");
        }

        public int Deadline()
        {
            return PostalTimingRules.CalculateDeadline(
                30,
                PostalTimingRules.GetTransitDays("FI", "FI"),
                5);
        }
    }

    [Test]
    public void FirstContact_ActualReplyCreatesBothDirections()
    {
        var f = new Fixture();

        Assert.IsTrue(f.Postal.TrySend(
            "B", true, "A",
            f.Copy("COPY1"), "INTRO1",
            out var introduction));

        // Ensimmäinen yhteydenotto ei vielä
        // ole pysyvä verkostosuhde.
        Assert.AreEqual(0,
            f.Scenario.Network.GetOutgoingConnections(
                "B",
                CommunicationLayer.Interpersonal).Count);

        f.Scenario.Clock.AdvanceDays(2);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            introduction.Status);

        Assert.IsTrue(
            f.Scenario.Exchange.TryOpenNext(
                "A", out _, out var opened));

        Assert.AreEqual("INTRO1", opened.Package.Id);

        // A saa B:n palautusosoitteen
        // vasta avattuaan viestin.
        Assert.IsTrue(
            f.Scenario.Network.TryGetKnownAddress(
                "A", "B", out var returnAddress));

        Assert.AreEqual("ADDR-B", returnAddress);

        f.Postal.ReplyWithAddress(opened, "REPLY1");

        f.Scenario.Clock.AdvanceDays(2);

        Assert.IsFalse(
            f.Outreach.HasPendingOutreach("B"));

        Assert.AreEqual(1,
            f.Scenario.Network.GetOutgoingConnections(
                "B",
                CommunicationLayer.Interpersonal).Count);

        Assert.AreEqual(1,
            f.Scenario.Network.GetOutgoingConnections(
                "A",
                CommunicationLayer.Interpersonal).Count);

        f.Scenario.Clock.AdvanceDays(10);

        Assert.AreEqual(0,
            f.Scenario.Skepticism.Get(
                "B", "A").UnansweredReplies);
    }

    [Test]
    public void FirstContact_PublicProfileMustMatch()
    {
        var f = new Fixture();

        Assert.IsFalse(f.Postal.TrySend(
            "B", false, "A",
            f.Copy("COPY2"), "INTRO2",
            out var shipment));

        Assert.IsNull(shipment);

        Assert.IsFalse(
            f.Outreach.HasPendingOutreach("B"));

        Assert.AreEqual(0,
            f.Scenario.Network.GetOutgoingConnections(
                "B",
                CommunicationLayer.Interpersonal).Count);
    }

    [Test]
    public void FirstContact_TimeoutReleasesCapacityButNotRelationship()
    {
        var f = new Fixture();

        Assert.IsTrue(f.Postal.TrySend(
            "B", true, "A",
            f.Copy("COPY3"), "INTRO3", out _));

        Assert.IsFalse(f.Postal.TrySend(
            "B", true, "C",
            f.Copy("COPY4"), "INTRO4", out _));

        f.Scenario.Clock.AdvanceDays(
            f.Deadline() - f.Scenario.Clock.CurrentDay + 1);

        Assert.IsFalse(
            f.Outreach.HasPendingOutreach("B"));

        Assert.AreEqual(0,
            f.Scenario.Network.GetOutgoingConnections(
                "B",
                CommunicationLayer.Interpersonal).Count);

        Assert.AreEqual(1,
            f.Scenario.Skepticism.Get(
                "B", "A").UnansweredReplies);

        // Sama vastaamaton kohde ei saa
        // uutta automaattista tarjousta.
        Assert.IsFalse(f.Postal.TrySend(
            "B", true, "A",
            f.Copy("COPY5"), "INTRO5", out _));

        // Toista kiinnostavaa kohdetta
        // voidaan nyt tunnustella.
        Assert.IsTrue(f.Postal.TrySend(
            "B", true, "C",
            f.Copy("COPY6"), "INTRO6", out _));
    }

    [Test]
    public void FirstContact_StaleZineAddressReturnsMail()
    {
        var f = new Fixture();

        var network = f.Scenario.Network;

        network.AddAddress("ADDR-A-NEW", "SE");
        network.MoveNode("A", "ADDR-A-NEW");

        Assert.IsTrue(f.Postal.TrySend(
            "B", true, "A",
            f.Copy("COPY7"), "INTRO7",
            out var shipment));

        int transit =
            PostalTimingRules.GetTransitDays("FI", "FI");

        f.Scenario.Clock.AdvanceDays(2 * transit);

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            shipment.Status);

        f.Scenario.Clock.AdvanceDays(
            f.Deadline() - f.Scenario.Clock.CurrentDay + 1);

        Assert.IsFalse(
            f.Outreach.HasPendingOutreach("B"));

        Assert.AreEqual(0,
            network.GetOutgoingConnections(
                "B",
                CommunicationLayer.Interpersonal).Count);

        network.TryGetKnownAddress(
            "B", "A", out var known);

        network.TryGetNodeAddress(
            "A", out var actual);

        Assert.AreEqual("ADDR-A", known);
        Assert.AreEqual("ADDR-A-NEW", actual);
    }

    [Test]
    public void FirstContact_LateReplyCanEstablishConnection()
    {
        var f = new Fixture();

        Assert.IsTrue(f.Postal.TrySend(
            "B", true, "A",
            f.Copy("COPY8"), "INTRO8", out _));

        f.Scenario.Clock.AdvanceDays(2);

        Assert.IsTrue(
            f.Scenario.Exchange.TryOpenNext(
                "A", out _, out var opened));

        // B:n odotusikkuna ehtii umpeutua.
        f.Scenario.Clock.AdvanceDays(
            f.Deadline() - f.Scenario.Clock.CurrentDay + 1);

        Assert.AreEqual(1,
            f.Scenario.Skepticism.Get(
                "B", "A").UnansweredReplies);

        // A vastaa vasta määräajan jälkeen.
        f.Postal.ReplyWithAddress(opened, "LATE-REPLY");

        f.Scenario.Clock.AdvanceDays(2);

        Assert.AreEqual(1,
            f.Scenario.Network.GetOutgoingConnections(
                "B",
                CommunicationLayer.Interpersonal).Count);

        // Myöhäinen vastaus ei itsessään
        // kompensoi aikaisempaa skeptisyyttä.
        Assert.AreEqual(1,
            f.Scenario.Skepticism.Get(
                "B", "A").UnansweredReplies);
    }
}
