using System.Collections;
using NUnit.Framework;
using SEMM91.Domain;
using SEMM91.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class
    Crucible92186CommunicationRecencyPlayModeTests
{
    private GameObject _host;

    [TearDown]
    public void TearDown()
    {
        if (_host != null)
            Object.Destroy(_host);
    }

    [UnityTest]
    public IEnumerator ArrowColor_ReflectsDirectionalRecency()
    {
        _host = new GameObject("RecencyPlayModeTest");

        var view = _host.AddComponent<
            ConfigurableCirculation3DView>();

        view.UseLegacyWorld();
        view.PauseSimulation();

        yield return null;

        Assert.IsTrue(
            view.TryGetConnectionColors(
                "AB",
                out var initialShaft,
                out _));

        Assert.That(
            initialShaft.r,
            Is.EqualTo(1f).Within(0.001f));

        view.StepOneDay();
        yield return null;

        Assert.AreEqual(1, view.CurrentDay);

        int lastSentDay = 0;

        view.Scenario.CommunicationArcs
            .TryGetLatestSentDay(
                "A", "B", out lastSentDay);

        float age = CommunicationRecency.Age01(
            lastSentDay,
            view.Scenario.Clock.CurrentDay);

        Color expected = Color.Lerp(
            Color.white,
            new Color(
                37f / 255f,
                37f / 255f,
                37f / 255f,
                1f),
            age);

        Assert.IsTrue(
            view.TryGetConnectionColors(
                "AB",
                out var shaftColor,
                out var arrowColor));

        TestContext.WriteLine(
            $"Day={view.CurrentDay} " +
            $"LastSent={lastSentDay} " +
            $"Age={age:F6} " +
            $"Expected={expected.r:F6} " +
            $"Actual={shaftColor.r:F6}");

        // LineRenderer colors may be quantized to 8-bit channels.
        // Compare at the same precision instead of using float tolerances.
        Color32 expected8 = (Color32)expected;
        Color32 shaft8 = (Color32)shaftColor;
        Color32 arrow8 = (Color32)arrowColor;

        Assert.AreEqual(expected8.r, shaft8.r);
        Assert.AreEqual(expected8.g, shaft8.g);
        Assert.AreEqual(expected8.b, shaft8.b);
        Assert.AreEqual(expected8.a, shaft8.a);

        // Shaft and arrowhead use identical colors.
        Assert.AreEqual(shaft8.r, arrow8.r);
        Assert.AreEqual(shaft8.g, arrow8.g);
        Assert.AreEqual(shaft8.b, arrow8.b);
        Assert.AreEqual(shaft8.a, arrow8.a);

        Assert.IsTrue(view.IsConnectionDisplayed("AB"));
    }
}