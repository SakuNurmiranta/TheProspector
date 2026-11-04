
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class AutonomousReactionIntegrationTests
{
    private static AutonomousCirculationScenario Create(
        ListeningReaction b,
        ListeningReaction? c = null)
    {
        return new AutonomousCirculationScenario(
            listeningDays: 2,
            bReaction: b,
            cReaction: c);
    }

    [Test]
    public void Reactions_LegacyListeningBehaviorIsPreserved()
    {
        var s = new AutonomousCirculationScenario(
            listeningDays: 2);

        s.World.AdvanceDays(12);

        Assert.IsNull(s.GetReactionPolicy("B"));
        Assert.IsNull(s.GetReactionPolicy("C"));

        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);
        Assert.AreEqual(1, s.GetPolicy("C").Dispatched.Count);

        Assert.AreEqual(0, s.Control.Transfers.Count);
        Assert.AreEqual(0, s.Control.Shelves.Count);
    }

    [Test]
    public void Reactions_SupportAtBothNodesCopiesToD()
    {
        var s = Create(
            ListeningReaction.SupportAndCopy,
            ListeningReaction.SupportAndCopy);

        s.World.AdvanceDays(12);

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            s.GetReactionPolicy("B").Decisions[0].Reaction);

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            s.GetReactionPolicy("C").Decisions[0].Reaction);

        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);
        Assert.AreEqual(1, s.GetPolicy("C").Dispatched.Count);

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(14, s.Capital.GetCapital("A"));
        Assert.AreEqual(14, s.Capital.GetCapital("B"));
        Assert.AreEqual(14, s.Capital.GetCapital("C"));

        Assert.AreEqual(0, s.Control.Transfers.Count);
        Assert.AreEqual(0, s.Control.Shelves.Count);
    }

    [Test]
    public void Reactions_SoftControlStartsOnlyAfterListening()
    {
        var s = Create(
            ListeningReaction.OpposeAndTransfer,
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(3);

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.AreEqual(0, s.Control.Transfers.Count);

        s.World.AdvanceDay();

        Assert.AreEqual(4, s.World.CurrentDay);

        Assert.AreEqual(
            ListeningPhase.Completed,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.AreEqual(1, s.Control.Transfers.Count);
        Assert.AreEqual(0, s.GetPolicy("B").Dispatched.Count);

        Assert.AreSame(
            s.SeedWave[0].Copy,
            s.Control.Transfers[0].Copy);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            s.Control.Transfers[0].Shipment.Status);

        Assert.AreEqual(2, s.CopyIndex.Count);
        Assert.AreEqual(10, s.Capital.GetCapital("B"));
    }

    [Test]
    public void Reactions_TransferredDislikeBecomesKnownOnOpening()
    {
        var s = Create(
            ListeningReaction.OpposeAndTransfer,
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(4);

        Assert.IsNull(
            s.Stances.GetLastThumb("C", "B", "D1"));

        s.World.AdvanceDays(2);

        Assert.AreEqual(6, s.World.CurrentDay);

        Assert.AreEqual(-1,
            s.Stances.GetLastThumb("C", "B", "D1"));

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("C", "D1").Phase);

        Assert.AreEqual("C", s.SeedWave[0].Copy.HolderNodeId);
        Assert.AreEqual(10, s.Capital.GetCapital("B"));
    }

    [Test]
    public void Reactions_TransferThenShelveStopsCirculation()
    {
        var s = Create(
            ListeningReaction.OpposeAndTransfer,
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(12);

        Assert.AreEqual(1, s.Control.Transfers.Count);
        Assert.AreEqual(1, s.Control.Shelves.Count);

        Assert.AreEqual("C", s.Control.Shelves[0].NodeId);

        Assert.AreEqual(
            PhysicalControlDisposition.Shelved,
            s.Control.GetDisposition(
                "C", s.SeedWave[0].Copy.Id));

        Assert.AreEqual("C", s.SeedWave[0].Copy.HolderNodeId);

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(2, s.CopyIndex.Count);

        Assert.AreEqual(14, s.Capital.GetCapital("A"));
        Assert.AreEqual(10, s.Capital.GetCapital("B"));
        Assert.AreEqual(10, s.Capital.GetCapital("C"));

        Assert.AreEqual(0, s.Clock.PendingCount);
    }

    [Test]
    public void Reactions_HardControlAtBStopsFirstBranch()
    {
        var s = Create(
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(20);

        Assert.AreEqual(1, s.Control.Shelves.Count);
        Assert.AreEqual(0, s.Control.Transfers.Count);

        Assert.AreEqual("B", s.SeedWave[0].Copy.HolderNodeId);

        Assert.AreEqual(
            PhysicalControlDisposition.Shelved,
            s.Control.GetDisposition(
                "B", s.SeedWave[0].Copy.Id));

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.AreEqual(1, s.World.OpenedMessages.Count);

        Assert.AreEqual(0, s.GetPolicy("B").Dispatched.Count);
        Assert.AreEqual(1, s.CopyIndex.Count);
    }

    [Test]
    public void Reactions_CopyThenHardControlStopsAtC()
    {
        var s = Create(
            ListeningReaction.SupportAndCopy,
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(10);

        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);
        Assert.AreEqual(0, s.GetPolicy("C").Dispatched.Count);

        Assert.AreEqual(1, s.Control.Shelves.Count);
        Assert.AreEqual(0, s.Control.Transfers.Count);

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(14, s.Capital.GetCapital("B"));
        Assert.AreEqual(10, s.Capital.GetCapital("C"));
    }

    [Test]
    public void Reactions_CopyThenTransferMovesExistingCopyToD()
    {
        var s = Create(
            ListeningReaction.SupportAndCopy,
            ListeningReaction.OpposeAndTransfer);

        s.World.AdvanceDays(10);

        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);
        Assert.AreEqual(0, s.GetPolicy("C").Dispatched.Count);

        Assert.AreEqual(1, s.Control.Transfers.Count);

        var copyFromB = s.GetPolicy("B").Dispatched[0].Copy;
        var transferredCopy = s.Control.Transfers[0].Copy;

        Assert.AreSame(copyFromB, transferredCopy);
        Assert.AreEqual("D", transferredCopy.HolderNodeId);

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(3, s.CopyIndex.Count);

        Assert.AreEqual(14, s.Capital.GetCapital("A"));
        Assert.AreEqual(14, s.Capital.GetCapital("B"));
        Assert.AreEqual(10, s.Capital.GetCapital("C"));
        Assert.AreEqual(10, s.Capital.GetCapital("D"));
    }

    [Test]
    public void Reactions_BatchedAndSteppedRunsMatch()
    {
        var first = Create(
            ListeningReaction.OpposeAndTransfer,
            ListeningReaction.OpposeAndTransfer);

        var second = Create(
            ListeningReaction.OpposeAndTransfer,
            ListeningReaction.OpposeAndTransfer);

        first.World.AdvanceDays(15);

        for (int i = 0; i < 15; i++)
            second.World.AdvanceDay();

        Assert.AreEqual(15, first.World.CurrentDay);
        Assert.AreEqual(15, second.World.CurrentDay);

        Assert.AreEqual(2, first.Control.Transfers.Count);
        Assert.AreEqual(2, second.Control.Transfers.Count);

        Assert.AreEqual(0, first.Control.Shelves.Count);
        Assert.AreEqual(0, second.Control.Shelves.Count);

        Assert.AreEqual(
            first.SeedWave[0].Copy.HolderNodeId,
            second.SeedWave[0].Copy.HolderNodeId);

        Assert.AreEqual("D", first.SeedWave[0].Copy.HolderNodeId);

        Assert.AreEqual(
            first.World.OpenedMessages.Count,
            second.World.OpenedMessages.Count);

        foreach (string id in new[] { "A", "B", "C", "D" })
        {
            Assert.AreEqual(
                first.Capital.GetCapital(id),
                second.Capital.GetCapital(id));
        }

        Assert.AreEqual(0, first.Clock.PendingCount);
        Assert.AreEqual(0, second.Clock.PendingCount);
    }

    [Test]
    public void Reactions_RequireListeningAndDefinedReaction()
    {
        Assert.Throws<ArgumentException>(() =>
            new AutonomousCirculationScenario(
                listeningDays: 0,
                bReaction: ListeningReaction.OpposeAndTransfer));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AutonomousCirculationScenario(
                listeningDays: 2,
                bReaction: (ListeningReaction)999));
    }
}
