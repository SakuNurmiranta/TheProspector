
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class ZineEndToEndTests
{
    private sealed class Fixture : IDisposable
    {
        public readonly ConfigurableCirculationScenario Scenario;
        public readonly ZineEditorialCycle Editorial;
        public readonly ZineIssueKnowledgeRegistry Knowledge;
        public readonly ZineContactOutreachPolicy Outreach;
        public readonly ZineFirstContactPostalService Postal;
        public readonly ZineAutonomousContactCycle Auto;

        private readonly ZineEditorialMailCollector _collector;
        private readonly ZinePublicationDistribution _publication;

        public Fixture()
        {
            var definition = new CirculationWorldDefinition();

            definition.AddNode("B", "FI");
            definition.AddNode("A", "FI");
            definition.AddNode("E", "FI");

            definition.SetSeed("B", "D1");

            Scenario = new ConfigurableCirculationScenario(
                definition);

            var network = Scenario.Network;

            // A tuntee toimituksen E.
            network.AddConnection(
                "AE", "A", "E",
                CommunicationLayer.Interpersonal,
                "personal");

            network.LearnAddress("A", "E", "ADDR-E");

            // Toimitus levittää zinen B:lle.
            network.AddConnection(
                "EB", "E", "B",
                CommunicationLayer.Insert,
                "zine");

            Editorial = new ZineEditorialCycle(
                "Z1", "E", true, Scenario.Clock);

            Knowledge = new ZineIssueKnowledgeRegistry(
                network);

            var delivery = new ZineIssueInsertDelivery(
                new InsertDistributionRegistry(),
                Knowledge);

            _publication = new ZinePublicationDistribution(
                Editorial,
                delivery,
                () => network.GetOutgoingConnections(
                    "E", CommunicationLayer.Insert),
                recipient => recipient == "B");

            _collector = new ZineEditorialMailCollector(
                Editorial,
                Scenario.Exchange);

            Outreach = new ZineContactOutreachPolicy(
                network, Knowledge);

            Postal = new ZineFirstContactPostalService(
                network,
                Scenario.Clock,
                Outreach,
                Scenario.Exchange);

            // B:llä on fyysinen kasetti,
            // jonka voi lähettää ensikontaktissa.
            var copy = Scenario.Inventory.RecordCopy(
                Scenario.Original.Id,
                "E2E-COPY",
                "B",
                "B");

            Auto = new ZineAutonomousContactCycle(
                "B",
                Scenario.Clock,
                Outreach,
                Postal,
                () => true,
                () => copy.HolderNodeId == "B"
                    ? copy
                    : null);
        }

        public void SendContactDeclaration(
            bool publicProfile)
        {
            var route = Scenario.Network
                .GetOutgoingConnections(
                    "A", CommunicationLayer.Interpersonal)[0];

            var package = new MessagePackage(
                "CONTACT-DECLARATION",
                route,
                addressNoticeAddressId: "ADDR-A",
                declaredPublicProfile: publicProfile);

            Scenario.Exchange.Send(
                "SHIP-CONTACT-DECLARATION",
                package,
                "FI", "FI", 5);
        }

        public void OpenAtEditorial()
        {
            Assert.IsTrue(
                Scenario.Exchange.TryOpenNext(
                    "E", out _, out _));
        }

        public void Dispose()
        {
            Auto.Dispose();
            Postal.Dispose();
            _collector.Dispose();
            _publication.Dispose();
        }
    }

    [Test]
    public void Zine_EndToEnd_CreatesNewRelationship()
    {
        using (var f = new Fixture())
        {
            f.SendContactDeclaration(true);

            // Toimitus ei tiedä kirjeen sisältöä
            // ennen avaamista.
            f.Scenario.Clock.AdvanceDays(2);

            Assert.AreEqual(0, f.Editorial.PendingCount);

            f.OpenAtEditorial();

            Assert.AreEqual(1, f.Editorial.PendingCount);

            // Päivä 30: numero julkaistaan.
            // Viimeistään päivänä 31 B on
            // tehnyt autonomisen kontaktipäätöksen.
            f.Scenario.Clock.AdvanceDays(29);

            Assert.AreEqual(1, f.Editorial.Issues.Count);

            var issue = f.Editorial.Issues[0];

            Assert.IsTrue(
                f.Knowledge.HasReceived(
                    "B", issue.ProductId));

            Assert.IsTrue(
                f.Scenario.Network.TryGetKnownAddress(
                    "B", "A", out var knownAddress));

            Assert.AreEqual("ADDR-A", knownAddress);

            Assert.AreEqual(1, f.Auto.SentCount);
            Assert.AreEqual("A", f.Auto.LastTargetNodeId);

            // Viestin lähetys ei vielä luo suhdetta.
            Assert.AreEqual(0,
                f.Scenario.Network.GetOutgoingConnections(
                    "B",
                    CommunicationLayer.Interpersonal).Count);

            // Kasetti saapuu A:lle.
            f.Scenario.Clock.AdvanceDays(2);

            Assert.IsTrue(
                f.Scenario.Exchange.TryOpenNext(
                    "A", out _, out var opened));

            Assert.AreEqual("B",
                opened.Package.SourceNodeId);

            Assert.AreEqual("A",
                opened.Package.TargetNodeId);

            // A vastaa todellisen postipalvelun kautta.
            f.Postal.ReplyWithAddress(
                opened, "E2E-REPLY");

            f.Scenario.Clock.AdvanceDays(2);

            Assert.AreEqual(2,
                f.Scenario.Network.GetOutgoingConnections(
                    "A",
                    CommunicationLayer.Interpersonal).Count);

            Assert.AreEqual(1,
                f.Scenario.Network.GetOutgoingConnections(
                    "B",
                    CommunicationLayer.Interpersonal).Count);

            Assert.IsFalse(
                f.Outreach.HasPendingOutreach("B"));

            Assert.AreEqual(0,
                f.Scenario.Skepticism.Get(
                    "B", "A").UnansweredReplies);
        }
    }

    [Test]
    public void Zine_UnopenedEditorialMailMissesFirstIssue()
    {
        using (var f = new Fixture())
        {
            f.SendContactDeclaration(true);

            // Kirje saapuu toimitukseen mutta
            // sitä ei avata ennen julkaisua.
            f.Scenario.Clock.AdvanceDays(30);

            Assert.AreEqual(1, f.Editorial.Issues.Count);

            Assert.AreEqual(0,
                f.Editorial.Issues[0].Entries.Count);

            Assert.AreEqual(0, f.Auto.SentCount);

            // Avataan vasta ensimmäisen numeron jälkeen.
            f.OpenAtEditorial();

            Assert.AreEqual(1, f.Editorial.PendingCount);

            // Päivä 60: uusi numero sisältää tiedon.
            f.Scenario.Clock.AdvanceDays(31);

            Assert.AreEqual(2, f.Editorial.Issues.Count);

            Assert.AreEqual(1,
                f.Editorial.Issues[1].Entries.Count);

            Assert.IsTrue(
                f.Knowledge.HasReceived(
                    "B",
                    f.Editorial.Issues[1].ProductId));

            Assert.AreEqual(1, f.Auto.SentCount);
            Assert.AreEqual("A", f.Auto.LastTargetNodeId);
        }
    }

    [Test]
    public void Zine_PublicContactProfileControlsOutreach()
    {
        using (var f = new Fixture())
        {
            // Zine itsessään on true.
            // A ilmoittaa julkiseksi profiilikseen false.
            f.SendContactDeclaration(false);

            f.Scenario.Clock.AdvanceDays(2);
            f.OpenAtEditorial();

            f.Scenario.Clock.AdvanceDays(40);

            Assert.AreEqual(1, f.Editorial.Issues.Count);

            Assert.IsTrue(
                f.Knowledge.HasReceived(
                    "B",
                    f.Editorial.Issues[0].ProductId));

            // B lukee zinen mutta ei aloita
            // yhteyttä profiililtaan eroavaan A:han.
            Assert.AreEqual(0,
                f.Outreach.GetCandidates(
                    "B", true).Count);

            Assert.AreEqual(1,
                f.Outreach.GetCandidates(
                    "B", false).Count);

            Assert.AreEqual(0, f.Auto.SentCount);
        }
    }

    [Test]
    public void Zine_StalePublishedAddressCausesPostalReturn()
    {
        using (var f = new Fixture())
        {
            f.SendContactDeclaration(true);

            f.Scenario.Clock.AdvanceDays(2);
            f.OpenAtEditorial();

            // A muuttaa toimitukselle tehdyn
            // osoiteilmoituksen jälkeen.
            var network = f.Scenario.Network;

            network.AddAddress("ADDR-A-NEW", "SE");
            network.MoveNode("A", "ADDR-A-NEW");

            // Zine julkaisee edelleen vanhan,
            // aiemmin oikean osoitetiedon.
            f.Scenario.Clock.AdvanceDays(29);

            Assert.AreEqual(1, f.Auto.SentCount);

            Assert.IsTrue(
                network.TryGetKnownAddress(
                    "B", "A", out var known));

            Assert.IsTrue(
                network.TryGetNodeAddress(
                    "A", out var actual));

            Assert.AreEqual("ADDR-A", known);
            Assert.AreEqual("ADDR-A-NEW", actual);

            // Kirje kulkee vanhaan osoitteeseen
            // ja palautuu lähettäjälle.
            f.Scenario.Clock.AdvanceDays(4);

            Assert.AreEqual(
                PostalShipmentStatus.Returned,
                f.Auto.LastShipment.Status);

            Assert.AreEqual(0,
                network.GetOutgoingConnections(
                    "B",
                    CommunicationLayer.Interpersonal).Count);
        }
    }
}
