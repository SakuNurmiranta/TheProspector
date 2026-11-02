using NUnit.Framework;
using SEMM91.Domain;

public class AddressLayerTests
{
    [Test]
    public void Address_ExistsWithoutOccupant()
    {
        var network = new Network();

        network.AddAddress("H1");

        Assert.IsTrue(network.HasAddress("H1"));
        Assert.IsFalse(
            network.TryGetOccupant("H1", out _));
    }

    [Test]
    public void Node_CanOccupyEmptyAddress()
    {
        var network = new Network();
        var node = new Node("A");

        network.AddNode(node);
        network.AddAddress("H1");

        network.PlaceNode("A", "H1");

        bool found = network.TryGetOccupant(
            "H1", out Node occupant);

        Assert.IsTrue(found);
        Assert.AreSame(node, occupant);
    }
}