using System;
using NUnit.Framework;
using SEMM91.Domain;

public class NodeMovementTests
{
    [Test]
    public void MoveNode_OccupiesNewAddress()
    {
        var network = new Network();
        var node = new Node("A");

        network.AddNode(node);
        network.AddAddress("H1");
        network.AddAddress("H2");

        network.PlaceNode("A", "H1");
        network.MoveNode("A", "H2");

        Assert.IsTrue(
            network.TryGetOccupant("H2", out Node occupant));

        Assert.AreSame(node, occupant);
    }

    [Test]
    public void MoveNode_ReleasesOldAddress()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddAddress("H1");
        network.AddAddress("H2");

        network.PlaceNode("A", "H1");
        network.MoveNode("A", "H2");

        Assert.IsFalse(
            network.TryGetOccupant("H1", out _));
    }

    [Test]
    public void MoveNode_RejectsOccupiedDestination()
    {
        var network = new Network();
        var a = new Node("A");
        var b = new Node("B");

        network.AddNode(a);
        network.AddNode(b);

        network.AddAddress("H1");
        network.AddAddress("H2");

        network.PlaceNode("A", "H1");
        network.PlaceNode("B", "H2");

        Assert.Throws<InvalidOperationException>(
            () => network.MoveNode("A", "H2"));

        network.TryGetOccupant("H1", out Node first);
        network.TryGetOccupant("H2", out Node second);

        Assert.AreSame(a, first);
        Assert.AreSame(b, second);
    }

    [Test]
    public void MoveNode_PreservesNodeIdentity()
    {
        var network = new Network();
        var original = new Node("A");

        network.AddNode(original);
        network.AddAddress("H1");
        network.AddAddress("H2");

        network.PlaceNode("A", "H1");
        network.MoveNode("A", "H2");

        network.TryGetNode("A", out Node result);

        Assert.AreSame(original, result);
    }
    
    [Test]
    public void MoveNode_ChangesPostalDistance()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));
        network.AddNode(new Node("C"));

        network.AddAddress("FI-01", "FI");
        network.AddAddress("FI-02", "FI");
        network.AddAddress("SE-01", "SE");
        network.AddAddress("SE-02", "SE");

        network.PlaceNode("A", "FI-01");
        network.PlaceNode("B", "FI-02");
        network.PlaceNode("C", "SE-02");

        network.TryGetNodeAddress("A", out string aAddress);
        network.TryGetNodeAddress("B", out string bAddress);
        network.TryGetNodeAddress("C", out string cAddress);

        Assert.AreEqual(
            PostalDistance.Short,
            network.GetPostalDistance(aAddress, bAddress));

        Assert.AreEqual(
            PostalDistance.Long,
            network.GetPostalDistance(bAddress, cAddress));

        // B muuttaa Ruotsiin.
        network.MoveNode("B", "SE-01");

        network.TryGetNodeAddress("B", out bAddress);

        Assert.AreEqual("SE-01", bAddress);

        Assert.AreEqual(
            PostalDistance.Long,
            network.GetPostalDistance(aAddress, bAddress));

        Assert.AreEqual(
            PostalDistance.Short,
            network.GetPostalDistance(bAddress, cAddress));
    }

    [Test]
    public void UnplacedNode_HasNoAddress()
    {
        var network = new Network();
        network.AddNode(new Node("A"));

        Assert.IsFalse(
            network.TryGetNodeAddress("A", out _));
    }

    
}