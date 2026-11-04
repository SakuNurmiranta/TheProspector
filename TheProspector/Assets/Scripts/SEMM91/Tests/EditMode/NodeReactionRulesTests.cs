
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class NodeReactionRulesTests
{
    private static NodeReactionRules Rules()
    {
        return new NodeReactionRules();
    }

    private static AutonomousCirculationScenario Create(
        NodeReactionRules rules)
    {
        return new AutonomousCirculationScenario(
            listeningDays: 2,
            reactionRules: rules);
    }

    [Test]
    public void Rules_DefaultIsSupportAndCopy()
    {
        var rules = Rules();

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            rules.Choose("A", "D1"));

        Assert.AreEqual(0, rules.Count);
    }

    [Test]
    public void Rules_AreSpecificToNodeAndProduct()
    {
        var rules = Rules();

        rules.Set(
            "B", "D1",
            ListeningReaction.OpposeAndTransfer);

        rules.Set(
            "B", "D2",
            ListeningReaction.OpposeAndShelve);

        Assert.AreEqual(
            ListeningReaction.OpposeAndTransfer,
            rules.Choose("B", "D1"));

        Assert.AreEqual(
            ListeningReaction.OpposeAndShelve,
            rules.Choose("B", "D2"));

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            rules.Choose("C", "D1"));

        Assert.AreEqual(2, rules.Count);
    }

    [Test]
    public void Rules_CanBeUpdated()
    {
        var rules = Rules();

        rules.Set(
            "B", "D1",
            ListeningReaction.SupportAndCopy);

        rules.Set(
            "B", "D1",
            ListeningReaction.OpposeAndShelve);

        Assert.AreEqual(
            ListeningReaction.OpposeAndShelve,
            rules.Choose("B", "D1"));

        Assert.AreEqual(1, rules.Count);
    }

    [Test]
    public void Rules_RejectInvalidInput()
    {
        var rules = Rules();

        Assert.Throws<ArgumentException>(() =>
            rules.Set(
                "", "D1",
                ListeningReaction.SupportAndCopy));

        Assert.Throws<ArgumentException>(() =>
            rules.Set(
                "B", "",
                ListeningReaction.SupportAndCopy));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            rules.Set(
                "B", "D1",
                (ListeningReaction)999));

        Assert.Throws<ArgumentException>(() =>
            rules.Choose("", "D1"));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new NodeReactionRules(
                (ListeningReaction)999));

        Assert.AreEqual(0, rules.Count);
    }

    [Test]
    public void Scenario_RegistersReactionPolicyForEveryNode()
    {
        var s = Create(Rules());

        foreach (string id in new[] { "A", "B", "C", "D" })
        {
            Assert.IsNotNull(s.GetReactionPolicy(id));
        }

        Assert.AreEqual(4, s.World.RegisteredNodeCount);
    }

    [Test]
    public void Scenario_DefaultRulesPreservePositiveChain()
    {
        var s = Create(Rules());

        s.World.AdvanceDays(12);

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            s.GetReactionPolicy("B")
                .Decisions[0].Reaction);

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            s.GetReactionPolicy("C")
                .Decisions[0].Reaction);

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(14, s.Capital.GetCapital("B"));
        Assert.AreEqual(14, s.Capital.GetCapital("C"));

        Assert.AreEqual(0, s.Control.Transfers.Count);
        Assert.AreEqual(0, s.Control.Shelves.Count);
    }

    [Test]
    public void Scenario_RuleCanShelveAtB()
    {
        var rules = Rules();

        rules.Set(
            "B", "D1",
            ListeningReaction.OpposeAndShelve);

        var s = Create(rules);

        s.World.AdvanceDays(12);

        Assert.AreEqual(1, s.Control.Shelves.Count);
        Assert.AreEqual(0, s.Control.Transfers.Count);

        Assert.AreEqual("B", s.SeedWave[0].Copy.HolderNodeId);

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.AreEqual(10, s.Capital.GetCapital("B"));
    }

    [Test]
    public void Scenario_DifferentNodesChooseDifferentReactions()
    {
        var rules = Rules();

        rules.Set(
            "B", "D1",
            ListeningReaction.OpposeAndTransfer);

        rules.Set(
            "C", "D1",
            ListeningReaction.OpposeAndShelve);

        var s = Create(rules);

        s.World.AdvanceDays(12);

        Assert.AreEqual(1, s.Control.Transfers.Count);
        Assert.AreEqual(1, s.Control.Shelves.Count);

        Assert.AreEqual("C", s.SeedWave[0].Copy.HolderNodeId);

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(10, s.Capital.GetCapital("B"));
        Assert.AreEqual(10, s.Capital.GetCapital("C"));
    }

    [Test]
    public void Scenario_UsesRuleAtListeningCompletion()
    {
        var rules = Rules();
        var s = Create(rules);

        s.World.AdvanceDays(3);

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("B", "D1").Phase);

        // Alkuperäinen oletus oli kannatus.
        // Vaihdetaan sääntö ennen päätöshetkeä.
        rules.Set(
            "B", "D1",
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDay();

        Assert.AreEqual(4, s.World.CurrentDay);

        Assert.AreEqual(
            ListeningReaction.OpposeAndShelve,
            s.GetReactionPolicy("B")
                .Decisions[0].Reaction);

        Assert.AreEqual(1, s.Control.Shelves.Count);
        Assert.AreEqual(0, s.GetPolicy("B").Dispatched.Count);
    }

    [Test]
    public void Scenario_CompletedDecisionIsNotReplayed()
    {
        var rules = Rules();
        var s = Create(rules);

        s.World.AdvanceDays(4);

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            s.GetReactionPolicy("B")
                .Decisions[0].Reaction);

        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);

        // Päätöksen jälkeinen muutos ei saa
        // peruuttaa jo tehtyä kopiointia.
        rules.Set(
            "B", "D1",
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(8);

        Assert.AreEqual(1,
            s.GetReactionPolicy("B").Decisions.Count);

        Assert.AreEqual(1,
            s.GetPolicy("B").Dispatched.Count);

        Assert.AreEqual(0, s.Control.Shelves.Count);

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("D", "D1"));
    }
}
