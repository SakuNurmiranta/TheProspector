
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible9211DynamicFieldDemosTests
{
    private static DemoSession Create(
        int interval = 14,
        int seed = 921)
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = seed
            },
            listeningDays: 2,
            fieldDemoIntervalDays: interval);
    }

    [Test]
    public void NewDemo_IsBornOnlyAtConfiguredInterval()
    {
        var session = Create();

        session.AdvanceDays(13);

        Assert.AreEqual(13, session.Day);
        Assert.AreEqual(15, session.ProductCount);
        Assert.AreEqual(0, session.FieldDemoProduction.BirthCount);

        session.AdvanceDay();

        Assert.AreEqual(14, session.Day);
        Assert.AreEqual(16, session.ProductCount);
        Assert.AreEqual(1, session.FieldDemoProduction.BirthCount);

        session.AdvanceDays(14);

        Assert.AreEqual(28, session.Day);
        Assert.AreEqual(17, session.ProductCount);
        Assert.AreEqual(2, session.FieldDemoProduction.BirthCount);
    }

    [Test]
    public void RuntimeDemo_RegistersWithExistingDomainSubsystems()
    {
        var session = Create(interval: 0);
        var scenario = session.Scenario;

        var born = scenario.SpawnFieldDemo(
            "TEST-FIELD-DEMO",
            "TEST-FIELD-ARTIST",
            new Random(11));

        Assert.AreEqual(16, session.ProductCount);
        Assert.AreEqual(100, session.NodeCount);

        Assert.AreEqual(
            "TEST-FIELD-DEMO",
            born.Product.Id);

        Assert.AreEqual(
            "TEST-FIELD-ARTIST",
            born.Artist.Id);

        Assert.AreEqual(
            scenario.Field.Id,
            born.Artist.OriginFieldId);

        Assert.AreEqual(
            born.Artist.PrimaryContactNodeId,
            born.InitialHolderNodeId);

        Assert.AreEqual(
            born.Product.Id,
            scenario.GetDemo(born.Product.Id).Id);

        Assert.AreEqual(
            born.OriginalCopy.Id,
            scenario.GetOriginal(born.Product.Id).Id);

        Assert.IsNotNull(
            scenario.GetFieldWave(born.Product.Id));

        Assert.IsNotNull(
            scenario.GetSeedWave(born.Product.Id));

        Assert.IsTrue(
            scenario.Inventory.HasEverHeldProduct(
                born.InitialHolderNodeId,
                born.Product.Id));

        Assert.IsTrue(
            scenario.Observation.Capture()
                .ProductIds.Contains(born.Product.Id));
    }

    [Test]
    public void NewDemo_UsesExistingPhysicalCopyDistribution()
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");

        definition.AddConnection("AB", "A", "B");
        definition.AddConnection("BA", "B", "A");

        definition.MarkKnownMissing("A", "B");
        definition.MarkKnownMissing("B", "A");

        definition.SetSeed("A", "D1");

        var scenario = new ConfigurableCirculationScenario(
            definition,
            listeningDays: 1,
            fieldDiscoveryRandom: new Random(44));

        var born = scenario.SpawnFieldDemo(
            "RUNTIME-D1",
            "RUNTIME-ART1",
            new Random(12));

        var dispatches =
            scenario.GetSeedWave(born.Product.Id);

        Assert.AreEqual(1, dispatches.Count);

        Assert.AreEqual(
            born.Product.Id,
            dispatches[0].Copy.ProductId);

        Assert.AreEqual(
            born.InitialHolderNodeId,
            dispatches[0].Shipment.Package.SourceNodeId);

        Assert.AreNotEqual(
            born.InitialHolderNodeId,
            dispatches[0].RecipientNodeId);

        Assert.AreEqual(
            1,
            scenario.GetFieldWave(born.Product.Id)
                .ActiveCopierCount);

        Assert.AreEqual(
            2,
            scenario.ProductIds.Count);
    }

    [Test]
    public void IsolatedContact_CanReceiveAFieldDemo()
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.SetSeed("A", "D1");

        var scenario = new ConfigurableCirculationScenario(
            definition);

        var born = scenario.SpawnFieldDemo(
            "ISOLATED-DEMO",
            "ISOLATED-ARTIST",
            new Random(55));

        Assert.AreEqual("A", born.InitialHolderNodeId);

        Assert.AreEqual(
            0,
            scenario.GetSeedWave("ISOLATED-DEMO").Count);

        Assert.IsTrue(
            scenario.Inventory.HasEverHeldProduct(
                "A",
                "ISOLATED-DEMO"));

        Assert.AreEqual(
            2,
            scenario.ProductIds.Count);
    }

    [Test]
    public void NewDemo_ReceivesSharedSixAxisProfile()
    {
        var session = Create();

        session.AdvanceDays(14);

        var birth =
            session.FieldDemoProduction.Births.Single();

        var profile =
            session.AestheticProfiles.GetProfile(
                SixAxisProfileRole.DemoAtmosphere,
                birth.ProductId);

        Assert.IsNotNull(profile);
        Assert.GreaterOrEqual(profile.Mask, 0);
        Assert.LessOrEqual(profile.Mask, 63);

        Assert.AreEqual(
            15,
            session.InitialWorld.Definition.Seeds.Count);

        // New content must not rewrite the initial issue.
        Assert.AreEqual(
            7,
            session.AestheticProfiles.GetComposition(
                session.InitialZine.Issue.ProductId)
                .SourceCount);
    }

    [Test]
    public void DisabledGeneration_PreservesInitialProducts()
    {
        var session = Create(interval: 0);

        Assert.IsNull(session.FieldDemoProduction);

        session.AdvanceDays(60);

        Assert.AreEqual(15, session.ProductCount);
        Assert.AreEqual(100, session.NodeCount);
    }

    [Test]
    public void SameSeed_ReproducesFieldDemoBirths()
    {
        var first = Create(interval: 10, seed: 24680);
        var second = Create(interval: 10, seed: 24680);

        first.AdvanceDays(35);
        second.AdvanceDays(35);

        Assert.AreEqual(3, first.FieldDemoProduction.BirthCount);
        Assert.AreEqual(3, second.FieldDemoProduction.BirthCount);

        for (int i = 0; i < 3; i++)
        {
            var a = first.FieldDemoProduction.Births[i];
            var b = second.FieldDemoProduction.Births[i];

            Assert.AreEqual(a.Day, b.Day);
            Assert.AreEqual(a.ProductId, b.ProductId);
            Assert.AreEqual(a.ArtistId, b.ArtistId);
            Assert.AreEqual(
                a.InitialHolderNodeId,
                b.InitialHolderNodeId);

            Assert.AreEqual(
                first.AestheticProfiles.GetProfile(
                    SixAxisProfileRole.DemoAtmosphere,
                    a.ProductId).Mask,
                second.AestheticProfiles.GetProfile(
                    SixAxisProfileRole.DemoAtmosphere,
                    b.ProductId).Mask);
        }
    }

    [Test]
    public void InvalidRegistration_DoesNotDuplicateExistingProduct()
    {
        var session = Create(interval: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DemoSession.CreateConfigured(
                new CirculationInitializationParameters(),
                fieldDemoIntervalDays: -1));

        int before = session.ProductCount;

        Assert.Throws<ArgumentException>(() =>
            session.Scenario.SpawnFieldDemo(
                "D001",
                "INVALID-ARTIST",
                new Random(1)));

        Assert.AreEqual(before, session.ProductCount);
    }
}
