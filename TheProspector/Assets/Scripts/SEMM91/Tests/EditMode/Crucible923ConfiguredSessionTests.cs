
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible923ConfiguredSessionTests
{
    private static DemoSession Create()
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters());
    }

    [Test]
    public void ConfiguredSession_StartsWithExpectedPopulation()
    {
        var session = Create();

        Assert.AreEqual(0, session.Day);
        Assert.AreEqual(100, session.NodeCount);
        Assert.AreEqual(200, session.AddressCount);
        Assert.AreEqual(15, session.ProductCount);

        Assert.AreEqual(
            100,
            session.Scenario.World.RegisteredNodeCount);

        Assert.AreEqual(
            "D001",
            session.PrimaryProductId);
    }

    [Test]
    public void ConfiguredSession_PreservesInitialWorldMetadata()
    {
        var session = Create();
        var initial = session.InitialWorld;

        Assert.IsNotNull(initial);
        Assert.AreEqual(7, initial.ZineListedNodeIds.Count);
        Assert.AreEqual(8, initial.UnlistedDemoOwnerIds.Count);

        Assert.Contains(
            initial.EditorNodeId,
            initial.ZineListedNodeIds.ToList());

        foreach (var seed in initial.Definition.Seeds)
        {
            Assert.IsTrue(
                session.Scenario.Inventory.HasEverHeldProduct(
                    seed.SourceNodeId,
                    seed.DemoId));
        }
    }

    [Test]
    public void ConfiguredSession_AdvancesSimulationDays()
    {
        var session = Create();

        session.AdvanceDay();
        Assert.AreEqual(1, session.Day);

        session.AdvanceDays(2);
        Assert.AreEqual(3, session.Day);

        Assert.AreEqual(
            100,
            session.Scenario.World.RegisteredNodeCount);
    }

    [Test]
    public void ConfiguredSession_ProducesCompleteVisualSnapshot()
    {
        var session = Create();

        var observation =
            session.Scenario.Observation.Capture();

        var visual =
            CirculationVisualProjection.Build(observation);

        Assert.AreEqual(100, observation.Nodes.Count);
        Assert.AreEqual(15, observation.ProductIds.Count);
        Assert.AreEqual(100, visual.Nodes.Count);

        Assert.AreEqual(
            observation.Connections.Count,
            visual.Connections.Count);

        Assert.IsTrue(
            visual.Nodes.Any(n => n.Id == session.InitialWorld.EditorNodeId));
    }

    [Test]
    public void ConfiguredSession_LegacyIndicatorsDoNotThrow()
    {
        var session = Create();

        Assert.DoesNotThrow(() =>
        {
            var state = session.FieldState;
            var reach = session.DeliveredReach;
            var copiers = session.ActiveCopiers;
            var shipments = session.BCopyShipments;
            var discovered = session.DHasDiscovered;
            var listening = session.GetListeningPhase("A001");
        });

        // These legacy indicators refer to nodes B and D.
        // Neither exists in the configured A001/B001 population.
        Assert.AreEqual(0, session.BCopyShipments);
        Assert.IsFalse(session.DHasDiscovered);
    }

    [Test]
    public void ConfiguredSession_SameSeedProducesSameInitialState()
    {
        var parameters =
            new CirculationInitializationParameters
            {
                RandomSeed = 24680
            };

        var first = DemoSession.CreateConfigured(parameters);
        var second = DemoSession.CreateConfigured(parameters);

        Assert.AreEqual(
            first.InitialWorld.EditorNodeId,
            second.InitialWorld.EditorNodeId);

        CollectionAssert.AreEqual(
            first.InitialWorld.ZineListedNodeIds,
            second.InitialWorld.ZineListedNodeIds);

        CollectionAssert.AreEqual(
            first.InitialWorld.UnlistedDemoOwnerIds,
            second.InitialWorld.UnlistedDemoOwnerIds);

        CollectionAssert.AreEqual(
            first.InitialWorld.Definition.Placements
                .Select(p => p.NodeId + "@" + p.AddressId),
            second.InitialWorld.Definition.Placements
                .Select(p => p.NodeId + "@" + p.AddressId));

        CollectionAssert.AreEqual(
            first.Scenario.ProductIds,
            second.Scenario.ProductIds);
    }

    [Test]
    public void ConfiguredSession_AcceptsSmallerValidPopulation()
    {
        var parameters =
            new CirculationInitializationParameters
            {
                CountryAPopulation = 10,
                CountryBPopulation = 20,
                AddressSlotsPerCountry = 20,
                FriendshipParticipationPercent = 20,
                RandomSeed = 923
            };

        var session =
            DemoSession.CreateConfigured(parameters);

        Assert.AreEqual(30, session.NodeCount);
        Assert.AreEqual(40, session.AddressCount);
        Assert.AreEqual(15, session.ProductCount);

        session.AdvanceDay();

        Assert.AreEqual(1, session.Day);
    }

    [Test]
    public void ConfiguredSession_DoesNotBreakLegacyDemo()
    {
        var legacy = DemoSession.CreateDefault();

        Assert.IsNull(legacy.InitialWorld);
        Assert.AreEqual(4, legacy.NodeCount);
        Assert.AreEqual(4, legacy.AddressCount);
        Assert.AreEqual(1, legacy.ProductCount);
        Assert.AreEqual("D1", legacy.PrimaryProductId);

        legacy.AdvanceDay();

        Assert.AreEqual(1, legacy.Day);
    }

    [Test]
    public void ConfiguredSession_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DemoSession.CreateConfigured(null));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DemoSession.CreateConfigured(
                new CirculationInitializationParameters(),
                listeningDays: 0));
    }
}
