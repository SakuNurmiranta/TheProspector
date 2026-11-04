
using NUnit.Framework;
using SEMM91.Domain;

public class AutonomousCirculationScenarioTests
{
    [Test]
    public void Scenario_StartsWithOneSeedShipment()
    {
        var s = new AutonomousCirculationScenario();

        Assert.AreEqual(0, s.World.CurrentDay);
        Assert.AreEqual(4, s.World.RegisteredNodeCount);

        Assert.AreEqual(1, s.SeedWave.Count);
        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            s.SeedWave[0].Shipment.Status);

        Assert.AreEqual(1, s.CopyIndex.Count);
        Assert.AreEqual(0, s.World.OpenedMessages.Count);
        Assert.AreEqual(1, s.Clock.PendingCount);

        Assert.AreEqual(0, s.GetPolicy("B").Dispatched.Count);
    }

    [Test]
    public void Scenario_NoAutomaticForwardBeforeArrival()
    {
        var s = new AutonomousCirculationScenario();

        s.World.AdvanceDay();

        Assert.AreEqual(1, s.World.CurrentDay);
        Assert.AreEqual(0, s.World.OpenedMessages.Count);
        Assert.AreEqual(0, s.GetPolicy("B").Dispatched.Count);
    }

    [Test]
    public void Scenario_BAutomaticallyForwardsOnDayTwo()
    {
        var s = new AutonomousCirculationScenario();

        s.World.AdvanceDays(2);

        Assert.AreEqual(1, s.World.OpenedMessages.Count);
        Assert.AreEqual(
            "B", s.World.OpenedMessages[0].NodeId);

        Assert.AreEqual(1, s.GetPolicy("B").CopyDecisions);
        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);

        var copy = s.GetPolicy("B").Dispatched[0];

        Assert.AreEqual("C", copy.RecipientNodeId);
        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            copy.Shipment.Status);

        Assert.AreEqual(2, s.CopyIndex.Count);
        Assert.AreEqual(14, s.Capital.GetCapital("A"));
        Assert.AreEqual(10, s.Capital.GetCapital("B"));
    }

    [Test]
    public void Scenario_CAutomaticallyForwardsOnDayFour()
    {
        var s = new AutonomousCirculationScenario();

        s.World.AdvanceDays(4);

        Assert.AreEqual(2, s.World.OpenedMessages.Count);
        Assert.AreEqual(
            "C", s.World.OpenedMessages[1].NodeId);

        Assert.AreEqual(1, s.GetPolicy("C").CopyDecisions);
        Assert.AreEqual(1, s.GetPolicy("C").Dispatched.Count);

        Assert.AreEqual(
            "D",
            s.GetPolicy("C").Dispatched[0].RecipientNodeId);

        Assert.AreEqual(3, s.CopyIndex.Count);

        Assert.AreEqual(14, s.Capital.GetCapital("A"));
        Assert.AreEqual(14, s.Capital.GetCapital("B"));
        Assert.AreEqual(10, s.Capital.GetCapital("C"));
    }

    [Test]
    public void Scenario_DReceivesLastCopyOnDaySix()
    {
        var s = new AutonomousCirculationScenario();

        s.World.AdvanceDays(6);

        Assert.AreEqual(3, s.World.OpenedMessages.Count);

        Assert.AreEqual(
            "D", s.World.OpenedMessages[2].NodeId);

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("B", "D1"));

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(14, s.Capital.GetCapital("A"));
        Assert.AreEqual(14, s.Capital.GetCapital("B"));
        Assert.AreEqual(14, s.Capital.GetCapital("C"));
        Assert.AreEqual(10, s.Capital.GetCapital("D"));

        Assert.AreEqual(0, s.Clock.PendingCount);
    }

    [Test]
    public void Scenario_UnknownCCollectionStopsPropagationAtB()
    {
        var s = new AutonomousCirculationScenario(
            bKnowsCIsMissing: false);

        s.World.AdvanceDays(8);

        Assert.AreEqual(1, s.GetPolicy("B").CopyDecisions);
        Assert.AreEqual(0, s.GetPolicy("B").Dispatched.Count);

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.AreEqual(1, s.World.OpenedMessages.Count);
        Assert.AreEqual(10, s.Capital.GetCapital("B"));
    }

    [Test]
    public void Scenario_UnknownDCollectionStopsPropagationAtC()
    {
        var s = new AutonomousCirculationScenario(
            cKnowsDIsMissing: false);

        s.World.AdvanceDays(8);

        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);
        Assert.AreEqual(1, s.GetPolicy("C").CopyDecisions);
        Assert.AreEqual(0, s.GetPolicy("C").Dispatched.Count);

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(2, s.World.OpenedMessages.Count);
        Assert.AreEqual(14, s.Capital.GetCapital("B"));
        Assert.AreEqual(10, s.Capital.GetCapital("C"));
    }

    [Test]
    public void Scenario_CMayDeclineCopying()
    {
        var s = new AutonomousCirculationScenario(
            cCopies: false);

        s.World.AdvanceDays(8);

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(1, s.GetPolicy("C").CopyDecisions);
        Assert.AreEqual(0, s.GetPolicy("C").Dispatched.Count);

        Assert.AreEqual(14, s.Capital.GetCapital("B"));
        Assert.AreEqual(10, s.Capital.GetCapital("C"));
    }

    [Test]
    public void Scenario_CanContinueBeyondTheLastDelivery()
    {
        var s = new AutonomousCirculationScenario();

        s.World.AdvanceDays(30);

        Assert.AreEqual(30, s.World.CurrentDay);
        Assert.AreEqual(3, s.World.OpenedMessages.Count);

        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);
        Assert.AreEqual(1, s.GetPolicy("C").Dispatched.Count);
        Assert.AreEqual(0, s.GetPolicy("D").Dispatched.Count);

        Assert.AreEqual(14, s.Capital.GetCapital("B"));
        Assert.AreEqual(14, s.Capital.GetCapital("C"));

        Assert.AreEqual(0, s.Clock.PendingCount);
    }

    [Test]
    public void Scenario_BatchedAndSteppedExecutionMatch()
    {
        var batched = new AutonomousCirculationScenario();
        var stepped = new AutonomousCirculationScenario();

        batched.World.AdvanceDays(10);

        for (int day = 0; day < 10; day++)
            stepped.World.AdvanceDay();

        Assert.AreEqual(
            batched.World.CurrentDay,
            stepped.World.CurrentDay);

        Assert.AreEqual(
            batched.World.OpenedMessages.Count,
            stepped.World.OpenedMessages.Count);

        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual(
                batched.World.OpenedMessages[i].Day,
                stepped.World.OpenedMessages[i].Day);

            Assert.AreEqual(
                batched.World.OpenedMessages[i].NodeId,
                stepped.World.OpenedMessages[i].NodeId);
        }

        foreach (string id in new[] { "A", "B", "C", "D" })
        {
            Assert.AreEqual(
                batched.Capital.GetCapital(id),
                stepped.Capital.GetCapital(id));

            Assert.AreEqual(
                batched.Inventory.HasEverHeldProduct(id, "D1"),
                stepped.Inventory.HasEverHeldProduct(id, "D1"));
        }

        Assert.AreEqual(
            batched.CopyIndex.Count,
            stepped.CopyIndex.Count);
    }
}
