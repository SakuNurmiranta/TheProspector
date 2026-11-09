
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible929DynamicRoutesTests
{
    private static ConfigurableCirculationScenario Create()
    {
        var definition = new CirculationWorldDefinition();

        definition.AddNode("A", "FI");
        definition.AddNode("B", "FI");
        definition.AddNode("C", "SE");

        definition.AddConnection("AB", "A", "B");
        definition.MarkKnownMissing("A", "B");
        definition.SetSeed("A", "D1");

        return new ConfigurableCirculationScenario(
            definition,
            listeningDays: 1,
            fieldDiscoveryRandom: new Random(929));
    }

    private static IReadOnlyList<CopyDistributionRoute>
        OriginalARoutes(ConfigurableCirculationScenario scenario)
    {
        return scenario.Network
            .GetOutgoingConnections(
                "A", CommunicationLayer.Interpersonal)
            .Select(connection => new CopyDistributionRoute(
                connection.TargetNodeId,
                connection,
                "FI"))
            .ToList();
    }

    [Test]
    public void InitialRoute_IsPreservedWithoutDuplication()
    {
        var scenario = Create();
        var originals = OriginalARoutes(scenario);

        var resolved = LiveInterpersonalRoutes.Resolve(
            scenario.Network, "A", originals);

        Assert.AreEqual(1, resolved.Count);
        Assert.AreEqual("AB", resolved[0].Connection.Id);
        Assert.AreSame(
            originals[0].Connection,
            resolved[0].Connection);

        var again = LiveInterpersonalRoutes.Resolve(
            scenario.Network, "A", originals);

        Assert.AreEqual(1, again.Count);
    }

    [Test]
    public void NewKnownInterpersonalArc_BecomesRouteImmediately()
    {
        var scenario = Create();

        scenario.Network.LearnAddress(
            "B", "C", "ADDR-C");

        scenario.Network.AddConnection(
            "BC-NEW",
            "B",
            "C",
            CommunicationLayer.Interpersonal,
            "personal");

        var routes = LiveInterpersonalRoutes.Resolve(
            scenario.Network,
            "B",
            Array.Empty<CopyDistributionRoute>());

        Assert.AreEqual(1, routes.Count);
        Assert.AreEqual("C", routes[0].RecipientNodeId);
        Assert.AreEqual("BC-NEW", routes[0].Connection.Id);
        Assert.AreEqual("SE", routes[0].RecipientCountryCode);
    }

    [Test]
    public void MissingPostalAddress_DoesNotCreateUsableRoute()
    {
        var scenario = Create();

        scenario.Network.AddConnection(
            "BC-NO-ADDRESS",
            "B",
            "C",
            CommunicationLayer.Interpersonal,
            "personal");

        Assert.AreEqual(
            0,
            LiveInterpersonalRoutes.Resolve(
                scenario.Network,
                "B",
                Array.Empty<CopyDistributionRoute>()).Count);

        scenario.Network.LearnAddress(
            "B", "C", "ADDR-C");

        Assert.AreEqual(
            1,
            LiveInterpersonalRoutes.Resolve(
                scenario.Network,
                "B",
                Array.Empty<CopyDistributionRoute>()).Count);
    }

    [Test]
    public void InsertArcs_AndReverseArcs_AreNotIncluded()
    {
        var scenario = Create();

        scenario.Network.LearnAddress(
            "A", "C", "ADDR-C");

        scenario.Network.AddConnection(
            "AC-INSERT",
            "A",
            "C",
            CommunicationLayer.Insert,
            "zine");

        scenario.Network.AddConnection(
            "CA-PERSONAL",
            "C",
            "A",
            CommunicationLayer.Interpersonal,
            "personal");

        var routes = LiveInterpersonalRoutes.Resolve(
            scenario.Network,
            "A",
            OriginalARoutes(scenario));

        Assert.AreEqual(1, routes.Count);
        Assert.AreEqual("AB", routes[0].Connection.Id);
    }

    [Test]
    public void ParallelPersonalArcs_RemainDistinctAndDeterministic()
    {
        var scenario = Create();

        scenario.Network.LearnAddress(
            "B", "C", "ADDR-C");

        scenario.Network.AddConnection(
            "BC-Z",
            "B", "C",
            CommunicationLayer.Interpersonal,
            "personal");

        scenario.Network.AddConnection(
            "BC-A",
            "B", "C",
            CommunicationLayer.Interpersonal,
            "other-personal-role");

        var routes = LiveInterpersonalRoutes.Resolve(
            scenario.Network,
            "B",
            Array.Empty<CopyDistributionRoute>());

        CollectionAssert.AreEqual(
            new[] { "BC-A", "BC-Z" },
            routes.Select(r => r.Connection.Id).ToArray());
    }

    [Test]
    public void NewRoute_DoesNotInventCollectionKnowledge()
    {
        var scenario = Create();

        scenario.Network.LearnAddress(
            "B", "C", "ADDR-C");

        scenario.Network.AddConnection(
            "BC-KNOWLEDGE",
            "B", "C",
            CommunicationLayer.Interpersonal,
            "personal");

        Assert.IsNull(
            scenario.Known.IsKnownToHold("B", "C", "D1"));

        var routes = LiveInterpersonalRoutes.Resolve(
            scenario.Network,
            "B",
            Array.Empty<CopyDistributionRoute>());

        Assert.AreEqual(1, routes.Count);

        Assert.IsNull(
            scenario.Known.IsKnownToHold("B", "C", "D1"));
    }

    [Test]
    public void NewRoute_AllowsCopyDistributionOutsideOriginalCircle()
    {
        var scenario = Create();

        // B initially receives D1 from A.
        // C belongs to no initial friendship component.

        scenario.Network.LearnAddress(
            "B", "C", "ADDR-C");

        scenario.Network.AddConnection(
            "BC-NEW-CONTACT",
            "B", "C",
            CommunicationLayer.Interpersonal,
            "personal");

        // Explicit test fixture: B has learned that C
        // has not reported owning D1.
        //
        // This is not inferred automatically from
        // the existence of the network connection.
        scenario.Known.RecordSnapshot(
            "B", "C", Array.Empty<string>());

        // Delivery A -> B, listening, copying B -> C,
        // and the international postal journey.
        scenario.World.AdvanceDays(14);

        Assert.IsTrue(
            scenario.GetPolicy("B").Dispatched.Any(
                shipment =>
                    shipment.RecipientNodeId == "C" &&
                    shipment.Copy.ProductId == "D1"));

        Assert.IsTrue(
            scenario.Inventory.HasEverHeldProduct(
                "C", "D1"));
    }
}
