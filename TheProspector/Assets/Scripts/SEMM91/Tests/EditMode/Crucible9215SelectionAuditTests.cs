using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible9215SelectionAuditTests
{
    private static DemoSession Create(int seed = 921, int interval = 14)
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters { RandomSeed = seed },
            fieldDemoIntervalDays: interval);
    }

    [Test]
    public void VerdictComparison_DistinguishesSilenceFromDisagreement()
    {
        Assert.IsTrue(CirculationSelectionReport.AreVerdictsDifferent(
            ListeningReaction.SupportAndCopy,
            ZineFirstContactOutcome.NegativeReply));
        Assert.IsTrue(CirculationSelectionReport.AreVerdictsDifferent(
            ListeningReaction.OpposeAndShelve,
            ZineFirstContactOutcome.PositiveReply));
        Assert.IsTrue(CirculationSelectionReport.AreVerdictsDifferent(
            ListeningReaction.OpposeAndTransfer,
            ZineFirstContactOutcome.PositiveReply));
        Assert.IsFalse(CirculationSelectionReport.AreVerdictsDifferent(
            ListeningReaction.SupportAndCopy,
            ZineFirstContactOutcome.PositiveReply));
        Assert.IsFalse(CirculationSelectionReport.AreVerdictsDifferent(
            ListeningReaction.OpposeAndShelve,
            ZineFirstContactOutcome.NegativeReply));
        Assert.IsFalse(CirculationSelectionReport.AreVerdictsDifferent(
            ListeningReaction.SupportAndCopy,
            ZineFirstContactOutcome.Silence));
    }

    [Test]
    public void DayZero_NoListeningOrZineDecisionsAreInvented()
    {
        var session = Create();
        var report = CirculationSelectionReport.Capture(session);

        Assert.AreEqual(0, report.Day);
        Assert.AreEqual(session.ProductCount, report.Demos.Count);
        Assert.AreEqual(0, report.PhysicalDecisions.Count);
        Assert.AreEqual(0, report.FirstContactAssessments.Count);
        Assert.AreEqual(0, report.Divergences.Count);
        Assert.AreEqual(0, session.Day);
    }

    [Test]
    public void FirstContactOutcomes_AreRecordedWithoutReclassifyingSilence()
    {
        var session = Create(interval: 0);
        session.AdvanceDays(120);
        var report = CirculationSelectionReport.Capture(session);

        Assert.AreEqual(session.FirstContactCycle.PositiveReplyCount,
            report.PositiveReplyCount);
        Assert.AreEqual(session.FirstContactCycle.NegativeReplyCount,
            report.NegativeReplyCount);
        Assert.AreEqual(session.FirstContactCycle.SilentDecisionCount,
            report.SilentAssessmentCount);

        Assert.IsTrue(report.FirstContactAssessments.All(a =>
            a.Day <= session.Day &&
            a.MatchingAxes >= 0 && a.MatchingAxes <= 6 &&
            !string.IsNullOrWhiteSpace(a.RequestPackageId)));

        Assert.IsTrue(report.FirstContactAssessments.All(a =>
            a.Outcome == ZineFirstContactOutcome.Silence ||
            (a.Outcome == ZineFirstContactOutcome.PositiveReply
                ? a.MatchingAxes >= session.FirstContactCycle.MinimumPositiveMatchingAxes
                : a.MatchingAxes < session.FirstContactCycle.MinimumPositiveMatchingAxes)));
    }

    [Test]
    public void ExplicitShelving_IsVisibleSeparatelyFromNoDelivery()
    {
        var session = Create(interval: 0);
        var seed = session.InitialWorld.Definition.Seeds.First(s =>
            session.Scenario.GetSeedWave(s.DemoId).Count > 0);
        var target = session.Scenario.GetSeedWave(seed.DemoId)[0].RecipientNodeId;

        session.Scenario.ReactionRules.Set(
            target, seed.DemoId, ListeningReaction.OpposeAndShelve);
        session.AdvanceDays(10);

        var report = CirculationSelectionReport.Capture(session);
        var row = report.Demos.Single(d => d.ProductId == seed.DemoId);

        Assert.IsTrue(report.PhysicalDecisions.Any(d =>
            d.NodeId == target && d.ProductId == seed.DemoId &&
            d.Reaction == ListeningReaction.OpposeAndShelve));
        Assert.GreaterOrEqual(row.ShelveCount, 1);
        Assert.GreaterOrEqual(session.Scenario.Control.Shelves.Count, 1);
    }

    [Test]
    public void Divergences_OnlyComeFromSameNodeAndProductWithBothRecords()
    {
        var session = Create();
        session.AdvanceDays(180);
        var report = CirculationSelectionReport.Capture(session);

        foreach (var difference in report.Divergences)
        {
            Assert.IsTrue(report.PhysicalDecisions.Any(d =>
                d.NodeId == difference.NodeId &&
                d.ProductId == difference.ProductId &&
                d.Reaction == difference.PhysicalReaction &&
                d.Day == difference.PhysicalDay));

            Assert.IsTrue(report.FirstContactAssessments.Any(a =>
                a.NodeId == difference.NodeId &&
                a.ProductId == difference.ProductId &&
                a.Outcome == difference.ZineOutcome &&
                a.Day == difference.ReplyDay &&
                a.RequestPackageId == difference.RequestPackageId));

            Assert.IsTrue(CirculationSelectionReport.AreVerdictsDifferent(
                difference.PhysicalReaction, difference.ZineOutcome));
        }

        int expectedDifferences = report.FirstContactAssessments.Count(a =>
            report.PhysicalDecisions.Any(d =>
                d.NodeId == a.NodeId && d.ProductId == a.ProductId &&
                CirculationSelectionReport.AreVerdictsDifferent(
                    d.Reaction, a.Outcome)));

        Assert.AreEqual(expectedDifferences, report.Divergences.Count);
        Assert.AreEqual(report.Divergences.Count,
            report.Demos.Sum(d => d.DivergenceCount));

        TestContext.WriteLine(report.ToString());
        TestContext.WriteLine("--- Field-born demo decisions ---");
        foreach (var demo in report.Demos.Where(d =>
                     d.ProductId.StartsWith("FIELD-DEMO-", StringComparison.Ordinal)))
            TestContext.WriteLine(demo.ToString());
        TestContext.WriteLine("--- Cross-channel differences ---");
        foreach (var difference in report.Divergences)
            TestContext.WriteLine(difference.ToString());
    }

    [Test]
    public void SameSeed_ProducesStableReadOnlyAuditSnapshots()
    {
        var first = Create(seed: 24680);
        var second = Create(seed: 24680);
        first.AdvanceDays(42);
        second.AdvanceDays(42);

        var a = CirculationSelectionReport.Capture(first);
        var repeated = CirculationSelectionReport.Capture(first);
        var b = CirculationSelectionReport.Capture(second);

        Assert.AreEqual(a.ToString(), repeated.ToString());
        Assert.AreEqual(a.ToString(), b.ToString());
        CollectionAssert.AreEqual(
            a.Demos.Select(d => d.ToString()).ToArray(),
            b.Demos.Select(d => d.ToString()).ToArray());
        CollectionAssert.AreEqual(
            a.FirstContactAssessments.Select(d => d.ToString()).ToArray(),
            b.FirstContactAssessments.Select(d => d.ToString()).ToArray());

        int earlierDay = a.Day;
        first.AdvanceDay();
        Assert.AreEqual(earlierDay, a.Day);
        Assert.AreEqual(earlierDay + 1, first.Day);
    }

    [Test]
    public void LegacySession_DoesNotRequireOptionalAesthetics()
    {
        var session = DemoSession.CreateDefault(new Random(15));
        session.AdvanceDays(8);

        var report = CirculationSelectionReport.Capture(session);
        Assert.IsNull(session.AestheticProfiles);
        Assert.AreEqual(0, report.FirstContactAssessments.Count);
        Assert.AreEqual(0, report.Divergences.Count);
        Assert.AreEqual(session.ProductCount, report.Demos.Count);
    }
}