
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class MultipleDemoCirculationTests
{
    private static CirculationWorldDefinition Definition()
    {
        var d = new CirculationWorldDefinition();

        d.AddNode("A", "FI");
        d.AddNode("B", "FI");
        d.AddNode("C", "FI");

        d.AddConnection("AB", "A", "B");
        d.AddConnection("BC", "B", "C");

        d.MarkKnownMissing("A", "B");
        d.MarkKnownMissing("B", "C");

        d.SetSeed("A", "D1");
        d.AddSeed("A", "D2");

        return d;
    }

    [Test]
    public void Definition_StoresTwoIndependentSeeds()
    {
        var d = Definition();

        Assert.AreEqual(2, d.Seeds.Count);
        Assert.AreEqual("A", d.Seeds[0].SourceNodeId);
        Assert.AreEqual("D1", d.Seeds[0].DemoId);
        Assert.AreEqual("D2", d.Seeds[1].DemoId);

        // Vanha rajapinta osoittaa ensimmäiseen.
        Assert.AreEqual("A", d.SeedNodeId);
        Assert.AreEqual("D1", d.DemoId);
    }

    [Test]
    public void Definition_RejectsDuplicateDemoIds()
    {
        var d = Definition();

        Assert.Throws<ArgumentException>(() =>
            d.AddSeed("A", "D1"));

        Assert.Throws<ArgumentException>(() =>
            d.AddSeed("UNKNOWN", "D3"));

        Assert.AreEqual(2, d.Seeds.Count);
    }

    [Test]
    public void Definition_SetSeedReplacesPreviousSeeds()
    {
        var d = Definition();

        d.SetSeed("B", "D3");

        Assert.AreEqual(1, d.Seeds.Count);
        Assert.AreEqual("B", d.SeedNodeId);
        Assert.AreEqual("D3", d.DemoId);
    }

    [Test]
    public void World_InitializesBothPhysicalProducts()
    {
        var s = new ConfigurableCirculationScenario(
            Definition());

        CollectionAssert.AreEqual(
            new[] { "D1", "D2" },
            s.ProductIds);

        Assert.IsNotNull(s.GetDemo("D1"));
        Assert.IsNotNull(s.GetDemo("D2"));

        Assert.AreNotSame(
            s.GetOriginal("D1"),
            s.GetOriginal("D2"));

        Assert.AreEqual("A",
            s.GetOriginal("D1").HolderNodeId);

        Assert.AreEqual("A",
            s.GetOriginal("D2").HolderNodeId);

        Assert.AreEqual(1, s.GetSeedWave("D1").Count);
        Assert.AreEqual(1, s.GetSeedWave("D2").Count);

        Assert.AreEqual(2, s.CopyIndex.Count);
        Assert.AreEqual(2, s.Clock.PendingCount);

        // Vanhan yhden demon rajapinnan
        // yhteensopivuus säilyy.
        Assert.AreSame(s.GetDemo("D1"), s.Demo);
        Assert.AreSame(s.GetOriginal("D1"), s.Original);
    }

    [Test]
    public void World_BacklogSeparatesTwoDemoMessages()
    {
        var s = new ConfigurableCirculationScenario(
            Definition());

        s.World.AdvanceDays(2);

        Assert.AreEqual(1, s.World.OpenedMessages.Count);
        Assert.AreEqual(1, s.Backlog.GetPendingCount("B"));

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.IsNull(
            s.GetListeningStatus("B", "D2"));

        s.World.AdvanceDay();

        Assert.AreEqual(2, s.World.OpenedMessages.Count);
        Assert.AreEqual(0, s.Backlog.GetPendingCount("B"));

        Assert.AreEqual(
            ListeningPhase.Queued,
            s.GetListeningStatus("B", "D2").Phase);
    }

    [Test]
    public void World_ListensToEachDemoIndependently()
    {
        var s = new ConfigurableCirculationScenario(
            Definition());

        s.World.AdvanceDays(4);

        Assert.AreEqual(
            ListeningPhase.Completed,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("B", "D2").Phase);

        Assert.AreEqual(4,
            s.GetListeningStatus("B", "D2").StartedDay);

        Assert.AreEqual(1,
            s.GetPolicy("B").Dispatched.Count);

        s.World.AdvanceDays(2);

        Assert.AreEqual(
            ListeningPhase.Completed,
            s.GetListeningStatus("B", "D2").Phase);

        Assert.AreEqual(2,
            s.GetPolicy("B").Dispatched.Count);

        CollectionAssert.AreEquivalent(
            new[] { "D1", "D2" },
            s.GetPolicy("B").Dispatched
                .Select(x => x.Copy.ProductId)
                .ToArray());
    }

    [Test]
    public void World_TwoDemosReachCWithoutMixingCopies()
    {
        var s = new ConfigurableCirculationScenario(
            Definition());

        s.World.AdvanceDays(12);

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("C", "D2"));

        Assert.AreEqual(4, s.CopyIndex.Count);
        Assert.AreEqual(4, s.World.OpenedMessages.Count);

        Assert.AreEqual(18, s.Capital.GetCapital("A"));
        Assert.AreEqual(18, s.Capital.GetCapital("B"));
        Assert.AreEqual(10, s.Capital.GetCapital("C"));

        Assert.AreEqual(0, s.Clock.PendingCount);
    }

    [Test]
    public void World_ReactionsRemainProductSpecific()
    {
        var rules = new NodeReactionRules();

        rules.Set(
            "B", "D1",
            ListeningReaction.OpposeAndShelve);

        rules.Set(
            "B", "D2",
            ListeningReaction.SupportAndCopy);

        var s = new ConfigurableCirculationScenario(
            Definition(),
            reactionRules: rules);

        s.World.AdvanceDays(12);

        Assert.AreEqual(1, s.Control.Shelves.Count);
        Assert.AreEqual("D1",
            s.Control.Shelves[0].ProductId);

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("C", "D2"));

        Assert.AreEqual(1,
            s.GetPolicy("B").Dispatched.Count);

        Assert.AreEqual("D2",
            s.GetPolicy("B").Dispatched[0].Copy.ProductId);

        Assert.AreEqual(14, s.Capital.GetCapital("B"));
    }

    [Test]
    public void World_AllowsDifferentSeedNodes()
    {
        var d = new CirculationWorldDefinition();

        d.AddNode("A", "FI");
        d.AddNode("B", "FI");
        d.AddNode("C", "FI");

        d.AddConnection("AB", "A", "B");
        d.AddConnection("CB", "C", "B");

        d.MarkKnownMissing("A", "B");
        d.MarkKnownMissing("C", "B");

        d.SetSeed("A", "D1");
        d.AddSeed("C", "D2");

        var s = new ConfigurableCirculationScenario(d);

        Assert.AreEqual("A",
            s.GetOriginal("D1").HolderNodeId);

        Assert.AreEqual("C",
            s.GetOriginal("D2").HolderNodeId);

        s.World.AdvanceDays(3);

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("B", "D1"));

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("B", "D2"));

        Assert.AreEqual(2, s.World.OpenedMessages.Count);
    }

    [Test]
    public void World_MultipleDemoRunsAreDeterministic()
    {
        var a = new ConfigurableCirculationScenario(
            Definition());

        var b = new ConfigurableCirculationScenario(
            Definition());

        a.World.AdvanceDays(12);

        for (int i = 0; i < 12; i++)
            b.World.AdvanceDay();

        Assert.AreEqual(
            a.World.OpenedMessages.Count,
            b.World.OpenedMessages.Count);

        Assert.AreEqual(
            a.CopyIndex.Count,
            b.CopyIndex.Count);

        foreach (string id in a.NodeIds)
        {
            Assert.AreEqual(
                a.Capital.GetCapital(id),
                b.Capital.GetCapital(id));

            foreach (string productId in a.ProductIds)
            {
                Assert.AreEqual(
                    a.Inventory.HasEverHeldProduct(id, productId),
                    b.Inventory.HasEverHeldProduct(id, productId));
            }
        }
    }
}
