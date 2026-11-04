
using NUnit.Framework;
using SEMM91.Domain;

public class CommunicatedDislikeScenarioTests
{
    [Test]
    public void Scenario_RejectionIsDispatchedNotPreloaded()
    {
        var scenario = new KnowledgeDrivenScenario(
            cCommunicatedDislike: true);

        Assert.IsNotNull(scenario.ShipCB);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            scenario.ShipCB.Status);

        Assert.IsFalse(
            scenario.CommunicatedStances
                .HasCommunicatedDislike("B", "C", "D1"));

        Assert.AreEqual(0, scenario.Clock.CurrentDay);
        Assert.AreEqual(3, scenario.Clock.PendingCount);
    }

    [Test]
    public void Scenario_RejectionRemainsUnknownBeforeArrival()
    {
        var scenario = new KnowledgeDrivenScenario(
            cCommunicatedDislike: true);

        scenario.Clock.AdvanceTo(1);

        Assert.IsFalse(
            scenario.CommunicatedStances
                .HasCommunicatedDislike("B", "C", "D1"));

        Assert.AreEqual(0, scenario.SecondWave.Count);
        Assert.IsNull(scenario.ShipBC);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            scenario.ShipCB.Status);
    }

    [Test]
    public void Scenario_OpeningRejectionBlocksForwarding()
    {
        var scenario = new KnowledgeDrivenScenario(
            cCommunicatedDislike: true);

        scenario.Clock.AdvanceTo(2);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.ShipCB.Status);

        Assert.IsTrue(
            scenario.CommunicatedStances
                .HasCommunicatedDislike("B", "C", "D1"));

        Assert.AreEqual(-1,
            scenario.CommunicatedStances
                .GetLastThumb("B", "C", "D1"));

        Assert.AreEqual(0, scenario.SecondWave.Count);
        Assert.IsNull(scenario.ShipBC);
        Assert.IsNull(scenario.BCCopy);

        Assert.AreEqual(0,
            scenario.Backlog.GetPendingCount("B"));
    }

    [Test]
    public void Scenario_OnlyBReceivesCsStance()
    {
        var scenario = new KnowledgeDrivenScenario(
            cCommunicatedDislike: true);

        scenario.Clock.AdvanceTo(2);

        Assert.AreEqual(-1,
            scenario.CommunicatedStances
                .GetLastThumb("B", "C", "D1"));

        Assert.IsNull(
            scenario.CommunicatedStances
                .GetLastThumb("A", "C", "D1"));
    }

    [Test]
    public void Scenario_RejectionDoesNotPreventFirstDelivery()
    {
        var scenario = new KnowledgeDrivenScenario(
            cCommunicatedDislike: true);

        scenario.Clock.AdvanceTo(2);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.ShipAB.Status);

        Assert.AreEqual("B",
            scenario.ABCopy.HolderNodeId);

        Assert.AreEqual(14,
            scenario.Capital.GetCapital("A"));

        Assert.AreEqual(10,
            scenario.Capital.GetCapital("B"));
    }

    [Test]
    public void Scenario_RejectionDoesNotPreventReplyToA()
    {
        var scenario = new KnowledgeDrivenScenario(
            cCommunicatedDislike: true);

        scenario.Run();

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.ShipBA.Status);

        Assert.AreEqual(2, scenario.AOpeningReward);

        Assert.AreEqual(15,
            scenario.Capital.GetCapital("A"));

        Assert.AreEqual(11,
            scenario.Capital.GetCapital("B"));

        Assert.AreEqual(10,
            scenario.Capital.GetCapital("C"));

        Assert.IsNull(scenario.ShipBC);
    }

    [Test]
    public void Scenario_RejectionMessageAlsoConveysCollectionHistory()
    {
        var scenario = new KnowledgeDrivenScenario(
            bKnowsCIsMissing: false,
            cCommunicatedDislike: true);

        // Ennen viestintää B ei tiedä,
        // onko C vastaanottanut D1:n.
        Assert.IsNull(
            scenario.Known.IsKnownToHold(
                "B", "C", "D1"));

        scenario.Clock.AdvanceTo(2);

        // C:n viestin kokoelmatilannekuva
        // kertoo, ettei D1:tä ole vastaanotettu.
        Assert.AreEqual(false,
            scenario.Known.IsKnownToHold(
                "B", "C", "D1"));

        // Samalla C:n kielteinen kannanotto
        // estää B:tä lähettämästä kopiota.
        Assert.IsTrue(
            scenario.CommunicatedStances
                .HasCommunicatedDislike("B", "C", "D1"));

        Assert.AreEqual(0, scenario.SecondWave.Count);
    }

    [Test]
    public void Scenario_DefaultRunStillForwardsToC()
    {
        var scenario = new KnowledgeDrivenScenario();

        Assert.IsNull(scenario.ShipCB);

        scenario.Run();

        Assert.IsFalse(
            scenario.CommunicatedStances
                .HasCommunicatedDislike("B", "C", "D1"));

        Assert.AreEqual(1, scenario.SecondWave.Count);
        Assert.AreEqual("C", scenario.BCCopy.HolderNodeId);

        Assert.AreEqual(15,
            scenario.Capital.GetCapital("A"));

        Assert.AreEqual(15,
            scenario.Capital.GetCapital("B"));
    }

    [Test]
    public void Playback_RecordsWhenRejectionBecomesKnown()
    {
        var playback = new KnowledgeDrivenScenarioPlayback(
            cCommunicatedDislike: true);

        var dayZero = playback.Current;

        Assert.IsFalse(dayZero.BKnowsCDislikesD1);
        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            dayZero.ShipCBStatus);

        playback.Step();

        Assert.IsFalse(
            playback.Current.BKnowsCDislikesD1);

        playback.Step();

        Assert.AreEqual(2, playback.Current.Day);

        Assert.IsTrue(
            playback.Current.BKnowsCDislikesD1);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            playback.Current.ShipCBStatus);

        Assert.AreEqual(0,
            playback.Current.SecondWaveCount);

        playback.RunToEnd();

        Assert.IsTrue(
            playback.Current.BKnowsCDislikesD1);

        // Aiempi tilannekuva säilyy muuttumattomana.
        Assert.IsFalse(dayZero.BKnowsCDislikesD1);
    }

    [Test]
    public void Scenario_RejectionRunIsDeterministicAndIdempotent()
    {
        var first = new KnowledgeDrivenScenario(
            cCommunicatedDislike: true);

        var second = new KnowledgeDrivenScenario(
            cCommunicatedDislike: true);

        first.Run();
        second.Run();

        CollectionAssert.AreEqual(
            first.Trace,
            second.Trace);

        Assert.AreEqual(6, first.Trace.Count);

        first.Run();

        Assert.AreEqual(6, first.Trace.Count);

        Assert.AreEqual(15,
            first.Capital.GetCapital("A"));

        Assert.AreEqual(11,
            first.Capital.GetCapital("B"));

        Assert.AreEqual(0, first.Clock.PendingCount);
    }
}
