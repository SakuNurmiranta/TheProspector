
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class LateDislikeScenarioTests
{
    private static KnowledgeDrivenScenario Create(
        bool knowsMissing = true)
    {
        return new KnowledgeDrivenScenario(
            bKnowsCIsMissing: knowsMissing,
            cCommunicatedDislike: true,
            cDislikeSentDay: 1);
    }

    [Test]
    public void Late_DislikeIsNotSentAtDayZero()
    {
        var scenario = Create();

        Assert.AreEqual(0, scenario.Clock.CurrentDay);
        Assert.IsNull(scenario.ShipCB);

        Assert.IsFalse(
            scenario.CommunicatedStances
                .HasCommunicatedDislike("B", "C", "D1"));

        Assert.AreEqual(0, scenario.SecondWave.Count);
    }

    [Test]
    public void Late_DislikeIsInTransitOnDayOne()
    {
        var scenario = Create();

        scenario.Clock.AdvanceTo(1);

        Assert.IsNotNull(scenario.ShipCB);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            scenario.ShipCB.Status);

        Assert.IsFalse(
            scenario.CommunicatedStances
                .HasCommunicatedDislike("B", "C", "D1"));
    }

    [Test]
    public void Late_BForwardsBeforeLearningDislike()
    {
        var scenario = Create();

        scenario.Clock.AdvanceTo(2);

        Assert.IsFalse(
            scenario.CommunicatedStances
                .HasCommunicatedDislike("B", "C", "D1"));

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            scenario.ShipCB.Status);

        Assert.AreEqual(1, scenario.SecondWave.Count);
        Assert.AreEqual(
            "C",
            scenario.SecondWave[0].RecipientNodeId);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            scenario.ShipBC.Status);

        Assert.AreEqual(14,
            scenario.Capital.GetCapital("A"));

        Assert.AreEqual(10,
            scenario.Capital.GetCapital("B"));
    }

    [Test]
    public void Late_RejectionOpensOnDayThree()
    {
        var scenario = Create();

        scenario.Clock.AdvanceTo(3);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.ShipCB.Status);

        Assert.IsTrue(
            scenario.CommunicatedStances
                .HasCommunicatedDislike("B", "C", "D1"));

        Assert.AreEqual(-1,
            scenario.CommunicatedStances
                .GetLastThumb("B", "C", "D1"));

        // B:n jo lähettämä kopio ei peruunnu.
        Assert.AreEqual(1, scenario.SecondWave.Count);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            scenario.ShipBC.Status);

        Assert.AreEqual(0,
            scenario.Backlog.GetPendingCount("B"));
    }

    [Test]
    public void Late_CopyArrivesDespiteNewDislike()
    {
        var scenario = Create();

        scenario.Run();

        Assert.AreEqual(4, scenario.Clock.CurrentDay);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.ShipBC.Status);

        Assert.AreEqual("C",
            scenario.BCCopy.HolderNodeId);

        Assert.IsTrue(
            scenario.Inventory.HasEverHeldProduct(
                "C", "D1"));

        Assert.IsTrue(
            scenario.CommunicatedStances
                .HasCommunicatedDislike("B", "C", "D1"));

        Assert.AreEqual(15,
            scenario.Capital.GetCapital("A"));

        Assert.AreEqual(15,
            scenario.Capital.GetCapital("B"));

        Assert.AreEqual(10,
            scenario.Capital.GetCapital("C"));

        Assert.AreEqual(0, scenario.Clock.PendingCount);
    }

    [Test]
    public void Late_UnknownCollectionStillPreventsCopy()
    {
        var scenario = Create(knowsMissing: false);

        scenario.Clock.AdvanceTo(2);

        // B ei tiedä C:n vastaanottohistoriaa
        // kopiointipäätöksen hetkellä.
        Assert.IsNull(
            scenario.Known.IsKnownToHold(
                "B", "C", "D1"));

        Assert.AreEqual(0, scenario.SecondWave.Count);

        scenario.Run();

        // Päivänä 3 B oppii sekä C:n
        // kokoelmahistorian että torjunnan.
        Assert.AreEqual(false,
            scenario.Known.IsKnownToHold(
                "B", "C", "D1"));

        Assert.IsTrue(
            scenario.CommunicatedStances
                .HasCommunicatedDislike("B", "C", "D1"));

        Assert.IsNull(scenario.ShipBC);

        Assert.AreEqual(11,
            scenario.Capital.GetCapital("B"));
    }

    [Test]
    public void Late_RunsAreDeterministicAndIdempotent()
    {
        var first = Create();
        var second = Create();

        first.Run();
        second.Clock.AdvanceTo(2);
        second.Run();

        CollectionAssert.AreEqual(
            first.Trace,
            second.Trace);

        Assert.AreEqual(8, first.Trace.Count);

        first.Run();

        Assert.AreEqual(8, first.Trace.Count);

        Assert.AreEqual(15,
            first.Capital.GetCapital("B"));
    }

    [Test]
    public void Late_RejectsUnsupportedSendDays()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new KnowledgeDrivenScenario(
                cCommunicatedDislike: true,
                cDislikeSentDay: -1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new KnowledgeDrivenScenario(
                cCommunicatedDislike: true,
                cDislikeSentDay: 2));
    }
}
