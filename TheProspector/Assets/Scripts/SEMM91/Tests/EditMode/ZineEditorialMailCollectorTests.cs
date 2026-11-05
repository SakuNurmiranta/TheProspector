
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class ZineEditorialMailCollectorTests
{
    private sealed class Fixture : IDisposable
    {
        public readonly ConfigurableCirculationScenario Scenario;
        public readonly ZineEditorialCycle Zine;
        public readonly ZineEditorialMailCollector Collector;

        public Fixture()
        {
            var definition = new CirculationWorldDefinition();

            definition.AddNode("A", "FI");
            definition.AddNode("E", "FI");
            definition.AddNode("B", "FI");
            definition.AddNode("X", "FI");
            definition.SetSeed("A", "D1");

            Scenario = new ConfigurableCirculationScenario(
                definition);

            Scenario.Network.AddConnection(
                "AE", "A", "E",
                CommunicationLayer.Interpersonal,
                "personal");

            Scenario.Network.AddConnection(
                "AB", "A", "B",
                CommunicationLayer.Interpersonal,
                "personal");

            Scenario.Network.LearnAddress(
                "A", "E", "ADDR-E");

            Scenario.Network.LearnAddress(
                "A", "B", "ADDR-B");

            Zine = new ZineEditorialCycle(
                "Z1", "E", true, Scenario.Clock);

            Collector = new ZineEditorialMailCollector(
                Zine, Scenario.Exchange);
        }

        public void Send(
            string id,
            string recipient = "E",
            int? thumb = null,
            string address = null,
            bool? profile = null,
            string skepticismTarget = null)
        {
            var route = Scenario.Network
                .GetOutgoingConnections(
                    "A", CommunicationLayer.Interpersonal)
                .Single(c => c.TargetNodeId == recipient);

            var package = new MessagePackage(
                id,
                route,
                feedbackProduct: thumb.HasValue
                    ? Scenario.GetDemo("D1")
                    : null,
                thumb: thumb,
                addressNoticeAddressId: address,
                declaredPublicProfile: profile,
                skepticismTargetNodeId: skepticismTarget);

            Scenario.Exchange.Send(
                "SHIP-" + id,
                package,
                "FI", "FI", 5);
        }

        public bool Open(string recipient = "E")
        {
            return Scenario.Exchange.TryOpenNext(
                recipient, out _, out _);
        }

        public void Dispose()
        {
            Collector.Dispose();
        }
    }

    [Test]
    public void Editorial_DeliveryAloneDoesNotCollectOpinion()
    {
        using (var f = new Fixture())
        {
            f.Send("M1", thumb: +1);

            f.Scenario.Clock.AdvanceDays(2);

            Assert.AreEqual(0, f.Zine.PendingCount);
            Assert.AreEqual(0, f.Collector.CapturedPackages);

            Assert.IsTrue(f.Open());

            Assert.AreEqual(1, f.Zine.PendingCount);
            Assert.AreEqual(1, f.Collector.CapturedPackages);

            f.Scenario.Clock.AdvanceDays(28);

            var entry = f.Zine.Issues[0].Entries.Single();

            Assert.AreEqual(
                ZineEditorialEntryKind.ProductOpinion,
                entry.Kind);

            Assert.AreEqual("A", entry.SubjectNodeId);
            Assert.AreEqual("D1", entry.ProductId);
            Assert.AreEqual(true, entry.Opinion);
        }
    }

    [Test]
    public void Editorial_SelfDeclaredAddressAndProfileAreCollected()
    {
        using (var f = new Fixture())
        {
            f.Send(
                "M2",
                address: "ADDR-A",
                profile: false);

            f.Scenario.Clock.AdvanceDays(2);

            Assert.IsTrue(f.Open());

            var issueDay = 30 - f.Scenario.Clock.CurrentDay;

            f.Scenario.Clock.AdvanceDays(issueDay);

            var entry = f.Zine.Issues[0].Entries.Single();

            Assert.AreEqual(
                ZineEditorialEntryKind.Address,
                entry.Kind);

            Assert.AreEqual("A", entry.SubjectNodeId);
            Assert.AreEqual("ADDR-A", entry.AddressId);
            Assert.AreEqual(false,
                entry.SelfDeclaredPublicProfile);
        }
    }

    [Test]
    public void Editorial_SkepticismClaimIsAttributedToSender()
    {
        using (var f = new Fixture())
        {
            f.Send(
                "M3",
                skepticismTarget: "X");

            f.Scenario.Clock.AdvanceDays(2);
            Assert.IsTrue(f.Open());

            Assert.AreEqual(1, f.Zine.PendingCount);

            f.Scenario.Clock.AdvanceDays(28);

            var entry = f.Zine.Issues[0].Entries.Single();

            Assert.AreEqual(
                ZineEditorialEntryKind.Skepticism,
                entry.Kind);

            Assert.AreEqual("A", entry.SubjectNodeId);
            Assert.AreEqual("X", entry.TargetNodeId);

            // Toimituksen kuulema väite ei muuta
            // toimittajan omaa skeptisyyttä.
            Assert.AreEqual(0,
                f.Scenario.Skepticism.Get(
                    "E", "X").Total);
        }
    }

    [Test]
    public void Editorial_UnrelatedMailIsNotCollected()
    {
        using (var f = new Fixture())
        {
            f.Send("M4", recipient: "B", thumb: -1);

            f.Scenario.Clock.AdvanceDays(2);

            Assert.IsTrue(f.Open("B"));

            Assert.AreEqual(0, f.Zine.PendingCount);
            Assert.AreEqual(0, f.Collector.CapturedPackages);
        }
    }

    [Test]
    public void Editorial_LateOpeningBelongsToNextIssue()
    {
        using (var f = new Fixture())
        {
            f.Send("M5", thumb: -1);

            // Kirje saapuu mutta jää avaamatta.
            f.Scenario.Clock.AdvanceDays(30);

            Assert.AreEqual(1, f.Zine.Issues.Count);
            Assert.AreEqual(0,
                f.Zine.Issues[0].Entries.Count);

            Assert.IsTrue(f.Open());

            Assert.AreEqual(1, f.Zine.PendingCount);

            f.Scenario.Clock.AdvanceDays(30);

            Assert.AreEqual(2, f.Zine.Issues.Count);
            Assert.AreEqual(0,
                f.Zine.Issues[0].Entries.Count);

            var entry = f.Zine.Issues[1].Entries.Single();

            Assert.AreEqual(false, entry.Opinion);
            Assert.AreEqual(60,
                f.Zine.Issues[1].PublishedDay);
        }
    }

    [Test]
    public void Editorial_DisposeStopsCollection()
    {
        using (var f = new Fixture())
        {
            f.Send("M6", thumb: +1);

            f.Collector.Dispose();

            f.Scenario.Clock.AdvanceDays(2);

            Assert.IsTrue(f.Open());

            Assert.AreEqual(0, f.Zine.PendingCount);
            Assert.AreEqual(0, f.Collector.CapturedPackages);
        }
    }
}
