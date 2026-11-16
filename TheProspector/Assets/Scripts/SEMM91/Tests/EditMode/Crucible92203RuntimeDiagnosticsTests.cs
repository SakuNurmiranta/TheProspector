
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible92203RuntimeDiagnosticsTests
{
    private static DemoSession Create(bool enabled)
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 0,
            enableInitialZineForwarding: true,
            enableRecurringZineIssues: enabled,
            enableMultiIssueZinePostalInserts: enabled,
            enableAutonomousMultiIssueContacts: enabled);
    }

    [Test]
    public void LiveConfiguration_EnablesAllZineSubsystems()
    {
        var session = Create(true);

        Assert.IsNotNull(session.RecurringZines);
        Assert.IsNotNull(
            session.MultiIssueZinePostalInserts);

        Assert.IsNotNull(
            session.FirstContactCycle.MultiIssueSelection);
    }

    [Test]
    public void InitialSnapshot_SeparatesIssueOneFromLaterIssues()
    {
        var session = Create(true);

        var report = ZineRuntimeSnapshot.Capture(session);

        Assert.AreEqual(0, report.Day);
        Assert.AreEqual(1, report.PublishedIssueCount);
        Assert.AreEqual(0, report.LaterIssueCount);
        Assert.AreEqual(0, report.LaterIssueReaders);
        Assert.AreEqual(0, report.LaterIssueContactsSent);
    }

    [Test]
    public void DayNinety_ReportsPublishedSecondIssue()
    {
        var session = Create(true);

        session.Scenario.Clock.AdvanceDays(90);

        var report = ZineRuntimeSnapshot.Capture(session);

        Assert.AreEqual(90, report.Day);
        Assert.AreEqual(2, report.PublishedIssueCount);
        Assert.AreEqual(1, report.LaterIssueCount);

        // The editor knows their own publication.
        Assert.GreaterOrEqual(
            report.LaterIssueReaders, 1);

        Assert.AreEqual(0, report.LaterIssueContactsSent);
    }

    [Test]
    public void DisabledRecurringIssues_AreReportedAsDisabled()
    {
        var session = Create(false);

        session.Scenario.Clock.AdvanceDays(180);

        var report = ZineRuntimeSnapshot.Capture(session);

        Assert.IsFalse(report.RecurringEnabled);
        Assert.AreEqual(1, report.PublishedIssueCount);
        Assert.AreEqual(0, report.LaterIssueCount);
    }
}
