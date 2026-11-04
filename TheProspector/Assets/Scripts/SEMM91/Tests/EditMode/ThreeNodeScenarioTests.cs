
using NUnit.Framework;
using SEMM91.Domain;

public class ThreeNodeScenarioTests
{
    [Test]
    public void Scenario_InitializesThreeNodesAndOneDemo()
    {
        var scenario = new ThreeNodeScenario();

        Assert.AreEqual(3, scenario.Network.NodeCount);
        Assert.AreEqual(0, scenario.Clock.CurrentDay);

        Assert.AreEqual("A",
            scenario.Original.HolderNodeId);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            scenario.ShipAB.Status);

        Assert.AreEqual(10, scenario.Capital.GetCapital("A"));
        Assert.AreEqual(10, scenario.Capital.GetCapital("B"));
        Assert.AreEqual(10, scenario.Capital.GetCapital("C"));
    }

    [Test]
    public void Scenario_NothingArrivesBeforeDayTwo()
    {
        var scenario = new ThreeNodeScenario();

        scenario.Clock.AdvanceTo(1);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            scenario.ShipAB.Status);

        Assert.AreEqual(0,
            scenario.Backlog.GetPendingCount("B"));

        Assert.AreEqual(10,
            scenario.Capital.GetCapital("A"));
    }

    [Test]
    public void Scenario_RunCompletesWithoutManualMessages()
    {
        var scenario = new ThreeNodeScenario();

        scenario.Run();

        Assert.AreEqual(4, scenario.Clock.CurrentDay);
        Assert.AreEqual(0, scenario.Clock.PendingCount);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.ShipAB.Status);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.ShipBC.Status);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.ShipBA.Status);

        Assert.AreEqual(0,
            scenario.Backlog.GetPendingCount("A"));

        Assert.AreEqual(0,
            scenario.Backlog.GetPendingCount("B"));

        Assert.AreEqual(0,
            scenario.Backlog.GetPendingCount("C"));
    }

    [Test]
    public void Scenario_DemoReachesBAndCAsCopies()
    {
        var scenario = new ThreeNodeScenario();

        scenario.Run();

        Assert.AreEqual("A",
            scenario.Original.HolderNodeId);

        Assert.AreEqual("B",
            scenario.ABCopy.HolderNodeId);

        Assert.AreEqual("C",
            scenario.BCCopy.HolderNodeId);

        Assert.IsTrue(
            scenario.Inventory.HasEverHeldProduct("A", "D1"));

        Assert.IsTrue(
            scenario.Inventory.HasEverHeldProduct("B", "D1"));

        Assert.IsTrue(
            scenario.Inventory.HasEverHeldProduct("C", "D1"));
    }

    [Test]
    public void Scenario_CapitalReflectsCopiesAndReply()
    {
        var scenario = new ThreeNodeScenario();

        scenario.Run();

        // A: 10 + 4 ensimmäisestä kopiosta
        //       + 1 vastavuoroisesta vaihdosta.
        Assert.AreEqual(15,
            scenario.Capital.GetCapital("A"));

        // B: 10 + 4 C:lle tehdystä kopiosta
        //       + 1 vastavuoroisesta vaihdosta.
        Assert.AreEqual(15,
            scenario.Capital.GetCapital("B"));

        // C ei vielä kopioi eikä vastaa.
        Assert.AreEqual(10,
            scenario.Capital.GetCapital("C"));

        Assert.AreEqual(2,
            scenario.AOpeningReward);
    }

    [Test]
    public void Scenario_KnowledgeFollowsOpenedMessages()
    {
        var scenario = new ThreeNodeScenario();

        scenario.Run();

        Assert.AreEqual(
            true,
            scenario.Known.IsKnownToHold("B", "A", "D1"));

        Assert.AreEqual(
            true,
            scenario.Known.IsKnownToHold("C", "B", "D1"));

        Assert.AreEqual(
            true,
            scenario.Known.IsKnownToHold("A", "B", "D1"));
    }

    [Test]
    public void Scenario_CopyAndReplyRewardsOccurAtDifferentTimes()
    {
        var scenario = new ThreeNodeScenario();

        scenario.Clock.AdvanceTo(2);

        // A:n kopio on toimitettu B:lle.
        // B on myös lähettänyt C:lle uuden kopion,
        // mutta sitä ei ole vielä toimitettu.
        Assert.AreEqual(14,
            scenario.Capital.GetCapital("A"));

        Assert.AreEqual(10,
            scenario.Capital.GetCapital("B"));

        Assert.AreEqual(10,
            scenario.Capital.GetCapital("C"));

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            scenario.ShipBC.Status);

        scenario.Run();

        Assert.AreEqual(15,
            scenario.Capital.GetCapital("A"));

        Assert.AreEqual(15,
            scenario.Capital.GetCapital("B"));
    }

    [Test]
    public void Scenario_RepeatedRunsAreDeterministic()
    {
        var first = new ThreeNodeScenario();
        var second = new ThreeNodeScenario();

        first.Run();
        second.Run();

        CollectionAssert.AreEqual(
            first.Trace,
            second.Trace);

        foreach (var nodeId in new[] { "A", "B", "C" })
        {
            Assert.AreEqual(
                first.Capital.GetCapital(nodeId),
                second.Capital.GetCapital(nodeId));
        }

        // Saman instanssin toinen Run-kutsu
        // ei toista jo suoritettuja tapahtumia.
        first.Run();

        Assert.AreEqual(15,
            first.Capital.GetCapital("A"));

        Assert.AreEqual(15,
            first.Capital.GetCapital("B"));

        Assert.AreEqual(6, first.Trace.Count);
    }
}
