
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible922ZinePhysicalBulkTests
{
    private sealed class World
    {
        public readonly Network Network = new Network();
        public readonly SimulationClock Clock = new SimulationClock();
        public readonly ZineIssueKnowledgeRegistry Knowledge;
        public readonly InsertDistributionRegistry Insert = new InsertDistributionRegistry();
        public readonly ZineIssueInsertDelivery Delivery;
        public readonly ZinePhysicalBulkDistribution Bulk;
        public readonly ZineEditorialCycle Editorial;

        public World()
        {
            Network.AddNode(new Node("EDITOR"));
            for (int i = 1; i <= 9; i++)
                Network.AddNode(new Node("R" + i));
            Knowledge = new ZineIssueKnowledgeRegistry(Network);
            Delivery = new ZineIssueInsertDelivery(Insert, Knowledge);
            Bulk = new ZinePhysicalBulkDistribution(Delivery);
            Editorial = new ZineEditorialCycle(
                "Z", "EDITOR", true, Clock);
        }

        public ZineEditorialIssue Publish()
        {
            Editorial.RecordProductOpinion("EDITOR", "DEMO", true);
            Clock.AdvanceDays(30); // Current legacy ZineEditorialCycle schedule.
            return Editorial.Issues.Last();
        }

        public NetworkConnection Route(string from, string to,
            CommunicationLayer layer = CommunicationLayer.Insert,
            string suffix = "")
        {
            string id = "ROUTE:" + from + ":" + to + suffix;
            Network.AddConnection(id, from, to, layer, "distributor");
            return Network.GetOutgoingConnections(from, layer)
                .Single(x => x.Id == id);
        }
    }

    [Test]
    public void HundredNodes_ProduceExactlyTwentyFiveCopies()
    {
        var world = new World();
        var issue = world.Publish();
        Assert.AreEqual(25, world.Bulk.PrintIssue(issue, 100));
        Assert.AreEqual(25, world.Bulk.GetCopies(issue, "EDITOR"));
        Assert.AreEqual(25, world.Bulk.GetTotalCopiesHeld(issue));
    }

    [Test]
    public void Ratio_RoundsUp_AndRejectsSecondPrintingOfSameIssue()
    {
        var world = new World();
        var issue = world.Publish();
        Assert.AreEqual(26, world.Bulk.PrintIssue(issue, 101));
        Assert.Throws<InvalidOperationException>(
            () => world.Bulk.PrintIssue(issue, 101));
        Assert.AreEqual(26, world.Bulk.GetTotalCopiesHeld(issue));
    }

    [Test]
    public void InitialBulk_IsSharedEquallyAmongFiveDistributors()
    {
        var world = new World();
        var issue = world.Publish();
        world.Bulk.PrintIssue(issue, 100);
        var routes = Enumerable.Range(1, 5)
            .Select(i => world.Route("EDITOR", "R" + i)).ToList();

        var shipments = world.Bulk.DistributeInitial(issue, routes);
        Assert.AreEqual(5, shipments.Count);
        Assert.IsTrue(shipments.All(s => s.CopyCount == 5));
        Assert.AreEqual(0, world.Bulk.GetCopies(issue, "EDITOR"));
        Assert.AreEqual(25, world.Bulk.GetTotalCopiesHeld(issue));
        foreach (var route in routes)
        {
            Assert.AreEqual(5, world.Bulk.GetCopies(issue, route.TargetNodeId));
            Assert.IsTrue(world.Knowledge.HasReceived(
                route.TargetNodeId, issue.ProductId));
            Assert.AreEqual(1, world.Insert.GetForRecipient(
                route.TargetNodeId).Count);
        }
    }

    [Test]
    public void UnevenDivision_GivesRemainderInStableNodeIdOrder()
    {
        var world = new World();
        var issue = world.Publish();
        world.Bulk.PrintIssue(issue, 100);
        var routes = new[] {
            world.Route("EDITOR", "R4"),
            world.Route("EDITOR", "R2"),
            world.Route("EDITOR", "R3"),
            world.Route("EDITOR", "R1")
        };

        var shipments = world.Bulk.DistributeInitial(issue, routes);
        CollectionAssert.AreEqual(
            new[] { "R1", "R2", "R3", "R4" },
            shipments.Select(s => s.RecipientNodeId).ToArray());
        CollectionAssert.AreEqual(
            new[] { 7, 6, 6, 6 },
            shipments.Select(s => s.CopyCount).ToArray());
        Assert.AreEqual(25, world.Bulk.GetTotalCopiesHeld(issue));
    }

    [Test]
    public void ParallelInsertArcs_CountOnlyOnceForDistributorAllocation()
    {
        var world = new World();
        var issue = world.Publish();
        world.Bulk.PrintIssue(issue, 100);
        var routes = new[] {
            world.Route("EDITOR", "R1", suffix: ":B"),
            world.Route("EDITOR", "R2"),
            world.Route("EDITOR", "R1", suffix: ":A")
        };
        var shipments = world.Bulk.DistributeInitial(issue, routes);
        Assert.AreEqual(2, shipments.Count);
        Assert.AreEqual(13, world.Bulk.GetCopies(issue, "R1"));
        Assert.AreEqual(12, world.Bulk.GetCopies(issue, "R2"));
        Assert.AreEqual(25, world.Bulk.GetTotalCopiesHeld(issue));
    }

    [Test]
    public void ForwardingMovesPhysicalStock_WithoutMintingCopiesOrDuplicatingKnowledge()
    {
        var world = new World();
        var issue = world.Publish();
        world.Bulk.PrintIssue(issue, 100);
        var first = world.Route("EDITOR", "R1");
        world.Bulk.DistributeInitial(issue, new[] { first });
        var forward = world.Route("R1", "R2");

        world.Bulk.Transfer(issue, forward, 4);
        world.Bulk.Transfer(issue, forward, 3);

        Assert.AreEqual(18, world.Bulk.GetCopies(issue, "R1"));
        Assert.AreEqual(7, world.Bulk.GetCopies(issue, "R2"));
        Assert.AreEqual(25, world.Bulk.GetTotalCopiesHeld(issue));
        Assert.AreEqual(1, world.Knowledge.ReceivedIssueCount("R2"));
        Assert.AreEqual(1, world.Insert.GetForRecipient("R2").Count);
        Assert.Throws<InvalidOperationException>(
            () => world.Bulk.Transfer(issue, forward, 19));
        Assert.AreEqual(25, world.Bulk.GetTotalCopiesHeld(issue));
    }

    [Test]
    public void PhysicalDelivery_IsNotBlockedByBooleanEditorialPreference()
    {
        var world = new World();
        var issue = world.Publish(); // Issue.Profile == true.
        var route = world.Route("EDITOR", "R1");

        // Legacy informational distribution still allows filtering.
        Assert.AreEqual(0, world.Delivery.Distribute(
            issue, new[] { route }, _ => false).Count);

        world.Bulk.PrintIssue(issue, 100);
        world.Bulk.DistributeInitial(issue, new[] { route });
        Assert.IsTrue(world.Knowledge.HasReceived("R1", issue.ProductId));
        Assert.AreEqual(25, world.Bulk.GetCopies(issue, "R1"));
    }

    [Test]
    public void NoDistributors_LeavesAllStockAtEditor()
    {
        var world = new World();
        var issue = world.Publish();
        world.Bulk.PrintIssue(issue, 100);
        Assert.AreEqual(0, world.Bulk.DistributeInitial(
            issue, Array.Empty<NetworkConnection>()).Count);
        Assert.AreEqual(25, world.Bulk.GetCopies(issue, "EDITOR"));
        var route = world.Route("EDITOR", "R1");
        Assert.AreEqual(1, world.Bulk.DistributeInitial(
            issue, new[] { route }).Count);
        Assert.AreEqual(25, world.Bulk.GetCopies(issue, "R1"));
        Assert.Throws<InvalidOperationException>(() => world.Bulk.DistributeInitial(
            issue, new[] { route }));
    }


    [Test]
    public void InvalidLayerOrOverspend_DoesNotConsumePhysicalStock()
    {
        var world = new World();
        var issue = world.Publish();
        world.Bulk.PrintIssue(issue, 100);

        var interpersonal = world.Route(
            "EDITOR",
            "R1",
            CommunicationLayer.Interpersonal,
            suffix: ":PERSONAL");

        Assert.Throws<ArgumentException>(() =>
            world.Bulk.DistributeInitial(
                issue,
                new[] { interpersonal }));

        Assert.AreEqual(
            25,
            world.Bulk.GetCopies(issue, "EDITOR"));

        var route = world.Route("EDITOR", "R1");

        Assert.Throws<InvalidOperationException>(() =>
            world.Bulk.Transfer(issue, route, 26));

        Assert.AreEqual(
            25,
            world.Bulk.GetCopies(issue, "EDITOR"));

        Assert.IsFalse(
            world.Knowledge.HasReceived("R1", issue.ProductId));
    }


    [Test]
    public void PublicationBridge_UsesFiniteBulkWhenNewOverloadSelected()
    {
        var world = new World();
        var routes = new List<NetworkConnection> {
            world.Route("EDITOR", "R1"),
            world.Route("EDITOR", "R2")
        };
        using (var bridge = new ZinePublicationDistribution(
            world.Editorial, world.Bulk, () => routes, 100))
        {
            var issue = world.Publish();
            Assert.AreEqual(25, world.Bulk.GetPrintedCopies(issue));
            Assert.AreEqual(13, world.Bulk.GetCopies(issue, "R1"));
            Assert.AreEqual(12, world.Bulk.GetCopies(issue, "R2"));
            Assert.AreEqual(0, world.Bulk.GetCopies(issue, "EDITOR"));
        }
    }
}
