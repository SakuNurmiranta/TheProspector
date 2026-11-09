
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;
using SEMM91.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class Crucible924RuntimeTests
{
    private GameObject _host;

    private ConfigurableCirculation3DView CreateView()
    {
        _host = new GameObject("Crucible924RuntimeTest");
        var view = _host.AddComponent<ConfigurableCirculation3DView>();
        view.PauseSimulation();
        return view;
    }

    [TearDown]
    public void TearDown()
    {
        if (_host != null)
            UnityEngine.Object.Destroy(_host);
    }

    [UnityTest]
    public IEnumerator DefaultView_CreatesHundredVisiblePeople()
    {
        var view = CreateView();
        yield return null;

        Assert.IsNotNull(view.Session);
        Assert.AreEqual(100, view.Session.NodeCount);
        Assert.AreEqual(200, view.Session.AddressCount);
        Assert.AreEqual(15, view.Session.ProductCount);
        Assert.AreEqual(100, view.DisplayedNodeCount);
        Assert.AreEqual(
            view.Scenario.Observation.Capture().Connections.Count,
            view.DisplayedConnectionCount);
        Assert.AreEqual(0, view.CurrentDay);
        Assert.Greater(Camera.main.orthographicSize, 6.5f);
    }

    [UnityTest]
    public IEnumerator StepPauseAndReset_UseTheSameScenario()
    {
        var view = CreateView();

        view.StepOneDay();
        Assert.AreEqual(1, view.CurrentDay);
        Assert.IsFalse(view.IsRunning);

        view.PlaySimulation();
        Assert.IsTrue(view.IsRunning);

        view.PauseSimulation();
        Assert.IsFalse(view.IsRunning);

        view.ResetSimulation();
        view.PauseSimulation();
        yield return null;

        Assert.AreEqual(0, view.CurrentDay);
        Assert.AreEqual(100, view.DisplayedNodeCount);
    }

    [UnityTest]
    public IEnumerator InspectorParameters_CanRebuildSmallerWorld()
    {
        var view = CreateView();

        view.ConfigureRuntime(new CirculationInitializationParameters
        {
            CountryAPopulation = 10,
            CountryBPopulation = 20,
            AddressSlotsPerCountry = 20,
            FriendshipParticipationPercent = 20,
            RandomSeed = 924
        });

        view.PauseSimulation();
        yield return null;

        Assert.AreEqual(30, view.Session.NodeCount);
        Assert.AreEqual(40, view.Session.AddressCount);
        Assert.AreEqual(15, view.Session.ProductCount);
        Assert.AreEqual(30, view.DisplayedNodeCount);
    }

    [UnityTest]
    public IEnumerator LegacyView_RetainsExistingFiveNodeScene()
    {
        var view = CreateView();
        view.UseLegacyWorld();
        view.PauseSimulation();
        yield return null;

        Assert.IsNull(view.Session);
        Assert.AreEqual(5, view.Scenario.Network.NodeCount);
        Assert.AreEqual(2, view.Scenario.ProductIds.Count);
        Assert.AreEqual(5, view.DisplayedNodeCount);
        Assert.AreEqual(6, view.DisplayedConnectionCount);
    }

    [UnityTest]
    public IEnumerator Reset_ReproducesEditorAndAddressesWithSameSeed()
    {
        var view = CreateView();
        var first = view.Session.InitialWorld;

        var editor = first.EditorNodeId;
        var addresses = first.Definition.Placements
            .Select(p => p.NodeId + "@" + p.AddressId)
            .ToArray();

        view.ResetSimulation();
        view.PauseSimulation();
        yield return null;

        Assert.AreEqual(
            editor,
            view.Session.InitialWorld.EditorNodeId);

        CollectionAssert.AreEqual(
            addresses,
            view.Session.InitialWorld.Definition.Placements
                .Select(p => p.NodeId + "@" + p.AddressId)
                .ToArray());
    }

    
    [UnityTest]
    public IEnumerator NewNetworkArc_ProducesNewVisibleConnection()
    {
        var view = CreateView();

        // Day 1: allow the initial zine to create
        // its five Layer 3 distribution connections.
        view.StepOneDay();

        Assert.AreEqual(1, view.CurrentDay);

        // Measure the network after initial distribution.
        var before = view.DisplayedConnectionCount;

        // Add one independent interpersonal connection.
        view.Scenario.Network.AddConnection(
            "RUNTIME-A001-B001",
            "A001",
            "B001",
            CommunicationLayer.Interpersonal,
            "personal");

        // Refresh through the normal runtime stepping path.
        view.StepOneDay();
        yield return null;

        Assert.AreEqual(
            before + 1,
            view.DisplayedConnectionCount);
    }

}
