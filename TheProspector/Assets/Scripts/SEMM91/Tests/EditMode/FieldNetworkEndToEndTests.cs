
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class FieldNetworkEndToEndTests
{
    private sealed class FixedRandom : Random
    {
        public int Calls { get; private set; }

        public override double NextDouble()
        {
            Calls++;
            return 0.0;
        }
    }

    private static ConfigurableCirculationScenario Create(
        FixedRandom random,
        bool enableSecondHop,
        bool positiveReferrals = false)
    {
        var definition =
            new CirculationWorldDefinition();

        foreach (var id in new[] { "A", "B", "C", "D" })
            definition.AddNode(id, "FI");

        definition.AddConnection("AB", "A", "B");
        definition.AddConnection("BC", "B", "C");

        definition.MarkKnownMissing("A", "B");

        if (enableSecondHop)
            definition.MarkKnownMissing("B", "C");

        definition.SetSeed("A", "D1");

        return new ConfigurableCirculationScenario(
            definition,
            listeningDays: 2,
            explicitPositiveReferrals: positiveReferrals,
            fieldDiscoveryRandom: random);
    }

    private static SIRState FieldState(
        ConfigurableCirculationScenario scenario)
    {
        return scenario.FieldSIR.GetFieldState(
            scenario.Field, scenario.Demo);
    }

    [Test]
    public void EndToEnd_AutonomousTwoHopDistributionClosesAndDiscovers()
    {
        var random = new FixedRandom();
        var s = Create(random, enableSecondHop: true);
        var wave = s.GetFieldWave("D1");

        // Päivä 0: A lähettää B:lle.
        Assert.AreEqual(1, s.SeedWave.Count);
        Assert.AreEqual(SIRState.I, FieldState(s));
        Assert.AreEqual(1, wave.ActiveCopierCount);

        Assert.AreEqual(0, random.Calls);

        // Päivä 2: B saa kasetin ja aloittaa kuuntelun.
        s.World.AdvanceDays(2);

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.AreEqual(0, s.GetPolicy("B").Dispatched.Count);
        Assert.AreEqual(SIRState.I, FieldState(s));

        // Päivä 4: B kuuntelee loppuun ja
        // lähettää automaattisesti kopion C:lle.
        s.World.AdvanceDays(2);

        Assert.AreEqual(
            ListeningPhase.Completed,
            s.GetListeningStatus("B", "D1").Phase);

        Assert.AreEqual(1, s.GetPolicy("B").Dispatched.Count);

        var secondHop = s.GetPolicy("B").Dispatched[0];

        Assert.AreEqual("C", secondHop.RecipientNodeId);
        Assert.AreEqual("D1", secondHop.Copy.ProductId);

        Assert.AreEqual(SIRState.I, FieldState(s));
        Assert.AreEqual(2, wave.ActiveCopierCount);
        Assert.AreEqual(0, random.Calls);

        // Päivä 6: C vastaanottaa kopion.
        s.World.AdvanceDays(2);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            secondHop.Shipment.Status);

        Assert.AreEqual(
            ListeningPhase.Listening,
            s.GetListeningStatus("C", "D1").Phase);

        Assert.AreEqual(
            2.0 / 4.0,
            wave.ObservedDeliveredReach,
            1e-9);

        // Aalto ei sulkeudu C:n kuuntelun aikana.
        Assert.AreEqual(SIRState.I, FieldState(s));
        Assert.AreEqual(0, random.Calls);

        // Päivä 8: C:n kuuntelu valmistuu.
        // Uusia kopioita ei synny.
        // Kenttäaalto sulkeutuu.
        s.World.AdvanceDays(2);

        Assert.AreEqual(
            ListeningPhase.Completed,
            s.GetListeningStatus("C", "D1").Phase);

        Assert.AreEqual(SIRState.R, FieldState(s));
        Assert.AreEqual(0, wave.ActiveCopierCount);
        Assert.IsTrue(wave.IsPassivelyDiscoverable);

        // D löytää demon kentästä.
        Assert.IsTrue(
            s.HasDiscoveredFromField("D", "D1"));

        Assert.AreEqual(1, random.Calls);

        // Löytyminen ei tarkoita kasetin omistamista.
        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        Assert.AreEqual(
            0,
            s.Network.GetOutgoingConnections(
                "D",
                CommunicationLayer.Interpersonal).Count);

        // Myöhemmät päivät eivät avaa aaltoa
        // eivätkä arvo samaa löytöä uudestaan.
        s.World.AdvanceDays(12);

        Assert.AreEqual(SIRState.R, FieldState(s));
        Assert.AreEqual(1, random.Calls);
    }

    [Test]
    public void EndToEnd_NoSecondHopClosesEarlierAndPreservesReferrals()
    {
        var random = new FixedRandom();

        var s = Create(
            random,
            enableSecondHop: false,
            positiveReferrals: true);

        // A:n eksplisiittinen myönteinen
        // suositus säilyy SIR-integraatiossa.
        Assert.AreEqual(
            1,
            s.SeedWave[0].Shipment.Package.Thumb);

        // B:llä on yhteys C:hen, mutta B ei
        // tiedä C:n tarvitsevan demoa.
        // Kopiota ei tämän vuoksi lähetetä.
        s.World.AdvanceDays(4);

        Assert.AreEqual(
            0, s.GetPolicy("B").Dispatched.Count);

        Assert.AreEqual(SIRState.R, FieldState(s));

        Assert.AreEqual(
            1.0 / 4.0,
            s.GetFieldWave("D1").ObservedDeliveredReach,
            1e-9);

        // C ja D voivat löytää demon kentästä.
        Assert.IsTrue(
            s.HasDiscoveredFromField("C", "D1"));

        Assert.IsTrue(
            s.HasDiscoveredFromField("D", "D1"));

        Assert.AreEqual(2, random.Calls);

        // Kasettien määrä tai omistajuus
        // ei muutu passiivisten löytöjen vuoksi.
        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("C", "D1"));

        Assert.IsFalse(
            s.Inventory.HasEverHeldProduct("D", "D1"));

        s.World.AdvanceDays(10);

        Assert.AreEqual(2, random.Calls);
        Assert.AreEqual(SIRState.R, FieldState(s));
    }
}
