
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class ListeningReactionNodePolicyTests
{
    private sealed class RecordingControl
        : IPhysicalControlActions
    {
        public readonly List<string> Transfers =
            new List<string>();

        public readonly List<string> Shelves =
            new List<string>();

        public void TransferOne(
            string nodeId,
            CassetteCopy copy,
            int day)
        {
            Transfers.Add(
                nodeId + ":" + copy.Id + ":" + day);
        }

        public void Shelve(
            string nodeId,
            CassetteCopy copy,
            int day)
        {
            Shelves.Add(
                nodeId + ":" + copy.Id + ":" + day);
        }
    }

    private sealed class Fixture
    {
        public AutonomousCirculationScenario Scenario;
        public BacklogItem Item;
        public RecordingControl Control;
        public ListeningReactionNodePolicy Policy;

        public void React(int day = 2)
        {
            Policy.OnMessageOpened(
                "B", Item, 0, day);
        }
    }

    private static Fixture Create(
        ListeningReaction reaction)
    {
        var s = new AutonomousCirculationScenario();

        // Saapuva posti käsitellään tässä testissä
        // käsin, jotta voimme testata juuri
        // kuuntelun jälkeistä reaktiota.
        s.Clock.AdvanceTo(2);

        Assert.IsTrue(
            s.Backlog.TryTakeNext(
                "B", out BacklogItem item));

        var control = new RecordingControl();

        var policy = new ListeningReactionNodePolicy(
            "B",
            s.GetPolicy("B"),
            s.CopyIndex.Resolve,
            (nodeId, productId) => reaction,
            control);

        return new Fixture
        {
            Scenario = s,
            Item = item,
            Control = control,
            Policy = policy
        };
    }

    [Test]
    public void Reaction_DoesNotRunBeforeMessageIsHandled()
    {
        var f = Create(ListeningReaction.SupportAndCopy);

        Assert.AreEqual(0, f.Policy.Decisions.Count);
        Assert.AreEqual(0, f.Control.Transfers.Count);
        Assert.AreEqual(0, f.Control.Shelves.Count);
        Assert.AreEqual(
            0, f.Scenario.GetPolicy("B").Dispatched.Count);
    }

    [Test]
    public void Reaction_SupportUsesExistingCopyService()
    {
        var f = Create(ListeningReaction.SupportAndCopy);

        f.React();

        Assert.AreEqual(1, f.Policy.Decisions.Count);
        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            f.Policy.Decisions[0].Reaction);

        Assert.AreEqual(
            1, f.Scenario.GetPolicy("B").Dispatched.Count);

        Assert.AreEqual(
            "C",
            f.Scenario.GetPolicy("B")
                .Dispatched[0].RecipientNodeId);

        Assert.AreEqual(0, f.Control.Transfers.Count);
        Assert.AreEqual(0, f.Control.Shelves.Count);
    }

    [Test]
    public void Reaction_SoftControlDoesNotCopyOrShelve()
    {
        var f = Create(ListeningReaction.OpposeAndTransfer);

        f.React();

        Assert.AreEqual(
            ListeningReaction.OpposeAndTransfer,
            f.Policy.Decisions[0].Reaction);

        Assert.AreEqual(1, f.Control.Transfers.Count);
        Assert.AreEqual(0, f.Control.Shelves.Count);

        Assert.AreEqual(
            0, f.Scenario.GetPolicy("B").Dispatched.Count);

        Assert.AreEqual(
            0, f.Scenario.GetPolicy("B").CopyDecisions);
    }

    [Test]
    public void Reaction_HardControlDoesNotCopyOrTransfer()
    {
        var f = Create(ListeningReaction.OpposeAndShelve);

        f.React();

        Assert.AreEqual(
            ListeningReaction.OpposeAndShelve,
            f.Policy.Decisions[0].Reaction);

        Assert.AreEqual(0, f.Control.Transfers.Count);
        Assert.AreEqual(1, f.Control.Shelves.Count);

        Assert.AreEqual(
            0, f.Scenario.GetPolicy("B").Dispatched.Count);
    }

    [Test]
    public void Reaction_DoesNotRepeatForSameDemo()
    {
        var f = Create(ListeningReaction.OpposeAndTransfer);

        f.React();
        f.React();

        Assert.AreEqual(1, f.Policy.Decisions.Count);
        Assert.AreEqual(1, f.Control.Transfers.Count);
    }

    [Test]
    public void Reaction_RejectsWrongNode()
    {
        var f = Create(ListeningReaction.SupportAndCopy);

        Assert.Throws<ArgumentException>(() =>
            f.Policy.OnMessageOpened(
                "C", f.Item, 0, 2));

        Assert.AreEqual(0, f.Policy.Decisions.Count);
    }

    [Test]
    public void Reaction_FeedbackOnlyDoesNotCreateDecision()
    {
        var f = Create(ListeningReaction.OpposeAndShelve);
        var s = f.Scenario;

        var ab = s.Network.GetOutgoingConnections(
                "A", CommunicationLayer.Interpersonal)
            .Single(c => c.Id == "AB");

        s.Exchange.Send(
            "FEEDBACK-SHIP",
            new MessagePackage(
                "FEEDBACK-MSG",
                ab,
                feedbackProduct: s.Demo,
                thumb: +1),
            "FI", "FI", 5);

        s.Clock.AdvanceTo(4);

        Assert.IsTrue(
            s.Backlog.TryTakeNext(
                "B", out BacklogItem feedback));

        f.Policy.OnMessageOpened(
            "B", feedback, 0, 4);

        Assert.AreEqual(0, f.Policy.Decisions.Count);
        Assert.AreEqual(0, f.Control.Shelves.Count);
        Assert.AreEqual(0, f.Control.Transfers.Count);
    }

    [Test]
    public void Reaction_RejectsUndefinedDecisionWithoutAction()
    {
        var f = Create((ListeningReaction)999);

        Assert.Throws<InvalidOperationException>(() =>
            f.React());

        Assert.AreEqual(0, f.Policy.Decisions.Count);
        Assert.AreEqual(0, f.Control.Transfers.Count);
        Assert.AreEqual(0, f.Control.Shelves.Count);
        Assert.AreEqual(
            0, f.Scenario.GetPolicy("B").Dispatched.Count);
    }
}
