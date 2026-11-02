using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class PreferenceTests
{
    private static (Node node, CulturalProduct demo)
        CreateExample(bool? demoPreference)
    {
        var network = new Network();
        var node = new Node("A");

        network.AddNode(node);
        network.AddAddress("ADDR-A", "FI");
        network.PlaceNode("A", "ADDR-A");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        field.SpawnArtist(
            "ART1",
            new Dictionary<string, int>
            {
                ["A"] = 10
            },
            new Random(123));

        var demo = field.CreateDemo(
            "D1", "ART1", demoPreference);

        return (node, demo);
    }

    [Test]
    public void Preference_MatchingValuesProduceSupport()
    {
        var (node, demo) = CreateExample(true);

        node.SetPreference(true);

        Assert.IsTrue(node.Supports(demo));
    }

    [Test]
    public void Preference_DifferentValuesProduceRejection()
    {
        var (node, demo) = CreateExample(false);

        node.SetPreference(true);

        Assert.IsFalse(node.Supports(demo));
    }

    [Test]
    public void Preference_RequiresExplicitInitialization()
    {
        var (node, demo) = CreateExample(true);

        // Solmun mieltymystä ei ole alustettu.
        Assert.Throws<InvalidOperationException>(
            () => node.Supports(demo));

        node.SetPreference(true);

        // Kulttuurituotteen ominaisuutta ei ole alustettu.
        var (_, unconfiguredDemo) = CreateExample(null);

        Assert.Throws<InvalidOperationException>(
            () => node.Supports(unconfiguredDemo));
    }
}