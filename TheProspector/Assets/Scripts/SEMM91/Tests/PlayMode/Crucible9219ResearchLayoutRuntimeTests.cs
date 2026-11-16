using System.Collections;
using NUnit.Framework;
using SEMM91.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class Crucible9219ResearchLayoutRuntimeTests
{
    private GameObject _host;

    [TearDown]
    public void TearDown()
    {
        if (_host != null)
            Object.Destroy(_host);
    }

    [UnityTest]
    public IEnumerator CapitalRank_ChangesRenderedNodeSize()
    {
        _host = new GameObject(
            "ResearchLayoutRuntimeTest");

        var view = _host.AddComponent<
            ConfigurableCirculation3DView>();

        view.PauseSimulation();

        yield return null;

        var node = GameObject.Find("Node_A001");

        Assert.IsNotNull(node);

        float before =
            node.transform.localScale.x;

        view.Scenario.Capital.Credit(
            "A001", 100);

        view.StepOneDay();

        yield return null;

        Assert.Greater(
            node.transform.localScale.x,
            before);

        Assert.AreEqual(
            1, view.CurrentDay);
    }
}