using System;
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
    
    
    [Test]
    public void AddressSlot_StoresCountryId()
    {
        var address = new AddressSlot("FI-01", "FI");

        Assert.AreEqual("FI", address.CountryId);
    }
    
    
    [Test]
    public void Network_RegistersAddressWithCountry()
    {
        var network = new Network();

        network.AddAddress("FI-01", "FI");

        bool found = network.TryGetAddress(
            "FI-01", out AddressSlot address);

        Assert.IsTrue(found);
        Assert.AreEqual("FI", address.CountryId);
    }
    
    
    [Test]
    public void Network_ClassifiesPostalDistanceByCountry()
    {
        var network = new Network();

        network.AddAddress("FI-01", "FI");
        network.AddAddress("FI-02", "FI");
        network.AddAddress("SE-01", "SE");

        Assert.AreEqual(
            PostalDistance.Short,
            network.GetPostalDistance("FI-01", "FI-02"));

        Assert.AreEqual(
            PostalDistance.Long,
            network.GetPostalDistance("FI-01", "SE-01"));
    }
    
    
    [Test]
    public void Network_RejectsPostalDistanceWithoutCountry()
    {
        var network = new Network();

        network.AddAddress("H1");
        network.AddAddress("H2");

        Assert.Throws<InvalidOperationException>(
            () => network.GetPostalDistance("H1", "H2"));
    }




}