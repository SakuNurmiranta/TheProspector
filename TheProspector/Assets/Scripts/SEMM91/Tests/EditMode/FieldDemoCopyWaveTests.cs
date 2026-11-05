
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class FieldDemoCopyWaveTests
{
    private sealed class Fixture
    {
        public readonly ConfigurableCirculationScenario Scenario;
        public readonly Field Field;
        public readonly SIRStateRegistry Sir;
        public readonly FieldDemoCopyWave Wave;

        public Fixture()
        {
            var definition = new CirculationWorldDefinition();

            definition.AddNode("A", "FI");
            definition.AddNode("B", "FI");
            definition.AddNode("C", "FI");

            
            definition.AddConnection("AB", "A", "B");
            definition.AddConnection("AC", "A", "C");

// A tietää, ettei B:llä tai C:llä ole D1:tä.
// B tietää, ettei C:llä ole D1:tä.
            definition.MarkKnownMissing("A", "B");
            definition.MarkKnownMissing("A", "C");
            definition.MarkKnownMissing("B", "C");

            definition.SetSeed("A", "D1");


            Scenario =
                new ConfigurableCirculationScenario(definition);

            Field = new Field("FIELD-1", Scenario.Network);

            Field.RegisterLocalNode("A");
            Field.RegisterLocalNode("B");
            Field.RegisterLocalNode("C");

            Sir = new SIRStateRegistry();

            Wave = new FieldDemoCopyWave(
                Field,
                Scenario.Network,
                Scenario.Demo,
                Sir);
        }

        public CopyDistributionShipment SeedTo(string target)
        {
            return Scenario.SeedWave.Single(
                x => x.RecipientNodeId == target);
        }

        public SIRState NodeState(string id)
        {
            Scenario.Network.TryGetNode(id, out var node);

            return Sir.GetNodeState(
                node, Scenario.Demo);
        }

        public SIRState FieldState()
        {
            return Sir.GetFieldState(
                Field, Scenario.Demo);
        }

        public CopyDistributionShipment DispatchFromBToC()
        {
            // Alustettu A -> B -kasetti ehtii B:lle.
            Scenario.Clock.AdvanceDays(2);

            var received = SeedTo("B").Copy;

            Assert.AreEqual("B", received.HolderNodeId);

            Scenario.Network.AddConnection(
                "BC", "B", "C",
                CommunicationLayer.Interpersonal,
                "personal");

            Scenario.Network.LearnAddress(
                "B", "C", "ADDR-C");

            var connection =
                Scenario.Network.GetOutgoingConnections(
                    "B",
                    CommunicationLayer.Interpersonal)
                .Single(c => c.TargetNodeId == "C");

            // Todellinen kopio, ei fyysinen siirto.
            var source = Scenario.Inventory.RecordCopy(
                received.Id,
                "B-SOURCE-COPY",
                "B",
                "B");

            var service = new PositiveCopyDistributionService(
                Scenario.Inventory,
                Scenario.Known,
                Scenario.Exchange);

            var shipments = service.DispatchAll(
                "B-SECOND-WAVE",
                "B",
                source,
                new[]
                {
                    new CopyDistributionRoute(
                        "C", connection, "FI")
                },
                "FI",
                5,
                recipient => false,
                recipient => true);

            Assert.AreEqual(1, shipments.Count);

            return shipments[0];
        }
    }

    [Test]
    public void Wave_ActualCopyDispatchStartsFieldAndNodeI()
    {
        var f = new Fixture();

        Assert.AreEqual(SIRState.S, f.FieldState());
        Assert.AreEqual(SIRState.S, f.NodeState("A"));

        Assert.IsTrue(
            f.Wave.ObserveCopyDispatch(f.SeedTo("B")));

        Assert.AreEqual(SIRState.I, f.FieldState());
        Assert.AreEqual(SIRState.I, f.NodeState("A"));

        // Kasetin vastaanottaja ei automaattisesti
        // ala kopioida sitä.
        Assert.AreEqual(SIRState.S, f.NodeState("B"));

        Assert.AreEqual(1, f.Wave.ActiveCopierCount);
        Assert.IsFalse(f.Wave.IsPassivelyDiscoverable);
    }

    [Test]
    public void Wave_MultipleShipmentsDoNotDuplicateCopier()
    {
        var f = new Fixture();

        Assert.IsTrue(
            f.Wave.ObserveCopyDispatch(f.SeedTo("B")));

        Assert.IsTrue(
            f.Wave.ObserveCopyDispatch(f.SeedTo("C")));

        // Sama solmu lähettää kaksi fyysistä kopiota,
        // mutta osallistuu vain yhteen I-vaiheeseen.
        Assert.AreEqual(1, f.Wave.ActiveCopierCount);

        Assert.AreEqual(SIRState.I, f.NodeState("A"));

        // Sama lähetys ei tuota uutta tapahtumaa.
        Assert.IsFalse(
            f.Wave.ObserveCopyDispatch(f.SeedTo("B")));

        Assert.AreEqual(1, f.Wave.ActiveCopierCount);
    }

    [Test]
    public void Wave_DoesNotCloseAutomaticallyWhenCopierStops()
    {
        var f = new Fixture();

        f.Wave.ObserveCopyDispatch(f.SeedTo("B"));

        Assert.IsTrue(f.Wave.FinishCopying("A"));

        Assert.AreEqual(SIRState.R, f.NodeState("A"));

        // Kenttä ei vielä siirry R-tilaan.
        Assert.AreEqual(SIRState.I, f.FieldState());

        Assert.AreEqual(0, f.Wave.ActiveCopierCount);

        Assert.IsFalse(f.Wave.IsPassivelyDiscoverable);

        // Sulkeminen vaatii erillisen päätöksen.
        Assert.IsTrue(f.Wave.TryCloseWave());

        Assert.AreEqual(SIRState.R, f.FieldState());
        Assert.IsTrue(f.Wave.IsPassivelyDiscoverable);

        Assert.IsFalse(f.Wave.TryCloseWave());
    }

    [Test]
    public void Wave_WaitsUntilEveryActiveCopierHasFinished()
    {
        var f = new Fixture();

        // Ensimmäinen oikea kopiointilähetys A:lta.
        Assert.IsTrue(
            f.Wave.ObserveCopyDispatch(f.SeedTo("B")));

        // B saa A:n kasetin ja aloittaa oman
        // kopiointilähetyksensä C:lle.
        var bc = f.DispatchFromBToC();

        Assert.IsTrue(
            f.Wave.ObserveCopyDispatch(bc));

        Assert.AreEqual(2, f.Wave.ActiveCopierCount);

        Assert.AreEqual(SIRState.I, f.NodeState("A"));
        Assert.AreEqual(SIRState.I, f.NodeState("B"));

        Assert.IsTrue(f.Wave.FinishCopying("A"));

        Assert.AreEqual(SIRState.R, f.NodeState("A"));
        Assert.AreEqual(SIRState.I, f.NodeState("B"));

        // B jatkaa vielä kopiointia.
        Assert.IsFalse(f.Wave.TryCloseWave());

        Assert.AreEqual(SIRState.I, f.FieldState());

        Assert.IsTrue(f.Wave.FinishCopying("B"));

        Assert.IsTrue(f.Wave.TryCloseWave());

        Assert.AreEqual(SIRState.R, f.FieldState());

        Assert.IsTrue(f.Wave.IsPassivelyDiscoverable);
    }

    [Test]
    public void Wave_ClosureIsIrreversible()
    {
        var f = new Fixture();

        f.Wave.ObserveCopyDispatch(f.SeedTo("B"));
        f.Wave.FinishCopying("A");

        Assert.IsTrue(f.Wave.TryCloseWave());

        // Myöhempi saman demon kopiointitapahtuma
        // ei käynnistä suljettua aaltoa uudelleen.
        Assert.Throws<InvalidOperationException>(() =>
            f.Wave.ObserveCopyDispatch(f.SeedTo("C")));

        Assert.AreEqual(SIRState.R, f.FieldState());
    }

    [Test]
    public void Wave_RejectsNonLocalSender()
    {
        var f = new Fixture();

        var otherField =
            new Field("OTHER-FIELD", f.Scenario.Network);

        // A ei kuulu tähän kenttään.
        otherField.RegisterLocalNode("B");
        otherField.RegisterLocalNode("C");

        var otherWave = new FieldDemoCopyWave(
            otherField,
            f.Scenario.Network,
            f.Scenario.Demo,
            f.Sir);

        Assert.IsFalse(
            otherWave.ObserveCopyDispatch(f.SeedTo("B")));

        Assert.AreEqual(SIRState.S,
            f.Sir.GetFieldState(
                otherField, f.Scenario.Demo));
    }
}
