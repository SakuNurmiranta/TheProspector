
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible9214ResearchDiagnosticsTests
{
    private static DemoSession Create(
        int seed = 921,
        int interval = 14)
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = seed
            },
            fieldDemoIntervalDays: interval);
    }

    [Test]
    public void DayZero_ContainsEveryInitialDemo()
    {
        var session = Create();
        var report = CirculationResearchReport.Capture(session);

        Assert.AreEqual(0, report.Day);
        Assert.AreEqual(15, report.DemoCount);
        Assert.AreEqual(15, report.InitialDemoCount);
        Assert.AreEqual(0, report.FieldBornDemoCount);
        Assert.AreEqual(100, report.NodeCount);

        Assert.IsTrue(report.Demos.All(d => !d.IsFieldBorn));
        Assert.IsTrue(report.Demos.All(d => d.BirthDay == 0));
        Assert.IsTrue(report.Demos.All(d => d.EverHeldCount >= 1));
        Assert.IsTrue(report.Demos.All(d => d.CapitalAtBirth == 10));
    }

    [Test]
    public void DynamicDemo_RecordsOriginAtBirth()
    {
        var session = Create();
        session.AdvanceDays(14);

        var report = CirculationResearchReport.Capture(session);
        var demo = report.Demos.Single(
            d => d.ProductId == "FIELD-DEMO-0001");

        Assert.AreEqual(16, report.DemoCount);
        Assert.AreEqual(1, report.FieldBornDemoCount);
        Assert.IsTrue(demo.IsFieldBorn);
        Assert.AreEqual(14, demo.BirthDay);

        Assert.AreEqual(
            session.FieldDemoProduction.Births[0]
                .InitialHolderNodeId,
            demo.InitialHolderNodeId);

        Assert.GreaterOrEqual(demo.CapitalAtBirth, 10);
        Assert.GreaterOrEqual(demo.OutgoingArcsAtBirth, 0);
        Assert.GreaterOrEqual(demo.EverHeldCount, 1);
        Assert.GreaterOrEqual(
            demo.InitialComponentsReached, 1);

        // Birth statistics are fixed historical observations.
        session.Scenario.Capital.Credit(
            demo.InitialHolderNodeId, 20);

        var later = CirculationResearchReport.Capture(session)
            .Demos.Single(d => d.ProductId == demo.ProductId);

        Assert.AreEqual(
            demo.CapitalAtBirth,
            later.CapitalAtBirth);
    }

    [Test]
    public void ManualRuntimeBirth_IsAlsoObserved()
    {
        var session = Create(interval: 0);
        session.AdvanceDays(3);

        var registered = session.Scenario.SpawnFieldDemo(
            "MANUAL-DEMO",
            "MANUAL-ARTIST",
            new Random(214));

        var report = CirculationResearchReport.Capture(session);
        var demo = report.Demos.Single(
            d => d.ProductId == "MANUAL-DEMO");

        Assert.AreEqual(16, report.DemoCount);
        Assert.IsTrue(demo.IsFieldBorn);
        Assert.AreEqual(3, demo.BirthDay);
        Assert.AreEqual(
            registered.InitialHolderNodeId,
            demo.InitialHolderNodeId);
    }

    [Test]
    public void Journal_RecordsInitialPhysicalSeedShipmentsOnce()
    {
        var session = Create(interval: 0);
        var report = CirculationResearchReport.Capture(session);

        int expected = session.InitialWorld.Definition.Seeds
            .Sum(seed =>
                session.Scenario.GetSeedWave(seed.DemoId).Count);

        Assert.AreEqual(
            expected,
            report.Shipments.Count);

        Assert.IsTrue(report.Shipments.All(
            s => s.IsInitialSeed && s.IsCopyDistribution));

        Assert.IsTrue(report.Shipments.All(
            s => s.SentDay == 0));

        Assert.AreEqual(
            report.Shipments.Select(s => s.ShipmentId)
                .Distinct().Count(),
            report.Shipments.Count);
    }

    [Test]
    public void Journal_FollowsFuturePostalShipments()
    {
        var session = Create();
        int initial =
            CirculationResearchReport.Capture(session)
                .Shipments.Count;

        session.AdvanceDays(30);

        var report = CirculationResearchReport.Capture(session);

        Assert.GreaterOrEqual(report.Shipments.Count, initial);

        Assert.IsTrue(report.Shipments.All(
            s => !string.IsNullOrWhiteSpace(s.ProductId)));

        Assert.IsTrue(report.Shipments.All(
            s => s.SentDay <= report.Day));

        Assert.Greater(
            report.Shipments.Count(s =>
                s.Status == PostalShipmentStatus.Delivered),
            0);
    }

    [Test]
    public void Capture_IsReadOnlyAndSnapshotsRemainStable()
    {
        var session = Create();

        var first = CirculationResearchReport.Capture(session);
        var repeated = CirculationResearchReport.Capture(session);

        Assert.AreEqual(first.ToString(), repeated.ToString());
        Assert.AreEqual(0, session.Day);
        Assert.AreEqual(0, session.OpenedMessageCount);

        session.AdvanceDays(14);

        var later = CirculationResearchReport.Capture(session);

        Assert.AreEqual(15, first.DemoCount);
        Assert.AreEqual(16, later.DemoCount);
        Assert.AreEqual(0, first.Day);
        Assert.AreEqual(14, later.Day);
    }

    [Test]
    public void SameSeed_ReproducesResearchDiagnostics()
    {
        var a = Create(seed: 24680);
        var b = Create(seed: 24680);

        a.AdvanceDays(42);
        b.AdvanceDays(42);

        var first = CirculationResearchReport.Capture(a);
        var second = CirculationResearchReport.Capture(b);

        Assert.AreEqual(first.ToString(), second.ToString());

        CollectionAssert.AreEqual(
            first.Demos.Select(d => d.ToString()).ToArray(),
            second.Demos.Select(d => d.ToString()).ToArray());

        CollectionAssert.AreEqual(
            first.Shipments.Select(s => s.ToString()).ToArray(),
            second.Shipments.Select(s => s.ToString()).ToArray());
    }

    [Test]
    [Category("FullSimulation")]
    public void Autonomous180Days_ReportsAllDemoOriginsAndReach()
    {
        var session = Create();

        int[] checkpoints =
        {
            0, 14, 30, 60, 90, 120, 180
        };

        foreach (int day in checkpoints)
        {
            session.AdvanceDays(day - session.Day);

            var report =
                CirculationResearchReport.Capture(session);

            TestContext.WriteLine(report.ToString());

            Assert.AreEqual(day, report.Day);
            Assert.AreEqual(100, report.NodeCount);
            Assert.AreEqual(15 + day / 14, report.DemoCount);
            Assert.AreEqual(day / 14, report.FieldBornDemoCount);
            Assert.AreEqual(report.DemoCount, report.Demos.Count);
            Assert.AreEqual(session.ProductCount, report.DemoCount);

            Assert.IsTrue(report.Demos.All(d =>
                d.EverHeldCount >= 1 &&
                d.EverHeldCount <= report.NodeCount));

            Assert.GreaterOrEqual(report.MaximumDemoReach, 1);
            Assert.LessOrEqual(
                report.MaximumDemoReach, report.NodeCount);
        }

        var final = CirculationResearchReport.Capture(session);

        TestContext.WriteLine("--- Field-born demos ---");

        foreach (var demo in final.Demos.Where(d => d.IsFieldBorn))
            TestContext.WriteLine(demo.ToString());

        TestContext.WriteLine("--- Physical mail sample ---");

        foreach (var shipment in final.Shipments.Take(15))
            TestContext.WriteLine(shipment.ToString());
    }
}
