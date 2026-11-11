using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible9216SixAxisPhysicalSelectionTests
{
    private static ConfigurableCirculationScenario Create(
        int matchingAxes,
        bool outgoingFromB = true)
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddNode("C", "FI");

        definition.AddConnection("AB", "A", "B");

        if (outgoingFromB)
            definition.AddConnection("BC", "B", "C");

        definition.MarkKnownMissing("A", "B");
        definition.MarkKnownMissing("B", "C");

        definition.SetSeed("A", "D1");

        var scenario = new ConfigurableCirculationScenario(
            definition,
            listeningDays: 2);

        var catalog = new SixAxisProfileCatalog();

        catalog.AddProfile(
            SixAxisProfileRole.PersonalPreference,
            "B",
            new SixAxisProfile((1 << matchingAxes) - 1));

        catalog.AddProfile(
            SixAxisProfileRole.DemoAtmosphere,
            "D1",
            new SixAxisProfile(63));

        scenario.EnableSixAxisListening(catalog);

        return scenario;
    }

    [Test]
    public void AllSevenMatchingCounts_ProduceSpecifiedReaction()
    {
        for (int matches = 0; matches <= 6; matches++)
        {
            var expected = matches >= 4
                ? ListeningReaction.SupportAndCopy
                : matches >= 2
                    ? ListeningReaction.OpposeAndTransfer
                    : ListeningReaction.OpposeAndShelve;

            Assert.AreEqual(
                expected,
                SixAxisListeningReactionSelector
                    .FromMatchingAxes(matches),
                "MatchingAxes=" + matches);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SixAxisListeningReactionSelector
                .FromMatchingAxes(-1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SixAxisListeningReactionSelector
                .FromMatchingAxes(7));
    }

    [Test]
    public void FourMatches_CopiesAfterListening()
    {
        var scenario = Create(4);
        scenario.World.AdvanceDays(4);

        var decision = scenario.GetReactionPolicy("B")
            .Decisions.Single();

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            decision.Reaction);

        Assert.AreEqual(
            ListeningReactionSelectionSource.SixAxisProfile,
            decision.Source);

        Assert.AreEqual(4, decision.MatchingAxes);

        Assert.AreEqual(
            1, scenario.GetPolicy("B").Dispatched.Count);

        Assert.AreEqual(0, scenario.Control.Transfers.Count);
        Assert.AreEqual(0, scenario.Control.Shelves.Count);
    }

    [Test]
    public void TwoMatches_TransfersOriginalCopyWithNegativeThumb()
    {
        var scenario = Create(2);
        scenario.World.AdvanceDays(4);

        var decision = scenario.GetReactionPolicy("B")
            .Decisions.Single();

        Assert.AreEqual(
            ListeningReaction.OpposeAndTransfer,
            decision.Reaction);

        Assert.AreEqual(2, decision.MatchingAxes);
        Assert.AreEqual(1, scenario.Control.Transfers.Count);
        Assert.AreEqual(0, scenario.Control.Shelves.Count);
        Assert.AreEqual(0, scenario.GetPolicy("B").Dispatched.Count);

        Assert.AreSame(
            scenario.GetSeedWave("D1")[0].Copy,
            scenario.Control.Transfers[0].Copy);

        Assert.AreEqual(
            -1,
            scenario.Control.Transfers[0]
                .Shipment.Package.Thumb);
    }

    [Test]
    public void OneMatch_ShelvesAndSendsNegativeNotice()
    {
        var scenario = Create(1);
        scenario.World.AdvanceDays(4);

        var decision = scenario.GetReactionPolicy("B")
            .Decisions.Single();

        Assert.AreEqual(
            ListeningReaction.OpposeAndShelve,
            decision.Reaction);

        Assert.AreEqual(1, decision.MatchingAxes);
        Assert.AreEqual(1, scenario.Control.Shelves.Count);
        Assert.AreEqual(0, scenario.Control.Transfers.Count);
        Assert.AreEqual(0, scenario.GetPolicy("B").Dispatched.Count);

        Assert.AreEqual(1, scenario.HardControl.Notices.Count);
        Assert.AreEqual(
            -1,
            scenario.HardControl.Notices[0]
                .Shipment.Package.Thumb);
    }

    [Test]
    public void ExplicitReaction_OverridesSixAxisProfile()
    {
        var scenario = Create(0);

        scenario.ReactionRules.Set(
            "B", "D1", ListeningReaction.SupportAndCopy);

        scenario.World.AdvanceDays(4);

        var decision = scenario.GetReactionPolicy("B")
            .Decisions.Single();

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            decision.Reaction);

        Assert.AreEqual(
            ListeningReactionSelectionSource.ExplicitRule,
            decision.Source);

        Assert.IsNull(decision.MatchingAxes);
        Assert.AreEqual(1, scenario.GetPolicy("B").Dispatched.Count);
        Assert.AreEqual(0, scenario.Control.Shelves.Count);
    }

    [Test]
    public void MissingProfiles_UseExistingDefaultReaction()
    {
        var scenario = Create(0);

        var catalog = new SixAxisProfileCatalog();
        scenario.EnableSixAxisListening(catalog);

        scenario.World.AdvanceDays(4);

        var decision = scenario.GetReactionPolicy("B")
            .Decisions.Single();

        Assert.AreEqual(
            ListeningReaction.SupportAndCopy,
            decision.Reaction);

        Assert.AreEqual(
            ListeningReactionSelectionSource.DefaultRule,
            decision.Source);

        Assert.IsNull(decision.MatchingAxes);
    }

    [Test]
    public void SoftControlWithoutRoute_WaitsInsteadOfShelving()
    {
        var scenario = Create(3, outgoingFromB: false);

        scenario.World.AdvanceDays(4);

        var policy = scenario.GetReactionPolicy("B");

        Assert.AreEqual(
            ListeningReaction.OpposeAndTransfer,
            policy.Decisions.Single().Reaction);

        Assert.AreEqual(1, policy.PendingTransferCount);
        Assert.AreEqual(0, scenario.Control.Transfers.Count);
        Assert.AreEqual(0, scenario.Control.Shelves.Count);

        scenario.Network.LearnAddress(
            "B", "C", "ADDR-C");
        
        scenario.Network.AddConnection(
            "BC-LATER",
            "B",
            "C",
            CommunicationLayer.Interpersonal,
            "personal");

        scenario.World.AdvanceDay();

        Assert.AreEqual(0, policy.PendingTransferCount);
        Assert.AreEqual(1, scenario.Control.Transfers.Count);
        Assert.AreEqual(
            "C",
            scenario.Control.Transfers[0].RecipientNodeId);
    }

    [Test]
    public void ConfiguredSession_UsesAestheticsForPhysicalDecisions()
    {
        var session = DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            },
            fieldDemoIntervalDays: 14);

        session.AdvanceDays(30);

        var decisions = session.Scenario.NodeIds
            .SelectMany(id =>
                session.Scenario.GetReactionPolicy(id).Decisions)
            .ToList();

        Assert.Greater(decisions.Count, 0);

        foreach (var decision in decisions)
        {
            Assert.AreEqual(
                ListeningReactionSelectionSource.SixAxisProfile,
                decision.Source);

            var personal = session.AestheticProfiles.GetProfile(
                SixAxisProfileRole.PersonalPreference,
                decision.NodeId);

            var demo = session.AestheticProfiles.GetProfile(
                SixAxisProfileRole.DemoAtmosphere,
                decision.ProductId);

            int matches = personal.MatchingAxes(demo);

            Assert.AreEqual(matches, decision.MatchingAxes);

            Assert.AreEqual(
                SixAxisListeningReactionSelector
                    .FromMatchingAxes(matches),
                decision.Reaction);
        }
    }

    [Test]
    public void DefaultSession_RemainsIndependentOfAesthetics()
    {
        var session = DemoSession.CreateDefault(
            new Random(9216));

        Assert.IsNull(session.AestheticProfiles);

        session.AdvanceDays(12);

        foreach (string nodeId in session.Scenario.NodeIds)
        {
            foreach (var decision in session.Scenario
                         .GetReactionPolicy(nodeId).Decisions)
            {
                Assert.AreEqual(
                    ListeningReaction.SupportAndCopy,
                    decision.Reaction);

                Assert.AreEqual(
                    ListeningReactionSelectionSource.DefaultRule,
                    decision.Source);
            }
        }
    }
}