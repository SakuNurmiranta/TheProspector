
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class PostalTimingRulesTests
{
    [Test]
    public void Timing_DomesticMailTakesTwoDays()
    {
        Assert.AreEqual(
            2,
            PostalTimingRules.GetTransitDays("FI", "FI"));

        // Maakoodin kirjainkoko ei vaikuta.
        Assert.AreEqual(
            2,
            PostalTimingRules.GetTransitDays("fi", "FI"));
    }

    [Test]
    public void Timing_InternationalMailTakesFiveDays()
    {
        Assert.AreEqual(
            5,
            PostalTimingRules.GetTransitDays("FI", "SE"));

        Assert.AreEqual(
            5,
            PostalTimingRules.GetTransitDays("NO", "FI"));
    }

    [Test]
    public void Timing_DeadlineIncludesTransitAndAllowance()
    {
        var registry = new ReplyWindowRegistry();

        // Lähetys päivänä 0.
        // Kotimaan posti 2 päivää, lisäaika 5.
        var domestic = registry.Register(
            "MSG1", "A", "B",
            0, "FI", "FI", 5);

        Assert.AreEqual(2, domestic.TransitDays);
        Assert.AreEqual(7, domestic.DeadlineDay);

        // Lähetys päivänä 10.
        // Ulkomaan posti 5 päivää, lisäaika 7.
        var international = registry.Register(
            "MSG2", "A", "C",
            10, "FI", "NO", 7);

        Assert.AreEqual(5, international.TransitDays);
        Assert.AreEqual(22, international.DeadlineDay);
    }

    [Test]
    public void Timing_ReplyOnDeadlineIsOnTime()
    {
        var registry = new ReplyWindowRegistry();

        registry.Register(
            "MSG1", "A", "B",
            0, "FI", "FI", 5);

        Assert.AreEqual(
            ReplyTiming.OnTime,
            registry.Resolve(
                "MSG1", "B", "A", 7));
    }

    [Test]
    public void Timing_ReplyAfterDeadlineIsLate()
    {
        var registry = new ReplyWindowRegistry();

        registry.Register(
            "MSG1", "A", "B",
            0, "FI", "FI", 5);

        Assert.AreEqual(
            ReplyTiming.Late,
            registry.Resolve(
                "MSG1", "B", "A", 8));

        Assert.AreEqual(
            ReplyTiming.Late,
            registry.Resolve(
                "MSG1", "B", "A", 30));
    }

    [Test]
    public void Timing_UnknownOrWrongDirectionIsNotAReply()
    {
        var registry = new ReplyWindowRegistry();

        registry.Register(
            "MSG1", "A", "B",
            0, "FI", "FI", 5);

        Assert.AreEqual(
            ReplyTiming.Unknown,
            registry.Resolve(
                null, "B", "A", 5));

        Assert.AreEqual(
            ReplyTiming.Unknown,
            registry.Resolve(
                "UNKNOWN", "B", "A", 5));

        // C ei voi vastata A:n ja B:n väliseen
        // viestiin B:n puolesta.
        Assert.AreEqual(
            ReplyTiming.Unknown,
            registry.Resolve(
                "MSG1", "C", "A", 5));

        // Vastaus ei voi kulkea samaan suuntaan
        // alkuperäisen viestin kanssa.
        Assert.AreEqual(
            ReplyTiming.Unknown,
            registry.Resolve(
                "MSG1", "A", "B", 5));
    }

    [Test]
    public void Timing_WindowsAreIndependent()
    {
        var registry = new ReplyWindowRegistry();

        registry.Register(
            "MSG1", "A", "B",
            0, "FI", "FI", 5);

        registry.Register(
            "MSG2", "A", "B",
            3, "FI", "FI", 7);

        // Päivänä 9 ensimmäinen vastaus
        // on myöhässä, toinen ajoissa.
        Assert.AreEqual(
            ReplyTiming.Late,
            registry.Resolve(
                "MSG1", "B", "A", 9));

        Assert.AreEqual(
            ReplyTiming.OnTime,
            registry.Resolve(
                "MSG2", "B", "A", 9));

        Assert.AreEqual(2, registry.Count);
    }

    [Test]
    public void Timing_RejectsInvalidRegistration()
    {
        var registry = new ReplyWindowRegistry();

        Assert.Throws<ArgumentException>(() =>
            registry.Register(
                "", "A", "B",
                0, "FI", "FI", 5));

        Assert.Throws<ArgumentException>(() =>
            registry.Register(
                "MSG1", "A", "A",
                0, "FI", "FI", 5));

        Assert.Throws<ArgumentException>(() =>
            registry.Register(
                "MSG1", "A", "B",
                0, "", "FI", 5));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            registry.Register(
                "MSG1", "A", "B",
                -1, "FI", "FI", 5));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            registry.Register(
                "MSG1", "A", "B",
                0, "FI", "FI", 4));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            registry.Register(
                "MSG1", "A", "B",
                0, "FI", "FI", 8));

        Assert.AreEqual(0, registry.Count);
    }

    [Test]
    public void Timing_DuplicateRequestIdIsRejected()
    {
        var registry = new ReplyWindowRegistry();

        registry.Register(
            "MSG1", "A", "B",
            0, "FI", "FI", 5);

        Assert.Throws<ArgumentException>(() =>
            registry.Register(
                "MSG1", "A", "C",
                3, "FI", "NO", 7));

        Assert.AreEqual(1, registry.Count);

        Assert.AreEqual(
            ReplyTiming.OnTime,
            registry.Resolve(
                "MSG1", "B", "A", 7));
    }

    [Test]
    public void Timing_OverflowDoesNotRegisterWindow()
    {
        var registry = new ReplyWindowRegistry();

        Assert.Throws<OverflowException>(() =>
            registry.Register(
                "MSG1", "A", "B",
                int.MaxValue - 5,
                "FI", "NO", 5));

        Assert.AreEqual(0, registry.Count);

        // Epäonnistunut rekisteröinti ei
        // kuluta viestitunnistetta.
        var window = registry.Register(
            "MSG1", "A", "B",
            0, "FI", "FI", 5);

        Assert.AreEqual(7, window.DeadlineDay);
        Assert.AreEqual(1, registry.Count);
    }
}
