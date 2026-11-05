
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class FieldWaveQuiescenceTests
{
    private static ConfigurableCirculationScenario Create(
        bool sendSeed = true,
        int listeningDays = 2)
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddNode("C", "FI");

        definition.AddConnection("AB", "A", "B");
        definition.AddConnection("AC", "A", "C");

        if (sendSeed)
            definition.MarkKnownMissing("A", "B");

        definition.SetSeed("A", "D1");

        return new ConfigurableCirculationScenario(
            definition,
            listeningDays: listeningDays);
    }

    private static SIRState State(
        ConfigurableCirculationScenario scenario)
    {
        return scenario.FieldSIR.GetFieldState(
            scenario.Field, scenario.Demo);
    }

    [Test]
    public void Quiescence_DoesNotCloseWhileMailIsInTransit()
    {
        var s = Create();
        var wave = s.GetFieldWave("D1");

        Assert.AreEqual(1, s.SeedWave.Count);
        Assert.AreEqual(SIRState.I, State(s));

        s.World.AdvanceDay();

        Assert.AreEqual(SIRState.I, State(s));
        Assert.AreEqual(1, wave.ActiveCopierCount);
        Assert.IsFalse(wave.IsPassivelyDiscoverable);
    }

    [Test]
    public void Quiescence_WaitsForCompletedListening()
    {
        var s = Create();
        var wave = s.GetFieldWave("D1");

        s.World.AdvanceDays(2);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            s.SeedWave[0].Shipment.Status);

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.AreEqual(SIRState.I, State(s));

        s.World.AdvanceDay();

        Assert.AreEqual(SIRState.I, State(s));

        // B:n kahden päivän kuuntelu valmistuu.
        s.World.AdvanceDay();

        Assert.AreEqual(
            ListeningPhase.Completed,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.AreEqual(SIRState.R, State(s));

        Assert.AreEqual(0, wave.ActiveCopierCount);
        Assert.IsTrue(wave.IsPassivelyDiscoverable);

        s.Network.TryGetNode("A", out var a);

        Assert.AreEqual(
            SIRState.R,
            s.FieldSIR.GetNodeState(a, s.Demo));
    }

    [Test]
    public void Quiescence_UnopenedMailPreventsClosure()
    {
        var s = Create();

        // Vain kello etenee. Maailman solmut
        // eivät vielä avaa tai kuuntele postia.
        s.Clock.AdvanceDays(4);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            s.SeedWave[0].Shipment.Status);

        Assert.IsNull(
            s.GetListeningStatus("B", "D1"));

        Assert.AreEqual(SIRState.I, State(s));

        // Maailman päivittäinen toiminta alkaa.
        s.World.AdvanceDay();

        Assert.AreEqual(SIRState.I, State(s));

        s.World.AdvanceDays(2);

        Assert.AreEqual(SIRState.R, State(s));
    }

    [Test]
    public void Quiescence_RespectsLongerListeningTime()
    {
        var s = Create(listeningDays: 5);

        s.World.AdvanceDays(4);

        Assert.AreEqual(SIRState.I, State(s));

        s.World.AdvanceDays(2);

        Assert.AreEqual(SIRState.I, State(s));

        // Kuuntelu alkoi päivänä 2.
        s.World.AdvanceDay();

        Assert.AreEqual(SIRState.R, State(s));
    }

    [Test]
    public void Quiescence_TracksPhysicalPostOutsideCopyService()
    {
        var s = Create();

        s.World.AdvanceDays(2);

        // B:n kuuntelu on kesken.
        Assert.AreEqual(SIRState.I, State(s));

        // A lähettää toisen fyysisen kopion C:lle
        // suoraan olemassa olevan postipalvelun
        // kautta. Ei Distribution.DispatchAll().
        var connection = s.Network
            .GetOutgoingConnections(
                "A", CommunicationLayer.Interpersonal)
            .Single(c => c.TargetNodeId == "C");

        var copy = s.Inventory.RecordCopy(
            s.Original.Id,
            "EXTRA-COPY",
            "A",
            "A");

        var package = new MessagePackage(
            "EXTRA-PACKAGE",
            connection,
            cassetteCopy: copy);

        var shipment = s.Exchange.Send(
            "EXTRA-SHIPMENT",
            package,
            "FI",
            "FI",
            5);

        // Rekisteröidään fyysinen siirto myös
        // nykyiseen kasetinratkaisijaan, jotta
        // C:n kuuntelu tunnistaa kappaleen.
        s.CopyIndex.RegisterTransfer(shipment, copy);

        s.World.AdvanceDays(2);

        // B:n kuuntelu on valmistunut,
        // mutta C:n vasta alkanut.
        Assert.AreEqual(
            ListeningPhase.Completed,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("C", "D1").Phase);

        Assert.AreEqual(SIRState.I, State(s));

        s.World.AdvanceDays(2);

        Assert.AreEqual(SIRState.R, State(s));
    }

    [Test]
    public void Quiescence_ReturnedMailCanFinishWave()
    {
        var s = Create();

        // B muuttaa sen jälkeen, kun
        // A on jo postittanut kasetin.
        s.Network.AddAddress("ADDR-B-NEW", "SE");
        s.Network.MoveNode("B", "ADDR-B-NEW");

        s.World.AdvanceDays(4);

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            s.SeedWave[0].Shipment.Status);

        Assert.AreEqual(SIRState.R, State(s));

        Assert.AreEqual(
            0.0,
            s.GetFieldWave("D1").ObservedDeliveredReach,
            1e-9);
    }

    [Test]
    public void Quiescence_NoCopyingLeavesFieldInS()
    {
        var s = Create(sendSeed: false);

        Assert.AreEqual(0, s.SeedWave.Count);

        s.World.AdvanceDays(12);

        Assert.AreEqual(SIRState.S, State(s));

        Assert.IsFalse(
            s.GetFieldWave("D1")
                .IsPassivelyDiscoverable);
    }
}
