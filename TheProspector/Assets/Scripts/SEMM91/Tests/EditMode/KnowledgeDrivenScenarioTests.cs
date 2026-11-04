
using NUnit.Framework;
using SEMM91.Domain;

public class KnowledgeDrivenScenarioTests
{
    [Test]
    public void Scenario_InitializesAndDispatchesFirstWave()
    {
        var scenario = new KnowledgeDrivenScenario();

        Assert.AreEqual(3, scenario.Network.NodeCount);
        Assert.AreEqual(0, scenario.Clock.CurrentDay);

        Assert.AreEqual(1, scenario.FirstWave.Count);
        Assert.AreEqual("B",
            scenario.FirstWave[0].RecipientNodeId);

        Assert.AreEqual(0, scenario.SecondWave.Count);
        Assert.IsNull(scenario.ShipBC);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            scenario.ShipAB.Status);
    }

    [Test]
    public void Scenario_DoesNotForwardBeforeArrival()
    {
        var scenario = new KnowledgeDrivenScenario();

        scenario.Clock.AdvanceTo(1);

        Assert.AreEqual(0, scenario.SecondWave.Count);
        Assert.IsNull(scenario.BCCopy);

        Assert.AreEqual(10,
            scenario.Capital.GetCapital("A"));

        Assert.AreEqual(10,
            scenario.Capital.GetCapital("B"));
    }

    [Test]
    public void Scenario_DayTwoSelectsCFromKnowledge()
    {
        var scenario = new KnowledgeDrivenScenario();

        scenario.Clock.AdvanceTo(2);

        // B on avannut A:n viestin.
        Assert.AreEqual(
            true,
            scenario.Known.IsKnownToHold("B", "A", "D1"));

        // B tietää C:n kokoelmasta,
        // ettei D1 kuulu siihen.
        Assert.AreEqual(
            false,
            scenario.Known.IsKnownToHold("B", "C", "D1"));

        Assert.AreEqual(1, scenario.SecondWave.Count);

        Assert.AreEqual(
            "C",
            scenario.SecondWave[0].RecipientNodeId);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            scenario.ShipBC.Status);
    }

    [Test]
    public void Scenario_BDoesNotSendDemoBackToA()
    {
        var scenario = new KnowledgeDrivenScenario();

        scenario.Clock.AdvanceTo(2);

        // B:n molemmat kontaktit A ja C
        // ovat mahdollisia reittejä.
        // A suljetaan pois kokoelmatiedon
        // perusteella.
        Assert.AreEqual(1, scenario.SecondWave.Count);

        Assert.AreEqual(
            "C",
            scenario.SecondWave[0].RecipientNodeId);

        Assert.AreEqual(3, scenario.Clock.PendingCount);
    }

    [Test]
    public void Scenario_FullRunProducesExpectedCapital()
    {
        var scenario = new KnowledgeDrivenScenario();

        scenario.Run();

        Assert.AreEqual(4, scenario.Clock.CurrentDay);

        Assert.AreEqual(15,
            scenario.Capital.GetCapital("A"));

        Assert.AreEqual(15,
            scenario.Capital.GetCapital("B"));

        Assert.AreEqual(10,
            scenario.Capital.GetCapital("C"));

        Assert.AreEqual(2, scenario.AOpeningReward);
        Assert.AreEqual(0, scenario.Clock.PendingCount);
    }

    [Test]
    public void Scenario_FullRunDistributesPhysicalCopies()
    {
        var scenario = new KnowledgeDrivenScenario();

        scenario.Run();

        Assert.AreEqual("A",
            scenario.Original.HolderNodeId);

        Assert.AreEqual("B",
            scenario.ABCopy.HolderNodeId);

        Assert.AreEqual("C",
            scenario.BCCopy.HolderNodeId);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.ShipAB.Status);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.ShipBC.Status);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            scenario.ShipBA.Status);

        Assert.IsTrue(
            scenario.Inventory.HasEverHeldProduct(
                "C", "D1"));
    }

    [Test]
    public void Scenario_UnknownCCollectionPreventsCopy()
    {
        var scenario = new KnowledgeDrivenScenario(
            bKnowsCIsMissing: false);

        scenario.Run();

        Assert.AreEqual(0, scenario.SecondWave.Count);

        Assert.IsNull(scenario.ShipBC);
        Assert.IsNull(scenario.BCCopy);

        // A saa kopiointipalkkion ja +1.
        Assert.AreEqual(15,
            scenario.Capital.GetCapital("A"));

        // B saa vain +1 vastavuoroisuudesta.
        Assert.AreEqual(11,
            scenario.Capital.GetCapital("B"));

        Assert.IsFalse(
            scenario.Inventory.HasEverHeldProduct(
                "C", "D1"));
    }

    [Test]
    public void Scenario_CommunicatedDislikePreventsCopy()
    {
        var scenario = new KnowledgeDrivenScenario(
            cCommunicatedDislike: true);

        scenario.Run();

        // B tietää C:n kokoelmatilanteen.
        Assert.AreEqual(
            false,
            scenario.Known.IsKnownToHold(
                "B", "C", "D1"));

        // Mutta kielteinen viesti estää lähetyksen.
        Assert.AreEqual(0, scenario.SecondWave.Count);
        Assert.IsNull(scenario.ShipBC);

        Assert.AreEqual(11,
            scenario.Capital.GetCapital("B"));
    }

    [Test]
    public void Scenario_KnowledgeChangesThroughCommunication()
    {
        var scenario = new KnowledgeDrivenScenario();

        // B ei vielä tunne A:n D1-historiaa.
        Assert.IsNull(
            scenario.Known.IsKnownToHold(
                "B", "A", "D1"));

        scenario.Clock.AdvanceTo(2);

        Assert.AreEqual(
            true,
            scenario.Known.IsKnownToHold(
                "B", "A", "D1"));

        scenario.Run();

        // C oppii B:n D1-historiasta.
        Assert.AreEqual(
            true,
            scenario.Known.IsKnownToHold(
                "C", "B", "D1"));

        // A oppii B:n D1-historiasta
        // vastauksen avaamisen yhteydessä.
        Assert.AreEqual(
            true,
            scenario.Known.IsKnownToHold(
                "A", "B", "D1"));
    }

    [Test]
    public void Scenario_RunsAreDeterministicAndIdempotent()
    {
        var first = new KnowledgeDrivenScenario();
        var second = new KnowledgeDrivenScenario();

        first.Run();
        second.Clock.AdvanceTo(2);
        second.Run();

        CollectionAssert.AreEqual(
            first.Trace,
            second.Trace);

        Assert.AreEqual(6, first.Trace.Count);

        foreach (var nodeId in new[] { "A", "B", "C" })
        {
            Assert.AreEqual(
                first.Capital.GetCapital(nodeId),
                second.Capital.GetCapital(nodeId));
        }

        first.Run();

        Assert.AreEqual(6, first.Trace.Count);
        Assert.AreEqual(15,
            first.Capital.GetCapital("A"));
        Assert.AreEqual(15,
            first.Capital.GetCapital("B"));
    }
}
