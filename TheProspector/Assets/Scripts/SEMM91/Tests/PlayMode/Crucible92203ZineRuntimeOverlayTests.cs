using System.Collections;
using NUnit.Framework;
using SEMM91.Domain;
using SEMM91.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class Crucible92203ZineRuntimeOverlayTests
{
    private GameObject _host;

    [TearDown]
    public void TearDown()
    {
        if (_host != null)
            Object.Destroy(_host);
    }

    [UnityTest]
    public IEnumerator Overlay_ReportsRecurringZinePublication()
    {
        var session = DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 0,
            enableInitialZineForwarding: true,
            enableRecurringZineIssues: true,
            enableMultiIssueZinePostalInserts: true,
            enableAutonomousMultiIssueContacts: true);

        _host = new GameObject(
            "ZineRuntimeOverlayTest");

        var overlay =
            _host.AddComponent<ZineRuntimeOverlay>();

        overlay.Bind(session);

        yield return null;

        Assert.IsNotNull(overlay.CurrentSnapshot);

        Assert.AreEqual(
            1,
            overlay.CurrentSnapshot.PublishedIssueCount);

        session.Scenario.Clock.AdvanceDays(90);

        overlay.RefreshNow();

        yield return null;

        Assert.AreEqual(
            90,
            overlay.CurrentSnapshot.Day);

        Assert.AreEqual(
            2,
            overlay.CurrentSnapshot.PublishedIssueCount);

        Assert.AreEqual(
            1,
            overlay.CurrentSnapshot.LaterIssueCount);

        Assert.GreaterOrEqual(
            overlay.CurrentSnapshot.LaterIssueReaders,
            1);

        overlay.Bind(null);

        Assert.IsNull(overlay.CurrentSnapshot);
    }
}