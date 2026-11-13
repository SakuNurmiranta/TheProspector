using System;
using NUnit.Framework;
using SEMM91.Domain;
using System.Linq;

public sealed class Crucible92185ReplyGuideTests
{
    private SimulationClock _clock;
    private DirectionalSkepticismLedger _ledger;
    private ReplyWindowSkepticismMonitor _monitor;

    [SetUp]
    public void SetUp()
    {
        _clock = new SimulationClock();
        _ledger = new DirectionalSkepticismLedger();

        _monitor = new ReplyWindowSkepticismMonitor(
            _clock, _ledger);
    }

    private ReplyGuide[] Guides(double day)
    {
        return ReplyExpectationProjection.Build(
            _monitor.GetExpectations(),
            day).ToArray();
    }

    [Test]
    public void Request_CreatesReverseDirectedGuide()
    {
        _monitor.Track("M1", "A", "B", 10);

        var guides = Guides(0);

        Assert.AreEqual(1, guides.Length);
        Assert.AreEqual("B", guides[0].SourceNodeId);
        Assert.AreEqual("A", guides[0].TargetNodeId);
        Assert.AreEqual(0f, guides[0].Progress01);
        Assert.IsFalse(guides[0].IsOverdue);
    }

    [Test]
    public void Progress_UsesFractionalSimulationDays()
    {
        _monitor.Track("M1", "A", "B", 10);

        Assert.AreEqual(
            0.25f, Guides(2.5)[0].Progress01, 0.0001f);

        Assert.AreEqual(
            0.50f, Guides(5.0)[0].Progress01, 0.0001f);

        Assert.AreEqual(
            0.75f, Guides(7.5)[0].Progress01, 0.0001f);

        Assert.AreEqual(
            1.0f, Guides(10.0)[0].Progress01, 0.0001f);
    }

    [Test]
    public void DeadlinePassed_KeepsRedGuide()
    {
        _monitor.Track("M1", "A", "B", 5);
        _clock.AdvanceDays(6);

        var guide = Guides(6)[0];

        Assert.IsTrue(guide.IsOverdue);
        Assert.AreEqual(1f, guide.Progress01);
        Assert.AreEqual(
            1, _ledger.Get("A", "B").UnansweredReplies);
    }

    [Test]
    public void OnTimeReply_RemovesGuide()
    {
        _monitor.Track("M1", "A", "B", 5);
        _clock.AdvanceDays(3);

        Assert.IsTrue(
            _monitor.ReplyArrived("M1", "B", "A"));

        Assert.AreEqual(0, Guides(3).Length);
    }

    [Test]
    public void LateReplyWithoutCompensation_RemainsRed()
    {
        _monitor.Track("M1", "A", "B", 5);
        _clock.AdvanceDays(6);

        Assert.IsTrue(
            _monitor.ReplyArrived(
                "M1", "B", "A", compensates: false));

        Assert.AreEqual(1, Guides(6).Length);
        Assert.IsTrue(Guides(6)[0].IsOverdue);
        Assert.AreEqual(
            1, _ledger.Get("A", "B").UnansweredReplies);
    }

    [Test]
    public void CompensatedLateReply_RemovesGuide()
    {
        _monitor.Track("M1", "A", "B", 5);
        _clock.AdvanceDays(6);

        Assert.IsTrue(
            _monitor.ReplyArrived(
                "M1", "B", "A", compensates: true));

        Assert.AreEqual(
            0, _ledger.Get("A", "B").UnansweredReplies);

        Assert.AreEqual(0, Guides(6).Length);
    }

    [Test]
    public void SeparateCompensation_AlsoRemovesGuide()
    {
        _monitor.Track("M1", "A", "B", 5);
        _clock.AdvanceDays(6);

        _monitor.ReplyArrived(
            "M1", "B", "A", compensates: false);

        Assert.IsTrue(_ledger.CompensateUnanswered(
            "A", "B", "M1"));

        Assert.AreEqual(0, Guides(6).Length);
    }

    [Test]
    public void OverdueGuide_PrioritizedOverNewerWindow()
    {
        _monitor.Track("OLD", "A", "B", 2);
        _clock.AdvanceDays(3);

        _monitor.Track("NEW", "A", "B", 8);

        var guides = Guides(3);

        Assert.AreEqual(1, guides.Length);
        Assert.IsTrue(guides[0].IsOverdue);
        Assert.AreEqual("OLD", guides[0].PackageId);

        _monitor.ReplyArrived(
            "OLD", "B", "A", compensates: true);

        guides = Guides(3);

        Assert.AreEqual(1, guides.Length);
        Assert.IsFalse(guides[0].IsOverdue);
        Assert.AreEqual("NEW", guides[0].PackageId);
    }
}