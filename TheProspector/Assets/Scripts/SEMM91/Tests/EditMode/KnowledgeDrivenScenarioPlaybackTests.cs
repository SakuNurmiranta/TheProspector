
using NUnit.Framework;
using SEMM91.Domain;

public class KnowledgeDrivenScenarioPlaybackTests
{
    [Test]
    public void Playback_StartsAtDayZero()
    {
        var playback = new KnowledgeDrivenScenarioPlayback();
        var state = playback.Current;

        Assert.AreEqual(0, state.Day);
        Assert.AreEqual(1, playback.History.Count);
        Assert.IsFalse(playback.IsComplete);

        Assert.AreEqual(10, state.CapitalA);
        Assert.AreEqual(10, state.CapitalB);
        Assert.AreEqual(10, state.CapitalC);

        Assert.AreEqual(1, state.FirstWaveCount);
        Assert.AreEqual(0, state.SecondWaveCount);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            state.ShipABStatus);

        Assert.IsNull(state.ShipBCStatus);
        Assert.AreEqual(false, state.BThinksCHasD1);
        Assert.IsNull(state.BThinksAHasD1);
    }

    [Test]
    public void Playback_DayOneHasNoDeliveries()
    {
        var playback = new KnowledgeDrivenScenarioPlayback();

        Assert.IsTrue(playback.Step());

        var state = playback.Current;

        Assert.AreEqual(1, state.Day);
        Assert.AreEqual(10, state.CapitalA);
        Assert.AreEqual(10, state.CapitalB);
        Assert.AreEqual(0, state.SecondWaveCount);
        Assert.AreEqual(1, state.Trace.Count);
    }

    [Test]
    public void Playback_DefaultDayTwoForwardsToC()
    {
        var playback = new KnowledgeDrivenScenarioPlayback();

        playback.Step();
        playback.Step();

        var state = playback.Current;

        Assert.AreEqual(2, state.Day);
        Assert.AreEqual(14, state.CapitalA);
        Assert.AreEqual(10, state.CapitalB);

        Assert.AreEqual(true, state.BThinksAHasD1);
        Assert.AreEqual(false, state.BThinksCHasD1);

        Assert.AreEqual(1, state.SecondWaveCount);
        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            state.ShipBCStatus);

        Assert.AreEqual(0, state.BacklogB);
        Assert.AreEqual(4, state.Trace.Count);
    }

    [Test]
    public void Playback_UnknownCollectionPreventsForwarding()
    {
        var playback = new KnowledgeDrivenScenarioPlayback(
            bKnowsCIsMissing: false);

        playback.Step();
        playback.Step();

        var state = playback.Current;

        Assert.AreEqual(2, state.Day);
        Assert.IsNull(state.BThinksCHasD1);
        Assert.AreEqual(0, state.SecondWaveCount);
        Assert.IsNull(state.ShipBCStatus);
        Assert.AreEqual(3, state.Trace.Count);
    }

    [Test]
    public void Playback_CommunicatedDislikePreventsForwarding()
    {
        var playback = new KnowledgeDrivenScenarioPlayback(
            cCommunicatedDislike: true);

        playback.Step();
        playback.Step();

        var state = playback.Current;

        // B tietää, että D1 puuttuu C:ltä,
        // mutta C:n viestimä torjunta estää
        // kopion lähettämisen.
        Assert.AreEqual(false, state.BThinksCHasD1);

        Assert.AreEqual(0, state.SecondWaveCount);
        Assert.IsNull(state.ShipBCStatus);

        // Vastauksesta A:lle ei luovuta.
        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            state.ShipBAStatus);
    }

    [Test]
    public void Playback_DefaultRunProducesFinalObservations()
    {
        var playback = new KnowledgeDrivenScenarioPlayback();

        playback.RunToEnd();

        var state = playback.Current;

        Assert.AreEqual(4, state.Day);
        Assert.IsTrue(playback.IsComplete);

        Assert.AreEqual(15, state.CapitalA);
        Assert.AreEqual(15, state.CapitalB);
        Assert.AreEqual(10, state.CapitalC);

        Assert.AreEqual("A", state.OriginalHolder);
        Assert.AreEqual("B", state.ABCopyHolder);
        Assert.AreEqual("C", state.BCCopyHolder);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            state.ShipBCStatus);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            state.ShipBAStatus);

        Assert.IsTrue(state.CHasEverHeldD1);
        Assert.AreEqual(true, state.CThinksBHasD1);

        Assert.AreEqual(2, state.AOpeningReward);
        Assert.AreEqual(0, state.PendingEvents);
        Assert.AreEqual(6, state.Trace.Count);
    }

    [Test]
    public void Playback_UnknownCollectionChangesFinalResult()
    {
        var playback = new KnowledgeDrivenScenarioPlayback(
            bKnowsCIsMissing: false);

        playback.RunToEnd();

        var state = playback.Current;

        Assert.AreEqual(4, state.Day);

        Assert.AreEqual(15, state.CapitalA);
        Assert.AreEqual(11, state.CapitalB);
        Assert.AreEqual(10, state.CapitalC);

        Assert.IsFalse(state.CHasEverHeldD1);
        Assert.IsNull(state.BCCopyHolder);
        Assert.IsNull(state.ShipBCStatus);

        Assert.AreEqual(4, state.Trace.Count);
    }

    [Test]
    public void Playback_DislikeChangesFinalResult()
    {
        var playback = new KnowledgeDrivenScenarioPlayback(
            cCommunicatedDislike: true);

        playback.RunToEnd();

        var state = playback.Current;

        Assert.AreEqual(false, state.BThinksCHasD1);

        Assert.AreEqual(15, state.CapitalA);
        Assert.AreEqual(11, state.CapitalB);
        Assert.AreEqual(10, state.CapitalC);

        Assert.IsFalse(state.CHasEverHeldD1);
        Assert.AreEqual(0, state.SecondWaveCount);
    }

    [Test]
    public void Playback_SnapshotsRemainIndependent()
    {
        var playback = new KnowledgeDrivenScenarioPlayback();

        playback.Step();
        playback.Step();

        var dayTwo = playback.Current;

        playback.RunToEnd();

        Assert.AreEqual(2, dayTwo.Day);
        Assert.AreEqual(14, dayTwo.CapitalA);
        Assert.AreEqual(10, dayTwo.CapitalB);

        Assert.IsNull(dayTwo.BCCopyHolder);
        Assert.AreEqual(4, dayTwo.Trace.Count);

        Assert.AreEqual(4, playback.Current.Day);
        Assert.AreEqual(15, playback.Current.CapitalB);
        Assert.AreEqual("C", playback.Current.BCCopyHolder);

        Assert.AreEqual(5, playback.History.Count);
    }

    [Test]
    public void Playback_RepeatedRunsAreDeterministic()
    {
        var first = new KnowledgeDrivenScenarioPlayback();
        var second = new KnowledgeDrivenScenarioPlayback();

        first.RunToEnd();

        second.Step();
        second.Step();
        second.RunToEnd();

        Assert.AreEqual(5, first.History.Count);
        Assert.AreEqual(5, second.History.Count);

        for (int i = 0; i < 5; i++)
        {
            var a = first.History[i];
            var b = second.History[i];

            Assert.AreEqual(i, a.Day);
            Assert.AreEqual(a.Day, b.Day);
            Assert.AreEqual(a.CapitalA, b.CapitalA);
            Assert.AreEqual(a.CapitalB, b.CapitalB);
            Assert.AreEqual(a.CapitalC, b.CapitalC);

            CollectionAssert.AreEqual(a.Trace, b.Trace);
        }

        Assert.IsFalse(first.Step());

        first.RunToEnd();

        Assert.AreEqual(5, first.History.Count);
        Assert.AreEqual(15, first.Current.CapitalB);
    }
}
