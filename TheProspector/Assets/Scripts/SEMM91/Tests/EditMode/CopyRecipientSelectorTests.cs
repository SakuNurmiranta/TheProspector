
using System;
using NUnit.Framework;
using SEMM91.Domain;

public class CopyRecipientSelectorTests
{
    private static KnownCollectionRegistry CreateKnowledge()
    {
        var network = new Network();

        foreach (var id in new[]
        {
            "A", "B", "C", "D", "E", "F"
        })
        {
            network.AddNode(new Node(id));
        }

        return new KnownCollectionRegistry(network);
    }

    private static void MarkKnownMissing(
        KnownCollectionRegistry known,
        params string[] recipients)
    {
        foreach (string recipient in recipients)
        {
            // Tunnettu kokoelmahistoria sisältää
            // D2:n mutta ei tarkasteltavaa D1:tä.
            known.RecordSnapshot(
                "A", recipient, new[] { "D2" });
        }
    }

    private static string[] Select(
        KnownCollectionRegistry known,
        string[] contacts,
        Func<string, bool> dislikes = null,
        Func<string, bool> canOffer = null)
    {
        var selector = new CopyRecipientSelector();

        var selected = selector.SelectAll(
            "A",
            "D1",
            contacts,
            known,
            dislikes ?? (_ => false),
            canOffer ?? (_ => true));

        var result = new string[selected.Count];

        for (int i = 0; i < selected.Count; i++)
            result[i] = selected[i];

        return result;
    }

    [Test]
    public void Selector_SelectsAllKnownMissingRecipients()
    {
        var known = CreateKnowledge();

        MarkKnownMissing(known, "B", "C", "D");

        CollectionAssert.AreEqual(
            new[] { "B", "C", "D" },
            Select(known, new[] { "B", "C", "D" }));
    }

    [Test]
    public void Selector_DoesNotSelectUnknownCollections()
    {
        var known = CreateKnowledge();

        // B:n kokoelmatilanne tunnetaan.
        // C:n tilanteesta ei ole mitään tietoa.
        MarkKnownMissing(known, "B");

        CollectionAssert.AreEqual(
            new[] { "B" },
            Select(known, new[] { "B", "C" }));
    }

    [Test]
    public void Selector_SkipsKnownOwners()
    {
        var known = CreateKnowledge();

        known.RecordSnapshot(
            "A", "B", new[] { "D1" });

        MarkKnownMissing(known, "C");

        CollectionAssert.AreEqual(
            new[] { "C" },
            Select(known, new[] { "B", "C" }));
    }

    [Test]
    public void Selector_SkipsCommunicatedDislike()
    {
        var known = CreateKnowledge();

        MarkKnownMissing(known, "B", "C", "D");

        CollectionAssert.AreEqual(
            new[] { "B", "D" },
            Select(
                known,
                new[] { "B", "C", "D" },
                dislikes: id => id == "C"));
    }

    [Test]
    public void Selector_RespectsOtherDispatchRestrictions()
    {
        var known = CreateKnowledge();

        MarkKnownMissing(known, "B", "C", "D");

        CollectionAssert.AreEqual(
            new[] { "B", "D" },
            Select(
                known,
                new[] { "B", "C", "D" },
                canOffer: id => id != "C"));
    }

    [Test]
    public void Selector_IgnoresSelfAndDuplicates()
    {
        var known = CreateKnowledge();

        MarkKnownMissing(known, "B", "C");

        CollectionAssert.AreEqual(
            new[] { "B", "C" },
            Select(
                known,
                new[] { "A", "B", "B", "C", "C" }));
    }

    [Test]
    public void Selector_PreservesContactOrder()
    {
        var known = CreateKnowledge();

        MarkKnownMissing(known, "B", "C", "D");

        CollectionAssert.AreEqual(
            new[] { "D", "B", "C" },
            Select(
                known,
                new[] { "D", "B", "C" }));
    }

    [Test]
    public void Selector_ReactsToUpdatedKnowledge()
    {
        var known = CreateKnowledge();

        MarkKnownMissing(known, "B", "C");

        CollectionAssert.AreEqual(
            new[] { "B", "C" },
            Select(known, new[] { "B", "C" }));

        // A saa myöhemmin tietää, että B
        // on vastaanottanut myös D1:n.
        known.RecordSnapshot(
            "A", "B", new[] { "D1" });

        CollectionAssert.AreEqual(
            new[] { "C" },
            Select(known, new[] { "B", "C" }));
    }

    [Test]
    public void Selector_ReturnsEmptyWhenNoEligibleRecipients()
    {
        var known = CreateKnowledge();

        // Pelkkä tuntemattomien kontaktien joukko
        // ei tuota yhtäkään lähetyskohdetta.
        Assert.AreEqual(
            0,
            Select(known, new[] { "B", "C" }).Length);

        MarkKnownMissing(known, "B", "C");

        Assert.AreEqual(
            0,
            Select(
                known,
                new[] { "B", "C" },
                dislikes: _ => true).Length);

        Assert.AreEqual(
            0,
            Select(known, Array.Empty<string>()).Length);
    }

    [Test]
    public void Selector_RejectsInvalidArguments()
    {
        var known = CreateKnowledge();
        var selector = new CopyRecipientSelector();

        Func<string, bool> dislikes = _ => false;
        Func<string, bool> canOffer = _ => true;

        Assert.Throws<ArgumentException>(() =>
            selector.SelectAll(
                "", "D1", new[] { "B" },
                known, dislikes, canOffer));

        Assert.Throws<ArgumentException>(() =>
            selector.SelectAll(
                "A", "", new[] { "B" },
                known, dislikes, canOffer));

        Assert.Throws<ArgumentNullException>(() =>
            selector.SelectAll(
                "A", "D1", null,
                known, dislikes, canOffer));

        Assert.Throws<ArgumentNullException>(() =>
            selector.SelectAll(
                "A", "D1", new[] { "B" },
                null, dislikes, canOffer));

        Assert.Throws<ArgumentNullException>(() =>
            selector.SelectAll(
                "A", "D1", new[] { "B" },
                known, null, canOffer));

        Assert.Throws<ArgumentNullException>(() =>
            selector.SelectAll(
                "A", "D1", new[] { "B" },
                known, dislikes, null));

        Assert.Throws<ArgumentException>(() =>
            selector.SelectAll(
                "A", "D1", new[] { " " },
                known, dislikes, canOffer));
    }
}
