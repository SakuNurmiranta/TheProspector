
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class MessagePackageTests
{
    private static (
        NetworkConnection ab,
        NetworkConnection ba,
        NetworkConnection insert,
        CassetteCopy cassette,
        CulturalProduct demo
    ) CreateFixture()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");

        network.AddConnection(
            "CONN-AB",
            "A",
            "B",
            CommunicationLayer.Interpersonal,
            "personal");

        network.AddConnection(
            "CONN-BA",
            "B",
            "A",
            CommunicationLayer.Interpersonal,
            "personal");

        network.AddConnection(
            "CONN-INSERT",
            "A",
            "B",
            CommunicationLayer.Insert,
            "distribution");

        var ab = network.GetOutgoingConnections(
            "A", CommunicationLayer.Interpersonal)[0];

        var ba = network.GetOutgoingConnections(
            "B", CommunicationLayer.Interpersonal)[0];

        var insert = network.GetOutgoingConnections(
            "A", CommunicationLayer.Insert)[0];

        var field = new Field("F1", network);
        field.RegisterLocalNode("A");

        field.SpawnArtist(
            "ART1",
            new Dictionary<string, int>
            {
                ["A"] = 10
            },
            new Random(123));

        var demo = field.CreateDemo(
            "D1", "ART1", true);

        var inventory = new CassetteInventory(network);

        var cassette = inventory.RegisterExistingCopy(
            "TAPE1", demo, "A");

        return (ab, ba, insert, cassette, demo);
    }

    [Test]
    public void Package_CanContainCassetteWithoutFeedback()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG1",
            f.ab,
            cassetteCopy: f.cassette);

        Assert.AreEqual("MSG1", package.Id);
        Assert.AreEqual("CONN-AB", package.ConnectionId);

        Assert.AreEqual("A", package.SourceNodeId);
        Assert.AreEqual("B", package.TargetNodeId);

        Assert.AreEqual("TAPE1", package.CassetteCopyId);
        Assert.AreEqual("D1", package.PayloadProductId);

        Assert.IsNull(package.FeedbackProductId);
        Assert.IsFalse(package.Thumb.HasValue);

        // Paketin muodostaminen ei siirrä kasettia.
        Assert.AreEqual("A", f.cassette.HolderNodeId);
    }

    [Test]
    public void Package_CanContainFeedbackWithoutCassette()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG2",
            f.ab,
            feedbackProduct: f.demo,
            thumb: -1);

        Assert.IsNull(package.CassetteCopyId);
        Assert.IsNull(package.PayloadProductId);

        Assert.AreEqual("D1", package.FeedbackProductId);
        Assert.AreEqual(-1, package.Thumb.Value);
    }

    [Test]
    public void Package_CanContainCassetteAndFeedback()
    {
        var f = CreateFixture();

        var package = new MessagePackage(
            "MSG3",
            f.ab,
            cassetteCopy: f.cassette,
            feedbackProduct: f.demo,
            thumb: +1);

        Assert.AreEqual("TAPE1", package.CassetteCopyId);
        Assert.AreEqual("D1", package.PayloadProductId);

        Assert.AreEqual("D1", package.FeedbackProductId);
        Assert.AreEqual(+1, package.Thumb.Value);
    }

    [Test]
    public void Package_RejectsEmptyOrIncompleteContent()
    {
        var f = CreateFixture();

        Assert.Throws<ArgumentException>(() =>
            new MessagePackage("EMPTY", f.ab));

        Assert.Throws<ArgumentException>(() =>
            new MessagePackage(
                "NO-THUMB",
                f.ab,
                feedbackProduct: f.demo));

        Assert.Throws<ArgumentException>(() =>
            new MessagePackage(
                "NO-PRODUCT",
                f.ab,
                thumb: +1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessagePackage(
                "NEUTRAL",
                f.ab,
                feedbackProduct: f.demo,
                thumb: 0));
    }

    [Test]
    public void Package_RequiresSenderToHoldCassette()
    {
        var f = CreateFixture();

        // B -> A on eri suunnattu yhteys.
        // Kasetti on edelleen A:n hallussa.
        Assert.Throws<InvalidOperationException>(() =>
            new MessagePackage(
                "MSG4",
                f.ba,
                cassetteCopy: f.cassette));

        Assert.AreEqual("A", f.cassette.HolderNodeId);
    }

    [Test]
    public void Package_RequiresInterpersonalConnection()
    {
        var f = CreateFixture();

        Assert.Throws<ArgumentException>(() =>
            new MessagePackage(
                "MSG5",
                f.insert,
                cassetteCopy: f.cassette));

        Assert.Throws<ArgumentException>(() =>
            new MessagePackage(
                "",
                f.ab,
                cassetteCopy: f.cassette));

        Assert.Throws<ArgumentNullException>(() =>
            new MessagePackage(
                "MSG6",
                null,
                cassetteCopy: f.cassette));
    }
}
