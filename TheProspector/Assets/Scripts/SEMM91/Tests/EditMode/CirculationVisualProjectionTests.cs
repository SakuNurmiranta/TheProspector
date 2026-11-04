
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class CirculationVisualProjectionTests
{
    private static CirculationWorldDefinition Definition()
    {
        var d = new CirculationWorldDefinition();

        d.AddNode("A", "FI");
        d.AddNode("B", "FI");
        d.AddNode("C", "SE");

        d.AddConnection("AB", "A", "B");
        d.AddConnection("BC", "B", "C");
        d.AddConnection("CA", "C", "A");

        d.MarkKnownMissing("A", "B");
        d.MarkKnownMissing("B", "C");

        d.SetSeed("A", "D1");
        d.AddSeed("A", "D2");

        return d;
    }

    private static ConfigurableCirculationScenario Create(
        CirculationWorldDefinition definition = null)
    {
        return new ConfigurableCirculationScenario(
            definition ?? Definition(),
            listeningDays: 2);
    }

    private static CirculationVisualSnapshot Project(
        ConfigurableCirculationScenario scenario,
        string node = null,
        string demo = null)
    {
        return CirculationVisualProjection.Build(
            scenario.Observation.Capture(),
            node, demo);
    }

    [Test]
    public void Projection_NoSelectionKeepsNodesNeutral()
    {
        var s = Create();
        var view = Project(s);

        Assert.AreEqual(3, view.Nodes.Count);
        Assert.AreEqual(3, view.Connections.Count);

        Assert.IsTrue(view.Nodes.All(
            n => n.Tone == CirculationNodeTone.Neutral));

        Assert.IsTrue(view.Connections.All(
            c => !c.IsHighlighted));
    }

    [Test]
    public void Projection_SelectedNodeHasDistinctTone()
    {
        var s = Create();
        var view = Project(s, node: "B");

        Assert.AreEqual(
            CirculationNodeTone.SelectedNode,
            view.GetNode("B").Tone);

        Assert.AreEqual(
            CirculationNodeTone.OutgoingContact,
            view.GetNode("C").Tone);

        Assert.AreEqual(
            CirculationNodeTone.Neutral,
            view.GetNode("A").Tone);
    }

    [Test]
    public void Projection_IncomingArcIsNotOutgoingContact()
    {
        var s = Create();
        var view = Project(s, node: "A");

        Assert.AreEqual(
            CirculationNodeTone.OutgoingContact,
            view.GetNode("B").Tone);

        // C -> A on saapuva yhteys,
        // ei A:n lähtevä kontakti.
        Assert.AreEqual(
            CirculationNodeTone.Neutral,
            view.GetNode("C").Tone);

        Assert.IsTrue(view.Connections.Single(
            c => c.Id == "AB").IsHighlighted);

        Assert.IsFalse(view.Connections.Single(
            c => c.Id == "CA").IsHighlighted);
    }

    [Test]
    public void Projection_PreservesParallelDirectedArcs()
    {
        var d = Definition();
        d.AddConnection("BC2", "B", "C");

        var s = Create(d);
        var view = Project(s, node: "B");

        Assert.AreEqual(4, view.Connections.Count);

        CollectionAssert.AreEquivalent(
            new[] { "BC", "BC2" },
            view.Connections
                .Where(c => c.IsHighlighted)
                .Select(c => c.Id)
                .ToArray());

        Assert.AreEqual(
            CirculationNodeTone.OutgoingContact,
            view.GetNode("C").Tone);
    }

    [Test]
    public void Projection_InitialDemoStateDistinguishesOrigin()
    {
        var s = Create();
        var view = Project(s, demo: "D1");

        Assert.AreEqual(
            CirculationNodeTone.ReceivedUnopened,
            view.GetNode("A").Tone);

        Assert.AreEqual(
            CirculationNodeTone.NotReceived,
            view.GetNode("B").Tone);

        Assert.AreEqual(
            CirculationNodeTone.NotReceived,
            view.GetNode("C").Tone);
    }

    [Test]
    public void Projection_UnopenedAndListeningAreDistinct()
    {
        var s = Create();
        s.World.AdvanceDays(2);

        var d1 = Project(s, demo: "D1");
        var d2 = Project(s, demo: "D2");

        Assert.AreEqual(
            CirculationNodeTone.Listening,
            d1.GetNode("B").Tone);

        Assert.AreEqual(
            CirculationNodeTone.ReceivedUnopened,
            d2.GetNode("B").Tone);

        Assert.AreEqual(
            0f, d1.GetNode("B").Progress, 0.001f);
    }

    [Test]
    public void Projection_QueuedListeningIsVisible()
    {
        var s = Create();
        s.World.AdvanceDays(3);

        var d1 = Project(s, demo: "D1");
        var d2 = Project(s, demo: "D2");

        Assert.AreEqual(
            CirculationNodeTone.Listening,
            d1.GetNode("B").Tone);

        Assert.AreEqual(
            0.5f, d1.GetNode("B").Progress, 0.001f);

        Assert.AreEqual(
            CirculationNodeTone.Queued,
            d2.GetNode("B").Tone);
    }

    [Test]
    public void Projection_CompletedListeningIsVisible()
    {
        var s = Create();
        s.World.AdvanceDays(4);

        var view = Project(s, demo: "D1");

        Assert.AreEqual(
            CirculationNodeTone.Completed,
            view.GetNode("B").Tone);

        Assert.AreEqual(
            1f, view.GetNode("B").Progress, 0.001f);
    }

    [Test]
    public void Projection_DemosKeepIndependentStates()
    {
        var s = Create();
        s.World.AdvanceDays(4);

        var first = Project(s, demo: "D1");
        var second = Project(s, demo: "D2");

        Assert.AreEqual(
            CirculationNodeTone.Completed,
            first.GetNode("B").Tone);

        Assert.AreEqual(
            CirculationNodeTone.Listening,
            second.GetNode("B").Tone);

        Assert.AreEqual(
            0f, second.GetNode("B").Progress, 0.001f);
    }

    [Test]
    public void Projection_PreviousSnapshotRemainsUnchanged()
    {
        var s = Create();

        var original = s.Observation.Capture();
        var before = CirculationVisualProjection.Build(
            original, selectedProductId: "D1");

        s.World.AdvanceDays(4);

        var after = Project(s, demo: "D1");

        Assert.AreEqual(
            CirculationNodeTone.NotReceived,
            before.GetNode("B").Tone);

        Assert.AreEqual(
            CirculationNodeTone.Completed,
            after.GetNode("B").Tone);

        Assert.AreEqual(0, before.Day);
        Assert.AreEqual(4, after.Day);
    }

    [Test]
    public void Projection_DoesNotAdvanceSimulation()
    {
        var s = Create();

        var first = Project(s, demo: "D1");
        var second = Project(s, node: "A");

        Assert.AreEqual(0, first.Day);
        Assert.AreEqual(0, second.Day);

        Assert.AreEqual(0, s.World.CurrentDay);
        Assert.AreEqual(2, s.Clock.PendingCount);
        Assert.AreEqual(0, s.World.OpenedMessages.Count);
    }

    [Test]
    public void Projection_RejectsInvalidSelections()
    {
        var s = Create();
        var observation = s.Observation.Capture();

        Assert.Throws<ArgumentException>(() =>
            CirculationVisualProjection.Build(
                observation, selectedNodeId: "UNKNOWN"));

        Assert.Throws<ArgumentException>(() =>
            CirculationVisualProjection.Build(
                observation, selectedProductId: "UNKNOWN"));

        Assert.Throws<ArgumentException>(() =>
            CirculationVisualProjection.Build(
                observation,
                selectedNodeId: "A",
                selectedProductId: "D1"));

        Assert.Throws<ArgumentNullException>(() =>
            CirculationVisualProjection.Build(null));
    }
}
