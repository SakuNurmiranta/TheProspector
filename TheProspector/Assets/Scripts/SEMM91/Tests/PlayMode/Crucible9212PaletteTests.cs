
using System.Collections;
using NUnit.Framework;
using SEMM91.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class Crucible9212PaletteTests
{
    [UnityTest]
    public IEnumerator InspectorPalette_UpdatesCameraBackground()
    {
        var host = new GameObject("PaletteCameraTest");

        try
        {
            var view =
                host.AddComponent<ConfigurableCirculation3DView>();

            var expected = new Color(
                0.12f, 0.22f, 0.32f);

            view.Palette.SceneBackground = expected;

            yield return null;

            Assert.IsNotNull(Camera.main);
            Assert.AreEqual(
                expected,
                Camera.main.backgroundColor);
        }
        finally
        {
            Object.Destroy(host);
        }
    }

    [UnityTest]
    public IEnumerator NodePalette_AppliesToRenderedNodes()
    {
        var host = new GameObject("PaletteNodeTest");

        try
        {
            var view =
                host.AddComponent<ConfigurableCirculation3DView>();

            var color = new Color(
                0.92f, 0.24f, 0.71f);

            var p = view.Palette;

            p.SelectedNode = color;
            p.OutgoingContact = color;
            p.NotReceived = color;
            p.ReceivedUnopened = color;
            p.Queued = color;
            p.ListeningStart = color;
            p.ListeningEnd = color;
            p.Completed = color;

            view.StepOneDay();

            yield return null;

            Assert.AreEqual(100, view.DisplayedNodeCount);

            var node = GameObject.Find("Node_A001");
            Assert.IsNotNull(node);

            var block = new MaterialPropertyBlock();

            node.GetComponent<Renderer>()
                .GetPropertyBlock(block);

            var actual = block.GetColor("_BaseColor");

            Assert.That(actual.r,
                Is.EqualTo(color.r).Within(0.0001f));
            Assert.That(actual.g,
                Is.EqualTo(color.g).Within(0.0001f));
            Assert.That(actual.b,
                Is.EqualTo(color.b).Within(0.0001f));
        }
        finally
        {
            Object.Destroy(host);
        }
    }
}
