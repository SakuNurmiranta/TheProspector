
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class ScenarioPassiveFieldDiscoveryTests
{
    private sealed class FixedRandom : Random
    {
        private readonly double _sample;

        public int Calls { get; private set; }

        public FixedRandom(double sample)
        {
            _sample = sample;
        }

        public override double NextDouble()
        {
            Calls++;
            return _sample;
        }
    }

    private static ConfigurableCirculationScenario Create(
        Random random,
        bool sendSeed = true,
        bool secondDemo = false)
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddNode("C", "FI");

        definition.AddConnection("AB", "A", "B");

        if (sendSeed)
            definition.MarkKnownMissing("A", "B");

        definition.SetSeed("A", "D1");

        if (secondDemo)
            definition.AddSeed("B", "D2");

        return new ConfigurableCirculationScenario(
            definition,
            fieldDiscoveryRandom: random);
    }

    [Test]
    public void PassiveAuto_NoSamplingBeforeWaveClosure()
    {
        var random = new FixedRandom(0.0);
        var scenario = Create(random);

        scenario.World.AdvanceDays(3);

        Assert.AreEqual(
            SIRState.I,
            scenario.FieldSIR.GetFieldState(
                scenario.Field, scenario.Demo));

        Assert.AreEqual(0, random.Calls);
        Assert.IsFalse(
            scenario.GetFieldWave("D1")
                .HasPassiveExposure("C"));
    }

    [Test]
    public void PassiveAuto_DiscoversAfterClosureWithoutCassette()
    {
        var random = new FixedRandom(0.0);
        var scenario = Create(random);

        // Päivä 4: B:n kuuntelu valmistuu,
        // SIR-aalto sulkeutuu ja C voi löytää D1:n.
        scenario.World.AdvanceDays(4);

        var wave = scenario.GetFieldWave("D1");

        Assert.AreEqual(
            SIRState.R,
            scenario.FieldSIR.GetFieldState(
                scenario.Field, scenario.Demo));

        Assert.IsTrue(wave.HasPassiveExposure("C"));
        Assert.AreEqual(1, random.Calls);

        Assert.IsFalse(
            scenario.Inventory.HasEverHeldProduct(
                "C", "D1"));

        scenario.Network.TryGetNode("C", out var c);

        Assert.AreEqual(
            SIRState.S,
            scenario.FieldSIR.GetNodeState(
                c, scenario.Demo));

        Assert.AreEqual(0,
            scenario.Network.GetOutgoingConnections(
                "C",
                CommunicationLayer.Interpersonal).Count);

        Assert.IsTrue(
            scenario.HasDiscoveredFromField(
                "C", "D1"));
    }

    [Test]
    public void PassiveAuto_FailedSampleIsNotRepeated()
    {
        var random = new FixedRandom(0.99);
        var scenario = Create(random);

        scenario.World.AdvanceDays(4);

        Assert.AreEqual(1, random.Calls);

        Assert.IsFalse(
            scenario.HasDiscoveredFromField(
                "C", "D1"));

        scenario.World.AdvanceDays(12);

        Assert.AreEqual(1, random.Calls);

        Assert.IsFalse(
            scenario.HasDiscoveredFromField(
                "C", "D1"));
    }

    [Test]
    public void PassiveAuto_PhysicalRecipientsAreNotSampled()
    {
        var random = new FixedRandom(0.0);
        var scenario = Create(random);

        scenario.World.AdvanceDays(4);

        Assert.IsTrue(
            scenario.Inventory.HasEverHeldProduct(
                "A", "D1"));

        Assert.IsTrue(
            scenario.Inventory.HasEverHeldProduct(
                "B", "D1"));

        Assert.IsFalse(
            scenario.GetFieldWave("D1")
                .HasPassiveExposure("A"));

        Assert.IsFalse(
            scenario.GetFieldWave("D1")
                .HasPassiveExposure("B"));

        Assert.IsTrue(
            scenario.GetFieldWave("D1")
                .HasPassiveExposure("C"));

        // Vain C tarvitsi löytöarvonnan.
        Assert.AreEqual(1, random.Calls);
    }

    [Test]
    public void PassiveAuto_NoWaveMeansNoExposure()
    {
        var random = new FixedRandom(0.0);
        var scenario = Create(
            random, sendSeed: false);

        scenario.World.AdvanceDays(12);

        Assert.AreEqual(
            SIRState.S,
            scenario.FieldSIR.GetFieldState(
                scenario.Field, scenario.Demo));

        Assert.AreEqual(0, random.Calls);

        Assert.IsFalse(
            scenario.HasDiscoveredFromField(
                "C", "D1"));
    }

    [Test]
    public void PassiveAuto_ReturnedMailHasNoReach()
    {
        var random = new FixedRandom(0.0);
        var scenario = Create(random);

        scenario.Network.AddAddress(
            "ADDR-B-NEW", "SE");

        scenario.Network.MoveNode(
            "B", "ADDR-B-NEW");

        scenario.World.AdvanceDays(8);

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            scenario.SeedWave[0].Shipment.Status);

        Assert.AreEqual(
            SIRState.R,
            scenario.FieldSIR.GetFieldState(
                scenario.Field, scenario.Demo));

        Assert.AreEqual(
            0.0,
            scenario.GetFieldWave("D1")
                .ObservedDeliveredReach,
            1e-9);

        Assert.AreEqual(0, random.Calls);

        Assert.IsFalse(
            scenario.HasDiscoveredFromField(
                "C", "D1"));
    }

    [Test]
    public void PassiveAuto_DifferentDemosRemainIndependent()
    {
        var random = new FixedRandom(0.0);
        var scenario = Create(
            random, secondDemo: true);

        scenario.World.AdvanceDays(4);

        Assert.AreEqual(
            SIRState.R,
            scenario.FieldSIR.GetFieldState(
                scenario.Field,
                scenario.GetDemo("D1")));

        // D2:n lähettäjällä B ei ole
        // vielä sopivia jakelukontakteja.
        Assert.AreEqual(
            SIRState.S,
            scenario.FieldSIR.GetFieldState(
                scenario.Field,
                scenario.GetDemo("D2")));

        Assert.IsTrue(
            scenario.HasDiscoveredFromField(
                "C", "D1"));

        Assert.IsFalse(
            scenario.HasDiscoveredFromField(
                "C", "D2"));

        Assert.AreEqual(1, random.Calls);
    }
}
