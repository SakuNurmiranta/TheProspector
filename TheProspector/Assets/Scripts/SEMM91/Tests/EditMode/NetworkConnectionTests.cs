
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class NetworkConnectionTests
{
    [Test]
    public void Network_AllowsParallelConnections()
    {
        var network = new Network();
        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddConnection(
            "C1", "A", "B",
            CommunicationLayer.Interpersonal, "personal");

        network.AddConnection(
            "C2", "A", "B",
            CommunicationLayer.Interpersonal, "editor");

        var connections = network.GetOutgoingConnections(
            "A", CommunicationLayer.Interpersonal);

        Assert.AreEqual(2, connections.Count);
        Assert.AreEqual("personal", connections[0].Role);
        Assert.AreEqual("editor", connections[1].Role);
    }

    [Test]
    public void Network_ConnectionsAreDirected()
    {
        var network = new Network();
        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddConnection(
            "C1", "A", "B",
            CommunicationLayer.Interpersonal, "personal");

        Assert.AreEqual(1, network.GetOutgoingConnections(
            "A", CommunicationLayer.Interpersonal).Count);

        Assert.AreEqual(0, network.GetOutgoingConnections(
            "B", CommunicationLayer.Interpersonal).Count);
    }

    [Test]
    public void Network_SeparatesCommunicationLayers()
    {
        var network = new Network();
        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddConnection(
            "C1", "A", "B",
            CommunicationLayer.Interpersonal, "personal");

        network.AddConnection(
            "C2", "A", "B",
            CommunicationLayer.Insert, "distribution");

        Assert.AreEqual(1, network.GetOutgoingConnections(
            "A", CommunicationLayer.Interpersonal).Count);

        Assert.AreEqual(1, network.GetOutgoingConnections(
            "A", CommunicationLayer.Insert).Count);
    }

    [Test]
    public void Network_RejectsInvalidConnections()
    {
        var network = new Network();
        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddConnection(
            "C1", "A", "B",
            CommunicationLayer.Interpersonal, "personal");

        Assert.Throws<ArgumentException>(() =>
            network.AddConnection(
                "C1", "A", "B",
                CommunicationLayer.Interpersonal, "editor"));

        Assert.Throws<ArgumentException>(() =>
            network.AddConnection(
                "C2", "A", "UNKNOWN",
                CommunicationLayer.Interpersonal, "personal"));
    }
}
