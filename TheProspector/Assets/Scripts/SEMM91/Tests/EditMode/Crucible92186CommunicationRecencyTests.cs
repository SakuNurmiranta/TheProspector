using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible92186CommunicationRecencyTests
{
    private static ConfigurableCirculationScenario Create()
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "SE");
        definition.AddNode("C", "FI");

        definition.SetSeed("C", "D1");

        definition.AddConnection("AB", "A", "B");
        definition.AddConnection("BA", "B", "A");

        return new ConfigurableCirculationScenario(definition);
    }

    private static void Send(
        ConfigurableCirculationScenario scenario,
        string sender,
        string recipient,
        string messageId)
    {
        var route = scenario.Network
            .GetOutgoingConnections(
                sender,
                CommunicationLayer.Interpersonal)
            .Single(c => c.TargetNodeId == recipient);

        var package = new MessagePackage(
            messageId,
            route,
            addressNoticeAddressId: "ADDR-" + sender);

        scenario.Exchange.Send(
            "SHIP:" + messageId,
            package,
            sender == "B" ? "SE" : "FI",
            recipient == "B" ? "SE" : "FI",
            replyAllowanceDays: 5);
    }

    [Test]
    public void Fade_IsLinearAcross365Days()
    {
        Assert.AreEqual(
            0f, CommunicationRecency.Age01(0, 0));

        Assert.AreEqual(
            0.5f,
            CommunicationRecency.Age01(0, 182.5),
            0.0001f);

        Assert.AreEqual(
            1f, CommunicationRecency.Age01(0, 365));

        Assert.AreEqual(
            1f, CommunicationRecency.Age01(0, 730));
    }

    [Test]
    public void Fade_ClampsWithoutRemovingHistoricalDirection()
    {
        Assert.AreEqual(
            0f, CommunicationRecency.Age01(100, 99));

        Assert.AreEqual(
            0f, CommunicationRecency.Age01(100, 100));

        Assert.AreEqual(
            1f, CommunicationRecency.Age01(100, 465));

        Assert.AreEqual(
            1f, CommunicationRecency.Age01(100, 1000));
    }

    [Test]
    public void SameDirectionMessage_ResetsRecency()
    {
        var scenario = Create();

        Send(scenario, "A", "B", "FIRST");

        scenario.Clock.AdvanceDays(90);

        Assert.IsTrue(
            scenario.CommunicationArcs.TryGetLatestSentDay(
                "A", "B", out int firstDay));

        Assert.AreEqual(0, firstDay);

        Assert.Greater(
            CommunicationRecency.Age01(
                firstDay, 90), 0f);

        Send(scenario, "A", "B", "SECOND");

        Assert.IsTrue(
            scenario.CommunicationArcs.TryGetLatestSentDay(
                "A", "B", out int latestDay));

        Assert.AreEqual(90, latestDay);

        Assert.AreEqual(
            0f,
            CommunicationRecency.Age01(
                latestDay, 90));

        Assert.AreEqual(
            2,
            scenario.CommunicationArcs.Events.Count(
                e => e.SourceNodeId == "A" &&
                     e.TargetNodeId == "B"));
    }

    [Test]
    public void ReverseMessage_DoesNotRefreshOutboundDirection()
    {
        var scenario = Create();

        Send(scenario, "A", "B", "OUTBOUND");

        scenario.Clock.AdvanceDays(30);

        Send(scenario, "B", "A", "REVERSE");

        Assert.IsTrue(
            scenario.CommunicationArcs.TryGetLatestSentDay(
                "A", "B", out int outboundDay));

        Assert.IsTrue(
            scenario.CommunicationArcs.TryGetLatestSentDay(
                "B", "A", out int reverseDay));

        Assert.AreEqual(0, outboundDay);
        Assert.AreEqual(30, reverseDay);

        Assert.Greater(
            CommunicationRecency.Age01(
                outboundDay, 30), 0f);

        Assert.AreEqual(
            0f,
            CommunicationRecency.Age01(
                reverseDay, 30));
    }

    [Test]
    public void InitialRoute_DoesNotInventCommunicationEvent()
    {
        var scenario = Create();

        Assert.IsFalse(
            scenario.CommunicationArcs.TryGetLatestSentDay(
                "A", "B", out _));

        Assert.IsFalse(
            scenario.CommunicationArcs.TryGetLatestSentDay(
                "B", "A", out _));

        // Initial connections still exist as routes.
        Assert.AreEqual(
            1,
            scenario.Network.GetOutgoingConnections(
                "A",
                CommunicationLayer.Interpersonal).Count);

        // Visualization may use day zero as a
        // baseline, but no synthetic postal event
        // is inserted into the history.
        Assert.AreEqual(
            0f, CommunicationRecency.Age01(0, 0));
    }
}