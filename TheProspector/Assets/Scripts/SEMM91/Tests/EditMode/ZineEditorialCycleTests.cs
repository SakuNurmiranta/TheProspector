
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class ZineEditorialCycleTests
{
    private static (
        SimulationClock Clock,
        ZineEditorialCycle Zine) Create(
        bool profile = true)
    {
        var clock = new SimulationClock();

        var zine = new ZineEditorialCycle(
            "Z1",
            "EDITOR",
            profile,
            clock);

        return (clock, zine);
    }

    [Test]
    public void Zine_FirstIssueAppearsOnDay30()
    {
        var f = Create();

        f.Zine.RecordProductOpinion(
            "A", "D1", true);

        f.Clock.AdvanceDays(29);

        Assert.AreEqual(0, f.Zine.Issues.Count);
        Assert.AreEqual(1, f.Zine.PendingCount);

        f.Clock.AdvanceDays(1);

        Assert.AreEqual(1, f.Zine.Issues.Count);
        Assert.AreEqual(0, f.Zine.PendingCount);

        var issue = f.Zine.Issues[0];

        Assert.AreEqual(1, issue.Number);
        Assert.AreEqual(30, issue.PublishedDay);
        Assert.AreEqual("Z1", issue.ZineId);
        Assert.AreEqual("EDITOR", issue.EditorNodeId);
        Assert.AreEqual("ZINE-ISSUE:Z1:1",
            issue.ProductId);
    }

    [Test]
    public void Zine_CollectsAllThreeInformationTypes()
    {
        var f = Create();

        f.Zine.RecordProductOpinion(
            "A", "D4", false);

        f.Zine.RecordSkepticism(
            "R", "X");

        f.Zine.RecordAddress(
            "C", "ADDR-OLD-C");

        f.Clock.AdvanceDays(30);

        var entries = f.Zine.Issues[0].Entries;

        Assert.AreEqual(3, entries.Count);

        Assert.AreEqual(
            ZineEditorialEntryKind.ProductOpinion,
            entries[0].Kind);

        Assert.AreEqual("A",
            entries[0].SubjectNodeId);
        Assert.AreEqual("D4",
            entries[0].ProductId);
        Assert.AreEqual(false,
            entries[0].Opinion);

        Assert.AreEqual(
            ZineEditorialEntryKind.Skepticism,
            entries[1].Kind);

        Assert.AreEqual("R",
            entries[1].SubjectNodeId);
        Assert.AreEqual("X",
            entries[1].TargetNodeId);

        Assert.AreEqual(
            ZineEditorialEntryKind.Address,
            entries[2].Kind);

        Assert.AreEqual("C",
            entries[2].SubjectNodeId);
        Assert.AreEqual("ADDR-OLD-C",
            entries[2].AddressId);
    }

    [Test]
    public void Zine_NextIssueStartsWithEmptyBuffer()
    {
        var f = Create();

        f.Zine.RecordAddress("C", "ADDR-OLD");

        f.Clock.AdvanceDays(30);

        var first = f.Zine.Issues[0];

        Assert.AreEqual(1, first.Entries.Count);
        Assert.AreEqual(0, f.Zine.PendingCount);

        f.Zine.RecordAddress("C", "ADDR-NEW");

        f.Clock.AdvanceDays(30);

        var second = f.Zine.Issues[1];

        Assert.AreEqual(2, second.Number);
        Assert.AreEqual(60, second.PublishedDay);

        Assert.AreEqual("ADDR-OLD",
            first.Entries[0].AddressId);

        Assert.AreEqual("ADDR-NEW",
            second.Entries[0].AddressId);

        Assert.AreEqual(1, first.Entries.Count);
        Assert.AreEqual(1, second.Entries.Count);
        Assert.AreEqual(0, f.Zine.PendingCount);
    }

    [Test]
    public void Zine_FixedCycleContinuesWithoutContent()
    {
        var f = Create();

        for (int day = 0; day < 90; day++)
            f.Clock.AdvanceDays(1);

        Assert.AreEqual(3, f.Zine.Issues.Count);

        Assert.AreEqual(30,
            f.Zine.Issues[0].PublishedDay);

        Assert.AreEqual(60,
            f.Zine.Issues[1].PublishedDay);

        Assert.AreEqual(90,
            f.Zine.Issues[2].PublishedDay);

        Assert.AreEqual(0,
            f.Zine.Issues[2].Entries.Count);
    }

    [Test]
    public void Zine_BooleanProfilesAreIndependent()
    {
        var clock = new SimulationClock();

        var trueZine = new ZineEditorialCycle(
            "ZT", "EDITOR-T", true, clock);

        var falseZine = new ZineEditorialCycle(
            "ZF", "EDITOR-F", false, clock);

        trueZine.RecordProductOpinion(
            "A", "D1", true);

        falseZine.RecordProductOpinion(
            "B", "D2", false);

        clock.AdvanceDays(30);

        Assert.IsTrue(trueZine.Issues[0].Profile);
        Assert.IsFalse(falseZine.Issues[0].Profile);

        Assert.AreEqual(
            "D1",
            trueZine.Issues[0].Entries[0].ProductId);

        Assert.AreEqual(
            "D2",
            falseZine.Issues[0].Entries[0].ProductId);
    }

    [Test]
    public void Zine_ContradictoryOpinionsRemainSeparate()
    {
        var f = Create();

        f.Zine.RecordProductOpinion(
            "A", "D1", true);

        f.Zine.RecordProductOpinion(
            "A", "D1", false);

        f.Clock.AdvanceDays(30);

        var entries = f.Zine.Issues[0].Entries;

        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual(true, entries[0].Opinion);
        Assert.AreEqual(false, entries[1].Opinion);
    }

    [Test]
    public void Zine_RejectsInvalidIdentifiers()
    {
        var clock = new SimulationClock();

        Assert.Throws<ArgumentException>(() =>
            new ZineEditorialCycle(
                "", "EDITOR", true, clock));

        var f = Create();

        Assert.Throws<ArgumentException>(() =>
            f.Zine.RecordProductOpinion(
                "A", "", true));

        Assert.Throws<ArgumentException>(() =>
            f.Zine.RecordAddress(
                "C", ""));

        Assert.Throws<ArgumentException>(() =>
            f.Zine.RecordSkepticism(
                "", "X"));
    }
}
