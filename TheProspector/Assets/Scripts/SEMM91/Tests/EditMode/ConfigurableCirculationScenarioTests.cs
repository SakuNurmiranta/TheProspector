
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class ConfigurableCirculationScenarioTests
{
    private static CirculationWorldDefinition Chain()
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("X", "FI");
        definition.AddNode("Y", "FI");
        definition.AddNode("Z", "FI");

        definition.AddConnection("XY", "X", "Y");
        definition.AddConnection("YZ", "Y", "Z");

        definition.MarkKnownMissing("X", "Y");
        definition.MarkKnownMissing("Y", "Z");

        definition.SetSeed("X", "D1");

        return definition;
    }

    private static CirculationWorldDefinition Branch(
        bool bKnowsD = true)
    {
        var definition = new CirculationWorldDefinition();

        foreach (string id in new[] { "A", "B", "C", "D", "E" })
            definition.AddNode(id, "FI");

        definition.AddConnection("AB", "A", "B");
        definition.AddConnection("BC", "B", "C");
        definition.AddConnection("BD", "B", "D");
        definition.AddConnection("DE", "D", "E");

        definition.MarkKnownMissing("A", "B");
        definition.MarkKnownMissing("B", "C");

        if (bKnowsD)
            definition.MarkKnownMissing("B", "D");

        definition.MarkKnownMissing("D", "E");

        definition.SetSeed("A", "D1");

        return definition;
    }

    [Test]
    public void World_UsesArbitraryNodeIdentifiers()
    {
        var s = new ConfigurableCirculationScenario(Chain());

        CollectionAssert.AreEqual(
            new[] { "X", "Y", "Z" },
            s.NodeIds);

        Assert.AreEqual(3, s.World.RegisteredNodeCount);
        Assert.AreEqual(0, s.World.CurrentDay);
        Assert.AreEqual(1, s.SeedWave.Count);
        Assert.AreEqual("X", s.Original.HolderNodeId);
        Assert.AreEqual(1, s.CopyIndex.Count);

        Assert.IsNotNull(s.GetReactionPolicy("Y"));
        Assert.IsNotNull(s.GetListeningStatus("Y", "D1") == null
            ? s.GetReactionPolicy("Y")
            : null);
    }

    [Test]
    public void World_ThreeNodeChainRunsAutonomously()
    {
        var s = new ConfigurableCirculationScenario(Chain());

        s.World.AdvanceDays(10);

        Assert.AreEqual(10, s.World.CurrentDay);
        Assert.AreEqual(1, s.SeedWave.Count);
        Assert.AreEqual(1, s.GetPolicy("Y").Dispatched.Count);
        Assert.AreEqual(0, s.GetPolicy("Z").Dispatched.Count);

        // Y lähettää Z:lle yhden kopion.
        Assert.AreEqual(1, s.GetPolicy("Y").Dispatched.Count);
        Assert.AreEqual(0, s.GetPolicy("Z").Dispatched.Count);

        Assert.AreEqual(2, s.World.OpenedMessages.Count);
        Assert.IsTrue(s.Inventory.HasEverHeldProduct("Z", "D1"));

        Assert.AreEqual(14, s.Capital.GetCapital("X"));
        Assert.AreEqual(14, s.Capital.GetCapital("Y"));
        Assert.AreEqual(10, s.Capital.GetCapital("Z"));
    }

    [Test]
    public void World_FiveNodesSupportBranching()
    {
        var s = new ConfigurableCirculationScenario(Branch());

        s.World.AdvanceDays(12);

        Assert.AreEqual(5, s.World.RegisteredNodeCount);
        Assert.AreEqual(2, s.GetPolicy("B").Dispatched.Count);

        CollectionAssert.AreEquivalent(
            new[] { "C", "D" },
            s.GetPolicy("B").Dispatched
                .Select(x => x.RecipientNodeId)
                .ToArray());

        Assert.AreEqual(1, s.GetPolicy("D").Dispatched.Count);
        Assert.AreEqual(0, s.GetPolicy("C").Dispatched.Count);

        Assert.IsTrue(s.Inventory.HasEverHeldProduct("E", "D1"));

        Assert.AreEqual(14, s.Capital.GetCapital("A"));
        Assert.AreEqual(18, s.Capital.GetCapital("B"));
        Assert.AreEqual(14, s.Capital.GetCapital("D"));

        Assert.AreEqual(4, s.CopyIndex.Count);
        Assert.AreEqual(4, s.World.OpenedMessages.Count);
    }

    [Test]
    public void World_UnknownCollectionPreventsOneBranch()
    {
        var s = new ConfigurableCirculationScenario(
            Branch(bKnowsD: false));

        s.World.AdvanceDays(12);

        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);
        Assert.AreEqual(
            "C",
            s.GetPolicy("B").Dispatched[0].RecipientNodeId);

        Assert.IsTrue(s.Inventory.HasEverHeldProduct("C", "D1"));
        Assert.IsFalse(s.Inventory.HasEverHeldProduct("D", "D1"));
        Assert.IsFalse(s.Inventory.HasEverHeldProduct("E", "D1"));

        Assert.AreEqual(2, s.CopyIndex.Count);
        Assert.AreEqual(14, s.Capital.GetCapital("B"));
    }

    [Test]
    public void World_HardControlStopsPropagation()
    {
        var rules = new NodeReactionRules();

        rules.Set(
            "Y", "D1",
            ListeningReaction.OpposeAndShelve);

        var s = new ConfigurableCirculationScenario(
            Chain(), reactionRules: rules);

        s.World.AdvanceDays(12);

        Assert.AreEqual(1, s.Control.Shelves.Count);
        Assert.AreEqual(0, s.Control.Transfers.Count);
        Assert.AreEqual("Y", s.Control.Shelves[0].NodeId);

        Assert.IsFalse(s.Inventory.HasEverHeldProduct("Z", "D1"));
        Assert.AreEqual(1, s.CopyIndex.Count);
        Assert.AreEqual(10, s.Capital.GetCapital("Y"));
    }

    [Test]
    public void World_SoftControlTransfersSameCassetteThroughChain()
    {
        var definition = Chain();

        definition.AddNode("W", "FI");
        definition.AddConnection("ZW", "Z", "W");
        definition.MarkKnownMissing("Z", "W");

        var rules = new NodeReactionRules();
        rules.Set("Y", "D1", ListeningReaction.OpposeAndTransfer);
        rules.Set("Z", "D1", ListeningReaction.OpposeAndTransfer);

        var s = new ConfigurableCirculationScenario(
            definition, reactionRules: rules);

        s.World.AdvanceDays(12);

        Assert.AreEqual(2, s.Control.Transfers.Count);
        Assert.AreEqual(0, s.Control.Shelves.Count);

        var copy = s.SeedWave[0].Copy;

        Assert.AreSame(copy, s.Control.Transfers[0].Copy);
        Assert.AreSame(copy, s.Control.Transfers[1].Copy);

        Assert.AreEqual("W", copy.HolderNodeId);
        Assert.IsTrue(s.Inventory.HasEverHeldProduct("W", "D1"));

        Assert.AreEqual(3, s.CopyIndex.Count);

        Assert.AreEqual(14, s.Capital.GetCapital("X"));
        Assert.AreEqual(10, s.Capital.GetCapital("Y"));
        Assert.AreEqual(10, s.Capital.GetCapital("Z"));
    }

    [Test]
    public void World_ReactionRulesAreReadOnCompletion()
    {
        var rules = new NodeReactionRules();
        var s = new ConfigurableCirculationScenario(
            Chain(), reactionRules: rules);

        s.World.AdvanceDays(3);

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("Y", "D1").Phase);

        rules.Set(
            "Y", "D1",
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDay();

        Assert.AreEqual(1, s.Control.Shelves.Count);
        Assert.AreEqual(0, s.GetPolicy("Y").Dispatched.Count);
    }

    [Test]
    public void World_InternationalMailUsesActualTransitRules()
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("X", "FI");
        definition.AddNode("Y", "SE");
        definition.AddNode("Z", "SE");

        definition.AddConnection("XY", "X", "Y");
        definition.AddConnection("YZ", "Y", "Z");

        definition.MarkKnownMissing("X", "Y");
        definition.MarkKnownMissing("Y", "Z");
        definition.SetSeed("X", "D1");

        var s = new ConfigurableCirculationScenario(definition);

        s.World.AdvanceDays(5);

        Assert.AreEqual(1, s.World.OpenedMessages.Count);
        Assert.AreEqual(5, s.World.OpenedMessages[0].Day);
        Assert.AreEqual("Y", s.World.OpenedMessages[0].NodeId);

        s.World.AdvanceDays(4);

        Assert.AreEqual(2, s.World.OpenedMessages.Count);
        Assert.AreEqual(9, s.World.OpenedMessages[1].Day);
        Assert.AreEqual("Z", s.World.OpenedMessages[1].NodeId);
    }

    [Test]
    public void World_CanStartWithoutEligibleRecipients()
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("X", "FI");
        definition.AddNode("Y", "FI");
        definition.AddConnection("XY", "X", "Y");
        definition.SetSeed("X", "D1");

        var s = new ConfigurableCirculationScenario(definition);

        Assert.AreEqual(0, s.SeedWave.Count);

        s.World.AdvanceDays(30);

        Assert.AreEqual(30, s.World.CurrentDay);
        Assert.AreEqual(0, s.World.OpenedMessages.Count);
        Assert.AreEqual(0, s.CopyIndex.Count);
        Assert.AreEqual(0, s.Clock.PendingCount);

        Assert.AreEqual(10, s.Capital.GetCapital("X"));
        Assert.AreEqual(10, s.Capital.GetCapital("Y"));
    }

    [Test]
    public void World_DefinitionRejectsInvalidReferences()
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("X", "FI");

        Assert.Throws<ArgumentException>(() =>
            definition.AddNode("X", "FI"));

        Assert.Throws<ArgumentException>(() =>
            definition.AddConnection("BAD", "X", "UNKNOWN"));

        Assert.Throws<ArgumentException>(() =>
            definition.MarkKnownMissing("X", "UNKNOWN"));

        Assert.Throws<ArgumentException>(() =>
            definition.SetSeed("UNKNOWN", "D1"));

        Assert.Throws<InvalidOperationException>(() =>
            new ConfigurableCirculationScenario(definition));

        definition.AddNode("Y", "FI");
        definition.AddConnection("XY", "X", "Y");

        Assert.Throws<ArgumentException>(() =>
            definition.AddConnection("XY", "X", "Y"));

        definition.SetSeed("X", "D1");
    }

    [Test]
    public void World_ParallelArcsDoNotDuplicateRecipients()
    {
        var definition = Chain();

        definition.AddConnection("YZ-SECOND", "Y", "Z");

        var s = new ConfigurableCirculationScenario(definition);

        s.World.AdvanceDays(8);

        Assert.AreEqual(1, s.GetPolicy("Y").Dispatched.Count);
        Assert.AreEqual(2, s.CopyIndex.Count);
        Assert.IsTrue(s.Inventory.HasEverHeldProduct("Z", "D1"));
    }

    [Test]
    public void World_BatchedAndSteppedExecutionMatch()
    {
        var first = new ConfigurableCirculationScenario(Branch());
        var second = new ConfigurableCirculationScenario(Branch());

        first.World.AdvanceDays(15);

        for (int day = 0; day < 15; day++)
            second.World.AdvanceDay();

        Assert.AreEqual(15, first.World.CurrentDay);
        Assert.AreEqual(15, second.World.CurrentDay);

        Assert.AreEqual(
            first.CopyIndex.Count, second.CopyIndex.Count);

        Assert.AreEqual(
            first.World.OpenedMessages.Count,
            second.World.OpenedMessages.Count);

        foreach (string id in first.NodeIds)
        {
            Assert.AreEqual(
                first.Capital.GetCapital(id),
                second.Capital.GetCapital(id));

            Assert.AreEqual(
                first.Inventory.HasEverHeldProduct(id, "D1"),
                second.Inventory.HasEverHeldProduct(id, "D1"));
        }

        Assert.AreEqual(0, first.Clock.PendingCount);
        Assert.AreEqual(0, second.Clock.PendingCount);
    }
}
