
using System;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible9211OpeningDiagnosticsTests
{
    [Test]
    public void OriginalAndDynamicDemos_OpeningDiagnostics()
    {
        var parameters = new CirculationInitializationParameters
        {
            RandomSeed = 921
        };

        var baseline = DemoSession.CreateConfigured(
            parameters,
            fieldDemoIntervalDays: 0);

        var dynamic = DemoSession.CreateConfigured(
            parameters,
            fieldDemoIntervalDays: 14);

        // Dynamic production has not started yet.
        baseline.AdvanceDays(13);
        dynamic.AdvanceDays(13);

        TestContext.WriteLine(
            "Day 13, no production: " +
            baseline.OpenedMessageCount);

        TestContext.WriteLine(
            "Day 13, production enabled: " +
            dynamic.OpenedMessageCount);

        Assert.AreEqual(
            baseline.OpenedMessageCount,
            dynamic.OpenedMessageCount);

        Assert.Greater(
            baseline.OpenedMessageCount,
            0,
            "Original cassette mail has not opened.");

        // Compare behavior after new demos appear.
        baseline.AdvanceDays(17);
        dynamic.AdvanceDays(17);

        TestContext.WriteLine(
            "Day 30, no production: " +
            baseline.OpenedMessageCount);

        TestContext.WriteLine(
            "Day 30, production enabled: " +
            dynamic.OpenedMessageCount);

        var observation =
            dynamic.Scenario.Observation.Capture();

        foreach (string productId in dynamic.Scenario.ProductIds)
        {
            var details =
                CirculationInspectorProjection.ForProduct(
                    observation, productId);

            TestContext.WriteLine(
                productId +
                " SeedDispatches=" +
                dynamic.Scenario.GetSeedWave(productId).Count +
                " Received=" + details.EverReceivedCount +
                " Unopened=" + details.ReceivedUnopenedCount +
                " Queued=" + details.QueuedCount +
                " Listening=" + details.ListeningCount +
                " Completed=" + details.CompletedCount);
        }
    }
}
