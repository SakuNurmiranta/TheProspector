
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible92205EditorialSubmissionTests
{
    private sealed class Fixture : IDisposable
    {
        public readonly ConfigurableCirculationScenario Scenario;
        public readonly ZineEditorialCycle Editorial;
        public readonly ZineIssueKnowledgeRegistry Knowledge;
        public readonly ZineEditorialDemoReviewCollector Reviews;
        public readonly ZineAutonomousEditorialSubmissionCycle Submissions;

        public Fixture(bool supportive = true)
        {
            var definition = new CirculationWorldDefinition();

            definition.AddNode("A", "FI");
            definition.AddNode("E", "FI");

            definition.AddAddress("ADDR-A", "FI");
            definition.AddAddress("ADDR-E", "FI");

            definition.PlaceNode("A", "ADDR-A");
            definition.PlaceNode("E", "ADDR-E");

            definition.SetSeed("A", "D1");

            Scenario = new ConfigurableCirculationScenario(
                definition,
                listeningDays: 2);

            Scenario.ReactionRules.Set(
                "A",
                "D1",
                supportive
                    ? ListeningReaction.SupportAndCopy
                    : ListeningReaction.OpposeAndShelve);

            Scenario.ActivateLocalListening();

            Knowledge = new ZineIssueKnowledgeRegistry(
                Scenario.Network);

            Editorial = new ZineEditorialCycle(
                "TEST-ZINE",
                "E",
                false,
                Scenario.Clock,
                publicationIntervalDays: 10);

            var profiles = new SixAxisProfileCatalog();

            profiles.AddProfile(
                SixAxisProfileRole.EditorialPreference,
                "E",
                new SixAxisProfile(63));

            profiles.AddProfile(
                SixAxisProfileRole.DemoAtmosphere,
                "D1",
                new SixAxisProfile(63));

            Reviews = new ZineEditorialDemoReviewCollector(
                Editorial,
                Scenario,
                profiles);

            Submissions =
                new ZineAutonomousEditorialSubmissionCycle(
                    Scenario,
                    Knowledge,
                    Editorial);
        }

        public void PublishAndRead()
        {
            Scenario.World.AdvanceDays(10);

            Assert.AreEqual(1, Editorial.Issues.Count);

            Assert.IsTrue(
                Knowledge.ReceiveIssue(
                    "A", Editorial.Issues[0]));
        }

        public void Dispose()
        {
            Submissions.Dispose();
            Reviews.Dispose();
            Editorial.Stop();
        }
    }

    [Test]
    public void KnowingAddressWithoutReadingIssue_IsInsufficient()
    {
        using (var f = new Fixture())
        {
            f.Scenario.Network.LearnAddress(
                "A", "E", "ADDR-E");

            f.Scenario.World.AdvanceDays(10);

            Assert.IsFalse(
                f.Submissions.TrySubmit("A"));

            Assert.AreEqual(
                0, f.Submissions.SubmittedCount);
        }
    }

    [Test]
    public void ReadZine_AllowsSupportedPhysicalDemoSubmission()
    {
        using (var f = new Fixture())
        {
            f.PublishAndRead();

            var original =
                f.Scenario.GetOriginal("D1");

            Assert.IsTrue(
                f.Submissions.TrySubmit("A"));

            Assert.AreEqual(
                1, f.Submissions.SubmittedCount);

            // The original stays with A.
            Assert.IsTrue(
                f.Scenario.Inventory.GetHeldCopies("A")
                    .Any(c => c.Id == original.Id));

            Assert.AreEqual(
                0,
                f.Scenario.Network
                    .GetOutgoingConnections(
                        "A",
                        CommunicationLayer.Interpersonal)
                    .Count);

            Assert.AreEqual(
                1, f.Scenario.CopyIndex.Count);
        }
    }

    [Test]
    public void SameSenderCannotSubmitSameDemoTwice()
    {
        using (var f = new Fixture())
        {
            f.PublishAndRead();

            Assert.IsTrue(
                f.Submissions.TrySubmit("A"));

            Assert.IsFalse(
                f.Submissions.TrySubmit("A"));

            Assert.AreEqual(
                1, f.Submissions.SubmittedCount);

            Assert.AreEqual(
                1, f.Scenario.CopyIndex.Count);
        }
    }

    [Test]
    public void RejectedDemo_IsNotSubmitted()
    {
        using (var f = new Fixture(
                   supportive: false))
        {
            f.PublishAndRead();

            Assert.IsFalse(
                f.Submissions.TrySubmit("A"));

            Assert.AreEqual(
                0, f.Submissions.SubmittedCount);
        }
    }

    [Test]
    public void AutonomousSubmission_EntersEditorialReview()
    {
        using (var f = new Fixture())
        {
            f.PublishAndRead();

            // A has read the issue, so the next
            // autonomous day can initiate a submission.
            f.Scenario.World.AdvanceDay();

            Assert.AreEqual(
                1, f.Submissions.SubmittedCount);

            // Two days for domestic post, two days
            // for listening, then publication on day 20.
            f.Scenario.World.AdvanceDays(9);

            Assert.AreEqual(
                1, f.Reviews.CapturedDemoLetters);

            Assert.AreEqual(
                1, f.Reviews.AcceptedReviewCount);

            Assert.AreEqual(
                2, f.Editorial.Issues.Count);

            var advertisement =
                f.Editorial.Issues[1].Entries.Single(
                    e => e.Kind ==
                         ZineEditorialEntryKind
                             .DemoAdvertisement);

            Assert.AreEqual(
                "A", advertisement.SubjectNodeId);

            Assert.AreEqual(
                "D1", advertisement.ProductId);

            Assert.AreEqual(
                63, advertisement.PublishedAxisMask);
        }
    }

    [Test]
    public void ConfiguredAutonomousSubmissions_AreOptional()
    {
        var parameters = new CirculationInitializationParameters
        {
            RandomSeed = 921
        };

        var baseline = DemoSession.CreateConfigured(
            parameters,
            fieldDemoIntervalDays: 0);

        Assert.IsNull(
            baseline.EditorialDemoSubmissions);

        var enabled = DemoSession.CreateConfigured(
            parameters,
            fieldDemoIntervalDays: 0,
            enableRecurringZineIssues: true,
            enableEditorialReviewedDemos: true,
            enableEditorialDemoSubmissions: true);

        Assert.IsNotNull(
            enabled.EditorialDemoSubmissions);

        Assert.IsNotNull(
            enabled.EditorialDemoReviews);
    }
}
