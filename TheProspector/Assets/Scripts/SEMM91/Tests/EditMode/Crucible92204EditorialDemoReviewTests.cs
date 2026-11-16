
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible92204EditorialDemoReviewTests
{
    private sealed class Fixture : IDisposable
    {
        public readonly ConfigurableCirculationScenario Scenario;
        public readonly ZineEditorialCycle Editorial;
        public readonly ZineEditorialDemoReviewCollector Reviews;

        private int _serial;

        public Fixture(
            int demoMask = 63,
            int editorialMask = 63,
            bool includeDemoProfile = true)
        {
            var definition = new CirculationWorldDefinition();

            definition.AddNode("A", "FI");
            definition.AddNode("E", "FI");
            definition.SetSeed("A", "D1");

            Scenario = new ConfigurableCirculationScenario(
                definition,
                listeningDays: 2);

            Scenario.Network.AddConnection(
                "AE", "A", "E",
                CommunicationLayer.Interpersonal,
                "personal");

            Scenario.Network.LearnAddress(
                "A", "E", "ADDR-E");

            var catalog = new SixAxisProfileCatalog();

            catalog.AddProfile(
                SixAxisProfileRole.EditorialPreference,
                "E",
                new SixAxisProfile(editorialMask));

            // Deliberately different from the demo.
            // Editorial publishing must not use this.
            catalog.AddProfile(
                SixAxisProfileRole.PersonalPreference,
                "A",
                new SixAxisProfile(0));

            if (includeDemoProfile)
            {
                catalog.AddProfile(
                    SixAxisProfileRole.DemoAtmosphere,
                    "D1",
                    new SixAxisProfile(demoMask));
            }

            Editorial = new ZineEditorialCycle(
                "Z1", "E", true,
                Scenario.Clock,
                publicationIntervalDays: 10);

            Reviews = new ZineEditorialDemoReviewCollector(
                Editorial,
                Scenario,
                catalog);
        }

        public void SendDemo()
        {
            int id = ++_serial;

            var source = Scenario.GetOriginal("D1");

            var copy = Scenario.Inventory.RecordCopy(
                source.Id,
                "REVIEW-COPY:" + id,
                "A", "A");

            var route = Scenario.Network
                .GetOutgoingConnections(
                    "A", CommunicationLayer.Interpersonal)
                .Single(c => c.TargetNodeId == "E");

            var package = new MessagePackage(
                "REVIEW-LETTER:" + id,
                route,
                cassetteCopy: copy,
                addressNoticeAddressId: "ADDR-A");

            var shipment = Scenario.Exchange.Send(
                "REVIEW-SHIPMENT:" + id,
                package,
                "FI", "FI",
                replyAllowanceDays: 5);

            Scenario.CopyIndex.RegisterTransfer(
                shipment,
                copy);
        }

        public void SendFeedbackOnly()
        {
            var route = Scenario.Network
                .GetOutgoingConnections(
                    "A", CommunicationLayer.Interpersonal)
                .Single(c => c.TargetNodeId == "E");

            var package = new MessagePackage(
                "FEEDBACK-ONLY",
                route,
                feedbackProduct: Scenario.GetDemo("D1"),
                thumb: +1);

            Scenario.Exchange.Send(
                "FEEDBACK-SHIPMENT",
                package,
                "FI", "FI",
                replyAllowanceDays: 5);
        }

        public void Dispose()
        {
            Reviews.Dispose();
            Editorial.Stop();
        }
    }

    [Test]
    public void DeliveredAndOpenedDemo_WaitsForListening()
    {
        using (var f = new Fixture())
        {
            f.SendDemo();

            // Physical arrival without opening.
            f.Scenario.Clock.AdvanceDays(2);

            Assert.AreEqual(0,
                f.Reviews.CapturedDemoLetters);
            Assert.AreEqual(0, f.Editorial.PendingCount);

            // The simulation opens the delivered letter.
            f.Scenario.World.AdvanceDay();

            Assert.AreEqual(1,
                f.Reviews.CapturedDemoLetters);
            Assert.AreEqual(1,
                f.Reviews.PendingReviewCount);

            Assert.AreEqual(0,
                f.Reviews.AcceptedReviewCount);
            Assert.AreEqual(0, f.Editorial.PendingCount);

            // The current listening duration is two days.
            f.Scenario.World.AdvanceDays(2);

            Assert.AreEqual(
                ListeningPhase.Completed,
                f.Scenario.GetListeningStatus(
                    "E", "D1").Phase);

            Assert.AreEqual(1,
                f.Reviews.AcceptedReviewCount);
            Assert.AreEqual(0,
                f.Reviews.PendingReviewCount);

            Assert.AreEqual(1,
                f.Editorial.PendingCount);
        }
    }

    [Test]
    public void ReviewedDemo_UsesAtmosphereNotSenderPreference()
    {
        using (var f = new Fixture(
                   demoMask: 42,
                   editorialMask: 42))
        {
            f.SendDemo();

            f.Scenario.World.AdvanceDays(10);

            var issue = f.Editorial.Issues.Single();

            var ad = issue.Entries.Single(
                e => e.Kind ==
                     ZineEditorialEntryKind.DemoAdvertisement);

            Assert.AreEqual("A", ad.SubjectNodeId);
            Assert.AreEqual("D1", ad.ProductId);
            Assert.AreEqual(42, ad.PublishedAxisMask);

            // Issue content must not leak forward.
            f.Scenario.World.AdvanceDays(10);

            Assert.AreEqual(2, f.Editorial.Issues.Count);

            Assert.IsFalse(
                f.Editorial.Issues[1].Entries.Any(
                    e => e.Kind ==
                         ZineEditorialEntryKind
                             .DemoAdvertisement));
        }
    }

    [Test]
    public void EditorialMismatch_RejectsAdvertisement()
    {
        using (var f = new Fixture(
                   demoMask: 0,
                   editorialMask: 63))
        {
            f.SendDemo();

            f.Scenario.World.AdvanceDays(10);

            Assert.AreEqual(1,
                f.Reviews.ReviewedDemoCount);

            Assert.AreEqual(0,
                f.Reviews.AcceptedReviewCount);

            Assert.AreEqual(1,
                f.Reviews.RejectedReviewCount);

            Assert.IsFalse(
                f.Editorial.Issues[0].Entries.Any(
                    e => e.Kind ==
                         ZineEditorialEntryKind
                             .DemoAdvertisement));
        }
    }

    [Test]
    public void FeedbackOnly_DoesNotBecomeDemoAdvertisement()
    {
        using (var f = new Fixture())
        {
            f.SendFeedbackOnly();

            f.Scenario.World.AdvanceDays(10);

            Assert.AreEqual(0,
                f.Reviews.CapturedDemoLetters);

            Assert.AreEqual(0,
                f.Reviews.ReviewedDemoCount);

            Assert.AreEqual(0,
                f.Editorial.Issues[0].Entries.Count);
        }
    }

    [Test]
    public void RepeatedCopy_DoesNotRepublishSameSenderDemo()
    {
        using (var f = new Fixture())
        {
            f.SendDemo();
            f.Scenario.World.AdvanceDays(5);

            f.SendDemo();
            f.Scenario.World.AdvanceDays(5);

            Assert.AreEqual(1,
                f.Reviews.AcceptedReviewCount);

            Assert.AreEqual(1,
                f.Editorial.Issues[0].Entries.Count(
                    e => e.Kind ==
                         ZineEditorialEntryKind
                             .DemoAdvertisement));
        }
    }

    [Test]
    public void ConfiguredSession_EditorialReviewsAreOptional()
    {
        var parameters = new CirculationInitializationParameters
        {
            RandomSeed = 921
        };

        var baseline = DemoSession.CreateConfigured(
            parameters,
            fieldDemoIntervalDays: 0);

        Assert.IsNull(baseline.EditorialDemoReviews);

        var enabled = DemoSession.CreateConfigured(
            parameters,
            fieldDemoIntervalDays: 0,
            enableRecurringZineIssues: true,
            enableEditorialReviewedDemos: true);

        Assert.IsNotNull(enabled.EditorialDemoReviews);
        Assert.IsNotNull(enabled.RecurringZines);
    }
}
