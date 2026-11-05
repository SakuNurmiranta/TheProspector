
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class CirculationTimelineTests
{
    private static ConfigurableCirculationScenario Create(
        ListeningReaction d1 =
            ListeningReaction.SupportAndCopy,
        bool twoDemos = false,
        ListeningReaction d2 =
            ListeningReaction.SupportAndCopy)
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddNode("C", "FI");

        definition.AddConnection("AB", "A", "B");
        definition.AddConnection("BA", "B", "A");
        definition.AddConnection("BC", "B", "C");

        definition.MarkKnownMissing("A", "B");
        definition.MarkKnownMissing("B", "C");

        definition.SetSeed("A", "D1");

        if (twoDemos)
            definition.AddSeed("A", "D2");

        var rules = new NodeReactionRules();
        rules.Set("B", "D1", d1);

        if (twoDemos)
            rules.Set("B", "D2", d2);

        return new ConfigurableCirculationScenario(
            definition,
            listeningDays: 2,
            reactionRules: rules);
    }

    [Test]
    public void Timeline_IncludesSeedShipmentsAtDayZero()
    {
        var s = Create(twoDemos: true);
        var history = s.Timeline.Capture();

        Assert.AreEqual(0, history.Day);
        Assert.AreEqual(2, history.Events.Count);

        Assert.AreEqual(2, history.Events.Count(e =>
            e.Kind == CirculationHistoryKind.SeedDispatched));

        CollectionAssert.AreEquivalent(
            new[] { "D1", "D2" },
            history.Events.Select(e => e.ProductId).ToArray());

        Assert.IsTrue(history.Events.All(e =>
            e.Day == 0 &&
            e.NodeId == "A" &&
            e.TargetNodeId == "B"));
    }

    [Test]
    public void Timeline_OpeningPrecedesListeningDecision()
    {
        var s = Create();

        s.World.AdvanceDays(2);

        var history = s.Timeline.Capture();

        Assert.AreEqual(1, history.AtDay(2).Count);

        var opened = history.AtDay(2).Single();

        Assert.AreEqual(
            CirculationHistoryKind.MessageOpened,
            opened.Kind);

        Assert.AreEqual("B", opened.NodeId);
        Assert.AreEqual("D1", opened.ProductId);

        Assert.AreEqual(0, history.Events.Count(e =>
            e.Kind == CirculationHistoryKind.ListeningDecision));
    }

    [Test]
    public void Timeline_RecordsReactionOnlyAfterListening()
    {
        var s = Create();

        s.World.AdvanceDays(3);

        Assert.AreEqual(0, s.Timeline.Capture().Events.Count(e =>
            e.Kind == CirculationHistoryKind.ListeningDecision));

        s.World.AdvanceDay();

        var decision = s.Timeline.Capture().Events.Single(e =>
            e.Kind == CirculationHistoryKind.ListeningDecision);

        Assert.AreEqual(4, decision.Day);
        Assert.AreEqual("B", decision.NodeId);
        Assert.AreEqual("D1", decision.ProductId);

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            decision.Reaction);
    }

    [Test]
    public void Timeline_SeparatesDecisionFromPhysicalCopy()
    {
        var s = Create();

        s.World.AdvanceDays(4);

        var events = s.Timeline.Capture().AtDay(4);

        Assert.AreEqual(1, events.Count(e =>
            e.Kind == CirculationHistoryKind.ListeningDecision));

        Assert.AreEqual(1, events.Count(e =>
            e.Kind == CirculationHistoryKind.CopyDispatched));

        var copy = events.Single(e =>
            e.Kind == CirculationHistoryKind.CopyDispatched);

        Assert.AreEqual("B", copy.NodeId);
        Assert.AreEqual("C", copy.TargetNodeId);
        Assert.AreEqual("D1", copy.ProductId);

        Assert.AreEqual(0, events.Count(e =>
            e.Kind == CirculationHistoryKind.TransferDispatched));
    }

    [Test]
    public void Timeline_SoftControlDoesNotRecordNewCopy()
    {
        var s = Create(
            ListeningReaction.OpposeAndTransfer);

        s.World.AdvanceDays(4);

        var events = s.Timeline.Capture().AtDay(4);

        Assert.AreEqual(1, events.Count(e =>
            e.Kind == CirculationHistoryKind.TransferDispatched));

        Assert.AreEqual(0, events.Count(e =>
            e.Kind == CirculationHistoryKind.CopyDispatched));

        var transfer = events.Single(e =>
            e.Kind == CirculationHistoryKind.TransferDispatched);

        Assert.AreEqual("B", transfer.NodeId);
        Assert.AreEqual("C", transfer.TargetNodeId);
        Assert.AreEqual("D1", transfer.ProductId);
    }

    [Test]
    public void Timeline_HardControlRecordsShelfAndNotices()
    {
        var s = Create(
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(4);

        var events = s.Timeline.Capture().AtDay(4);

        Assert.AreEqual(1, events.Count(e =>
            e.Kind == CirculationHistoryKind.Shelved));

        Assert.AreEqual(2, events.Count(e =>
            e.Kind ==
            CirculationHistoryKind.NegativeNoticeDispatched));

        CollectionAssert.AreEquivalent(
            new[] { "A", "C" },
            events
                .Where(e => e.Kind ==
                    CirculationHistoryKind.NegativeNoticeDispatched)
                .Select(e => e.TargetNodeId)
                .ToArray());

        Assert.AreEqual(0, events.Count(e =>
            e.Kind == CirculationHistoryKind.CopyDispatched));
    }

    [Test]
    public void Timeline_NoticeOpeningIsSeparateFromSending()
    {
        var s = Create(
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(4);

        var before = s.Timeline.Capture();

        Assert.AreEqual(2, before.AtDay(4).Count(e =>
            e.Kind ==
            CirculationHistoryKind.NegativeNoticeDispatched));

        Assert.IsNull(
            s.Stances.GetLastThumb("C", "B", "D1"));

        s.World.AdvanceDays(2);

        var after = s.Timeline.Capture();

        Assert.IsTrue(after.AtDay(6).Any(e =>
            e.Kind == CirculationHistoryKind.MessageOpened &&
            e.NodeId == "C" &&
            e.ProductId == "D1"));

        Assert.AreEqual(-1,
            s.Stances.GetLastThumb("C", "B", "D1"));

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("C", "D1"));
    }

    [Test]
    public void Timeline_SeparatesTwoCulturalProducts()
    {
        var s = Create(
            ListeningReaction.OpposeAndShelve,
            twoDemos: true,
            d2: ListeningReaction.SupportAndCopy);

        s.World.AdvanceDays(6);

        var history = s.Timeline.Capture();

        Assert.AreEqual(1, history.ForProduct("D1").Count(e =>
            e.Kind == CirculationHistoryKind.Shelved));

        Assert.AreEqual(0, history.ForProduct("D2").Count(e =>
            e.Kind == CirculationHistoryKind.Shelved));

        Assert.AreEqual(0, history.ForProduct("D1").Count(e =>
            e.Kind == CirculationHistoryKind.CopyDispatched));

        Assert.AreEqual(1, history.ForProduct("D2").Count(e =>
            e.Kind == CirculationHistoryKind.CopyDispatched));
    }

    [Test]
    public void Timeline_OldCaptureIsStableAndReadingHasNoEffects()
    {
        var s = Create();

        var before = s.Timeline.Capture();

        Assert.AreEqual(0, s.World.CurrentDay);
        Assert.AreEqual(1, s.Clock.PendingCount);
        Assert.AreEqual(1, s.CopyIndex.Count);

        s.World.AdvanceDays(6);

        var after = s.Timeline.Capture();

        Assert.AreEqual(0, before.Day);
        Assert.AreEqual(6, after.Day);

        Assert.AreEqual(1, before.Events.Count);

        Assert.AreEqual(0, before.Events.Count(e =>
            e.Kind == CirculationHistoryKind.ListeningDecision));

        Assert.AreEqual(1, after.Events.Count(e =>
            e.Kind == CirculationHistoryKind.ListeningDecision));
    }

    [Test]
    public void Timeline_BatchedAndSteppedRunsMatch()
    {
        var first = Create(
            ListeningReaction.OpposeAndShelve,
            twoDemos: true);

        var second = Create(
            ListeningReaction.OpposeAndShelve,
            twoDemos: true);

        first.World.AdvanceDays(12);

        for (int i = 0; i < 12; i++)
            second.World.AdvanceDay();

        string Signature(CirculationHistoryEvent e)
        {
            return e.Day + "|" + e.Kind + "|" +
                   e.NodeId + "|" + e.TargetNodeId + "|" +
                   e.ProductId + "|" + e.ShipmentId + "|" +
                   e.Reaction;
        }

        CollectionAssert.AreEqual(
            first.Timeline.Capture().Events.Select(Signature),
            second.Timeline.Capture().Events.Select(Signature));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            first.Timeline.Capture().AtDay(-1));

        Assert.Throws<ArgumentException>(() =>
            first.Timeline.Capture().ForProduct("UNKNOWN"));
    }
}
