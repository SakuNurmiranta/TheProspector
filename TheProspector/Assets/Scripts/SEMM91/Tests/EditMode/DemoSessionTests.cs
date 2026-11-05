
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class DemoSessionTests
{
    private sealed class FixedRandom : Random
    {
        public int Calls { get; private set; }

        public override double NextDouble()
        {
            Calls++;
            return 0.0;
        }
    }

    [Test]
    public void Session_StartsWithActualSeedShipment()
    {
        var session = DemoSession.CreateDefault();

        Assert.AreEqual(0, session.Day);
        Assert.AreEqual(1,
            session.Scenario.SeedWave.Count);

        Assert.AreEqual(SIRState.I,
            session.FieldState);

        Assert.AreEqual(1, session.ActiveCopiers);
        Assert.AreEqual(0, session.OpenedMessageCount);

        Assert.IsFalse(session.DHasDiscovered);
    }

    [Test]
    public void Session_StepsAutonomousWorld()
    {
        var random = new FixedRandom();
        var session = DemoSession.CreateDefault(random);

        session.AdvanceDay();

        Assert.AreEqual(1, session.Day);

        session.AdvanceDay();

        Assert.AreEqual(2, session.Day);
        Assert.AreEqual(
            ListeningPhase.Listening,
            session.GetListeningPhase("B"));

        session.AdvanceDays(2);

        Assert.AreEqual(4, session.Day);
        Assert.AreEqual(1, session.BCopyShipments);
        Assert.AreEqual(SIRState.I,
            session.FieldState);

        session.AdvanceDays(4);

        Assert.AreEqual(8, session.Day);
        Assert.AreEqual(SIRState.R,
            session.FieldState);

        Assert.AreEqual(0, session.ActiveCopiers);

        Assert.AreEqual(
            0.5, session.DeliveredReach, 1e-9);

        Assert.IsTrue(session.DHasDiscovered);
        Assert.AreEqual(1, random.Calls);

        Assert.IsFalse(
            session.Scenario.Inventory
                .HasEverHeldProduct("D", "D1"));
    }

    [Test]
    public void Session_ObservationsDoNotAdvanceTime()
    {
        var random = new FixedRandom();
        var session = DemoSession.CreateDefault(random);

        for (int i = 0; i < 10; i++)
        {
            _ = session.Day;
            _ = session.FieldState;
            _ = session.DeliveredReach;
            _ = session.ActiveCopiers;
            _ = session.OpenedMessageCount;
            _ = session.BCopyShipments;
            _ = session.DHasDiscovered;
            _ = session.GetListeningPhase("B");
        }

        Assert.AreEqual(0, session.Day);
        Assert.AreEqual(0, random.Calls);
    }

    [Test]
    public void Session_NewInstanceHasIndependentState()
    {
        var first = DemoSession.CreateDefault(
            new FixedRandom());

        first.AdvanceDays(8);

        var second = DemoSession.CreateDefault(
            new FixedRandom());

        Assert.AreEqual(8, first.Day);
        Assert.AreEqual(SIRState.R,
            first.FieldState);

        Assert.AreEqual(0, second.Day);
        Assert.AreEqual(SIRState.I,
            second.FieldState);

        Assert.IsFalse(second.DHasDiscovered);

        Assert.AreNotSame(
            first.Scenario,
            second.Scenario);
    }
}
