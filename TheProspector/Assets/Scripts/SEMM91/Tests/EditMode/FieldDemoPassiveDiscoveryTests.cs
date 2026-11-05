
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class FieldDemoPassiveDiscoveryTests
{
    private sealed class FixedRandom : Random
    {
        public double Sample { get; set; }
        public int Calls { get; private set; }

        public FixedRandom(double sample)
        {
            Sample = sample;
        }

        public override double NextDouble()
        {
            Calls++;
            return Sample;
        }
    }

    private sealed class Fixture
    {
        public readonly ConfigurableCirculationScenario Scenario;
        public readonly Field Field;
        public readonly SIRStateRegistry Sir;
        public readonly FieldDemoCopyWave Wave;

        public readonly Dictionary<string, int> Capital =
            new Dictionary<string, int>
            {
                ["A"] = 10,
                ["B"] = 30,
                ["C"] = 60
            };

        public Fixture(bool secondTarget = false)
        {
            var definition = new CirculationWorldDefinition();

            definition.AddNode("A", "FI");
            definition.AddNode("B", "FI");
            definition.AddNode("C", "FI");

            definition.AddConnection("AB", "A", "B");
            definition.MarkKnownMissing("A", "B");

            if (secondTarget)
            {
                definition.AddConnection("AC", "A", "C");
                definition.MarkKnownMissing("A", "C");
            }

            definition.SetSeed("A", "D1");

            Scenario =
                new ConfigurableCirculationScenario(definition);

            Field = new Field("F1", Scenario.Network);
            Field.RegisterLocalNode("A");
            Field.RegisterLocalNode("B");
            Field.RegisterLocalNode("C");

            Sir = new SIRStateRegistry();

            Wave = new FieldDemoCopyWave(
                Field,
                Scenario.Network,
                Scenario.Demo,
                Sir);
        }

        public void ObserveAll()
        {
            foreach (var shipment in Scenario.SeedWave)
                Assert.IsTrue(Wave.ObserveCopyDispatch(shipment));
        }

        public void CloseWave()
        {
            Assert.IsTrue(Wave.FinishCopying("A"));
            Assert.IsTrue(Wave.TryCloseWave());
        }
    }

    [Test]
    public void Passive_OnlyDeliveredCopiesContributeToReach()
    {
        var f = new Fixture();

        f.ObserveAll();

        // Lähettäminen ei vielä ole toimitus.
        Assert.AreEqual(
            0.0, f.Wave.ObservedDeliveredReach, 1e-9);

        f.Scenario.Clock.AdvanceDays(2);

        // B on yksi kolmesta paikallisesta solmusta.
        Assert.AreEqual(
            1.0 / 3.0,
            f.Wave.ObservedDeliveredReach,
            1e-9);
    }

    [Test]
    public void Passive_RequiresClosedWaveAndDoesNotCreateCopy()
    {
        var f = new Fixture();
        var random = new FixedRandom(0.19);

        f.ObserveAll();
        f.Scenario.Clock.AdvanceDays(2);

        // SIR on edelleen I.
        Assert.IsFalse(f.Wave.TrySamplePassiveExposure(
            "C", f.Capital, random));

        Assert.AreEqual(0, random.Calls);

        f.CloseWave();

        // L = 1/3, C:n osuus = 0.6.
        // Löytötodennäköisyys = 0.2.
        Assert.IsTrue(f.Wave.TrySamplePassiveExposure(
            "C", f.Capital, random));

        Assert.IsTrue(f.Wave.HasPassiveExposure("C"));

        // Tieto tuotteen olemassaolosta ei
        // ole fyysisen kasetin vastaanottamista.
        Assert.IsFalse(
            f.Scenario.Inventory.HasEverHeldProduct(
                "C", "D1"));

        f.Scenario.Network.TryGetNode("C", out var c);

        // Passiivinen löytö ei aloita kopiointia.
        Assert.AreEqual(
            SIRState.S,
            f.Sir.GetNodeState(c, f.Scenario.Demo));

        // Löytö ei luo verkkokaarta.
        Assert.AreEqual(0,
            f.Scenario.Network.GetOutgoingConnections(
                "C",
                CommunicationLayer.Interpersonal).Count);
    }

    [Test]
    public void Passive_FailedSampleIsNotRepeated()
    {
        var f = new Fixture();
        f.ObserveAll();
        f.Scenario.Clock.AdvanceDays(2);
        f.CloseWave();

        var random = new FixedRandom(0.21);

        Assert.IsFalse(f.Wave.TrySamplePassiveExposure(
            "C", f.Capital, random));

        Assert.AreEqual(1, random.Calls);

        random.Sample = 0.0;

        // Sama kenttäaalto tarjoaa vain
        // yhden passiivisen löytöyrityksen.
        Assert.IsFalse(f.Wave.TrySamplePassiveExposure(
            "C", f.Capital, random));

        Assert.AreEqual(1, random.Calls);

        Assert.IsFalse(f.Wave.HasPassiveExposure("C"));
    }

    [Test]
    public void Passive_ReturnedShipmentDoesNotIncreaseReach()
    {
        var f = new Fixture();
        f.ObserveAll();

        // B muuttaa A:n tietämättä.
        f.Scenario.Network.AddAddress(
            "ADDR-B-NEW", "SE");

        f.Scenario.Network.MoveNode(
            "B", "ADDR-B-NEW");

        f.Scenario.Clock.AdvanceDays(4);

        Assert.AreEqual(
            PostalShipmentStatus.Returned,
            f.Scenario.SeedWave[0].Shipment.Status);

        Assert.AreEqual(
            0.0, f.Wave.ObservedDeliveredReach, 1e-9);

        f.CloseWave();

        var random = new FixedRandom(0.0);

        Assert.IsFalse(f.Wave.TrySamplePassiveExposure(
            "C", f.Capital, random));

        // Nollalevikillä arvontaa ei tehdä.
        Assert.AreEqual(0, random.Calls);
    }

    [Test]
    public void Passive_NonLocalRecipientDoesNotIncreaseReach()
    {
        var f = new Fixture();

        // Vaihtoehtoinen kenttä sisältää
        // A:n ja C:n mutta ei B:tä.
        var otherField = new Field(
            "OTHER", f.Scenario.Network);

        otherField.RegisterLocalNode("A");
        otherField.RegisterLocalNode("C");

        var wave = new FieldDemoCopyWave(
            otherField,
            f.Scenario.Network,
            f.Scenario.Demo,
            new SIRStateRegistry());

        Assert.IsTrue(
            wave.ObserveCopyDispatch(
                f.Scenario.SeedWave[0]));

        f.Scenario.Clock.AdvanceDays(2);

        // Lähetys toimitettiin B:lle, joka
        // ei kuulu tähän kenttään.
        Assert.AreEqual(
            0.0, wave.ObservedDeliveredReach, 1e-9);

        Assert.IsTrue(wave.FinishCopying("A"));
        Assert.IsTrue(wave.TryCloseWave());

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["C"] = 60
        };

        Assert.IsFalse(wave.TrySamplePassiveExposure(
            "C", capital, new FixedRandom(0.0)));
    }

    [Test]
    public void Passive_ReachCountsDistinctLocalRecipients()
    {
        var f = new Fixture(secondTarget: true);

        Assert.AreEqual(2, f.Scenario.SeedWave.Count);

        f.ObserveAll();
        f.Scenario.Clock.AdvanceDays(2);

        // B ja C ovat vastaanottaneet demon.
        Assert.AreEqual(
            2.0 / 3.0,
            f.Wave.ObservedDeliveredReach,
            1e-9);

        // A on yksi kopioija, vaikka sillä
        // oli kaksi eri vastaanottajaa.
        Assert.AreEqual(1, f.Wave.ActiveCopierCount);

        // Sama lähetys ei saa lisätä levikkiä.
        Assert.IsFalse(
            f.Wave.ObserveCopyDispatch(
                f.Scenario.SeedWave[0]));

        Assert.AreEqual(
            2.0 / 3.0,
            f.Wave.ObservedDeliveredReach,
            1e-9);
    }
}
