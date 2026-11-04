
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class ThreeNodeScenarioPlaybackTests
{
    [Test]
    public void Playback_StartsAtDayZero()
    {
        var playback = new ThreeNodeScenarioPlayback();
        var snapshot = playback.Current;

        Assert.AreEqual(0, snapshot.Day);
        Assert.AreEqual(1, playback.History.Count);
        Assert.IsFalse(playback.IsComplete);

        Assert.AreEqual(10, snapshot.CapitalA);
        Assert.AreEqual(10, snapshot.CapitalB);
        Assert.AreEqual(10, snapshot.CapitalC);

        Assert.AreEqual("A", snapshot.OriginalHolder);
        Assert.IsNull(snapshot.ABCopyHolder);
        Assert.IsNull(snapshot.BCCopyHolder);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            snapshot.ShipABStatus);

        Assert.IsNull(snapshot.ShipBCStatus);
        Assert.IsNull(snapshot.ShipBAStatus);
    }

    [Test]
    public void Playback_DayOneHasNoDeliveries()
    {
        var playback = new ThreeNodeScenarioPlayback();

        Assert.IsTrue(playback.Step());

        var snapshot = playback.Current;

        Assert.AreEqual(1, snapshot.Day);
        Assert.AreEqual(2, playback.History.Count);

        Assert.AreEqual(10, snapshot.CapitalA);
        Assert.AreEqual(10, snapshot.CapitalB);
        Assert.AreEqual(10, snapshot.CapitalC);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            snapshot.ShipABStatus);

        Assert.AreEqual(0, snapshot.BacklogB);
        Assert.AreEqual(1, snapshot.Trace.Count);
    }

    [Test]
    public void Playback_DayTwoRecordsFirstDelivery()
    {
        var playback = new ThreeNodeScenarioPlayback();

        playback.Step();
        playback.Step();

        var snapshot = playback.Current;

        Assert.AreEqual(2, snapshot.Day);

        Assert.AreEqual(14, snapshot.CapitalA);
        Assert.AreEqual(10, snapshot.CapitalB);
        Assert.AreEqual(10, snapshot.CapitalC);

        Assert.AreEqual("B", snapshot.ABCopyHolder);
        Assert.IsNull(snapshot.BCCopyHolder);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            snapshot.ShipABStatus);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            snapshot.ShipBCStatus);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            snapshot.ShipBAStatus);

        // B avasi viestinsä päivän 2 aikana.
        Assert.AreEqual(0, snapshot.BacklogB);
        Assert.AreEqual(4, snapshot.Trace.Count);
    }

    [Test]
    public void Playback_DayThreePreservesPendingMail()
    {
        var playback = new ThreeNodeScenarioPlayback();

        playback.Step();
        playback.Step();
        playback.Step();

        var snapshot = playback.Current;

        Assert.AreEqual(3, snapshot.Day);

        Assert.AreEqual(14, snapshot.CapitalA);
        Assert.AreEqual(10, snapshot.CapitalB);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            snapshot.ShipBCStatus);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            snapshot.ShipBAStatus);

        Assert.AreEqual(4, snapshot.Trace.Count);
    }

    [Test]
    public void Playback_DayFourContainsFinalResults()
    {
        var playback = new ThreeNodeScenarioPlayback();

        playback.RunToEnd();

        var snapshot = playback.Current;

        Assert.AreEqual(4, snapshot.Day);
        Assert.IsTrue(playback.IsComplete);

        Assert.AreEqual(15, snapshot.CapitalA);
        Assert.AreEqual(15, snapshot.CapitalB);
        Assert.AreEqual(10, snapshot.CapitalC);

        Assert.AreEqual("A", snapshot.OriginalHolder);
        Assert.AreEqual("B", snapshot.ABCopyHolder);
        Assert.AreEqual("C", snapshot.BCCopyHolder);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            snapshot.ShipABStatus);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            snapshot.ShipBCStatus);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            snapshot.ShipBAStatus);

        Assert.AreEqual(2, snapshot.AOpeningReward);
        Assert.AreEqual(0, snapshot.PendingEvents);
        Assert.AreEqual(0, snapshot.BacklogA);
        Assert.AreEqual(0, snapshot.BacklogB);
        Assert.AreEqual(0, snapshot.BacklogC);
    }

    [Test]
    public void Playback_RecordsEveryDayIncludingQuietDays()
    {
        var playback = new ThreeNodeScenarioPlayback();

        playback.RunToEnd();

        Assert.AreEqual(5, playback.History.Count);

        for (int day = 0; day <= 4; day++)
        {
            Assert.AreEqual(
                day,
                playback.History[day].Day);
        }

        CollectionAssert.AreEqual(
            new[] { 1, 1, 4, 4, 6 },
            new[]
            {
                playback.History[0].Trace.Count,
                playback.History[1].Trace.Count,
                playback.History[2].Trace.Count,
                playback.History[3].Trace.Count,
                playback.History[4].Trace.Count
            });
    }

    [Test]
    public void Playback_CannotAdvanceBeyondFinalDay()
    {
        var playback = new ThreeNodeScenarioPlayback();

        playback.RunToEnd();

        Assert.IsFalse(playback.Step());

        playback.RunToEnd();

        Assert.AreEqual(4, playback.Current.Day);
        Assert.AreEqual(5, playback.History.Count);
        Assert.AreEqual(15, playback.Current.CapitalA);
    }

    [Test]
    public void Playback_CanResumeFromDayTwo()
    {
        var playback = new ThreeNodeScenarioPlayback();

        playback.Step();
        playback.Step();

        Assert.AreEqual(2, playback.Current.Day);

        playback.RunToEnd();

        Assert.AreEqual(4, playback.Current.Day);
        Assert.AreEqual(5, playback.History.Count);
        Assert.AreEqual(15, playback.Current.CapitalB);
    }

    [Test]
    public void Playback_PreviousSnapshotsDoNotChange()
    {
        var playback = new ThreeNodeScenarioPlayback();

        playback.Step();
        playback.Step();

        var dayTwo = playback.Current;

        playback.RunToEnd();

        // Tallennettu havainto on tilannekuva,
        // ei viittaus myöhemmin muuttuvaan tilaan.
        Assert.AreEqual(2, dayTwo.Day);
        Assert.AreEqual(14, dayTwo.CapitalA);
        Assert.AreEqual(10, dayTwo.CapitalB);
        Assert.IsNull(dayTwo.BCCopyHolder);
        Assert.AreEqual(4, dayTwo.Trace.Count);

        Assert.AreEqual(4, playback.Current.Day);
        Assert.AreEqual(6, playback.Current.Trace.Count);
    }

    [Test]
    public void Playback_IndependentRunsAreDeterministic()
    {
        var first = new ThreeNodeScenarioPlayback();
        var second = new ThreeNodeScenarioPlayback();

        first.RunToEnd();
        second.RunToEnd();

        Assert.AreEqual(
            first.History.Count,
            second.History.Count);

        for (int i = 0; i < first.History.Count; i++)
        {
            var a = first.History[i];
            var b = second.History[i];

            Assert.AreEqual(a.Day, b.Day);
            Assert.AreEqual(a.CapitalA, b.CapitalA);
            Assert.AreEqual(a.CapitalB, b.CapitalB);
            Assert.AreEqual(a.CapitalC, b.CapitalC);

            Assert.AreEqual(a.ABCopyHolder, b.ABCopyHolder);
            Assert.AreEqual(a.BCCopyHolder, b.BCCopyHolder);

            CollectionAssert.AreEqual(a.Trace, b.Trace);
        }
    }
}
