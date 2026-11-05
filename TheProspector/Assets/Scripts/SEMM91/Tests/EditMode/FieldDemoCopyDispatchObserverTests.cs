
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class FieldDemoCopyDispatchObserverTests
{
    private sealed class Fixture : IDisposable
    {
        public readonly ConfigurableCirculationScenario Scenario;
        public readonly Field Field;
        public readonly SIRStateRegistry Sir;
        public readonly FieldDemoCopyWave Wave;
        public readonly PositiveCopyDistributionService Distribution;
        public readonly FieldDemoCopyDispatchObserver Observer;

        public Fixture()
        {
            var definition = new CirculationWorldDefinition();

            definition.AddNode("A", "FI");
            definition.AddNode("B", "FI");
            definition.AddNode("C", "FI");

            definition.AddConnection("AB", "A", "B");
            definition.AddConnection("AC", "A", "C");

            definition.SetSeed("A", "D1");

            Scenario =
                new ConfigurableCirculationScenario(definition);

            // Ei alustettua lähetysaaltoa.
            // Lähetykset syntyvät vasta testeissä.
            Assert.AreEqual(0, Scenario.SeedWave.Count);

            Scenario.Known.RecordSnapshot(
                "A", "B", Array.Empty<string>());

            Scenario.Known.RecordSnapshot(
                "A", "C", Array.Empty<string>());

            Field = new Field("F1", Scenario.Network);

            Field.RegisterLocalNode("A");
            Field.RegisterLocalNode("B");
            Field.RegisterLocalNode("C");

            Sir = new SIRStateRegistry();

            Wave = new FieldDemoCopyWave(
                Field,
                Scenario.Network,
                Scenario.Demo,
                Sir);

            Distribution = new PositiveCopyDistributionService(
                Scenario.Inventory,
                Scenario.Known,
                Scenario.Exchange);

            Observer = new FieldDemoCopyDispatchObserver(
                Distribution,
                Wave);
        }

        public CopyDistributionRoute Route(string target)
        {
            var connection = Scenario.Network
                .GetOutgoingConnections(
                    "A", CommunicationLayer.Interpersonal)
                .Single(c => c.TargetNodeId == target);

            return new CopyDistributionRoute(
                target, connection, "FI");
        }

        public IReadOnlyList<CopyDistributionShipment> Send(
            string batchId,
            params string[] targets)
        {
            return Distribution.DispatchAll(
                batchId,
                "A",
                Scenario.Original,
                targets.Select(Route),
                "FI",
                5,
                recipient => false,
                recipient => true);
        }

        public SIRState NodeState(string nodeId)
        {
            Scenario.Network.TryGetNode(nodeId, out var node);

            return Sir.GetNodeState(node, Scenario.Demo);
        }

        public SIRState FieldState()
        {
            return Sir.GetFieldState(Field, Scenario.Demo);
        }

        public void Dispose()
        {
            Observer.Dispose();
        }
    }

    [Test]
    public void Observer_ActualDispatchStartsSirAutomatically()
    {
        using (var f = new Fixture())
        {
            Assert.AreEqual(SIRState.S, f.FieldState());

            var shipments = f.Send("BATCH1", "B");

            Assert.AreEqual(1, shipments.Count);

            // ObserveCopyDispatch()-metodia
            // ei kutsuta testissä.
            Assert.AreEqual(SIRState.I, f.FieldState());
            Assert.AreEqual(SIRState.I, f.NodeState("A"));
            Assert.AreEqual(SIRState.S, f.NodeState("B"));

            Assert.AreEqual(1, f.Wave.ActiveCopierCount);

            Assert.AreEqual(
                0.0, f.Wave.ObservedDeliveredReach, 1e-9);

            f.Scenario.Clock.AdvanceDays(2);

            Assert.AreEqual(
                PostalShipmentStatus.Delivered,
                shipments[0].Shipment.Status);

            Assert.AreEqual(
                1.0 / 3.0,
                f.Wave.ObservedDeliveredReach,
                1e-9);
        }
    }

    [Test]
    public void Observer_MultipleCopiesCountOneActiveCopier()
    {
        using (var f = new Fixture())
        {
            var shipments = f.Send(
                "BATCH2", "B", "C");

            Assert.AreEqual(2, shipments.Count);

            Assert.AreEqual(SIRState.I, f.FieldState());

            Assert.AreEqual(1, f.Wave.ActiveCopierCount);

            f.Scenario.Clock.AdvanceDays(2);

            Assert.AreEqual(
                2.0 / 3.0,
                f.Wave.ObservedDeliveredReach,
                1e-9);
        }
    }

    [Test]
    public void Observer_EmptyDistributionDoesNotStartWave()
    {
        using (var f = new Fixture())
        {
            var shipments = f.Distribution.DispatchAll(
                "EMPTY",
                "A",
                f.Scenario.Original,
                new[] { f.Route("B") },
                "FI",
                5,
                recipient => false,
                recipient => false);

            Assert.AreEqual(0, shipments.Count);

            Assert.AreEqual(SIRState.S, f.FieldState());
            Assert.AreEqual(SIRState.S, f.NodeState("A"));

            Assert.AreEqual(0, f.Wave.ActiveCopierCount);

            // Seuraava todellinen lähetys
            // käynnistää edelleen aallon.
            Assert.AreEqual(
                1, f.Send("BATCH3", "B").Count);

            Assert.AreEqual(SIRState.I, f.FieldState());
        }
    }

    [Test]
    public void Observer_DisposeStopsAutomaticObservation()
    {
        using (var f = new Fixture())
        {
            f.Observer.Dispose();
            f.Observer.Dispose();

            var shipments = f.Send("BATCH4", "B");

            Assert.AreEqual(1, shipments.Count);

            Assert.AreEqual(SIRState.S, f.FieldState());
            Assert.AreEqual(SIRState.S, f.NodeState("A"));

            Assert.AreEqual(0, f.Wave.ActiveCopierCount);

            f.Scenario.Clock.AdvanceDays(2);

            // Postitus toimii edelleen normaalisti,
            // vaikka SIR-seuranta on irrotettu.
            Assert.AreEqual(
                PostalShipmentStatus.Delivered,
                shipments[0].Shipment.Status);
        }
    }

    [Test]
    public void Observer_ClosedWaveDoesNotRestart()
    {
        using (var f = new Fixture())
        {
            f.Send("BATCH5", "B");

            f.Scenario.Clock.AdvanceDays(2);

            Assert.IsTrue(f.Wave.FinishCopying("A"));
            Assert.IsTrue(f.Wave.TryCloseWave());

            Assert.AreEqual(SIRState.R, f.FieldState());

            // Myöhempi fyysinen lähetys ei
            // avaa suljettua kenttäaaltoa.
            // Lähetystä ei myöskään peruta vain
            // siksi, että SIR-aalto on suljettu.
            Assert.AreEqual(
                1, f.Send("BATCH6", "C").Count);

            Assert.AreEqual(SIRState.R, f.FieldState());
            Assert.AreEqual(SIRState.R, f.NodeState("A"));

            Assert.AreEqual(0, f.Wave.ActiveCopierCount);
            Assert.IsTrue(f.Wave.IsPassivelyDiscoverable);
        }
    }
}
