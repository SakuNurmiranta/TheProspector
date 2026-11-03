
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class KnownCollectionRegistryTests
{
    private static Network CreateNetwork()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));
        network.AddNode(new Node("C"));

        return network;
    }

    [Test]
    public void Collection_UnknownAndKnownEmptyAreDifferent()
    {
        var registry = new KnownCollectionRegistry(
            CreateNetwork());

        // A ei ole vielä saanut tietoa B:n kokoelmasta.
        Assert.IsFalse(
            registry.TryGetKnownCollection(
                "A", "B", out var unknown));

        Assert.IsNull(unknown);

        Assert.IsNull(
            registry.IsKnownToHold("A", "B", "D1"));

        // B ilmoittaa A:lle tyhjän kokoelman.
        registry.RecordSnapshot(
            "A", "B", Array.Empty<string>());

        Assert.IsTrue(
            registry.TryGetKnownCollection(
                "A", "B", out var known));

        Assert.AreEqual(0, known.Count);

        Assert.AreEqual(
            false,
            registry.IsKnownToHold("A", "B", "D1"));
    }

    [Test]
    public void Collection_KnowledgeIsDirectional()
    {
        var registry = new KnownCollectionRegistry(
            CreateNetwork());

        // A saa tiedon B:n kokoelmasta.
        registry.RecordSnapshot(
            "A", "B", new[] { "D1", "D2" });

        Assert.AreEqual(
            true,
            registry.IsKnownToHold("A", "B", "D1"));

        // B ei automaattisesti tiedä A:n kokoelmaa.
        Assert.IsNull(
            registry.IsKnownToHold("B", "A", "D1"));

        // C ei saa samaa tietoa automaattisesti.
        Assert.IsNull(
            registry.IsKnownToHold("C", "B", "D1"));
    }

    
    [Test]
    public void Collection_HistoricalSnapshotsAccumulateKnowledge()
    {
        var registry = new KnownCollectionRegistry(
            CreateNetwork());

        registry.RecordSnapshot(
            "A", "B", new[] { "D1", "D2" });

        // Uudempi kokoelmahistoria.
        registry.RecordSnapshot(
            "A", "B", new[] { "D1", "D2", "D3" });

        // Vanhempi ilmoitus saapuu myöhemmin.
        registry.RecordSnapshot(
            "A", "B", new[] { "D1" });

        Assert.AreEqual(
            true,
            registry.IsKnownToHold("A", "B", "D1"));

        Assert.AreEqual(
            true,
            registry.IsKnownToHold("A", "B", "D2"));

        Assert.AreEqual(
            true,
            registry.IsKnownToHold("A", "B", "D3"));

        Assert.IsTrue(
            registry.TryGetKnownCollection(
                "A", "B", out var collection));

        Assert.AreEqual(3, collection.Count);
    }


    [Test]
    public void Collection_ObserversMaintainIndependentSnapshots()
    {
        var registry = new KnownCollectionRegistry(
            CreateNetwork());

        registry.RecordSnapshot(
            "A", "B", new[] { "D1" });

        registry.RecordSnapshot(
            "C", "B", new[] { "D2" });

        Assert.AreEqual(
            true,
            registry.IsKnownToHold("A", "B", "D1"));

        Assert.AreEqual(
            false,
            registry.IsKnownToHold("A", "B", "D2"));

        Assert.AreEqual(
            false,
            registry.IsKnownToHold("C", "B", "D1"));

        Assert.AreEqual(
            true,
            registry.IsKnownToHold("C", "B", "D2"));
    }

    [Test]
    public void Collection_SnapshotIsIndependentAndReadOnly()
    {
        var registry = new KnownCollectionRegistry(
            CreateNetwork());

        var source = new List<string>
        {
            "D1"
        };

        registry.RecordSnapshot("A", "B", source);

        // Alkuperäisen listan muuttaminen ei muuta
        // vastaanottajan tallentamaa tietoa.
        source.Clear();
        source.Add("D2");

        Assert.IsTrue(
            registry.TryGetKnownCollection(
                "A", "B", out var snapshot));

        Assert.AreEqual(1, snapshot.Count);

        Assert.AreEqual(
            true,
            registry.IsKnownToHold("A", "B", "D1"));

        Assert.AreEqual(
            false,
            registry.IsKnownToHold("A", "B", "D2"));

        // Palautettu kokoelma ei ole muokattavissa.
        Assert.Throws<NotSupportedException>(() =>
            ((ICollection<string>)snapshot).Add("D3"));

        Assert.AreEqual(
            false,
            registry.IsKnownToHold("A", "B", "D3"));
    }

    [Test]
    public void Collection_InvalidSnapshotDoesNotChangeKnowledge()
    {
        var registry = new KnownCollectionRegistry(
            CreateNetwork());

        registry.RecordSnapshot(
            "A", "B", new[] { "D1" });

        Assert.Throws<ArgumentException>(() =>
            registry.RecordSnapshot(
                "UNKNOWN", "B", new[] { "D2" }));

        Assert.Throws<ArgumentException>(() =>
            registry.RecordSnapshot(
                "A", "UNKNOWN", new[] { "D2" }));

        Assert.Throws<ArgumentNullException>(() =>
            registry.RecordSnapshot(
                "A", "B", null));

        Assert.Throws<ArgumentException>(() =>
            registry.RecordSnapshot(
                "A", "B", new[] { "D2", " " }));

        // Virheelliset päivitykset eivät saa
        // tuhota aikaisempaa kokoelmatietoa.
        Assert.AreEqual(
            true,
            registry.IsKnownToHold("A", "B", "D1"));

        Assert.AreEqual(
            false,
            registry.IsKnownToHold("A", "B", "D2"));
    }

    [Test]
    public void Collection_DuplicateProductIdsRepresentOneDemo()
    {
        var registry = new KnownCollectionRegistry(
            CreateNetwork());

        registry.RecordSnapshot(
            "A",
            "B",
            new[] { "D1", "D1", "D2" });

        Assert.IsTrue(
            registry.TryGetKnownCollection(
                "A", "B", out var collection));

        // Kokoelmatieto ilmaisee demojen esiintymisen,
        // ei saman demon fyysisten kopioiden määrää.
        Assert.AreEqual(2, collection.Count);

        Assert.AreEqual(
            true,
            registry.IsKnownToHold("A", "B", "D1"));

        Assert.AreEqual(
            true,
            registry.IsKnownToHold("A", "B", "D2"));
    }
}
