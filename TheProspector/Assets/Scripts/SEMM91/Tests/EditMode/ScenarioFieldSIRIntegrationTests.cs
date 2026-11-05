
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class ScenarioFieldSIRIntegrationTests
{
    private static ConfigurableCirculationScenario Create(
        bool knowsMissing = true)
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddNode("C", "FI");

        definition.AddConnection("AB", "A", "B");
        definition.AddConnection("BC", "B", "C");

        if (knowsMissing)
        {
            definition.MarkKnownMissing("A", "B");
            definition.MarkKnownMissing("B", "C");
        }

        definition.SetSeed("A", "D1");

        return new ConfigurableCirculationScenario(definition);
    }

    [Test]
    public void Scenario_SeedAutomaticallyStartsFieldSIR()
    {
        var scenario = Create();
        var wave = scenario.GetFieldWave("D1");

        Assert.AreEqual(1, scenario.SeedWave.Count);
        Assert.AreEqual(3, scenario.Field.LocalNodeCount);

        Assert.AreEqual(SIRState.I,
            scenario.FieldSIR.GetFieldState(
                scenario.Field, scenario.Demo));

        scenario.Network.TryGetNode("A", out var a);
        scenario.Network.TryGetNode("B", out var b);

        Assert.AreEqual(SIRState.I,
            scenario.FieldSIR.GetNodeState(
                a, scenario.Demo));

        Assert.AreEqual(SIRState.S,
            scenario.FieldSIR.GetNodeState(
                b, scenario.Demo));

        Assert.AreEqual(1, wave.ActiveCopierCount);

        Assert.AreEqual(
            0.0, wave.ObservedDeliveredReach, 1e-9);
    }

    [Test]
    public void Scenario_ActualDeliveryIncreasesReach()
    {
        var scenario = Create();
        var wave = scenario.GetFieldWave("D1");

        scenario.Clock.AdvanceDays(2);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.SeedWave[0].Shipment.Status);

        Assert.AreEqual(
            1.0 / 3.0,
            wave.ObservedDeliveredReach,
            1e-9);

        Assert.AreEqual(SIRState.I,
            scenario.FieldSIR.GetFieldState(
                scenario.Field, scenario.Demo));
    }

    [Test]
    public void Scenario_NoEligibleSeedKeepsFieldInS()
    {
        var scenario = Create(knowsMissing: false);
        var wave = scenario.GetFieldWave("D1");

        Assert.AreEqual(0, scenario.SeedWave.Count);

        Assert.AreEqual(SIRState.S,
            scenario.FieldSIR.GetFieldState(
                scenario.Field, scenario.Demo));

        Assert.AreEqual(0, wave.ActiveCopierCount);

        Assert.AreEqual(
            0.0, wave.ObservedDeliveredReach, 1e-9);
    }

    [Test]
    public void Scenario_LaterDistributionUsesSameObserver()
    {
        var scenario = Create();
        var wave = scenario.GetFieldWave("D1");

        // Alustettu A -> B -lähetys saapuu.
        scenario.Clock.AdvanceDays(2);

        var received = scenario.SeedWave[0].Copy;

        Assert.AreEqual("B", received.HolderNodeId);

        var route = scenario.Network
            .GetOutgoingConnections(
                "B", CommunicationLayer.Interpersonal)
            .Single(c => c.TargetNodeId == "C");

        // Käytetään nimenomaan skenaarion
        // sisäistä jakelupalvelua.
        var shipments = scenario.Distribution.DispatchAll(
            "SCENARIO-BC",
            "B",
            received,
            new[]
            {
                new CopyDistributionRoute(
                    "C", route, "FI")
            },
            "FI",
            5,
            recipient => false,
            recipient => true);

        Assert.AreEqual(1, shipments.Count);

        // Ei käsin kutsuttua ObserveCopyDispatchia.
        Assert.AreEqual(2, wave.ActiveCopierCount);

        scenario.Network.TryGetNode("B", out var b);

        Assert.AreEqual(SIRState.I,
            scenario.FieldSIR.GetNodeState(
                b, scenario.Demo));

        scenario.Clock.AdvanceDays(2);

        Assert.AreEqual(
            2.0 / 3.0,
            wave.ObservedDeliveredReach,
            1e-9);
    }

    [Test]
    public void Scenario_EachDemoHasIndependentSIRWave()
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddNode("C", "FI");

        definition.AddConnection("AC", "A", "C");
        definition.AddConnection("BC", "B", "C");

        definition.MarkKnownMissing("A", "C");
        definition.MarkKnownMissing("B", "C");

        definition.AddSeed("A", "D1");
        definition.AddSeed("B", "D2");

        var scenario =
            new ConfigurableCirculationScenario(definition);

        var d1 = scenario.GetDemo("D1");
        var d2 = scenario.GetDemo("D2");

        var wave1 = scenario.GetFieldWave("D1");
        var wave2 = scenario.GetFieldWave("D2");

        Assert.AreEqual(1, wave1.ActiveCopierCount);
        Assert.AreEqual(1, wave2.ActiveCopierCount);

        scenario.Network.TryGetNode("A", out var a);
        scenario.Network.TryGetNode("B", out var b);

        Assert.AreEqual(SIRState.I,
            scenario.FieldSIR.GetNodeState(a, d1));

        Assert.AreEqual(SIRState.S,
            scenario.FieldSIR.GetNodeState(a, d2));

        Assert.AreEqual(SIRState.S,
            scenario.FieldSIR.GetNodeState(b, d1));

        Assert.AreEqual(SIRState.I,
            scenario.FieldSIR.GetNodeState(b, d2));

        scenario.Clock.AdvanceDays(2);

        Assert.AreEqual(
            1.0 / 3.0,
            wave1.ObservedDeliveredReach,
            1e-9);

        Assert.AreEqual(
            1.0 / 3.0,
            wave2.ObservedDeliveredReach,
            1e-9);
    }
}
