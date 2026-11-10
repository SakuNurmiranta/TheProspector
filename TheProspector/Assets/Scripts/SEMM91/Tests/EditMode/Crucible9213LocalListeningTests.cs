
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible9213LocalListeningTests
{
    private static ConfigurableCirculationScenario Isolated(
        int listeningDays = 2)
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.SetSeed("A", "D1");

        return new ConfigurableCirculationScenario(
            definition,
            listeningDays: listeningDays,
            fieldDiscoveryRandom: new Random(9213));
    }

    [Test]
    public void LegacyScenario_DoesNotAutomaticallyStartLocalListening()
    {
        var scenario = Isolated();

        scenario.World.AdvanceDays(4);

        Assert.IsNull(
            scenario.GetListeningStatus("A", "D1"));

        Assert.AreEqual(
            0,
            scenario.GetReactionPolicy("A").Decisions.Count);

        Assert.AreEqual(
            0,
            scenario.World.OpenedMessages.Count);
    }

    [Test]
    public void ConfiguredWorld_QueuesInitialOriginalAtDayZero()
    {
        var session = DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 0);

        var firstSeed = session.InitialWorld.Definition.Seeds[0];

        var status = session.Scenario.GetListeningStatus(
            firstSeed.SourceNodeId,
            firstSeed.DemoId);

        Assert.IsNotNull(status);
        Assert.AreEqual(
            ListeningPhase.Listening,
            status.Phase);

        Assert.AreEqual(0, status.StartedDay);
        Assert.AreEqual(0, session.OpenedMessageCount);
        Assert.AreEqual(15, session.ProductCount);
    }

    [Test]
    public void LocalOriginal_CompletesWithoutPostOrCapitalReward()
    {
        var scenario = Isolated();

        scenario.ActivateLocalListening();

        Assert.AreEqual(
            ListeningPhase.Listening,
            scenario.GetListeningStatus("A", "D1").Phase);

        scenario.World.AdvanceDay();

        Assert.AreEqual(
            ListeningPhase.Listening,
            scenario.GetListeningStatus("A", "D1").Phase);

        scenario.World.AdvanceDay();

        var status = scenario.GetListeningStatus("A", "D1");

        Assert.AreEqual(
            ListeningPhase.Completed,
            status.Phase);

        Assert.AreEqual(2, status.CompletedDay);

        Assert.AreEqual(
            1,
            scenario.GetReactionPolicy("A").Decisions.Count);

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            scenario.GetReactionPolicy("A").Decisions[0].Reaction);

        Assert.AreEqual(
            1,
            scenario.GetPolicy("A").CopyDecisions);

        Assert.AreEqual(
            0,
            scenario.GetPolicy("A").Dispatched.Count);

        Assert.AreEqual(
            0,
            scenario.World.OpenedMessages.Count);

        Assert.AreEqual(
            10,
            scenario.Capital.GetCapital("A"));
    }

    [Test]
    public void RepeatedActivation_DoesNotDuplicateListening()
    {
        var scenario = Isolated();

        scenario.ActivateLocalListening();
        scenario.ActivateLocalListening();

        scenario.World.AdvanceDays(5);

        Assert.AreEqual(
            1,
            scenario.GetReactionPolicy("A").Decisions.Count);

        Assert.AreEqual(
            1,
            scenario.GetPolicy("A").CopyDecisions);

        Assert.AreEqual(
            0,
            scenario.World.OpenedMessages.Count);
    }

    [Test]
    public void NewFieldDemo_WaitsInExistingListeningQueue()
    {
        var scenario = Isolated();

        scenario.ActivateLocalListening();

        var birth = scenario.SpawnFieldDemo(
            "D2",
            "ART-D2",
            new Random(42));

        Assert.AreEqual(
            "A",
            birth.InitialHolderNodeId);

        Assert.AreEqual(
            0,
            scenario.GetSeedWave("D2").Count);

        Assert.AreEqual(
            ListeningPhase.Listening,
            scenario.GetListeningStatus("A", "D1").Phase);

        Assert.AreEqual(
            ListeningPhase.Queued,
            scenario.GetListeningStatus("A", "D2").Phase);

        scenario.World.AdvanceDays(2);

        Assert.AreEqual(
            ListeningPhase.Completed,
            scenario.GetListeningStatus("A", "D1").Phase);

        Assert.AreEqual(
            ListeningPhase.Listening,
            scenario.GetListeningStatus("A", "D2").Phase);

        scenario.World.AdvanceDays(2);

        Assert.AreEqual(
            ListeningPhase.Completed,
            scenario.GetListeningStatus("A", "D2").Phase);

        Assert.AreEqual(
            2,
            scenario.GetReactionPolicy("A").Decisions.Count);

        Assert.AreEqual(
            2,
            scenario.GetPolicy("A").CopyDecisions);

        Assert.AreEqual(
            0,
            scenario.World.OpenedMessages.Count);

        Assert.AreEqual(
            10,
            scenario.Capital.GetCapital("A"));
    }

    [Test]
    public void LocalCompletion_UsesNewlyEstablishedDistributionRoute()
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.SetSeed("A", "D1");

        var scenario = new ConfigurableCirculationScenario(
            definition,
            listeningDays: 2);

        scenario.ActivateLocalListening();

        scenario.Network.LearnAddress(
            "A", "B", "ADDR-B");

        scenario.Network.AddConnection(
            "AB-LOCAL-9213",
            "A",
            "B",
            CommunicationLayer.Interpersonal,
            "personal");

        // The postal address and the collection information
        // are different things. Supply both explicitly.
        scenario.Known.RecordSnapshot(
            "A", "B", Array.Empty<string>());

        Assert.AreEqual(
            0,
            scenario.GetSeedWave("D1").Count);

        scenario.World.AdvanceDays(2);

        var dispatched =
            scenario.GetPolicy("A").Dispatched;

        Assert.AreEqual(1, dispatched.Count);

        Assert.AreEqual(
            "B",
            dispatched[0].RecipientNodeId);

        Assert.AreEqual(
            "D1",
            dispatched[0].Copy.ProductId);

        Assert.AreEqual(
            0,
            scenario.World.OpenedMessages.Count);
    }

    [Test]
    public void LocalNegativeDecision_CanShelveWithoutPost()
    {
        var scenario = Isolated();

        scenario.ReactionRules.Set(
            "A",
            "D1",
            ListeningReaction.OpposeAndShelve);

        scenario.ActivateLocalListening();
        scenario.World.AdvanceDays(2);

        Assert.AreEqual(
            ListeningReaction.OpposeAndShelve,
            scenario.GetReactionPolicy("A")
                .Decisions.Single().Reaction);

        Assert.AreEqual(
            1,
            scenario.Control.Shelves.Count);

        Assert.AreEqual(
            0,
            scenario.GetPolicy("A").Dispatched.Count);

        Assert.AreEqual(
            0,
            scenario.World.OpenedMessages.Count);

        Assert.AreEqual(
            10,
            scenario.Capital.GetCapital("A"));
    }
}
