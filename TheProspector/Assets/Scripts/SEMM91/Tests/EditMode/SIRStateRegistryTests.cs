
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class SIRStateRegistryTests
{
    private static (
        Field field,
        Node a,
        Node b,
        CulturalProduct d1,
        CulturalProduct d2
    ) CreateFixture()
    {
        var network = new Network();

        var a = new Node("A");
        var b = new Node("B");

        network.AddNode(a);
        network.AddNode(b);

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");
        field.RegisterLocalNode("B");

        field.SpawnArtist(
            "ART1",
            new Dictionary<string, int>
            {
                ["A"] = 10,
                ["B"] = 10
            },
            new Random(123));

        var d1 = field.CreateDemo("D1", "ART1", true);
        var d2 = field.CreateDemo("D2", "ART1", false);

        return (field, a, b, d1, d2);
    }

    [Test]
    public void Sir_UnseenPairsDefaultToS()
    {
        var (field, a, b, d1, d2) = CreateFixture();
        var sir = new SIRStateRegistry();

        Assert.AreEqual(SIRState.S,
            sir.GetNodeState(a, d1));

        Assert.AreEqual(SIRState.S,
            sir.GetNodeState(b, d2));

        Assert.AreEqual(SIRState.S,
            sir.GetFieldState(field, d1));
    }

    [Test]
    public void Sir_NodeStatesAreIndependent()
    {
        var (_, a, b, d1, d2) = CreateFixture();
        var sir = new SIRStateRegistry();

        sir.BeginNodeCopying(a, d1);

        Assert.AreEqual(SIRState.I,
            sir.GetNodeState(a, d1));

        Assert.AreEqual(SIRState.S,
            sir.GetNodeState(a, d2));

        Assert.AreEqual(SIRState.S,
            sir.GetNodeState(b, d1));

        Assert.AreEqual(SIRState.S,
            sir.GetNodeState(b, d2));
    }

    [Test]
    public void Sir_PositivePreferenceDoesNotStartCopying()
    {
        var (_, a, _, d1, _) = CreateFixture();
        var sir = new SIRStateRegistry();

        a.SetPreference(true);

        Assert.IsTrue(a.Supports(d1));

        // Myönteinen arvio ei käynnistä kopiointia.
        Assert.AreEqual(SIRState.S,
            sir.GetNodeState(a, d1));
    }

    [Test]
    public void Sir_FieldAndNodeStatesAreIndependent()
    {
        var (field, a, b, d1, _) = CreateFixture();
        var sir = new SIRStateRegistry();

        sir.BeginFieldWave(field, d1);

        Assert.AreEqual(SIRState.I,
            sir.GetFieldState(field, d1));

        Assert.AreEqual(SIRState.S,
            sir.GetNodeState(a, d1));

        sir.BeginNodeCopying(a, d1);
        sir.CloseFieldWave(field, d1);

        Assert.AreEqual(SIRState.R,
            sir.GetFieldState(field, d1));

        Assert.AreEqual(SIRState.I,
            sir.GetNodeState(a, d1));

        Assert.AreEqual(SIRState.S,
            sir.GetNodeState(b, d1));
    }

    [Test]
    public void Sir_NodeCopyingMustBeginBeforeItCanEnd()
    {
        var (_, a, _, d1, _) = CreateFixture();
        var sir = new SIRStateRegistry();

        Assert.Throws<InvalidOperationException>(() =>
            sir.EndNodeCopying(a, d1));

        sir.BeginNodeCopying(a, d1);

        // Jakso pysyy I-tilassa useiden kopioiden ajan.
        sir.BeginNodeCopying(a, d1);

        sir.EndNodeCopying(a, d1);

        Assert.AreEqual(SIRState.R,
            sir.GetNodeState(a, d1));

        Assert.Throws<InvalidOperationException>(() =>
            sir.BeginNodeCopying(a, d1));
    }

    [Test]
    public void Sir_FieldWaveCannotRestartAfterClosure()
    {
        var (field, _, _, d1, d2) = CreateFixture();
        var sir = new SIRStateRegistry();

        Assert.Throws<InvalidOperationException>(() =>
            sir.CloseFieldWave(field, d1));

        sir.BeginFieldWave(field, d1);
        sir.CloseFieldWave(field, d1);

        Assert.AreEqual(SIRState.R,
            sir.GetFieldState(field, d1));

        Assert.Throws<InvalidOperationException>(() =>
            sir.BeginFieldWave(field, d1));

        // Toisen demon leviäminen on itsenäistä.
        Assert.AreEqual(SIRState.S,
            sir.GetFieldState(field, d2));

        sir.BeginFieldWave(field, d2);

        Assert.AreEqual(SIRState.I,
            sir.GetFieldState(field, d2));
    }
}
