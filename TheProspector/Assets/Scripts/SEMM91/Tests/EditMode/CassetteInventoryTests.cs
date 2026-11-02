using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class CassetteInventoryTests
{
    private static Network CreateNetwork()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "SE");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");

        return network;
    }

    private static (Artist, CulturalProduct) CreateDemo(
        Network network)
    {
        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        var artist = field.SpawnArtist(
            "ART1",
            new Dictionary<string, int> { ["A"] = 10 },
            new Random(123));

        var demo = field.CreateDemo("D1", "ART1");

        return (artist, demo);
    }

    [Test]
    public void Inventory_RegistersPhysicalCassette()
    {
        var network = CreateNetwork();
        var (_, demo) = CreateDemo(network);

        var inventory = new CassetteInventory(network);

        inventory.RegisterExistingCopy("TAPE1", demo, "A");

        Assert.IsTrue(
            inventory.TryGetCopy("TAPE1", out var cassette));

        Assert.AreEqual("TAPE1", cassette.Id);
        Assert.AreEqual("D1", cassette.ProductId);
        Assert.AreEqual("A", cassette.HolderNodeId);
    }

    [Test]
    public void Inventory_TransferChangesHolderNotArtistContact()
    {
        var network = CreateNetwork();
        var (artist, demo) = CreateDemo(network);

        var inventory = new CassetteInventory(network);
        inventory.RegisterExistingCopy("TAPE1", demo, "A");

        inventory.RecordTransfer("TAPE1", "A", "B");

        inventory.TryGetCopy("TAPE1", out var cassette);

        Assert.AreEqual("B", cassette.HolderNodeId);
        Assert.AreEqual("D1", cassette.ProductId);

        // Artisti ei automaattisesti saa tietoa siirrosta.
        Assert.AreEqual("A", artist.PrimaryContactNodeId);
    }

    [Test]
    public void Inventory_RejectsTransferFromWrongHolder()
    {
        var network = CreateNetwork();
        var (_, demo) = CreateDemo(network);

        var inventory = new CassetteInventory(network);
        inventory.RegisterExistingCopy("TAPE1", demo, "A");

        Assert.Throws<InvalidOperationException>(() =>
            inventory.RecordTransfer("TAPE1", "B", "A"));

        inventory.TryGetCopy("TAPE1", out var cassette);

        Assert.AreEqual("A", cassette.HolderNodeId);
    }

    [Test]
    public void Inventory_RejectsDuplicateAndUnknownHolder()
    {
        var network = CreateNetwork();
        var (_, demo) = CreateDemo(network);

        var inventory = new CassetteInventory(network);
        inventory.RegisterExistingCopy("TAPE1", demo, "A");

        Assert.Throws<ArgumentException>(() =>
            inventory.RegisterExistingCopy("TAPE1", demo, "B"));

        Assert.Throws<ArgumentException>(() =>
            inventory.RegisterExistingCopy(
                "TAPE2", demo, "UNKNOWN"));
    }


    [Test]
    public void Inventory_CopyPreservesOriginal()
    {
        var network = CreateNetwork();
        var (artist, demo) = CreateDemo(network);
        var inventory = new CassetteInventory(network);

        inventory.RegisterExistingCopy("TAPE1", demo, "A");

        inventory.RecordCopy("TAPE1", "TAPE2", "A", "B");

        Assert.IsTrue(
            inventory.TryGetCopy("TAPE1", out var original));

        Assert.IsTrue(
            inventory.TryGetCopy("TAPE2", out var copy));

        Assert.AreNotSame(original, copy);

        Assert.AreEqual("A", original.HolderNodeId);
        Assert.AreEqual("B", copy.HolderNodeId);

        Assert.AreEqual("D1", original.ProductId);
        Assert.AreEqual("D1", copy.ProductId);

        // Kopiointi ei muuta artistin kontaktia.
        Assert.AreEqual("A", artist.PrimaryContactNodeId);
    }

    [Test]
    public void Inventory_CanCopyAnExistingCopy()
    {
        var network = CreateNetwork();
        var (_, demo) = CreateDemo(network);
        var inventory = new CassetteInventory(network);

        inventory.RegisterExistingCopy("TAPE1", demo, "A");

        inventory.RecordCopy("TAPE1", "TAPE2", "A", "B");

        // B voi tehdä saamastaan kopiosta uuden kopion.
        inventory.RecordCopy("TAPE2", "TAPE3", "B", "A");

        inventory.TryGetCopy("TAPE3", out var third);

        Assert.AreEqual("D1", third.ProductId);
        Assert.AreEqual("A", third.HolderNodeId);
    }

    [Test]
    public void Inventory_RejectsDuplicateCopyId()
    {
        var network = CreateNetwork();
        var (_, demo) = CreateDemo(network);
        var inventory = new CassetteInventory(network);

        inventory.RegisterExistingCopy("TAPE1", demo, "A");

        Assert.Throws<ArgumentException>(() =>
            inventory.RecordCopy(
                "TAPE1", "TAPE1", "A", "B"));

        Assert.IsFalse(
            inventory.TryGetCopy("TAPE2", out _));

        inventory.TryGetCopy("TAPE1", out var original);

        Assert.AreEqual("A", original.HolderNodeId);
    }

    [Test]
    public void Inventory_RejectsCopyFromWrongHolder()
    {
        var network = CreateNetwork();
        var (_, demo) = CreateDemo(network);
        var inventory = new CassetteInventory(network);

        inventory.RegisterExistingCopy("TAPE1", demo, "A");

        Assert.Throws<InvalidOperationException>(() =>
            inventory.RecordCopy(
                "TAPE1", "TAPE2", "B", "A"));

        Assert.Throws<ArgumentException>(() =>
            inventory.RecordCopy(
                "TAPE1", "TAPE2", "A", "UNKNOWN"));

        Assert.IsFalse(
            inventory.TryGetCopy("TAPE2", out _));
    }
}