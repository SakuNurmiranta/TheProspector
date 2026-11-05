
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class HardControlCommunicationTests
{
    private static ConfigurableCirculationScenario Create(
        bool parallelBC = false,
        bool bHasOutgoing = true,
        bool twoDemos = false,
        ListeningReaction bReaction =
            ListeningReaction.OpposeAndShelve)
    {
        var d = new CirculationWorldDefinition();

        d.AddNode("A", "FI");
        d.AddNode("B", "FI");
        d.AddNode("C", "FI");

        d.AddConnection("AB", "A", "B");

        if (bHasOutgoing)
        {
            d.AddConnection("BA", "B", "A");
            d.AddConnection("BC", "B", "C");

            if (parallelBC)
                d.AddConnection("BC2", "B", "C");
        }

        d.MarkKnownMissing("A", "B");

        // Pehmeä kontrolli tarvitsee tässä
        // testissä tunnetun kelvollisen
        // vastaanottajan.
        if (bReaction == ListeningReaction.OpposeAndTransfer)
            d.MarkKnownMissing("B", "C");

        d.SetSeed("A", "D1");

        if (twoDemos)
            d.AddSeed("A", "D2");

        var rules = new NodeReactionRules();

        rules.Set("B", "D1", bReaction);

        return new ConfigurableCirculationScenario(
            d,
            listeningDays: 2,
            reactionRules: rules);
    }

    [Test]
    public void HardControl_DoesNotBroadcastBeforeListeningEnds()
    {
        var s = Create();

        s.World.AdvanceDays(3);

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.AreEqual(0, s.Control.Shelves.Count);
        Assert.AreEqual(0, s.HardControl.Notices.Count);
    }

    [Test]
    public void HardControl_ShelvingBroadcastsToOutgoingContacts()
    {
        var s = Create();

        s.World.AdvanceDays(4);

        Assert.AreEqual(1, s.Control.Shelves.Count);
        Assert.AreEqual(2, s.HardControl.Notices.Count);

        CollectionAssert.AreEquivalent(
            new[] { "A", "C" },
            s.HardControl.Notices
                .Select(n => n.RecipientNodeId)
                .ToArray());

        foreach (var notice in s.HardControl.Notices)
        {
            Assert.AreEqual("B", notice.SenderNodeId);
            Assert.AreEqual("D1", notice.ProductId);
            Assert.AreEqual(4, notice.Day);
            Assert.AreEqual(-1, notice.Shipment.Package.Thumb);
            Assert.AreEqual(
                "D1",
                notice.Shipment.Package.FeedbackProductId);
        }

        Assert.AreEqual(2, s.Clock.PendingCount);
    }

    [Test]
    public void HardControl_ParallelArcsDoNotDuplicateNotice()
    {
        var s = Create(parallelBC: true);

        s.World.AdvanceDays(4);

        Assert.AreEqual(2, s.HardControl.Notices.Count);

        Assert.AreEqual(1,
            s.HardControl.Notices.Count(n =>
                n.RecipientNodeId == "C"));

        // Sama hyllytys ei saa lähettää
        // samaa kielteistä viestiä uudelleen.
        Assert.Throws<InvalidOperationException>(() =>
            s.HardControl.Shelve(
                "B", s.GetSeedWave("D1")[0].Copy, 4));

        Assert.AreEqual(2, s.HardControl.Notices.Count);
    }

    [Test]
    public void HardControl_InTransitOpinionIsNotKnown()
    {
        var s = Create();

        s.World.AdvanceDays(5);

        Assert.AreEqual(1, s.Control.Shelves.Count);

        var view = s.Observation.Capture();

        Assert.IsNull(view.GetKnownThumb("A", "B", "D1"));
        Assert.IsNull(view.GetKnownThumb("C", "B", "D1"));

        Assert.AreEqual(2, s.Clock.PendingCount);
    }

    [Test]
    public void HardControl_OpenedNoticesCreateDirectionalKnowledge()
    {
        var s = Create();

        s.World.AdvanceDays(6);

        var view = s.Observation.Capture();

        Assert.AreEqual(-1,
            view.GetKnownThumb("A", "B", "D1"));

        Assert.AreEqual(-1,
            view.GetKnownThumb("C", "B", "D1"));

        Assert.IsNull(
            view.GetKnownThumb("B", "C", "D1"));

        Assert.AreEqual(2, s.HardControl.Notices.Count);
        Assert.AreEqual(0, s.Clock.PendingCount);
    }

    [Test]
    public void HardControl_FeedbackDoesNotDuplicateCassette()
    {
        var s = Create();

        s.World.AdvanceDays(6);

        Assert.AreEqual(1, s.CopyIndex.Count);

        Assert.AreEqual(
            "B",
            s.GetSeedWave("D1")[0].Copy.HolderNodeId);

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.AreEqual(
            0, s.GetPolicy("B").Dispatched.Count);

        Assert.AreEqual(0, s.Control.Transfers.Count);
        Assert.AreEqual(1, s.Control.Shelves.Count);
    }

    [Test]
    public void HardControl_FeedbackOnlyDoesNotTriggerListening()
    {
        var s = Create();

        s.World.AdvanceDays(6);

        Assert.IsNull(
            s.GetListeningStatus("C", "D1"));

        Assert.AreEqual(
            0, s.GetReactionPolicy("C").Decisions.Count);

        // C on avannut kielteisen viestin,
        // mutta ei ole vastaanottanut kasettia.
        Assert.AreEqual(-1,
            s.Stances.GetLastThumb("C", "B", "D1"));

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("C", "D1"));
    }

    [Test]
    public void HardControl_NoOutgoingContactsStillShelves()
    {
        var s = Create(bHasOutgoing: false);

        s.World.AdvanceDays(8);

        Assert.AreEqual(1, s.Control.Shelves.Count);
        Assert.AreEqual(0, s.HardControl.Notices.Count);

        Assert.AreEqual(
            PhysicalControlDisposition.Shelved,
            s.Control.GetDisposition(
                "B", s.GetSeedWave("D1")[0].Copy.Id));

        Assert.AreEqual(0, s.Clock.PendingCount);
    }

    [Test]
    public void HardControl_NegativeOpinionIsProductSpecific()
    {
        var s = Create(twoDemos: true);

        s.World.AdvanceDays(6);

        var view = s.Observation.Capture();

        Assert.AreEqual(-1,
            view.GetKnownThumb("C", "B", "D1"));

        Assert.IsNull(
            view.GetKnownThumb("C", "B", "D2"));

        Assert.AreEqual(2,
            s.HardControl.Notices.Count);

        Assert.IsTrue(
            s.HardControl.Notices.All(
                n => n.ProductId == "D1"));
    }

    [Test]
    public void HardControl_SoftTransferDoesNotMassBroadcast()
    {
        var s = Create(
            bReaction: ListeningReaction.OpposeAndTransfer);

        s.World.AdvanceDays(6);

        Assert.AreEqual(1, s.Control.Transfers.Count);
        Assert.AreEqual(0, s.Control.Shelves.Count);
        Assert.AreEqual(0, s.HardControl.Notices.Count);

        Assert.AreEqual(
            "C",
            s.GetSeedWave("D1")[0].Copy.HolderNodeId);

        // Pehmeä kontrolli ilmoittaa
        // kielteisen kannan vain kasetin
        // vastaanottajalle.
        Assert.AreEqual(-1,
            s.Stances.GetLastThumb("C", "B", "D1"));

        Assert.IsNull(
            s.Stances.GetLastThumb("A", "B", "D1"));
    }
}
