
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class PostalReplySkepticismIntegrationTests
{
    private static ConfigurableCirculationScenario Create()
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");

        definition.AddConnection("AB", "A", "B");
        definition.AddConnection("BA", "B", "A");

        definition.SetSeed("A", "D1");

        // Ei MarkKnownMissing-kutsuja:
        // tähän testiin ei alusteta ylimääräisiä
        // fyysisiä kasettilähetyksiä.
        return new ConfigurableCirculationScenario(
            definition);
    }

    private static void Send(
        ConfigurableCirculationScenario s,
        string packageId,
        string sender,
        string recipient,
        string replyTo = null,
        bool expectsReply = false,
        bool compensates = false)
    {
        var connection = s.Network
            .GetOutgoingConnections(
                sender,
                CommunicationLayer.Interpersonal)
            .Single(c => c.TargetNodeId == recipient);

        // Palautetta sisältävä mutta kasetiton
        // viesti. MessagePackage vaatii
        // feedbackProductin ja thumbin yhdessä.
        var package = new MessagePackage(
            packageId,
            connection,
            feedbackProduct: s.GetDemo("D1"),
            thumb: +1,
            replyToPackageId: replyTo);

        s.Exchange.Send(
            "SHIP-" + packageId,
            package,
            "FI",
            "FI",
            5,
            expectsReplyForSkepticism: expectsReply,
            compensatesReply: compensates);
    }

    private static int Deadline()
    {
        return PostalTimingRules.CalculateDeadline(
            0,
            PostalTimingRules.GetTransitDays("FI", "FI"),
            5);
    }

    [Test]
    public void PostalSkepticism_TrackedRequestExpiresAfterDeadline()
    {
        var s = Create();

        Send(s, "REQ1", "A", "B", expectsReply: true);

        s.Clock.AdvanceDays(Deadline());

        Assert.AreEqual(0,
            s.Skepticism.Get("A", "B").Total);

        s.Clock.AdvanceDays(1);

        Assert.AreEqual(1,
            s.Skepticism.Get("A", "B").UnansweredReplies);

        Assert.AreEqual(0,
            s.Skepticism.Get("B", "A").Total);
    }

    [Test]
    public void PostalSkepticism_OnTimeArrivalCancelsWithoutOpening()
    {
        var s = Create();

        Send(s, "REQ1", "A", "B", expectsReply: true);

        // A:n viesti saapuu B:lle.
        s.Clock.AdvanceDays(2);

        Send(s, "REPLY1", "B", "A", replyTo: "REQ1");

        // B:n vastaus saapuu A:lle.
        // Emme avaa kumpaakaan viestiä.
        s.Clock.AdvanceDays(Deadline() - 2 + 1);

        Assert.AreEqual(0,
            s.Skepticism.Get("A", "B").Total);

        Assert.IsNull(
            s.Stances.GetLastThumb("A", "B", "D1"));

        s.Clock.AdvanceDays(10);

        Assert.AreEqual(0,
            s.Skepticism.Get("A", "B").Total);
    }

    [Test]
    public void PostalSkepticism_LateReplyDoesNotAutoCompensate()
    {
        var s = Create();

        Send(s, "REQ1", "A", "B", expectsReply: true);

        // B lähettää vastauksen vasta määräpäivänä.
        // Postimatkan vuoksi se saapuu myöhässä.
        s.Clock.AdvanceDays(Deadline());

        Send(s, "REPLY1", "B", "A", replyTo: "REQ1");

        s.Clock.AdvanceDays(1);

        Assert.AreEqual(1,
            s.Skepticism.Get("A", "B").Total);

        s.Clock.AdvanceDays(1);

        Assert.AreEqual(1,
            s.Skepticism.Get("A", "B").UnansweredReplies);
    }

    [Test]
    public void PostalSkepticism_ExplicitCompensationOnArrival()
    {
        var s = Create();

        Send(s, "REQ1", "A", "B", expectsReply: true);

        s.Clock.AdvanceDays(Deadline());

        Send(
            s,
            "REPLY1",
            "B",
            "A",
            replyTo: "REQ1",
            compensates: true);

        // Määräpäivän jälkeinen päivä:
        // vastaus ei ole vielä saapunut.
        s.Clock.AdvanceDays(1);

        Assert.AreEqual(1,
            s.Skepticism.Get("A", "B").UnansweredReplies);

        // Kompensoiva vastaus saapuu.
        s.Clock.AdvanceDays(1);

        Assert.AreEqual(0,
            s.Skepticism.Get("A", "B").UnansweredReplies);
    }

    [Test]
    public void PostalSkepticism_OrdinaryMessagesRemainUntracked()
    {
        var s = Create();

        // Oletusarvoinen lähetys ei käynnistä
        // skeptisyysseurantaa.
        Send(s, "ORDINARY1", "A", "B");

        s.Clock.AdvanceDays(30);

        Assert.AreEqual(0,
            s.Skepticism.Get("A", "B").Total);

        Assert.AreEqual(0,
            s.ReplySkepticism.Count);
    }

    [Test]
    public void PostalSkepticism_WrongDirectionIsNotAReply()
    {
        var s = Create();

        Send(s, "REQ1", "A", "B", expectsReply: true);

        s.Clock.AdvanceDays(2);

        // A lähettää toisen viestin B:lle ja
        // merkitsee sen virheellisesti vastaukseksi
        // omaan aikaisempaan viestiinsä.
        Send(
            s,
            "INVALID_REPLY",
            "A",
            "B",
            replyTo: "REQ1");

        s.Clock.AdvanceDays(Deadline() - 2 + 1);

        Assert.AreEqual(1,
            s.Skepticism.Get("A", "B").UnansweredReplies);
    }
}
