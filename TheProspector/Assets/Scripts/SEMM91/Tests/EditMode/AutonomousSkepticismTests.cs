
using NUnit.Framework;
using SEMM91.Domain;

public class AutonomousSkepticismTests
{
    private static ConfigurableCirculationScenario Create(
        ListeningReaction bReaction =
            ListeningReaction.SupportAndCopy,
        ListeningReaction cReaction =
            ListeningReaction.SupportAndCopy,
        bool twoDemos = false)
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddNode("C", "FI");

        definition.AddConnection("AB", "A", "B");
        definition.AddConnection("BC", "B", "C");

        definition.MarkKnownMissing("A", "B");
        definition.MarkKnownMissing("B", "C");

        definition.SetSeed("A", "D1");

        if (twoDemos)
            definition.AddSeed("A", "D2");

        var rules = new NodeReactionRules();

        rules.Set("B", "D1", bReaction);
        rules.Set("C", "D1", cReaction);

        if (twoDemos)
        {
            rules.Set(
                "B", "D2",
                ListeningReaction.SupportAndCopy);
        }


        return new ConfigurableCirculationScenario(
            definition,
            listeningDays: 2,
            reactionRules: rules,
            explicitPositiveReferrals: true);

    }

    [Test]
    public void Skepticism_SeedCarriesExplicitPositiveReferral()
    {
        var s = Create();

        var seed = s.GetSeedWave("D1")[0];

        Assert.AreEqual(
            1, seed.Shipment.Package.Thumb);

        Assert.AreEqual(0,
            s.Skepticism.Get("B", "A").Total);

        Assert.AreEqual(0, s.World.CurrentDay);
    }

    [Test]
    public void Skepticism_ReceivingAndListeningDoNotPenalize()
    {
        var s = Create(
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(3);

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.AreEqual(0,
            s.Skepticism.Get("B", "A").BadReferrals);

        Assert.AreEqual(0,
            s.GetReactionPolicy("B").Decisions.Count);
    }

    [Test]
    public void Skepticism_ShelvingPositiveReferralPenalizesSource()
    {
        var s = Create(
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(4);

        Assert.AreEqual(1, s.Control.Shelves.Count);

        Assert.AreEqual(1,
            s.Skepticism.Get("B", "A").BadReferrals);

        Assert.AreEqual(1,
            s.Skepticism.Get("B", "A").Total);
    }

    [Test]
    public void Skepticism_PenaltyDoesNotReverseDirection()
    {
        var s = Create(
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(8);

        Assert.AreEqual(1,
            s.Skepticism.Get("B", "A").Total);

        Assert.AreEqual(0,
            s.Skepticism.Get("A", "B").Total);

        Assert.AreEqual(0,
            s.Skepticism.Get("C", "B").Total);
    }

    [Test]
    public void Skepticism_SupportDoesNotPenalizeSource()
    {
        var s = Create(
            ListeningReaction.SupportAndCopy);

        s.World.AdvanceDays(10);

        Assert.AreEqual(1,
            s.GetPolicy("B").Dispatched.Count);

        Assert.AreEqual(0,
            s.Skepticism.Get("B", "A").BadReferrals);
    }

    [Test]
    public void Skepticism_SoftControlDoesNotPenalizeYet()
    {
        var s = Create(
            ListeningReaction.OpposeAndTransfer);

        s.World.AdvanceDays(10);

        Assert.AreEqual(1, s.Control.Transfers.Count);

        Assert.AreEqual(0,
            s.Skepticism.Get("B", "A").BadReferrals);
    }

    [Test]
    public void Skepticism_SecondNodeCanPenalizeItsOwnRecommender()
    {
        var s = Create(
            ListeningReaction.SupportAndCopy,
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(8);

        Assert.AreEqual(1, s.Control.Shelves.Count);
        Assert.AreEqual("C", s.Control.Shelves[0].NodeId);

        Assert.AreEqual(0,
            s.Skepticism.Get("B", "A").BadReferrals);

        Assert.AreEqual(1,
            s.Skepticism.Get("C", "B").BadReferrals);

        Assert.AreEqual(0,
            s.Skepticism.Get("C", "A").BadReferrals);
    }

    [Test]
    public void Skepticism_NegativeTransferDoesNotBecomeBadReferral()
    {
        var s = Create(
            ListeningReaction.OpposeAndTransfer,
            ListeningReaction.OpposeAndShelve);

        s.World.AdvanceDays(8);

        Assert.AreEqual(1, s.Control.Transfers.Count);
        Assert.AreEqual(1, s.Control.Shelves.Count);

        Assert.AreEqual(-1,
            s.Stances.GetLastThumb("C", "B", "D1"));

        Assert.AreEqual(0,
            s.Skepticism.Get("C", "B").BadReferrals);

        Assert.AreEqual(0,
            s.Skepticism.Get("B", "A").BadReferrals);
    }

    [Test]
    public void Skepticism_DifferentProductsAreIndependent()
    {
        var s = Create(
            ListeningReaction.OpposeAndShelve,
            twoDemos: true);

        s.World.AdvanceDays(6);

        Assert.AreEqual(
            ListeningReaction.OpposeAndShelve,
            s.GetReactionPolicy("B").Decisions[0].Reaction);

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            s.GetReactionPolicy("B").Decisions[1].Reaction);

        Assert.AreEqual(1,
            s.Skepticism.Get("B", "A").BadReferrals);

        Assert.AreEqual(1,
            s.Control.Shelves.Count);
    }

    [Test]
    public void Skepticism_LongAndSteppedRunsDoNotDuplicatePenalty()
    {
        var a = Create(
            ListeningReaction.SupportAndCopy,
            ListeningReaction.OpposeAndShelve);

        var b = Create(
            ListeningReaction.SupportAndCopy,
            ListeningReaction.OpposeAndShelve);

        a.World.AdvanceDays(30);

        for (int i = 0; i < 30; i++)
            b.World.AdvanceDay();

        Assert.AreEqual(30, a.World.CurrentDay);
        Assert.AreEqual(30, b.World.CurrentDay);

        Assert.AreEqual(1,
            a.Skepticism.Get("C", "B").Total);

        Assert.AreEqual(1,
            b.Skepticism.Get("C", "B").Total);

        Assert.AreEqual(0,
            a.Skepticism.Get("B", "A").Total);

        Assert.AreEqual(0,
            b.Skepticism.Get("B", "A").Total);
    }
}
