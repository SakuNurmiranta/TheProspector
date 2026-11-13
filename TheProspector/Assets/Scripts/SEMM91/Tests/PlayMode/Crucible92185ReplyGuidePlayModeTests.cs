using System.Collections;
using NUnit.Framework;
using SEMM91.Domain;
using SEMM91.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class Crucible92185ReplyGuidePlayModeTests
{
    private GameObject _host;

    [TearDown]
    public void TearDown()
    {
        if (_host != null)
            Object.Destroy(_host);
    }

    [UnityTest]
    public IEnumerator ReplyGuide_OccupiesActualReverseArcPath()
    {
        _host = new GameObject("ReplyGuidePlayModeTest");

        var view =
            _host.AddComponent<ConfigurableCirculation3DView>();

        view.PauseSimulation();

        // A expects a reply from B.
        view.Scenario.ReplySkepticism.Track(
            "VISUAL-PENDING-92185",
            "A001",
            "B001",
            view.Scenario.Clock.CurrentDay + 8);

        view.StepOneDay();
        yield return null;

        var guideObject = GameObject.Find(
            "ReplyGuide_B001_A001");

        Assert.IsNotNull(guideObject);

        var guide =
            guideObject.GetComponent<LineRenderer>();

        Assert.IsNotNull(guide);

        Vector3 originalStart = guide.GetPosition(0);
        Vector3 originalEnd = guide.GetPosition(1);

        float initialGreen = guide.startColor.g;

        // Introduce the actual B->A direction.
        const string reverseId =
            "PLAYMODE-REVERSE-92185";

        view.Scenario.Network.AddConnection(
            reverseId,
            "B001",
            "A001",
            CommunicationLayer.Interpersonal,
            "personal");

        view.StepOneDay();
        yield return null;

        var arrowObject = GameObject.Find(
            "Edge_" + reverseId);

        Assert.IsNotNull(arrowObject);

        var actual =
            arrowObject.GetComponent<LineRenderer>();

        Assert.IsNotNull(actual);

        // Both must occupy the same screen-space path.
        Assert.That(
            guide.GetPosition(0).x,
            Is.EqualTo(actual.GetPosition(0).x)
                .Within(0.0001f));

        Assert.That(
            guide.GetPosition(0).y,
            Is.EqualTo(actual.GetPosition(0).y)
                .Within(0.0001f));

        Assert.That(
            guide.GetPosition(1).x,
            Is.EqualTo(actual.GetPosition(1).x)
                .Within(0.0001f));

        Assert.That(
            guide.GetPosition(1).y,
            Is.EqualTo(actual.GetPosition(1).y)
                .Within(0.0001f));

        // Creating the actual arc must not visually
        // relocate the pending guide.
        Assert.That(
            guide.GetPosition(0).x,
            Is.EqualTo(originalStart.x)
                .Within(0.0001f));

        Assert.That(
            guide.GetPosition(1).y,
            Is.EqualTo(originalEnd.y)
                .Within(0.0001f));

        // Progressing time moves the RGB shade
        // from light blue toward red.
        Assert.Less(
            guide.startColor.g,
            initialGreen);

        Assert.IsNotNull(
            GameObject.Find("ReplyGuide_B001_A001"),
            "The expected reply guide must remain visible.");
    }
}