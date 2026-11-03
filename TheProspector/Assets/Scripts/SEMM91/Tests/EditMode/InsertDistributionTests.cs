
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class InsertDistributionTests
{
    private static (
        NetworkConnection ab,
        NetworkConnection ac,
        NetworkConnection personal,
        CulturalProduct demo,
        CassetteCopy tape,
        Node b
    ) CreateFixture()
    {
        var network = new Network();

        network.AddNode(new Node("A"));

        var b = new Node("B");
        network.AddNode(b);

        network.AddNode(new Node("C"));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "FI");
        network.AddAddress("ADDR-C", "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");
        network.PlaceNode("C", "ADDR-C");

        network.AddConnection(
            "INSERT-AB",
            "A",
            "B",
            CommunicationLayer.Insert,
            "distribution");

        network.AddConnection(
            "INSERT-AC",
            "A",
            "C",
            CommunicationLayer.Insert,
            "distribution");

        network.AddConnection(
            "PERSONAL-AB",
            "A",
            "B",
            CommunicationLayer.Interpersonal,
            "personal");

        var inserts = network.GetOutgoingConnections(
            "A", CommunicationLayer.Insert);

        var ab = inserts.Single(
            c => c.Id == "INSERT-AB");

        var ac = inserts.Single(
            c => c.Id == "INSERT-AC");

        var personal = network.GetOutgoingConnections(
            "A", CommunicationLayer.Interpersonal)[0];

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

        var tape = inventory.RegisterExistingCopy(
            "TAPE1", demo, "A");

        return (ab, ac, personal, demo, tape, b);
    }

    [Test]
    public void Insert_RecordsExposureImmediately()
    {
        var f = CreateFixture();
        var distribution = new InsertDistributionRegistry();

        var exposure = distribution.Distribute(
            "INS1", f.ab, f.demo);

        Assert.AreEqual("INS1", exposure.Id);
        Assert.AreEqual("INSERT-AB", exposure.ConnectionId);
        Assert.AreEqual("A", exposure.SourceNodeId);
        Assert.AreEqual("B", exposure.TargetNodeId);
        Assert.AreEqual("D1", exposure.ProductId);

        Assert.AreEqual(
            1, distribution.GetForRecipient("B").Count);
    }

    [Test]
    public void Insert_BypassesBacklogAndPersonalReview()
    {
        var f = CreateFixture();

        var distribution = new InsertDistributionRegistry();
        var backlog = new RecipientBacklog();
        var reception = new MessageReceptionRegistry();
        var sir = new SIRStateRegistry();

        distribution.Distribute(
            "INS1", f.ab, f.demo);

        // Insertti näkyy jakelurekisterissä.
        Assert.AreEqual(
            1, distribution.GetForRecipient("B").Count);

        // Ei henkilökohtaista käsittelyjonoa.
        Assert.AreEqual(
            0, backlog.GetPendingCount("B"));

        // Ei automaattista perehtymistä.
        Assert.AreEqual(
            ProductReviewState.NotReceived,
            reception.GetReviewState("B", "D1"));

        Assert.IsNull(
            reception.GetPersonalOpinion("B", "D1"));

        // Ei automaattista SIR-siirtymää.
        Assert.AreEqual(
            SIRState.S,
            sir.GetNodeState(f.b, f.demo));

        // Fyysinen kasetti ei liiku.
        Assert.AreEqual("A", f.tape.HolderNodeId);
    }

    [Test]
    public void Insert_UsesDirectedTarget()
    {
        var f = CreateFixture();
        var distribution = new InsertDistributionRegistry();

        distribution.Distribute(
            "INS1", f.ab, f.demo);

        Assert.AreEqual(
            1, distribution.GetForRecipient("B").Count);

        Assert.AreEqual(
            0, distribution.GetForRecipient("A").Count);

        Assert.AreEqual(
            0, distribution.GetForRecipient("C").Count);
    }

    [Test]
    public void Insert_CanReachMultipleRecipients()
    {
        var f = CreateFixture();
        var distribution = new InsertDistributionRegistry();

        distribution.Distribute(
            "INS1", f.ab, f.demo);

        distribution.Distribute(
            "INS2", f.ac, f.demo);

        var b = distribution.GetForRecipient("B");
        var c = distribution.GetForRecipient("C");

        Assert.AreEqual(1, b.Count);
        Assert.AreEqual(1, c.Count);

        Assert.AreEqual("D1", b[0].ProductId);
        Assert.AreEqual("D1", c[0].ProductId);

        Assert.AreEqual("B", b[0].TargetNodeId);
        Assert.AreEqual("C", c[0].TargetNodeId);
    }

    [Test]
    public void Insert_RepeatedDistributionRemainsObservable()
    {
        var f = CreateFixture();
        var distribution = new InsertDistributionRegistry();

        distribution.Distribute(
            "INS1", f.ab, f.demo);

        distribution.Distribute(
            "INS2", f.ab, f.demo);

        var exposures = distribution.GetForRecipient("B");

        Assert.AreEqual(2, exposures.Count);
        Assert.AreEqual("INS1", exposures[0].Id);
        Assert.AreEqual("INS2", exposures[1].Id);

        // Toistuva näkyvyys ei vielä tarkoita
        // toistuvaa legitimiteettipalkkiota.
    }

    [Test]
    public void Insert_RejectsInvalidDistribution()
    {
        var f = CreateFixture();
        var distribution = new InsertDistributionRegistry();

        Assert.Throws<ArgumentException>(() =>
            distribution.Distribute(
                "INS1", f.personal, f.demo));

        Assert.Throws<ArgumentException>(() =>
            distribution.Distribute(
                "  ", f.ab, f.demo));

        Assert.Throws<ArgumentNullException>(() =>
            distribution.Distribute(
                "INS1", null, f.demo));

        Assert.Throws<ArgumentNullException>(() =>
            distribution.Distribute(
                "INS1", f.ab, null));

        distribution.Distribute(
            "INS1", f.ab, f.demo);

        Assert.Throws<ArgumentException>(() =>
            distribution.Distribute(
                "INS1", f.ac, f.demo));

        // Epäonnistunut rekisteröinti ei
        // saa tuottaa osittaista jakelua.
        Assert.AreEqual(
            1, distribution.GetForRecipient("B").Count);

        Assert.AreEqual(
            0, distribution.GetForRecipient("C").Count);
    }
}
