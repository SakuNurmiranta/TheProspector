using System;
using NUnit.Framework;
using SEMM91.Domain;

public class NetworkTests
{
    [Test]
    public void Network_RegistersFiveNodes()
    {
        var network = new Network();

        for (int i = 0; i < 5; i++)
            network.AddNode(new Node($"N{i}"));

        Assert.AreEqual(5, network.NodeCount);
    }

    [Test]
    public void Network_RejectsDuplicateIds()
    {
        var network = new Network();
        network.AddNode(new Node("A"));

        Assert.Throws<ArgumentException>(
            () => network.AddNode(new Node("A")));

        Assert.AreEqual(1, network.NodeCount);
    }

    [Test]
    public void Network_ReturnsRegisteredNode()
    {
        var network = new Network();
        var original = new Node("A");

        network.AddNode(original);

        bool found = network.TryGetNode("A", out Node result);

        Assert.IsTrue(found);
        Assert.AreSame(original, result);
    }

    [Test]
    public void Network_RejectsNullNode()
    {
        var network = new Network();

        Assert.Throws<ArgumentNullException>(
            () => network.AddNode(null));
    }
}