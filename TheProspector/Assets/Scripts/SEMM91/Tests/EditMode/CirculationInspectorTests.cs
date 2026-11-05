
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class CirculationInspectorTests
{
    private static CirculationWorldDefinition Definition(
        bool parallel = false)
    {
        var d = new CirculationWorldDefinition();

        d.AddNode("A", "FI");
        d.AddNode("B", "FI");
        d.AddNode("C", "FI");

        d.AddConnection("AB", "A", "B");
        d.AddConnection("BC", "B", "C");

        if (parallel)
            d.AddConnection("BC2", "B", "C");

        d.MarkKnownMissing("A", "B");
        d.MarkKnownMissing("B", "C");

        d.SetSeed("A", "D1");
        d.AddSeed("A", "D2");

        return d;
    }

    private static ConfigurableCirculationScenario Create(
        NodeReactionRules rules = null,
        bool parallel = false)
    {
        return new ConfigurableCirculationScenario(
            Definition(parallel),
            listeningDays: 2,
            reactionRules: rules);
    }

    private static NodeReactionRules TransferD1()
    {
        var rules = new NodeReactionRules();

        rules.Set(
            "B", "D1",
            ListeningReaction.OpposeAndTransfer);

        return rules;
    }

    [Test]
    public void Inspector_NodeDetailsPreserveDirection()
    {
        var s = Create();
        var view = s.Observation.Capture();

        var details =
            CirculationInspectorProjection.ForNode(view, "B");

        Assert.AreEqual("B", details.NodeId);
        Assert.AreEqual("FI", details.CountryCode);
        Assert.AreEqual(10, details.Capital);

        Assert.AreEqual(1, details.Incoming.Count);
        Assert.AreEqual("AB", details.Incoming[0].Id);

        Assert.AreEqual(1, details.Outgoing.Count);
        Assert.AreEqual("BC", details.Outgoing[0].Id);

        Assert.AreEqual(2, details.Products.Count);
    }

    [Test]
    public void Inspector_NodeDetailsPreserveParallelArcs()
    {
        var s = Create(parallel: true);
        var view = s.Observation.Capture();

        var details =
            CirculationInspectorProjection.ForNode(view, "B");

        CollectionAssert.AreEquivalent(
            new[] { "BC", "BC2" },
            details.Outgoing
                .Select(c => c.Id)
                .ToArray());

        Assert.AreEqual(1, details.Incoming.Count);
    }

    [Test]
    public void Inspector_ProductSummaryStartsFromRealInventory()
    {
        var s = Create();
        var view = s.Observation.Capture();

        var details =
            CirculationInspectorProjection.ForProduct(
                view, "D1");

        Assert.AreEqual("D1", details.ProductId);
        Assert.AreEqual(3, details.NodeStates.Count);

        Assert.AreEqual(1, details.EverReceivedCount);
        Assert.AreEqual(2, details.NotReceivedCount);
        Assert.AreEqual(1, details.ReceivedUnopenedCount);

        Assert.AreEqual(0, details.ListeningCount);
        Assert.AreEqual(0, details.CompletedCount);
        Assert.AreEqual(0, details.SupportCount);
    }

    [Test]
    public void Inspector_DistinguishesQueuedAndListening()
    {
        var s = Create();

        s.World.AdvanceDays(3);

        var view = s.Observation.Capture();
        var details =
            CirculationInspectorProjection.ForNode(view, "B");

        var d1 = details.Products.Single(
            p => p.ProductId == "D1");

        var d2 = details.Products.Single(
            p => p.ProductId == "D2");

        Assert.AreEqual(
            ObservableDemoPhase.Listening, d1.Phase);

        Assert.AreEqual(0.5f, d1.Progress, 0.001f);

        Assert.AreEqual(
            ObservableDemoPhase.Queued, d2.Phase);

        Assert.IsNull(d1.Reaction);
        Assert.IsNull(d2.Reaction);
    }

    [Test]
    public void Inspector_CompletedListeningShowsPrivateReaction()
    {
        var s = Create();

        s.World.AdvanceDays(4);

        var details =
            CirculationInspectorProjection.ForProduct(
                s.Observation.Capture(), "D1");

        Assert.AreEqual(1, details.CompletedCount);
        Assert.AreEqual(1, details.SupportCount);

        var b = details.NodeStates.Single(
            state => state.NodeId == "B");

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            b.Reaction);

        Assert.AreEqual(4, b.ReactionDay);
    }

    [Test]
    public void Inspector_ShelvingDoesNotInventPublicDislike()
    {
        var rules = new NodeReactionRules();

        rules.Set(
            "B", "D1",
            ListeningReaction.OpposeAndShelve);

        var s = Create(rules);

        s.World.AdvanceDays(5);

        var view = s.Observation.Capture();
        var b = CirculationInspectorProjection.ForNode(
            view, "B");

        Assert.AreEqual(
            ListeningReaction.OpposeAndShelve,
            b.Products.Single(
                p => p.ProductId == "D1").Reaction);

        // B:n yksityisestä hyllytyspäätöksestä
        // ei saa päätellä C:n tietävän sitä.
        Assert.IsNull(
            view.GetKnownThumb("C", "B", "D1"));

        Assert.AreEqual(0,
            b.OpinionsKnownByOthers.Count(
                o => o.ObserverNodeId == "C" &&
                     o.ProductId == "D1"));
    }

    [Test]
    public void Inspector_InTransitNegativeOpinionIsNotKnown()
    {
        var s = Create(TransferD1());

        // B siirtää D1:n päivänä 4.
        s.World.AdvanceDays(4);

        var view = s.Observation.Capture();

        Assert.AreEqual(
            ListeningReaction.OpposeAndTransfer,
            view.GetState("B", "D1").Reaction);

        Assert.IsNull(
            view.GetKnownThumb("C", "B", "D1"));

        Assert.AreEqual(1, s.Control.Transfers.Count);
    }

    [Test]
    public void Inspector_OpenedTransferCreatesDirectionalKnowledge()
    {
        var s = Create(TransferD1());

        // Pehmeän kontrollin lähetys saapuu
        // päivänä 6, jolloin C avaa sen.
        s.World.AdvanceDays(6);

        var view = s.Observation.Capture();

        Assert.AreEqual(-1,
            view.GetKnownThumb("C", "B", "D1"));

        Assert.IsNull(
            view.GetKnownThumb("B", "C", "D1"));

        var b = CirculationInspectorProjection.ForNode(
            view, "B");

        var c = CirculationInspectorProjection.ForNode(
            view, "C");

        Assert.IsTrue(
            b.OpinionsKnownByOthers.Any(o =>
                o.ObserverNodeId == "C" &&
                o.ProductId == "D1" &&
                o.Thumb == -1));

        Assert.IsTrue(
            c.OpinionsReceived.Any(o =>
                o.SpeakerNodeId == "B" &&
                o.ProductId == "D1" &&
                o.Thumb == -1));

        // C ei ole vielä päättänyt omasta
        // reaktiostaan, vaikka se tietää
        // B:n kielteisen kannan.
        Assert.IsNull(view.GetState("C", "D1").Reaction);
    }

    [Test]
    public void Inspector_KnowledgeIsProductSpecific()
    {
        var s = Create(TransferD1());

        s.World.AdvanceDays(6);

        var view = s.Observation.Capture();

        Assert.AreEqual(-1,
            view.GetKnownThumb("C", "B", "D1"));

        Assert.IsNull(
            view.GetKnownThumb("C", "B", "D2"));

        var d1 =
            CirculationInspectorProjection.ForProduct(
                view, "D1");

        var d2 =
            CirculationInspectorProjection.ForProduct(
                view, "D2");

        Assert.AreEqual(1, d1.TransferCount);
        Assert.AreEqual(0, d2.TransferCount);

        Assert.AreEqual(1,
            d1.KnownCommunications.Count(o =>
                o.ObserverNodeId == "C" &&
                o.SpeakerNodeId == "B" &&
                o.Thumb == -1));
    }

    [Test]
    public void Inspector_OldSnapshotDoesNotLearnFutureOpinions()
    {
        var s = Create(TransferD1());

        s.World.AdvanceDays(5);
        var before = s.Observation.Capture();

        s.World.AdvanceDay();
        var after = s.Observation.Capture();

        Assert.AreEqual(5, before.Day);
        Assert.AreEqual(6, after.Day);

        Assert.IsNull(
            before.GetKnownThumb("C", "B", "D1"));

        Assert.AreEqual(-1,
            after.GetKnownThumb("C", "B", "D1"));
    }

    [Test]
    public void Inspector_ReadingDoesNotAdvanceSimulation()
    {
        var s = Create();

        var first = s.Observation.Capture();

        CirculationInspectorProjection.ForNode(
            first, "A");

        CirculationInspectorProjection.ForProduct(
            first, "D1");

        var second = s.Observation.Capture();

        Assert.AreEqual(0, second.Day);
        Assert.AreEqual(2, s.Clock.PendingCount);
        Assert.AreEqual(2, s.CopyIndex.Count);
        Assert.AreEqual(0, s.World.OpenedMessages.Count);
    }

    [Test]
    public void Inspector_RejectsUnknownSelections()
    {
        var s = Create();
        var view = s.Observation.Capture();

        Assert.Throws<ArgumentException>(() =>
            CirculationInspectorProjection.ForNode(
                view, "UNKNOWN"));

        Assert.Throws<ArgumentException>(() =>
            CirculationInspectorProjection.ForProduct(
                view, "UNKNOWN"));

        Assert.Throws<ArgumentException>(() =>
            view.GetKnownThumb("UNKNOWN", "B", "D1"));

        Assert.Throws<ArgumentException>(() =>
            view.GetKnownThumb("C", "B", "UNKNOWN"));

        Assert.Throws<ArgumentNullException>(() =>
            CirculationInspectorProjection.ForNode(
                null, "B"));
    }
}
