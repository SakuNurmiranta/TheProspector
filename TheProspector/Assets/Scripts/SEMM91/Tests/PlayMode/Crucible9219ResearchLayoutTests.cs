
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Presentation;
using UnityEngine;

public sealed class Crucible9219ResearchLayoutTests
{
    private static readonly string[] Nodes =
    {
        "A", "B", "C", "D", "E", "F"
    };

    private static Dictionary<string, int> Gains(
        params int[] values)
    {
        var result = new Dictionary<string, int>();

        for (int i = 0; i < values.Length; i++)
            result.Add(Nodes[i], values[i]);

        return result;
    }

    private static ResearchMail[] Mutual(
        string a,
        string b,
        int day = 1)
    {
        return new[]
        {
            new ResearchMail(a, b, day),
            new ResearchMail(b, a, day)
        };
    }

    [Test]
    public void EqualCapital_AllNodesHaveBaseSize()
    {
        var size =
            ResearchLayoutModel.CapitalSizeFactors(
                Gains(10, 10, 10));

        Assert.AreEqual(1f, size["A"]);
        Assert.AreEqual(1f, size["B"]);
        Assert.AreEqual(1f, size["C"]);
    }

    [Test]
    public void CapitalRank_NotAbsoluteMagnitude_DeterminesSize()
    {
        var low =
            ResearchLayoutModel.CapitalSizeFactors(
                Gains(0, 5, 10));

        var high =
            ResearchLayoutModel.CapitalSizeFactors(
                Gains(0, 500, 1000));

        Assert.AreEqual(low["C"], high["C"]);
        Assert.AreEqual(low["B"], high["B"]);

        Assert.AreEqual(
            1.18f, high["C"], 0.0001f);

        Assert.AreEqual(
            0.82f, high["A"], 0.0001f);
    }

    [Test]
    public void CapitalTies_GetIdenticalSize()
    {
        var sizes =
            ResearchLayoutModel.CapitalSizeFactors(
                Gains(1, 5, 5, 10));

        Assert.AreEqual(
            sizes["B"], sizes["C"]);
    }

    [Test]
    public void RollingGains_ExpireAtThirtyDays()
    {
        var balances = Gains(10, 10);

        var gains = new RollingCapitalGains(
            new[] { "A", "B" },
            id => balances[id],
            0);

        balances["A"] += 4;
        gains.RecordDay(1);

        Assert.AreEqual(
            4, gains.RecentGains["A"]);

        for (int day = 2; day <= 30; day++)
            gains.RecordDay(day);

        Assert.AreEqual(
            4, gains.RecentGains["A"]);

        gains.RecordDay(31);

        Assert.AreEqual(
            0, gains.RecentGains["A"]);
    }

    [Test]
    public void InactiveIsolatedNodes_RemainOnOuterRing()
    {
        var plan = ResearchLayoutModel.Build(
            Nodes,
            Gains(0, 0, 0, 0, 0, 0),
            Array.Empty<ResearchMail>(),
            10,
            10f);

        foreach (var id in Nodes)
        {
            Assert.AreEqual(
                10f,
                plan.Targets[id].magnitude,
                0.0001f);
        }
    }

    [Test]
    public void HighRecentCapitalGain_MovesGroupInward()
    {
        var plan = ResearchLayoutModel.Build(
            Nodes,
            Gains(20, 0, 0, 0, 0, 0),
            Array.Empty<ResearchMail>(),
            10,
            10f);

        Assert.Less(
            plan.Targets["A"].magnitude,
            plan.Targets["B"].magnitude);

        Assert.Greater(
            plan.Targets["A"].magnitude, 3f);
    }

    [Test]
    public void OneWayMail_DoesNotCluster()
    {
        var plan = ResearchLayoutModel.Build(
            Nodes,
            Gains(0, 0, 0, 0, 0, 0),
            new[]
            {
                new ResearchMail("A", "B", 5)
            },
            10,
            10f);

        Assert.AreNotEqual(
            plan.Groups["A"],
            plan.Groups["B"]);
    }

    [Test]
    public void ReciprocalMail_ClustersTwoNodes()
    {
        var plan = ResearchLayoutModel.Build(
            Nodes,
            Gains(0, 0, 0, 0, 0, 0),
            Mutual("A", "B"),
            10,
            10f);

        Assert.AreEqual(
            plan.Groups["A"],
            plan.Groups["B"]);
    }

    [Test]
    public void SingleMutualBridge_DoesNotMergeTwoCommunities()
    {
        var events = new List<ResearchMail>();

        foreach (var pair in new[]
        {
            ("A", "B"),
            ("B", "C"),
            ("C", "A"),
            ("D", "E"),
            ("E", "F"),
            ("F", "D"),
            ("C", "D")
        })
        {
            events.AddRange(
                Mutual(pair.Item1, pair.Item2));
        }

        var plan = ResearchLayoutModel.Build(
            Nodes,
            Gains(0, 0, 0, 0, 0, 0),
            events,
            10,
            10f);

        Assert.AreEqual(
            plan.Groups["A"],
            plan.Groups["C"]);

        Assert.AreEqual(
            plan.Groups["D"],
            plan.Groups["F"]);

        Assert.AreNotEqual(
            plan.Groups["C"],
            plan.Groups["D"]);
    }

    [Test]
    public void SameInput_ProducesSameTargets()
    {
        var gain = Gains(5, 4, 0, 0, 0, 0);
        var messages = Mutual("A", "B");

        var first = ResearchLayoutModel.Build(
            Nodes, gain, messages, 10, 10f);

        var second = ResearchLayoutModel.Build(
            Nodes, gain, messages, 10, 10f);

        foreach (var id in Nodes)
        {
            Assert.AreEqual(
                first.Targets[id],
                second.Targets[id]);
        }
    }
}
