
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class AutonomousZineOutreachTests
{
    private sealed class Fixture
    {
        public readonly ConfigurableCirculationScenario Scenario;
        public readonly ZineContactOutreachPolicy Outreach;
        public readonly ZineFirstContactPostalService Postal;

        private readonly List<CassetteCopy> _available =
            new List<CassetteCopy>();

        public Fixture(bool aProfile = true,
                       bool cProfile = true)
        {
            var definition = new CirculationWorldDefinition();

            definition.AddNode("B", "FI");
            definition.AddNode("A", "FI");
            definition.AddNode("C", "FI");
            definition.AddNode("E", "FI");
            definition.SetSeed("B", "D1");

            Scenario =
                new ConfigurableCirculationScenario(definition);

            var network = Scenario.Network;

            network.AddConnection(
                "EB", "E", "B",
                CommunicationLayer.Insert, "zine");

            var zine = new ZineEditorialCycle(
                "Z1", "E", true, Scenario.Clock);

            var knowledge = new ZineIssueKnowledgeRegistry(
                network);

            var delivery = new ZineIssueInsertDelivery(
                new InsertDistributionRegistry(),
                knowledge);

            _ = new ZinePublicationDistribution(
                zine,
                delivery,
                () => network.GetOutgoingConnections(
                    "E", CommunicationLayer.Insert),
                id => id == "B");

            zine.RecordSelfDeclaredContact(
                "A", "ADDR-A", aProfile);

            zine.RecordSelfDeclaredContact(
                "C", "ADDR-C", cProfile);

            Scenario.Clock.AdvanceDays(30);

            Outreach = new ZineContactOutreachPolicy(
                network, knowledge);

            Postal = new ZineFirstContactPostalService(
                network,
                Scenario.Clock,
                Outreach,
                Scenario.Exchange);
        }

        public void AddAvailableCopy(string id)
        {
            var copy = Scenario.Inventory.RecordCopy(
                Scenario.Original.Id,
                id,
                "B",
                "B");

            _available.Add(copy);
        }

        public CassetteCopy ChooseCopy()
        {
            foreach (var copy in _available)
            {
                if (copy.HolderNodeId == "B")
                    return copy;
            }

            return null;
        }

        public ZineAutonomousContactCycle Start(bool profile = true)
        {
            return new ZineAutonomousContactCycle(
                "B",
                Scenario.Clock,
                Outreach,
                Postal,
                () => profile,
                ChooseCopy);
        }
    }

    [Test]
    public void Autonomous_DoesNotSendBeforeNextDecisionDay()
    {
        var f = new Fixture();
        f.AddAvailableCopy("AUTO-COPY1");

        using (var cycle = f.Start())
        {
            Assert.AreEqual(0, cycle.SentCount);
            Assert.IsFalse(f.Outreach.HasPendingOutreach("B"));

            f.Scenario.Clock.AdvanceDays(1);

            Assert.AreEqual(1, cycle.SentCount);
            Assert.AreEqual("A", cycle.LastTargetNodeId);

            Assert.IsTrue(
                f.Outreach.HasPendingOutreach("B"));

            Assert.AreEqual(0,
                f.Scenario.Network.GetOutgoingConnections(
                    "B",
                    CommunicationLayer.Interpersonal).Count);
        }
    }

    [Test]
    public void Autonomous_DoesNotSendWithoutAvailableCassette()
    {
        var f = new Fixture();

        using (var cycle = f.Start())
        {
            f.Scenario.Clock.AdvanceDays(3);

            Assert.AreEqual(0, cycle.SentCount);

            f.AddAvailableCopy("AUTO-COPY2");
            f.Scenario.Clock.AdvanceDays(1);

            Assert.AreEqual(1, cycle.SentCount);
        }
    }

    [Test]
    public void Autonomous_ProfileMismatchPreventsContact()
    {
        var f = new Fixture(
            aProfile: false,
            cProfile: false);

        f.AddAvailableCopy("AUTO-COPY3");

        using (var cycle = f.Start(profile: true))
        {
            f.Scenario.Clock.AdvanceDays(5);

            Assert.AreEqual(0, cycle.SentCount);
            Assert.IsFalse(
                f.Outreach.HasPendingOutreach("B"));
        }
    }

    [Test]
    public void Autonomous_TimeoutAllowsNextTargetNotRepeat()
    {
        var f = new Fixture();

        f.AddAvailableCopy("AUTO-COPY4");
        f.AddAvailableCopy("AUTO-COPY5");

        using (var cycle = f.Start())
        {
            f.Scenario.Clock.AdvanceDays(1);

            Assert.AreEqual(1, cycle.SentCount);
            Assert.AreEqual("A", cycle.LastTargetNodeId);

            // A ei vastaa. Odotusikkuna sulkeutuu
            // ja seuraavaa kontaktia voidaan kokeilla.
            f.Scenario.Clock.AdvanceDays(12);

            Assert.AreEqual(2, cycle.SentCount);
            Assert.AreEqual("C", cycle.LastTargetNodeId);
        }
    }

    [Test]
    public void Autonomous_DisposeStopsFurtherDecisions()
    {
        var f = new Fixture();

        f.AddAvailableCopy("AUTO-COPY6");

        var cycle = f.Start();
        cycle.Dispose();

        f.Scenario.Clock.AdvanceDays(5);

        Assert.AreEqual(0, cycle.SentCount);
        Assert.IsFalse(f.Outreach.HasPendingOutreach("B"));
    }
}
