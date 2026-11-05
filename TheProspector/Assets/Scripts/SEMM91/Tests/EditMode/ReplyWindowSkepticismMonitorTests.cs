
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class ReplyWindowSkepticismMonitorTests
{
    private static (
        SimulationClock Clock,
        DirectionalSkepticismLedger Ledger,
        ReplyWindowSkepticismMonitor Monitor) Create()
    {
        var clock = new SimulationClock();
        var ledger = new DirectionalSkepticismLedger();

        return (
            clock,
            ledger,
            new ReplyWindowSkepticismMonitor(clock, ledger));
    }

    [Test]
    public void ReplySkepticism_ExpiresDayAfterDeadline()
    {
        var f = Create();

        int deadline = PostalTimingRules.CalculateDeadline(
            f.Clock.CurrentDay, 2, 5);

        f.Monitor.Track("MSG1", "A", "B", deadline);

        f.Clock.AdvanceDays(deadline);

        Assert.AreEqual(0,
            f.Ledger.Get("A", "B").UnansweredReplies);

        f.Clock.AdvanceDays(1);

        Assert.AreEqual(1,
            f.Ledger.Get("A", "B").UnansweredReplies);
        Assert.AreEqual(0,
            f.Ledger.Get("B", "A").Total);

        f.Clock.AdvanceDays(20);

        Assert.AreEqual(1,
            f.Ledger.Get("A", "B").Total);
    }

    [Test]
    public void ReplySkepticism_OnTimeArrivalPreventsPenalty()
    {
        var f = Create();

        f.Monitor.Track("MSG1", "A", "B", 3);

        f.Clock.AdvanceDays(3);

        Assert.IsTrue(
            f.Monitor.ReplyArrived("MSG1", "B", "A"));

        f.Clock.AdvanceDays(10);

        Assert.AreEqual(0,
            f.Ledger.Get("A", "B").Total);
    }

    [Test]
    public void ReplySkepticism_LateReplyDoesNotAutoCompensate()
    {
        var f = Create();

        f.Monitor.Track("MSG1", "A", "B", 3);

        f.Clock.AdvanceDays(4);

        Assert.AreEqual(1,
            f.Ledger.Get("A", "B").UnansweredReplies);

        Assert.IsTrue(
            f.Monitor.ReplyArrived("MSG1", "B", "A"));

        Assert.AreEqual(1,
            f.Ledger.Get("A", "B").Total);

        Assert.IsFalse(
            f.Monitor.ReplyArrived("MSG1", "B", "A"));
    }

    [Test]
    public void ReplySkepticism_CompensationPreservesBadReferrals()
    {
        var f = Create();

        f.Ledger.AssessReferral(
            "A", "B", "D1",
            +1, ListeningReaction.OpposeAndShelve);

        f.Monitor.Track("MSG1", "A", "B", 3);

        f.Clock.AdvanceDays(4);

        Assert.AreEqual(2,
            f.Ledger.Get("A", "B").Total);

        Assert.IsTrue(f.Monitor.ReplyArrived(
            "MSG1", "B", "A", compensates: true));

        Assert.AreEqual(1,
            f.Ledger.Get("A", "B").BadReferrals);

        Assert.AreEqual(0,
            f.Ledger.Get("A", "B").UnansweredReplies);

        Assert.AreEqual(1,
            f.Ledger.Get("A", "B").Total);
    }

    [Test]
    public void ReplySkepticism_WrongDirectionCannotResolveWindow()
    {
        var f = Create();

        f.Monitor.Track("MSG1", "A", "B", 3);

        Assert.IsFalse(
            f.Monitor.ReplyArrived("MSG1", "A", "B"));

        Assert.IsFalse(
            f.Monitor.ReplyArrived("MSG1", "C", "A"));

        Assert.IsFalse(
            f.Monitor.ReplyArrived("UNKNOWN", "B", "A"));

        f.Clock.AdvanceDays(4);

        Assert.AreEqual(1,
            f.Ledger.Get("A", "B").Total);

        Assert.Throws<ArgumentException>(() =>
            f.Monitor.Track("MSG1", "A", "B", 5));
    }

    [Test]
    public void ReplySkepticism_IndependentWindowsAndOptIn()
    {
        var f = Create();

        f.Monitor.Track("MSG1", "A", "B", 3);
        f.Monitor.Track("MSG2", "A", "B", 3);
        f.Monitor.Track("MSG3", "B", "A", 3);

        Assert.IsTrue(
            f.Monitor.ReplyArrived("MSG2", "B", "A"));

        f.Clock.AdvanceDays(4);

        Assert.AreEqual(1,
            f.Ledger.Get("A", "B").UnansweredReplies);

        Assert.AreEqual(1,
            f.Ledger.Get("B", "A").UnansweredReplies);

        Assert.IsTrue(
            f.Monitor.ReplyArrived(
                "MSG1", "B", "A", compensates: true));

        Assert.AreEqual(0,
            f.Ledger.Get("A", "B").Total);

        Assert.AreEqual(1,
            f.Ledger.Get("B", "A").Total);

        // Seuraamattomat viestit eivät
        // synnytä skeptisyysmerkintöjä.
        Assert.IsFalse(
            f.Monitor.ReplyArrived(
                "UNTRACKED", "B", "A"));
    }
}
