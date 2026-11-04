
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class CirculationObservationReaderTests
{
    private static CirculationWorldDefinition Definition()
    {
        var d = new CirculationWorldDefinition();

        d.AddNode("A", "FI");
        d.AddNode("B", "FI");
        d.AddNode("C", "SE");

        d.AddConnection("AB", "A", "B");
        d.AddConnection("BC", "B", "C");

        d.MarkKnownMissing("A", "B");
        d.MarkKnownMissing("B", "C");

        d.SetSeed("A", "D1");
        d.AddSeed("A", "D2");

        return d;
    }

    private static ConfigurableCirculationScenario Create(
        CirculationWorldDefinition definition = null,
        NodeReactionRules rules = null)
    {
        return new ConfigurableCirculationScenario(
            definition ?? Definition(),
            listeningDays: 2,
            reactionRules: rules);
    }

    [Test]
    public void Observation_ContainsNodesAndProducts()
    {
        var s = Create();
        var view = s.Observation.Capture();

        Assert.AreEqual(0, view.Day);
        Assert.AreEqual(3, view.Nodes.Count);
        Assert.AreEqual(2, view.ProductIds.Count);
        Assert.AreEqual(6, view.ProductStates.Count);

        CollectionAssert.AreEqual(
            new[] { "A", "B", "C" },
            view.Nodes.Select(n => n.Id).ToArray());

        CollectionAssert.AreEqual(
            new[] { "D1", "D2" },
            view.ProductIds);

        Assert.AreEqual("SE", view.GetNode("C").CountryCode);
        Assert.AreEqual(10, view.GetNode("B").Capital);
    }

    [Test]
    public void Observation_PreservesDirectedParallelArcs()
    {
        var definition = Definition();

        definition.AddConnection("BC2", "B", "C");

        var s = Create(definition);
        var view = s.Observation.Capture();

        Assert.AreEqual(3, view.Connections.Count);

        CollectionAssert.AreEquivalent(
            new[] { "BC", "BC2" },
            view.Connections
                .Where(c =>
                    c.SenderId == "B" &&
                    c.RecipientId == "C")
                .Select(c => c.Id)
                .ToArray());

        Assert.AreEqual(0, view.Connections.Count(c =>
            c.SenderId == "C" && c.RecipientId == "B"));
    }

    [Test]
    public void Observation_InTransitIsNotYetReceived()
    {
        var s = Create();

        s.World.AdvanceDay();

        var view = s.Observation.Capture();

        Assert.AreEqual(1, view.Day);

        Assert.IsTrue(
            view.GetState("A", "D1").EverReceived);

        Assert.IsFalse(
            view.GetState("B", "D1").EverReceived);

        Assert.AreEqual(
            ObservableDemoPhase.NotReceived,
            view.GetState("B", "D1").Phase);

        Assert.AreEqual(2, s.Clock.PendingCount);
    }

    [Test]
    public void Observation_SeparatesUnopenedAndListening()
    {
        var s = Create();

        s.World.AdvanceDays(2);

        var view = s.Observation.Capture();

        Assert.AreEqual(
            ObservableDemoPhase.Listening,
            view.GetState("B", "D1").Phase);

        Assert.AreEqual(
            ObservableDemoPhase.ReceivedUnopened,
            view.GetState("B", "D2").Phase);

        Assert.IsTrue(
            view.GetState("B", "D2").EverReceived);

        Assert.AreEqual(0f,
            view.GetState("B", "D1").Progress, 0.001f);
    }

    [Test]
    public void Observation_ExposesQueuedListeningProgress()
    {
        var s = Create();

        s.World.AdvanceDays(3);

        var view = s.Observation.Capture();

        Assert.AreEqual(
            ObservableDemoPhase.Listening,
            view.GetState("B", "D1").Phase);

        Assert.AreEqual(0.5f,
            view.GetState("B", "D1").Progress, 0.001f);

        Assert.AreEqual(
            ObservableDemoPhase.Queued,
            view.GetState("B", "D2").Phase);

        Assert.AreEqual(0f,
            view.GetState("B", "D2").Progress, 0.001f);
    }

    [Test]
    public void Observation_CompletedListeningShowsReaction()
    {
        var s = Create();

        s.World.AdvanceDays(4);

        var view = s.Observation.Capture();
        var state = view.GetState("B", "D1");

        Assert.AreEqual(
            ObservableDemoPhase.Completed,
            state.Phase);

        Assert.AreEqual(1f, state.Progress, 0.001f);

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            state.Reaction);

        Assert.AreEqual(4, state.ReactionDay);

        Assert.AreEqual(
            ObservableDemoPhase.Listening,
            view.GetState("B", "D2").Phase);
    }

    [Test]
    public void Observation_ReactionsAreProductSpecific()
    {
        var rules = new NodeReactionRules();

        rules.Set(
            "B", "D1",
            ListeningReaction.OpposeAndShelve);

        rules.Set(
            "B", "D2",
            ListeningReaction.SupportAndCopy);

        var s = Create(rules: rules);

        s.World.AdvanceDays(6);

        var view = s.Observation.Capture();

        Assert.AreEqual(
            ListeningReaction.OpposeAndShelve,
            view.GetState("B", "D1").Reaction);

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            view.GetState("B", "D2").Reaction);

        Assert.AreEqual(10, view.GetNode("B").Capital);

        Assert.AreEqual(
            ObservableDemoPhase.NotReceived,
            view.GetState("C", "D1").Phase);

        Assert.IsNull(
            view.GetState("C", "D1").Reaction);
    }

    [Test]
    public void Observation_OldSnapshotsRemainUnchanged()
    {
        var s = Create();

        var before = s.Observation.Capture();

        s.World.AdvanceDays(4);

        var after = s.Observation.Capture();

        Assert.AreEqual(0, before.Day);
        Assert.AreEqual(4, after.Day);

        Assert.AreEqual(10, before.GetNode("A").Capital);
        Assert.AreEqual(18, after.GetNode("A").Capital);

        Assert.AreEqual(
            ObservableDemoPhase.NotReceived,
            before.GetState("B", "D1").Phase);

        Assert.AreEqual(
            ObservableDemoPhase.Completed,
            after.GetState("B", "D1").Phase);
    }

    [Test]
    public void Observation_CaptureHasNoSimulationSideEffects()
    {
        var s = Create();

        var first = s.Observation.Capture();
        var second = s.Observation.Capture();

        Assert.AreEqual(first.Day, second.Day);

        Assert.AreEqual(0, s.World.CurrentDay);
        Assert.AreEqual(0, s.World.OpenedMessages.Count);

        Assert.AreEqual(2, s.Clock.PendingCount);
        Assert.AreEqual(2, s.CopyIndex.Count);

        Assert.AreEqual(0,
            s.GetReactionPolicy("B").Decisions.Count);
    }

    [Test]
    public void Observation_RejectsUnknownNodeOrProduct()
    {
        var s = Create();
        var view = s.Observation.Capture();

        Assert.Throws<ArgumentException>(() =>
            view.GetNode("UNKNOWN"));

        Assert.Throws<ArgumentException>(() =>
            view.GetState("UNKNOWN", "D1"));

        Assert.Throws<ArgumentException>(() =>
            view.GetState("A", "UNKNOWN"));

        Assert.AreEqual(6, view.ProductStates.Count);
    }
}
