
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible925InitialZineTests
{
    private static DemoSession Create(int seed = 921)
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = seed
            });
    }

    [Test]
    public void DayZero_HasPublishedIssueAndFinitePrintRun()
    {
        var session = Create();
        var zine = session.InitialZine;

        Assert.IsNotNull(zine);
        Assert.AreEqual(0, session.Day);
        Assert.AreEqual(1, zine.Issue.Number);
        Assert.AreEqual(0, zine.Issue.PublishedDay);
        Assert.AreEqual(
            session.InitialWorld.EditorNodeId,
            zine.Issue.EditorNodeId);

        Assert.AreEqual(25, zine.PrintedCopies);
        Assert.AreEqual(
            25,
            zine.Bulk.GetCopies(
                zine.Issue, zine.Issue.EditorNodeId));

        Assert.AreEqual(
            25,
            zine.Bulk.GetTotalCopiesHeld(zine.Issue));
    }

    [Test]
    public void DayZero_OnlyEditorKnowsTheIssue()
    {
        var session = Create();
        var zine = session.InitialZine;

        foreach (string id in session.Scenario.NodeIds)
        {
            Assert.AreEqual(
                id == zine.Issue.EditorNodeId,
                zine.Knowledge.HasReceived(
                    id, zine.Issue.ProductId));
        }

        Assert.AreEqual(100, session.NodeCount);
        Assert.AreEqual(15, session.ProductCount);
    }

    [Test]
    public void InitialIssue_PreservesSevenActualAddressSnapshots()
    {
        var session = Create();
        var zine = session.InitialZine;
        var listed = session.InitialWorld.ZineListedNodeIds;

        var addresses = zine.Issue.Entries
            .Where(e =>
                e.Kind == ZineEditorialEntryKind.Address)
            .ToList();

        Assert.AreEqual(7, addresses.Count);
        Assert.AreEqual(7, zine.AdvertisedDemoIds.Count);

        foreach (var id in listed)
        {
            Assert.IsTrue(
                session.Scenario.Network.TryGetNodeAddress(
                    id, out string currentAddress));

            Assert.IsTrue(addresses.Any(e =>
                e.SubjectNodeId == id &&
                e.AddressId == currentAddress));

            Assert.IsTrue(
                zine.AdvertisedDemoIds.ContainsKey(id));

            Assert.IsTrue(
                session.InitialWorld.Definition.Seeds.Any(s =>
                    s.SourceNodeId == id &&
                    s.DemoId == zine.AdvertisedDemoIds[id]));
        }
    }

    [Test]
    public void DayOne_DistributesTwentyFiveCopiesEqually()
    {
        var session = Create();
        var zine = session.InitialZine;

        session.AdvanceDay();

        Assert.AreEqual(1, session.Day);
        Assert.AreEqual(5, zine.DistributorNodeIds.Count);
        Assert.AreEqual(
            1,
            zine.DistributorNodeIds.Count(
                id => id.StartsWith("A")));
        Assert.AreEqual(
            4,
            zine.DistributorNodeIds.Count(
                id => id.StartsWith("B")));

        foreach (string id in zine.DistributorNodeIds)
        {
            Assert.AreEqual(
                5,
                zine.Bulk.GetCopies(zine.Issue, id));

            Assert.IsTrue(zine.Knowledge.HasReceived(
                id, zine.Issue.ProductId));
        }

        Assert.AreEqual(
            0,
            zine.Bulk.GetCopies(
                zine.Issue, zine.Issue.EditorNodeId));

        Assert.AreEqual(
            25,
            zine.Bulk.GetTotalCopiesHeld(zine.Issue));
    }

    [Test]
    public void Distributor_LearnsAllSevenPublishedAddresses()
    {
        var session = Create();
        var zine = session.InitialZine;
        session.AdvanceDay();

        foreach (string distributor in zine.DistributorNodeIds)
        {
            foreach (var entry in zine.Issue.Entries)
            {
                if (entry.Kind !=
                    ZineEditorialEntryKind.Address)
                    continue;

                Assert.IsTrue(
                    session.Scenario.Network.TryGetKnownAddress(
                        distributor,
                        entry.SubjectNodeId,
                        out string learned));

                Assert.AreEqual(entry.AddressId, learned);
            }
        }
    }

    [Test]
    public void BulkDistribution_DoesNotCreatePersonalContacts()
    {
        var session = Create();

        int before = session.Scenario.NodeIds.Sum(id =>
            session.Scenario.Network.GetOutgoingConnections(
                id, CommunicationLayer.Interpersonal).Count);

        session.AdvanceDay();

        int after = session.Scenario.NodeIds.Sum(id =>
            session.Scenario.Network.GetOutgoingConnections(
                id, CommunicationLayer.Interpersonal).Count);

        Assert.AreEqual(before, after);

        var editor = session.InitialWorld.EditorNodeId;

        Assert.AreEqual(
            5,
            session.Scenario.Network.GetOutgoingConnections(
                editor, CommunicationLayer.Insert).Count);
    }

    [Test]
    public void SubsequentDays_DoNotReprintOrResendInitialIssue()
    {
        var session = Create();
        session.AdvanceDays(10);

        var zine = session.InitialZine;

        Assert.AreEqual(25, zine.PrintedCopies);

        Assert.AreEqual(
            25,
            zine.Bulk.GetTotalCopiesHeld(zine.Issue) +
            zine.Bulk.GetPostalInTransitCopies(zine.Issue));


        Assert.AreEqual(
            5,
            zine.DistributorNodeIds.Count(id =>
                zine.Knowledge.HasReceived(
                    id, zine.Issue.ProductId)));

        Assert.AreEqual(15, session.ProductCount);
    }

    [Test]
    public void SameSeed_ReproducesDistributorSelection()
    {
        var first = Create(2468);
        var second = Create(2468);

        CollectionAssert.AreEqual(
            first.InitialZine.DistributorNodeIds,
            second.InitialZine.DistributorNodeIds);

        first.AdvanceDay();
        second.AdvanceDay();

        foreach (string id in first.InitialZine.DistributorNodeIds)
        {
            Assert.AreEqual(
                first.InitialZine.Bulk.GetCopies(
                    first.InitialZine.Issue, id),
                second.InitialZine.Bulk.GetCopies(
                    second.InitialZine.Issue, id));
        }
    }

    [Test]
    public void LegacySession_RemainsWithoutInitialZine()
    {
        var session = DemoSession.CreateDefault();

        Assert.IsNull(session.InitialWorld);
        Assert.IsNull(session.InitialZine);

        session.AdvanceDay();

        Assert.AreEqual(1, session.Day);
        Assert.AreEqual(4, session.NodeCount);
        Assert.AreEqual(1, session.ProductCount);
    }
}
