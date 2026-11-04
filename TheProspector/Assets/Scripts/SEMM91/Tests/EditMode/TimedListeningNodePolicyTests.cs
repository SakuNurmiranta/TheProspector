
using NUnit.Framework;
using SEMM91.Domain;

public class TimedListeningNodePolicyTests
{
    private static AutonomousCirculationScenario Create(
        bool bKnowsC = true,
        bool cKnowsD = true,
        bool cCopies = true)
    {
        return new AutonomousCirculationScenario(
            bKnowsCIsMissing: bKnowsC,
            cKnowsDIsMissing: cKnowsD,
            cCopies: cCopies,
            listeningDays: 2);
    }

    [Test]
    public void Listening_IsOptInAndOldBehaviorRemains()
    {
        var s = new AutonomousCirculationScenario();

        s.World.AdvanceDays(2);

        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);
        Assert.IsNull(s.GetListeningStatus("B", "D1"));
    }

    [Test]
    public void Listening_StartsWhenCassetteMessageIsOpened()
    {
        var s = Create();

        s.World.AdvanceDays(2);

        var status = s.GetListeningStatus("B", "D1");

        Assert.IsNotNull(status);
        Assert.AreEqual(ListeningPhase.Listening, status.Phase);
        Assert.AreEqual(2, status.StartedDay);
        Assert.AreEqual(0f, status.Progress, 0.001f);

        Assert.AreEqual(0, s.GetPolicy("B").CopyDecisions);
        Assert.AreEqual(0, s.GetPolicy("B").Dispatched.Count);
        Assert.AreEqual(1, s.World.OpenedMessages.Count);
    }

    [Test]
    public void Listening_ProgressesHalfwayOnNextDay()
    {
        var s = Create();

        s.World.AdvanceDays(3);

        var status = s.GetListeningStatus("B", "D1");

        Assert.AreEqual(ListeningPhase.Listening, status.Phase);
        Assert.AreEqual(0.5f, status.Progress, 0.001f);
        Assert.AreEqual(0, s.GetPolicy("B").Dispatched.Count);
    }

    [Test]
    public void Listening_CompletionTriggersCopying()
    {
        var s = Create();

        s.World.AdvanceDays(4);

        var status = s.GetListeningStatus("B", "D1");

        Assert.AreEqual(ListeningPhase.Completed, status.Phase);
        Assert.AreEqual(1f, status.Progress, 0.001f);
        Assert.AreEqual(4, status.CompletedDay);

        Assert.AreEqual(1, s.GetPolicy("B").CopyDecisions);
        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            s.GetPolicy("B").Dispatched[0].Shipment.Status);
    }

    [Test]
    public void Listening_CStartsAfterReceivingBCopy()
    {
        var s = Create();

        s.World.AdvanceDays(6);

        Assert.AreEqual(2, s.World.OpenedMessages.Count);

        var status = s.GetListeningStatus("C", "D1");

        Assert.AreEqual(ListeningPhase.Listening, status.Phase);
        Assert.AreEqual(6, status.StartedDay);
        Assert.AreEqual(0f, status.Progress, 0.001f);

        Assert.AreEqual(14, s.Capital.GetCapital("B"));
        Assert.AreEqual(0, s.GetPolicy("C").Dispatched.Count);
    }

    [Test]
    public void Listening_CCopiesAfterOwnListeningCompletes()
    {
        var s = Create();

        s.World.AdvanceDays(8);

        var status = s.GetListeningStatus("C", "D1");

        Assert.AreEqual(ListeningPhase.Completed, status.Phase);
        Assert.AreEqual(8, status.CompletedDay);

        Assert.AreEqual(1, s.GetPolicy("C").CopyDecisions);
        Assert.AreEqual(1, s.GetPolicy("C").Dispatched.Count);

        Assert.AreEqual(
            "D",
            s.GetPolicy("C").Dispatched[0].RecipientNodeId);
    }

    [Test]
    public void Listening_DReceivesAndCompletesWithoutFurtherCopying()
    {
        var s = Create();

        s.World.AdvanceDays(12);

        Assert.IsTrue(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(
            ListeningPhase.Completed,
            s.GetListeningStatus("D", "D1").Phase);

        Assert.AreEqual(12,
            s.GetListeningStatus("D", "D1").CompletedDay);

        Assert.AreEqual(0, s.GetPolicy("D").Dispatched.Count);

        Assert.AreEqual(14, s.Capital.GetCapital("A"));
        Assert.AreEqual(14, s.Capital.GetCapital("B"));
        Assert.AreEqual(14, s.Capital.GetCapital("C"));
        Assert.AreEqual(10, s.Capital.GetCapital("D"));
    }

    [Test]
    public void Listening_UnknownCollectionStillPreventsForwarding()
    {
        var s = Create(bKnowsC: false);

        s.World.AdvanceDays(10);

        Assert.AreEqual(
            ListeningPhase.Completed,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.AreEqual(1, s.GetPolicy("B").CopyDecisions);
        Assert.AreEqual(0, s.GetPolicy("B").Dispatched.Count);

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("C", "D1"));
    }

    [Test]
    public void Listening_DecliningCopyDoesNotStopListening()
    {
        var s = Create(cCopies: false);

        s.World.AdvanceDays(10);

        Assert.AreEqual(
            ListeningPhase.Completed,
            s.GetListeningStatus("C", "D1").Phase);

        Assert.AreEqual(1, s.GetPolicy("C").CopyDecisions);
        Assert.AreEqual(0, s.GetPolicy("C").Dispatched.Count);

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("D", "D1"));
    }

    [Test]
    public void Listening_DeterministicAndRejectsNegativeDuration()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            new AutonomousCirculationScenario(
                listeningDays: -1));

        var first = Create();
        var second = Create();

        first.World.AdvanceDays(20);

        for (int day = 0; day < 20; day++)
            second.World.AdvanceDay();

        Assert.AreEqual(20, first.World.CurrentDay);
        Assert.AreEqual(20, second.World.CurrentDay);

        Assert.AreEqual(3, first.World.OpenedMessages.Count);
        Assert.AreEqual(3, second.World.OpenedMessages.Count);

        foreach (string id in new[] { "A", "B", "C", "D" })
        {
            Assert.AreEqual(
                first.Capital.GetCapital(id),
                second.Capital.GetCapital(id));
        }

        Assert.AreEqual(3, first.CopyIndex.Count);
        Assert.AreEqual(3, second.CopyIndex.Count);
        Assert.AreEqual(0, first.Clock.PendingCount);
        Assert.AreEqual(0, second.Clock.PendingCount);
    }
}
