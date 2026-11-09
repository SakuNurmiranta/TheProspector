using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class Crucible921WorldInitializationTests
{
    [Test]
    public void ExplicitAddresses_AllowVacantSlotsAndCorrectKnownAddresses()
    {
        var definition = new CirculationWorldDefinition();
        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddAddress("H1", "FI");
        definition.AddAddress("H2", "FI");
        definition.AddAddress("H3", "FI");
        definition.PlaceNode("A", "H2");
        definition.PlaceNode("B", "H3");
        definition.AddConnection("A-B", "A", "B");
        definition.SetSeed("A", "D1");

        var scenario = new ConfigurableCirculationScenario(definition);

        Assert.AreEqual(2, scenario.Network.NodeCount);
        Assert.AreEqual(3, scenario.Network.AddressCount);
        Assert.IsFalse(scenario.Network.TryGetOccupant("H1", out _));
        Assert.IsTrue(scenario.Network.TryGetNodeAddress("A", out string aAddress));
        Assert.AreEqual("H2", aAddress);
        Assert.IsTrue(scenario.Network.TryGetKnownAddress("A", "B", out string known));
        Assert.AreEqual("H3", known);
    }

    [Test]
    public void ExplicitAddresses_RequireEveryNodeToBePlaced()
    {
        var definition = new CirculationWorldDefinition();
        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddAddress("H1", "FI");
        definition.PlaceNode("A", "H1");
        definition.SetSeed("A", "D1");

        Assert.Throws<InvalidOperationException>(
            () => new ConfigurableCirculationScenario(definition));
    }

    [Test]
    public void ExplicitAddresses_RejectDuplicateOccupancyAndForeignPlacement()
    {
        var definition = new CirculationWorldDefinition();
        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddNode("C", "SE");
        definition.AddAddress("H1", "FI");
        definition.AddAddress("H2", "SE");
        definition.PlaceNode("A", "H1");

        Assert.Throws<ArgumentException>(() => definition.PlaceNode("B", "H1"));
        Assert.Throws<ArgumentException>(() => definition.PlaceNode("C", "H1"));
        Assert.Throws<ArgumentException>(() => definition.PlaceNode("A", "H2"));
    }

    [Test]
    public void LegacyScenario_StillCreatesOneAddressPerPerson()
    {
        var definition = new CirculationWorldDefinition();
        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddConnection("A-B", "A", "B");
        definition.SetSeed("A", "D1");

        var scenario = new ConfigurableCirculationScenario(definition);

        Assert.AreEqual(2, scenario.Network.AddressCount);
        Assert.IsTrue(scenario.Network.HasAddress("ADDR-A"));
        Assert.IsTrue(scenario.Network.HasAddress("ADDR-B"));
        Assert.IsTrue(scenario.Network.TryGetKnownAddress("A", "B", out string address));
        Assert.AreEqual("ADDR-B", address);
    }

    [Test]
    public void DefaultPlan_HasHundredPeopleTwoHundredSlotsAndFifteenDemos()
    {
        var plan = CirculationInitialWorldFactory.Create(
            new CirculationInitializationParameters());
        var d = plan.Definition;

        Assert.AreEqual(100, d.Nodes.Count);
        Assert.AreEqual(200, d.Addresses.Count);
        Assert.AreEqual(100, d.Placements.Count);
        Assert.AreEqual(30, d.Nodes.Count(n => n.CountryCode == "A"));
        Assert.AreEqual(70, d.Nodes.Count(n => n.CountryCode == "B"));
        Assert.AreEqual(100, d.Addresses.Count(a => a.CountryCode == "A"));
        Assert.AreEqual(100, d.Addresses.Count(a => a.CountryCode == "B"));
        Assert.AreEqual(15, d.Seeds.Count);
        Assert.AreEqual(15, d.Seeds.Select(s => s.DemoId).Distinct().Count());
        Assert.AreEqual(15, d.Seeds.Select(s => s.SourceNodeId).Distinct().Count());
    }

    [Test]
    public void DefaultPlan_HasSevenListedBContactsAndEightOtherHolders()
    {
        var plan = CirculationInitialWorldFactory.Create(
            new CirculationInitializationParameters());

        Assert.AreEqual(7, plan.ZineListedNodeIds.Count);
        Assert.IsTrue(plan.ZineListedNodeIds.Contains(plan.EditorNodeId));
        Assert.IsTrue(plan.ZineListedNodeIds.All(id => id.StartsWith("B")));
        Assert.AreEqual(8, plan.UnlistedDemoOwnerIds.Count);
        Assert.AreEqual(2, plan.UnlistedDemoOwnerIds.Count(id => id.StartsWith("A")));
        Assert.AreEqual(6, plan.UnlistedDemoOwnerIds.Count(id => id.StartsWith("B")));
        Assert.IsFalse(plan.UnlistedDemoOwnerIds.Intersect(
            plan.ZineListedNodeIds).Any());
    }

    [Test]
    public void DefaultPlan_HasExactlyFortyFriendshipParticipants()
    {
        var plan = CirculationInitialWorldFactory.Create(
            new CirculationInitializationParameters());
        var members = plan.FriendCircles.SelectMany(circle => circle).ToList();

        Assert.AreEqual(40, members.Count);
        Assert.AreEqual(40, members.Distinct().Count());
        Assert.AreEqual(12, members.Count(id => id.StartsWith("A")));
        Assert.AreEqual(28, members.Count(id => id.StartsWith("B")));
        Assert.IsTrue(plan.FriendCircles.All(circle =>
            circle.Count >= 2 && circle.Count <= 5));
        Assert.IsTrue(plan.FriendCircles.All(circle =>
            circle.All(id => id[0] == circle[0][0])));
        Assert.IsTrue(plan.Definition.Connections.All(edge =>
            edge.SenderId[0] == edge.RecipientId[0]));
        Assert.IsTrue(plan.Definition.Connections.All(edge =>
            plan.Definition.Connections.Any(reverse =>
                reverse.SenderId == edge.RecipientId &&
                reverse.RecipientId == edge.SenderId)));
    }

    [Test]
    public void DefaultPlan_DoesNotCreateEditorialFriendships()
    {
        var plan = CirculationInitialWorldFactory.Create(
            new CirculationInitializationParameters());
        var knownFriends = plan.Definition.Connections
            .Where(e => e.SenderId == plan.EditorNodeId)
            .Select(e => e.RecipientId).Distinct().ToList();

        // The directory may mention editorially collected contacts, but
        // only actual friendships are represented as interpersonal arcs.
        Assert.Less(knownFriends.Count, plan.ZineListedNodeIds.Count);
        Assert.IsTrue(knownFriends.All(
            id => plan.ZineListedNodeIds.Contains(id)));
    }

    [Test]
    public void DefaultPlan_CreatesValidScenarioAndLeavesOneHundredSlotsVacant()
    {
        var plan = CirculationInitialWorldFactory.Create(
            new CirculationInitializationParameters());
        var scenario = new ConfigurableCirculationScenario(plan.Definition);

        Assert.AreEqual(100, scenario.Network.NodeCount);
        Assert.AreEqual(200, scenario.Network.AddressCount);
        Assert.AreEqual(15, scenario.ProductIds.Count);
        Assert.AreEqual(100, scenario.World.RegisteredNodeCount);

        int vacant = plan.Definition.Addresses.Count(address =>
            !scenario.Network.TryGetOccupant(address.Id, out _));
        Assert.AreEqual(100, vacant);

        foreach (var seed in plan.Definition.Seeds)
        {
            Assert.IsTrue(scenario.Inventory.HasEverHeldProduct(
                seed.SourceNodeId, seed.DemoId));
        }
    }

    [Test]
    public void SameSeed_ReproducesAllInitialStructuralDecisions()
    {
        var a = CirculationInitialWorldFactory.Create(
            new CirculationInitializationParameters { RandomSeed = 124 });
        var b = CirculationInitialWorldFactory.Create(
            new CirculationInitializationParameters { RandomSeed = 124 });

        Assert.AreEqual(a.EditorNodeId, b.EditorNodeId);
        CollectionAssert.AreEqual(a.ZineListedNodeIds, b.ZineListedNodeIds);
        CollectionAssert.AreEqual(a.UnlistedDemoOwnerIds, b.UnlistedDemoOwnerIds);
        CollectionAssert.AreEqual(
            a.Definition.Placements.Select(p => p.NodeId + "@" + p.AddressId),
            b.Definition.Placements.Select(p => p.NodeId + "@" + p.AddressId));
        CollectionAssert.AreEqual(
            a.Definition.Connections.Select(e => e.Id),
            b.Definition.Connections.Select(e => e.Id));
    }
}
