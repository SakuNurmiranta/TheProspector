using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class FieldTests
{
    [Test]
    public void Field_RegistersLocalNode()
    {
        var network = new Network();
        network.AddNode(new Node("A"));
        network.AddAddress("FI-01", "FI");
        network.PlaceNode("A", "FI-01");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        Assert.IsTrue(field.ContainsLocalNode("A"));
        Assert.AreEqual(1, field.LocalNodeCount);
    }

    [Test]
    public void Field_RejectsUnplacedOrUnknownNode()
    {
        var network = new Network();
        network.AddNode(new Node("A"));

        var field = new Field("F1", network);

        Assert.Throws<InvalidOperationException>(
            () => field.RegisterLocalNode("A"));

        Assert.Throws<ArgumentException>(
            () => field.RegisterLocalNode("UNKNOWN"));
    }

    [Test]
    public void Field_RejectsDuplicateMembership()
    {
        var network = new Network();
        network.AddNode(new Node("A"));
        network.AddAddress("FI-01", "FI");
        network.PlaceNode("A", "FI-01");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        Assert.Throws<ArgumentException>(
            () => field.RegisterLocalNode("A"));
    }

    [Test]
    public void Field_CalculatesRelativeShares()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");
        field.RegisterLocalNode("B");

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        var shares = field.CalculateShares(capital);

        Assert.AreEqual(0.25, shares["A"], 0.000001);
        Assert.AreEqual(0.75, shares["B"], 0.000001);
        Assert.AreEqual(1.0,
            shares["A"] + shares["B"], 0.000001);
    }

    [Test]
    public void Field_SharesChangeWithCapital()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");
        field.RegisterLocalNode("B");

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 90
        };

        var before = field.CalculateShares(capital);

        capital["A"] = 60;
        capital["B"] = 40;

        var after = field.CalculateShares(capital);

        Assert.AreEqual(0.10, before["A"], 0.000001);
        Assert.AreEqual(0.60, after["A"], 0.000001);
        Assert.AreEqual(1.0,
            after["A"] + after["B"], 0.000001);
    }

    [Test]
    public void Field_RejectsUndefinedZeroCapitalShares()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddAddress("ADDR-A", "FI");
        network.PlaceNode("A", "ADDR-A");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        var capital = new Dictionary<string, int>
        {
            ["A"] = 0
        };

        Assert.Throws<InvalidOperationException>(
            () => field.CalculateShares(capital));
    }

    [Test]
    public void Field_RejectsNegativeCapital()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddAddress("ADDR-A", "FI");
        network.PlaceNode("A", "ADDR-A");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        var capital = new Dictionary<string, int>
        {
            ["A"] = -5
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => field.CalculateShares(capital));
    }


    [Test]
    public void Field_SelectsRecipientByWeightedShare()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        Assert.AreEqual("A", field.SelectInitialRecipient(
            capital, new FixedRandom(0.249)));

        Assert.AreEqual("B", field.SelectInitialRecipient(
            capital, new FixedRandom(0.250)));

        Assert.AreEqual("B", field.SelectInitialRecipient(
            capital, new FixedRandom(0.999)));
    }

    [Test]
    public void Field_SelectionReflectsCapitalChanges()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        Assert.AreEqual("B", field.SelectInitialRecipient(
            capital, new FixedRandom(0.30)));

        capital["A"] = 30;
        capital["B"] = 10;

        Assert.AreEqual("A", field.SelectInitialRecipient(
            capital, new FixedRandom(0.30)));
    }

    [Test]
    public void Field_ZeroShareNodeCannotBeSelected()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 0,
            ["B"] = 10
        };

        Assert.AreEqual("B", field.SelectInitialRecipient(
            capital, new FixedRandom(0.0)));
    }

// Testiapurit samaan FieldTests-luokkaan:

    private static Field CreateTwoNodeField()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");
        field.RegisterLocalNode("B");

        return field;
    }

    private sealed class FixedRandom : Random
    {
        private readonly double _sample;

        public FixedRandom(double sample)
        {
            _sample = sample;
        }

        public override double NextDouble()
        {
            return _sample;
        }
    }


    [Test]
    public void Field_SpawnsArtistWithWeightedContact()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        var artist = field.SpawnArtist(
            "ART1", capital, new FixedRandom(0.30));

        Assert.AreEqual("ART1", artist.Id);
        Assert.AreEqual("F1", artist.OriginFieldId);
        Assert.AreEqual("B", artist.PrimaryContactNodeId);
    }

    [Test]
    public void Field_ArtistContactPersistsAcrossReleases()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        var artist = field.SpawnArtist(
            "ART1", capital, new FixedRandom(0.10));

        // Muutetaan kenttäosuuksia artistin synnyttyä.
        capital["A"] = 0;
        capital["B"] = 100;

        var first = field.CreateDemo("D1", "ART1");
        var second = field.CreateDemo("D2", "ART1");

        Assert.AreEqual("A", artist.PrimaryContactNodeId);

        Assert.AreEqual("D1", first.Id);
        Assert.AreEqual("D2", second.Id);

        Assert.AreEqual("ART1", first.ArtistId);
        Assert.AreEqual("ART1", second.ArtistId);
    }

    [Test]
    public void Field_RejectsDuplicateArtistId()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        field.SpawnArtist(
            "ART1", capital, new FixedRandom(0.10));

        Assert.Throws<ArgumentException>(() =>
            field.SpawnArtist(
                "ART1", capital, new FixedRandom(0.80)));
    }

    [Test]
    public void Field_RejectsUnknownArtistAndDuplicateDemo()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        Assert.Throws<ArgumentException>(() =>
            field.CreateDemo("D1", "UNKNOWN"));

        field.SpawnArtist(
            "ART1", capital, new FixedRandom(0.10));

        field.CreateDemo("D1", "ART1");

        Assert.Throws<ArgumentException>(() =>
            field.CreateDemo("D1", "ART1"));
    }


    [Test]
    public void Field_BlockingArtistDisablesPrimaryContact()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        var artist = field.SpawnArtist(
            "ART1", capital, new FixedRandom(0.30));

        var firstDemo = field.CreateDemo("D1", "ART1");

        Assert.IsTrue(field.TryGetUnblockedPrimaryContact(
            "ART1", out var before));

        Assert.AreEqual("B", before);

        field.BlockArtist("ART1", "B");

        Assert.IsFalse(field.TryGetUnblockedPrimaryContact(
            "ART1", out var after));

        Assert.IsNull(after);

        // Kontaktihistoria säilyy.
        Assert.AreEqual("B", artist.PrimaryContactNodeId);

        // Aikaisempi demo säilyy muuttumattomana.
        Assert.AreEqual("D1", firstDemo.Id);
        Assert.AreEqual("ART1", firstDemo.ArtistId);

        // Artisti voi edelleen luoda julkaisuja.
        var secondDemo = field.CreateDemo("D2", "ART1");

        Assert.AreEqual("ART1", secondDemo.ArtistId);
    }

    [Test]
    public void Field_BlockingIsSpecificToArtistAndNode()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        field.SpawnArtist(
            "ART1", capital, new FixedRandom(0.30));

        field.SpawnArtist(
            "ART2", capital, new FixedRandom(0.30));

        // A blokkaa ART1:n, mutta B on edelleen
        // tämän artistin ensisijainen kontakti.
        field.BlockArtist("ART1", "A");

        Assert.IsTrue(field.TryGetUnblockedPrimaryContact(
            "ART1", out var firstContact));

        Assert.AreEqual("B", firstContact);

        // B blokkaa vain ART1:n.
        field.BlockArtist("ART1", "B");

        Assert.IsFalse(field.TryGetUnblockedPrimaryContact(
            "ART1", out _));

        // ART2 voi edelleen tavoittaa B:n.
        Assert.IsTrue(field.TryGetUnblockedPrimaryContact(
            "ART2", out var secondContact));

        Assert.AreEqual("B", secondContact);
    }

    [Test]
    public void Field_RejectsInvalidArtistBlock()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        field.SpawnArtist(
            "ART1", capital, new FixedRandom(0.30));

        Assert.Throws<ArgumentException>(() =>
            field.BlockArtist("UNKNOWN", "B"));

        Assert.Throws<ArgumentException>(() =>
            field.BlockArtist("ART1", "UNKNOWN"));

        Assert.IsTrue(field.TryGetUnblockedPrimaryContact(
            "ART1", out var contact));

        Assert.AreEqual("B", contact);
    }


    [Test]
    public void Field_RemoteNodeCanBlockArtist()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "SE");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10
        };

        field.SpawnArtist(
            "ART1", capital, new FixedRandom(0.5));

        // B ei kuulu artistin lähtökenttään.
        Assert.IsTrue(
            field.CanOfferDemoDirectly("ART1", "B"));

        field.BlockArtist("ART1", "B");

        Assert.IsFalse(
            field.CanOfferDemoDirectly("ART1", "B"));

        // B:n blokkaus ei vaikuta ensikontaktiin A.
        Assert.IsTrue(
            field.TryGetUnblockedPrimaryContact(
                "ART1", out var contact));

        Assert.AreEqual("A", contact);
    }

    [Test]
    public void Field_DirectOfferBlockIsArtistSpecific()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        field.SpawnArtist(
            "ART1", capital, new FixedRandom(0.1));

        field.SpawnArtist(
            "ART2", capital, new FixedRandom(0.1));

        field.BlockArtist("ART1", "A");

        Assert.IsFalse(
            field.CanOfferDemoDirectly("ART1", "A"));

        Assert.IsTrue(
            field.CanOfferDemoDirectly("ART2", "A"));

        Assert.IsTrue(
            field.CanOfferDemoDirectly("ART1", "B"));
    }

    [Test]
    public void Field_DirectOfferRejectsUnknownIdentifiers()
    {
        var field = CreateTwoNodeField();

        var capital = new Dictionary<string, int>
        {
            ["A"] = 10,
            ["B"] = 30
        };

        field.SpawnArtist(
            "ART1", capital, new FixedRandom(0.1));

        Assert.Throws<ArgumentException>(() =>
            field.CanOfferDemoDirectly(
                "UNKNOWN", "A"));

        Assert.Throws<ArgumentException>(() =>
            field.CanOfferDemoDirectly(
                "ART1", "UNKNOWN"));
    }
}