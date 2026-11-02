
using NUnit.Framework;
using SEMM91.Domain;

public class AddressKnowledgeTests
{
    [Test]
    public void Node_CanLearnAnotherNodesAddress()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));
        network.AddAddress("FI-01", "FI");

        network.LearnAddress("A", "B", "FI-01");

        bool found = network.TryGetKnownAddress(
            "A", "B", out string address);

        Assert.IsTrue(found);
        Assert.AreEqual("FI-01", address);
    }

    [Test]
    public void MovingDoesNotUpdateOtherNodesKnowledge()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddAddress("FI-01", "FI");
        network.AddAddress("SE-01", "SE");

        network.PlaceNode("B", "FI-01");
        network.LearnAddress("A", "B", "FI-01");

        network.MoveNode("B", "SE-01");

        network.TryGetKnownAddress(
            "A", "B", out string knownAddress);

        Assert.AreEqual("FI-01", knownAddress);
    }

    [Test]
    public void Node_CanUpdateKnownAddress()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddAddress("FI-01", "FI");
        network.AddAddress("SE-01", "SE");

        network.LearnAddress("A", "B", "FI-01");
        network.LearnAddress("A", "B", "SE-01");

        network.TryGetKnownAddress(
            "A", "B", out string address);

        Assert.AreEqual("SE-01", address);
    }

    [Test]
    public void MovingNodeRetainsItsOwnKnownAddresses()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddAddress("FI-01", "FI");
        network.AddAddress("FI-02", "FI");
        network.AddAddress("SE-01", "SE");

        network.PlaceNode("A", "FI-01");
        network.PlaceNode("B", "FI-02");

        network.LearnAddress("A", "B", "FI-02");

        network.MoveNode("A", "SE-01");

        bool found = network.TryGetKnownAddress(
            "A", "B", out string address);

        Assert.IsTrue(found);
        Assert.AreEqual("FI-02", address);
    }
}
