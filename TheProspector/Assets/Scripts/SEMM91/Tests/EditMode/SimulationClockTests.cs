
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class SimulationClockTests
{
    [Test]
    public void Clock_StartsAtDayZero()
    {
        var clock = new SimulationClock();

        Assert.AreEqual(0, clock.CurrentDay);
        Assert.AreEqual(0, clock.PendingCount);

        clock.AdvanceTo(5);

        Assert.AreEqual(5, clock.CurrentDay);
        Assert.AreEqual(0, clock.PendingCount);
    }

    [Test]
    public void Clock_EventExecutesOnlyOnScheduledDay()
    {
        var clock = new SimulationClock();
        int executedOnDay = -1;

        clock.ScheduleAfter("EVENT1", 3, () =>
        {
            executedOnDay = clock.CurrentDay;
        });

        clock.AdvanceDays(2);

        Assert.AreEqual(-1, executedOnDay);
        Assert.AreEqual(1, clock.PendingCount);

        clock.AdvanceDays(1);

        Assert.AreEqual(3, executedOnDay);
        Assert.AreEqual(0, clock.PendingCount);
    }

    [Test]
    public void Clock_SameDayEventsKeepRegistrationOrder()
    {
        var clock = new SimulationClock();
        var order = new List<string>();

        clock.ScheduleAfter(
            "A", 2, () => order.Add("A"));

        clock.ScheduleAfter(
            "B", 2, () => order.Add("B"));

        clock.ScheduleAfter(
            "C", 2, () => order.Add("C"));

        clock.AdvanceTo(2);

        CollectionAssert.AreEqual(
            new[] { "A", "B", "C" },
            order);
    }

    [Test]
    public void Clock_EventsExecuteChronologically()
    {
        var clock = new SimulationClock();
        var order = new List<int>();

        // Rekisteröintijärjestys ei määrää
        // eri päivien tapahtumajärjestystä.
        clock.ScheduleAfter(
            "LATE", 8,
            () => order.Add(clock.CurrentDay));

        clock.ScheduleAfter(
            "EARLY", 2,
            () => order.Add(clock.CurrentDay));

        clock.ScheduleAfter(
            "MIDDLE", 5,
            () => order.Add(clock.CurrentDay));

        clock.AdvanceTo(10);

        CollectionAssert.AreEqual(
            new[] { 2, 5, 8 },
            order);

        Assert.AreEqual(10, clock.CurrentDay);
    }

    [Test]
    public void Clock_AdvancingInStepsDoesNotRepeatEvents()
    {
        var clock = new SimulationClock();
        int executions = 0;

        clock.ScheduleAfter(
            "EVENT1", 2, () => executions++);

        clock.AdvanceDays(1);
        Assert.AreEqual(0, executions);

        clock.AdvanceDays(1);
        Assert.AreEqual(1, executions);

        clock.AdvanceDays(5);
        Assert.AreEqual(1, executions);

        Assert.AreEqual(7, clock.CurrentDay);
    }

    [Test]
    public void Clock_EventCanScheduleAnotherFutureEvent()
    {
        var clock = new SimulationClock();
        var log = new List<string>();

        clock.ScheduleAfter("FIRST", 2, () =>
        {
            log.Add("First:" + clock.CurrentDay);

            // Viive lasketaan tapahtuman
            // suorituspäivästä, ei koko ajon
            // lopullisesta tavoitepäivästä.
            clock.ScheduleAfter("SECOND", 3, () =>
            {
                log.Add("Second:" + clock.CurrentDay);
            });
        });

        clock.AdvanceTo(10);

        CollectionAssert.AreEqual(
            new[]
            {
                "First:2",
                "Second:5"
            },
            log);

        Assert.AreEqual(0, clock.PendingCount);
    }

    [Test]
    public void Clock_RejectsInvalidAndDuplicateEvents()
    {
        var clock = new SimulationClock();

        Assert.Throws<ArgumentException>(() =>
            clock.ScheduleAfter(
                "", 1, () => { }));

        Assert.Throws<ArgumentNullException>(() =>
            clock.ScheduleAfter(
                "NULL", 1, null));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            clock.ScheduleAfter(
                "ZERO", 0, () => { }));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            clock.ScheduleAfter(
                "NEGATIVE", -1, () => { }));

        clock.ScheduleAfter(
            "EVENT1", 1, () => { });

        Assert.Throws<ArgumentException>(() =>
            clock.ScheduleAfter(
                "EVENT1", 2, () => { }));

        clock.AdvanceTo(1);

        // Käytetty tapahtumatunniste ei vapaudu
        // tapahtuman suorittamisen jälkeen.
        Assert.Throws<ArgumentException>(() =>
            clock.ScheduleAfter(
                "EVENT1", 2, () => { }));

        Assert.AreEqual(0, clock.PendingCount);
    }

    [Test]
    public void Clock_RejectsBackwardsTimeAndOverflow()
    {
        var clock = new SimulationClock();

        clock.AdvanceTo(10);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            clock.AdvanceTo(9));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            clock.AdvanceDays(-1));

        Assert.AreEqual(10, clock.CurrentDay);

        clock.AdvanceTo(int.MaxValue - 1);

        Assert.Throws<OverflowException>(() =>
            clock.ScheduleAfter(
                "OVERFLOW", 2, () => { }));

        Assert.Throws<OverflowException>(() =>
            clock.AdvanceDays(2));

        Assert.AreEqual(
            int.MaxValue - 1,
            clock.CurrentDay);
    }

    [Test]
    public void Clock_FailedEventDoesNotReplay()
    {
        var clock = new SimulationClock();
        var log = new List<string>();

        clock.ScheduleAfter("FAIL", 2, () =>
        {
            throw new InvalidOperationException(
                "Simulated event failure.");
        });

        clock.ScheduleAfter(
            "SAME-DAY", 2,
            () => log.Add("SameDay"));

        clock.ScheduleAfter(
            "LATER", 3,
            () => log.Add("Later"));

        Assert.Throws<InvalidOperationException>(() =>
            clock.AdvanceTo(5));

        // Suoritus pysähtyy virhepäivään.
        Assert.AreEqual(2, clock.CurrentDay);

        // Epäonnistunut tapahtuma on kulutettu.
        // Muut tapahtumat säilyvät jonossa.
        Assert.AreEqual(2, clock.PendingCount);

        clock.AdvanceTo(5);

        CollectionAssert.AreEqual(
            new[] { "SameDay", "Later" },
            log);

        Assert.AreEqual(0, clock.PendingCount);
    }

    [Test]
    public void Clock_RejectsReentrantTimeAdvancement()
    {
        var clock = new SimulationClock();

        clock.ScheduleAfter("EVENT1", 1, () =>
        {
            // Tapahtuma ei saa käynnistää
            // toista sisäkkäistä ajanajoa.
            Assert.Throws<InvalidOperationException>(() =>
                clock.AdvanceDays(1));
        });

        clock.AdvanceTo(3);

        Assert.AreEqual(3, clock.CurrentDay);
        Assert.AreEqual(0, clock.PendingCount);
    }
}
