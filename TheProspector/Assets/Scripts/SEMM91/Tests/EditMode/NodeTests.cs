using System;
using NUnit.Framework;
using SEMM91.Domain;

public class NodeTests
{
    [Test]
    public void Node_StoresProvidedId()
    {
        var node = new Node("A");

        Assert.AreEqual("A", node.Id);
    }

    [Test]
    public void Node_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(
            () => new Node(""));
    }

    [Test]
    public void Nodes_CanHaveDifferentIds()
    {
        var a = new Node("A");
        var b = new Node("B");

        Assert.AreNotEqual(a.Id, b.Id);
    }
    
    [Test]
    public void Node_RejectsWhitespaceId()
    {
        Assert.Throws<ArgumentException>(
            () => new Node("   "));
    }
}