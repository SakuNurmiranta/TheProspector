
using System;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible9210AutonomyDiagnosticsTests
{
    private static DemoSession Create(int seed = 921)
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = seed
            });
    }

    [Test]
    public void DayZero_ReportMatchesInitializedWorld()
    {
        var session = Create();
        var report =
            AutonomousCirculationDiagnostics.Capture(session);

        Assert.AreEqual(0, report.Day);
        Assert.AreEqual(100, report.NodeCount);
        Assert.AreEqual(0, report.NewPersonalConnections);
        Assert.AreEqual(0, report.CrossCirclePersonalConnections);
        Assert.AreEqual(0, report.FirstLettersSent);

        Assert.AreEqual(1, report.ZineReaders);
        Assert.AreEqual(25, report.ZinePrintedCopies);
        Assert.AreEqual(25, report.ZineCopiesHeld);
        Assert.AreEqual(0, report.ZineCopiesInTransit);

        Assert.IsTrue(report.ZineCopiesConserved);
    }

    [Test]
    public void DayOne_ReportsInitialPhysicalBulkDistribution()
    {
        var session = Create();
        session.AdvanceDay();

        var report =
            AutonomousCirculationDiagnostics.Capture(session);

        Assert.AreEqual(1, report.Day);
        Assert.AreEqual(5, report.LayerThreeConnections);
        Assert.AreEqual(6, report.ZineReaders);
        Assert.AreEqual(1, report.ZineReadersCountryA);
        Assert.AreEqual(5, report.ZineReadersCountryB);

        Assert.AreEqual(0, report.FirstLettersSent);
        Assert.IsTrue(report.ZineCopiesConserved);
    }

    [Test]
    public void NewForeignPersonalArc_IsDetectedWithoutInventingTraffic()
    {
        var session = Create();

        var before =
            AutonomousCirculationDiagnostics.Capture(session);

        session.Scenario.Network.AddConnection(
            "DIAGNOSTIC-FOREIGN-ARC",
            "A001",
            "B001",
            CommunicationLayer.Interpersonal,
            "personal");

        var after =
            AutonomousCirculationDiagnostics.Capture(session);

        Assert.AreEqual(
            before.NewPersonalConnections + 1,
            after.NewPersonalConnections);

        Assert.AreEqual(
            before.CrossCirclePersonalConnections + 1,
            after.CrossCirclePersonalConnections);

        Assert.AreEqual(
            before.CrossCircleCopyDispatches,
            after.CrossCircleCopyDispatches);

        Assert.AreEqual(
            before.FirstLettersSent,
            after.FirstLettersSent);

        Assert.AreEqual(
            before.OpenedMessages,
            after.OpenedMessages);
    }

    [Test]
    public void SameSeed_ReproducesThirtyDayDiagnostics()
    {
        var first = Create(24680);
        var second = Create(24680);

        first.AdvanceDays(30);
        second.AdvanceDays(30);

        var a =
            AutonomousCirculationDiagnostics.Capture(first);

        var b =
            AutonomousCirculationDiagnostics.Capture(second);

        Assert.AreEqual(a.ToString(), b.ToString());
        Assert.IsTrue(a.ZineCopiesConserved);
        Assert.IsTrue(b.ZineCopiesConserved);
    }

    [Test]
    public void Autonomous180Days_ReportsActualNetworkDevelopment()
    {
        var session = Create();

        var checkpoints = new[]
        {
            0, 1, 15, 30, 60, 90, 120, 180
        };

        foreach (int checkpoint in checkpoints)
        {
            session.AdvanceDays(checkpoint - session.Day);

            var report =
                AutonomousCirculationDiagnostics.Capture(session);

            TestContext.WriteLine(report.ToString());

            Assert.AreEqual(checkpoint, report.Day);
            Assert.AreEqual(100, report.NodeCount);
            Assert.IsTrue(
                report.ZineCopiesConserved,
                "Physical zine stock is not conserved " +
                "at day " + checkpoint);

            Assert.GreaterOrEqual(
                report.ZineReaders, 1);
        }

        Assert.AreEqual(180, session.Day);
    }

    [Test]
    public void LegacyDemo_DoesNotRequireAutonomyDiagnostics()
    {
        var session = DemoSession.CreateDefault();

        Assert.Throws<InvalidOperationException>(() =>
            AutonomousCirculationDiagnostics.Capture(session));
    }
}
