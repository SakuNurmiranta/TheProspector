using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible92184CommunicationArcTests
{
    private sealed class Fixture : IDisposable
    {
        public readonly ConfigurableCirculationScenario Scenario;
        public readonly ZineFirstContactPostalService Postal;

        public Fixture()
        {
            var definition = new CirculationWorldDefinition();

            definition.AddNode("A", "FI");
            definition.AddNode("B", "SE");
            definition.SetSeed("A", "D1");

            Scenario = new ConfigurableCirculationScenario(
                definition);

            // A knows B's published address, but there
            // is no established interpersonal route.
            Scenario.Network.LearnAddress(
                "A", "B", "ADDR-B");

            Postal = new ZineFirstContactPostalService(
                Scenario.Network,
                Scenario.Clock,
                new ZineContactOutreachPolicy(
                    Scenario.Network,
                    new ZineIssueKnowledgeRegistry(
                        Scenario.Network)),
                Scenario.Exchange,
                (sender, target) =>
                    sender == "A" && target == "B");
        }

        public PostalShipment Send(string id = "INTRO")
        {
            var copy = Scenario.Inventory.RecordCopy(
                Scenario.Original.Id,
                "COPY:" + id,
                "A",
                "A");

            Assert.IsTrue(Postal.TrySend(
                "A", false, "B", copy, id,
                out var shipment));

            return shipment;
        }

        public BacklogItem Open(string id = "INTRO")
        {
            Scenario.Clock.AdvanceDays(
                PostalTimingRules.InternationalDays);

            Assert.IsTrue(
                Scenario.Exchange.TryOpenNext(
                    "B", out _, out var opened));

            Assert.AreEqual(id, opened.Package.Id);
            return opened;
        }

        public CommunicationArcProjectionResult Visible()
        {
            return CommunicationArcProjection.Build(
                Scenario.Network,
                Scenario.NodeIds,
                Scenario.CommunicationArcs);
        }

        public void Dispose()
        {
            Postal.Dispose();
        }
    }

    [Test]
    public void FirstContact_IsRecordedAtDispatch()
    {
        using (var f = new Fixture())
        {
            var shipment = f.Send();

            var events = f.Scenario.CommunicationArcs.Events;

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("INTRO", events[0].PackageId);
            Assert.AreEqual(shipment.Id, events[0].ShipmentId);
            Assert.AreEqual("A", events[0].SourceNodeId);
            Assert.AreEqual("B", events[0].TargetNodeId);
            Assert.AreEqual(0, events[0].SentDay);

            // A new visual direction exists immediately.
            Assert.AreEqual(1, f.Visible().Edges.Count);
            Assert.AreEqual("A",
                f.Visible().Edges[0].SourceNodeId);
            Assert.AreEqual("B",
                f.Visible().Edges[0].TargetNodeId);

            // The old usable-route mechanism is unchanged.
            Assert.AreEqual(0,
                f.Scenario.Network.GetOutgoingConnections(
                    "A",
                    CommunicationLayer.Interpersonal).Count);
        }
    }

    [Test]
    public void Reply_CreatesReverseEventWhenSent()
    {
        using (var f = new Fixture())
        {
            f.Send();
            var opened = f.Open();

            var reply = f.Postal.ReplyWithAddress(
                opened,
                "NEGATIVE-REPLY",
                feedbackProduct: f.Scenario.Demo,
                thumb: -1);

            var events = f.Scenario.CommunicationArcs.Events;

            Assert.AreEqual(2, events.Count);
            Assert.AreEqual("B", events[1].SourceNodeId);
            Assert.AreEqual("A", events[1].TargetNodeId);
            Assert.AreEqual("INTRO",
                events[1].ReplyToPackageId);
            Assert.AreEqual(5, events[1].SentDay);

            Assert.AreEqual(
                PostalShipmentStatus.InTransit,
                reply.Status);

            // GUI has both directions before the reply
            // reaches A.
            Assert.AreEqual(2, f.Visible().Edges.Count);

            // But existing route establishment still
            // waits for delivery.
            Assert.AreEqual(0,
                f.Scenario.Network.GetOutgoingConnections(
                    "B",
                    CommunicationLayer.Interpersonal).Count);

            f.Scenario.Clock.AdvanceDays(
                PostalTimingRules.InternationalDays);

            // Established routes must not add duplicate
            // visual arrows.
            Assert.AreEqual(2, f.Visible().Edges.Count);
            Assert.AreEqual(2,
                f.Scenario.CommunicationArcs.Events.Count);
        }
    }

    [Test]
    public void UnansweredLetter_KeepsOnlyOutboundHistory()
    {
        using (var f = new Fixture())
        {
            f.Send();
            f.Scenario.Clock.AdvanceDays(20);

            Assert.AreEqual(1,
                f.Scenario.CommunicationArcs.Events.Count);

            Assert.AreEqual(1, f.Visible().Edges.Count);

            Assert.IsFalse(
                f.Scenario.CommunicationArcs.Events.Any(
                    e => e.SourceNodeId == "B" &&
                         e.TargetNodeId == "A"));
        }
    }

    [Test]
    public void RepeatedMessages_AreSeparateEventInstances()
    {
        using (var f = new Fixture())
        {
            var network = f.Scenario.Network;

            network.AddConnection(
                "AB", "A", "B",
                CommunicationLayer.Interpersonal,
                "personal");

            var route = network.GetOutgoingConnections(
                "A",
                CommunicationLayer.Interpersonal).Single();

            for (int i = 1; i <= 2; i++)
            {
                var package = new MessagePackage(
                    "MSG-" + i,
                    route,
                    addressNoticeAddressId: "ADDR-A");

                f.Scenario.Exchange.Send(
                    "SHIP-" + i,
                    package,
                    "FI",
                    "SE",
                    5);
            }

            var events = f.Scenario.CommunicationArcs.Events;

            Assert.AreEqual(2, events.Count);
            Assert.AreNotSame(events[0], events[1]);
            Assert.AreNotEqual(
                events[0].PackageId,
                events[1].PackageId);

            Assert.IsTrue(events.All(
                e => e.SourceNodeId == "A" &&
                     e.TargetNodeId == "B"));

            // Two historical events, one visible
            // directional connection.
            Assert.AreEqual(1, f.Visible().Edges.Count);
        }
    }

    [Test]
    public void ReturnedLetter_RemainsInCommunicationHistory()
    {
        using (var f = new Fixture())
        {
            var network = f.Scenario.Network;

            network.AddAddress("ADDR-B-NEW", "SE");
            network.MoveNode("B", "ADDR-B-NEW");

            var shipment = f.Send();

            f.Scenario.Clock.AdvanceDays(
                PostalTimingRules.InternationalDays * 2);

            Assert.AreEqual(
                PostalShipmentStatus.Returned,
                shipment.Status);

            var history = f.Scenario.CommunicationArcs.Events;

            Assert.AreEqual(1, history.Count);
            Assert.AreEqual(
                PostalShipmentStatus.Returned,
                history[0].Status);

            Assert.AreEqual("A",
                history[0].SourceNodeId);
            Assert.AreEqual("B",
                history[0].TargetNodeId);

            Assert.AreEqual(1, f.Visible().Edges.Count);
        }
    }
}