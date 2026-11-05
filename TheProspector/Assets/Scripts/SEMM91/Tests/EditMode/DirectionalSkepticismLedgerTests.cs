
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class DirectionalSkepticismLedgerTests
{
    [Test]
    public void Skepticism_NewRelationStartsAtZero()
    {
        var ledger = new DirectionalSkepticismLedger();

        var state = ledger.Get("B", "A");

        Assert.AreEqual(0, state.BadReferrals);
        Assert.AreEqual(0, state.UnansweredReplies);
        Assert.AreEqual(0, state.Total);
    }

    [Test]
    public void Skepticism_PositiveReferralFollowedByShelvingIncreases()
    {
        var ledger = new DirectionalSkepticismLedger();

        bool changed = ledger.AssessReferral(
            "B", "A", "D1",
            +1,
            ListeningReaction.OpposeAndShelve);

        Assert.IsTrue(changed);
        Assert.AreEqual(1, ledger.Get("B", "A").BadReferrals);
        Assert.AreEqual(1, ledger.Get("B", "A").Total);
    }

    [Test]
    public void Skepticism_NegativeOrNeutralReferralDoesNotPenalize()
    {
        var ledger = new DirectionalSkepticismLedger();

        Assert.IsFalse(ledger.AssessReferral(
            "B", "A", "D1",
            -1,
            ListeningReaction.OpposeAndShelve));

        Assert.IsFalse(ledger.AssessReferral(
            "B", "A", "D2",
            0,
            ListeningReaction.OpposeAndShelve));

        Assert.AreEqual(0, ledger.Get("B", "A").Total);
    }

    [Test]
    public void Skepticism_SupportAndSoftControlDoNotPenalizeYet()
    {
        var ledger = new DirectionalSkepticismLedger();

        Assert.IsFalse(ledger.AssessReferral(
            "B", "A", "D1",
            +1,
            ListeningReaction.SupportAndCopy));

        Assert.IsFalse(ledger.AssessReferral(
            "B", "A", "D2",
            +1,
            ListeningReaction.OpposeAndTransfer));

        Assert.AreEqual(0, ledger.Get("B", "A").Total);
    }

    [Test]
    public void Skepticism_RelationsAreDirectional()
    {
        var ledger = new DirectionalSkepticismLedger();

        ledger.AssessReferral(
            "B", "A", "D1",
            +1,
            ListeningReaction.OpposeAndShelve);

        Assert.AreEqual(1, ledger.Get("B", "A").Total);
        Assert.AreEqual(0, ledger.Get("A", "B").Total);
        Assert.AreEqual(0, ledger.Get("C", "A").Total);
    }

    [Test]
    public void Skepticism_ReferralPenaltiesAreProductSpecificAndIdempotent()
    {
        var ledger = new DirectionalSkepticismLedger();

        Assert.IsTrue(ledger.AssessReferral(
            "B", "A", "D1",
            +1,
            ListeningReaction.OpposeAndShelve));

        Assert.IsFalse(ledger.AssessReferral(
            "B", "A", "D1",
            +1,
            ListeningReaction.OpposeAndShelve));

        Assert.IsTrue(ledger.AssessReferral(
            "B", "A", "D2",
            +1,
            ListeningReaction.OpposeAndShelve));

        Assert.AreEqual(2, ledger.Get("B", "A").BadReferrals);
        Assert.AreEqual(2, ledger.Get("B", "A").Total);
    }

    [Test]
    public void Skepticism_UnansweredReplyIsTemporaryAndDirectional()
    {
        var ledger = new DirectionalSkepticismLedger();

        Assert.IsTrue(
            ledger.RecordUnanswered("A", "B", "MSG1"));

        Assert.IsFalse(
            ledger.RecordUnanswered("A", "B", "MSG1"));

        Assert.AreEqual(1,
            ledger.Get("A", "B").UnansweredReplies);

        Assert.AreEqual(0,
            ledger.Get("B", "A").UnansweredReplies);
    }

    [Test]
    public void Skepticism_CompensationPreservesBadReferralHistory()
    {
        var ledger = new DirectionalSkepticismLedger();

        ledger.AssessReferral(
            "A", "B", "D1",
            +1,
            ListeningReaction.OpposeAndShelve);

        ledger.RecordUnanswered("A", "B", "MSG1");

        Assert.AreEqual(2, ledger.Get("A", "B").Total);

        Assert.IsTrue(
            ledger.CompensateUnanswered("A", "B", "MSG1"));

        var state = ledger.Get("A", "B");

        Assert.AreEqual(1, state.BadReferrals);
        Assert.AreEqual(0, state.UnansweredReplies);
        Assert.AreEqual(1, state.Total);
    }

    [Test]
    public void Skepticism_CompensationTargetsSpecificMessageOnce()
    {
        var ledger = new DirectionalSkepticismLedger();

        ledger.RecordUnanswered("A", "B", "MSG1");
        ledger.RecordUnanswered("A", "B", "MSG2");

        Assert.AreEqual(2, ledger.Get("A", "B").Total);

        Assert.IsTrue(
            ledger.CompensateUnanswered("A", "B", "MSG1"));

        Assert.IsFalse(
            ledger.CompensateUnanswered("A", "B", "MSG1"));

        Assert.IsFalse(
            ledger.CompensateUnanswered("A", "B", "UNKNOWN"));

        // Sama tapahtuma ei voi avautua uudelleen
        // kompensaation jälkeen.
        Assert.IsFalse(
            ledger.RecordUnanswered("A", "B", "MSG1"));

        Assert.AreEqual(1, ledger.Get("A", "B").Total);

        Assert.IsTrue(
            ledger.CompensateUnanswered("A", "B", "MSG2"));

        Assert.AreEqual(0, ledger.Get("A", "B").Total);
    }

    [Test]
    public void Skepticism_ValidatesInputsAndSnapshotsAreStable()
    {
        var ledger = new DirectionalSkepticismLedger();

        Assert.Throws<ArgumentException>(() =>
            ledger.RecordUnanswered("A", "A", "MSG1"));

        Assert.Throws<ArgumentException>(() =>
            ledger.RecordUnanswered("A", "B", ""));

        Assert.Throws<ArgumentException>(() =>
            ledger.Get("", "B"));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ledger.AssessReferral(
                "B", "A", "D1",
                2,
                ListeningReaction.OpposeAndShelve));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ledger.AssessReferral(
                "B", "A", "D1",
                +1,
                (ListeningReaction)999));

        var before = ledger.Get("B", "A");

        ledger.RecordUnanswered("B", "A", "MSG2");

        var after = ledger.Get("B", "A");

        Assert.AreEqual(0, before.Total);
        Assert.AreEqual(1, after.Total);
    }
}
