using System.Collections;
using NUnit.Framework;
using SEMM91.Domain;
using SEMM91.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class Crucible92203ConfiguredRuntimeTests
{
    private GameObject _host;

    [TearDown]
    public void TearDown()
    {
        if (_host != null)
            Object.Destroy(_host);
    }

    [UnityTest]
    public IEnumerator ConfiguredView_ActivatesAndBindsRecurringZineCycle()
    {
        _host = new GameObject("ZineConfiguredRuntimeTest");

        var view = _host.AddComponent<ConfigurableCirculation3DView>();

        view.PauseSimulation();

        yield return null;

        Assert.IsNotNull(view.Session);
        Assert.IsNotNull(view.Session.RecurringZines);
        Assert.IsNotNull(view.Session.MultiIssueZinePostalInserts);
        Assert.IsNotNull(view.Session.FirstContactCycle.MultiIssueSelection);

        var overlay = _host.GetComponent<ZineRuntimeOverlay>();

        Assert.IsNotNull(overlay);
        Assert.IsNotNull(overlay.CurrentSnapshot);

        Assert.AreEqual(
            view.CurrentDay,
            overlay.CurrentSnapshot.Day);

        // Legacy mode must not keep the previous
        // configured world's statistics.
        view.UseLegacyWorld();

        yield return null;

        Assert.IsNull(view.Session);

        Assert.IsNull(
            overlay.CurrentSnapshot,
            "Legacy world must not retain prior " +
            "configured-session telemetry.");
    }
}