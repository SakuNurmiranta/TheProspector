
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Domain;

public class CulturalCapitalLedgerTests
{
    private static (Network network, Field field)
        CreateFixture()
    {
        var network = new Network();

        network.AddNode(new Node("A"));
        network.AddNode(new Node("B"));

        network.AddAddress("ADDR-A", "FI");
        network.AddAddress("ADDR-B", "FI");

        network.PlaceNode("A", "ADDR-A");
        network.PlaceNode("B", "ADDR-B");

        var field = new Field("F1", network);

        field.RegisterLocalNode("A");
        field.RegisterLocalNode("B");

        return (network, field);
    }

    [Test]
    public void Capital_InitialValuesDriveFieldShares()
    {
        var f = CreateFixture();
        var ledger = new CulturalCapitalLedger(f.network);

        ledger.InitializeNode("A", 10);
        ledger.InitializeNode("B", 30);

        Assert.AreEqual(10, ledger.GetCapital("A"));
        Assert.AreEqual(30, ledger.GetCapital("B"));

        var shares = f.field.CalculateShares(
            ledger.Snapshot());

        Assert.AreEqual(0.25, shares["A"], 0.000001);
        Assert.AreEqual(0.75, shares["B"], 0.000001);
    }

    [Test]
    public void Capital_CreditChangesFutureFieldShares()
    {
        var f = CreateFixture();
        var ledger = new CulturalCapitalLedger(f.network);

        ledger.InitializeNode("A", 10);
        ledger.InitializeNode("B", 30);

        var before = f.field.CalculateShares(
            ledger.Snapshot());

        ledger.Credit("A", 20);

        var after = f.field.CalculateShares(
            ledger.Snapshot());

        Assert.AreEqual(0.25, before["A"], 0.000001);
        Assert.AreEqual(0.50, after["A"], 0.000001);

        Assert.AreEqual(30, ledger.GetCapital("A"));
        Assert.AreEqual(30, ledger.GetCapital("B"));
    }

    [Test]
    public void Capital_CassetteCopyDoesNotAutomaticallyAward()
    {
        var f = CreateFixture();
        var ledger = new CulturalCapitalLedger(f.network);

        ledger.InitializeNode("A", 10);
        ledger.InitializeNode("B", 10);

        f.field.SpawnArtist(
            "ART1",
            ledger.Snapshot(),
            new Random(123));

        var demo = f.field.CreateDemo(
            "D1", "ART1", true);

        var inventory = new CassetteInventory(f.network);

        inventory.RegisterExistingCopy(
            "TAPE1", demo, "A");

        inventory.RecordCopy(
            "TAPE1", "TAPE2", "A", "B");

        // Pelkkä kopion rekisteröinti ei vielä
        // suorita palkkiosääntöä.
        Assert.AreEqual(10, ledger.GetCapital("A"));
        Assert.AreEqual(10, ledger.GetCapital("B"));
    }

    [Test]
    public void Capital_RejectsInvalidInitializationAndCredits()
    {
        var f = CreateFixture();
        var ledger = new CulturalCapitalLedger(f.network);

        Assert.Throws<ArgumentException>(() =>
            ledger.InitializeNode("UNKNOWN", 10));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ledger.InitializeNode("A", -1));

        ledger.InitializeNode("A", 0);

        Assert.Throws<InvalidOperationException>(() =>
            ledger.InitializeNode("A", 10));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ledger.Credit("A", 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ledger.Credit("A", -4));

        Assert.Throws<InvalidOperationException>(() =>
            ledger.Credit("B", 4));

        Assert.AreEqual(0, ledger.GetCapital("A"));
    }

    [Test]
    public void Capital_SnapshotIsReadOnlyAndIndependent()
    {
        var f = CreateFixture();
        var ledger = new CulturalCapitalLedger(f.network);

        ledger.InitializeNode("A", 10);
        ledger.InitializeNode("B", 20);

        var original = ledger.Snapshot();

        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, int>)original)["A"] = 999);

        ledger.Credit("A", 4);

        // Aiempi snapshot ei muutu.
        Assert.AreEqual(10, original["A"]);

        // Uusi snapshot sisältää päivitetyn arvon.
        var updated = ledger.Snapshot();

        Assert.AreEqual(14, updated["A"]);
        Assert.AreEqual(20, updated["B"]);
    }

    [Test]
    public void Capital_OverflowDoesNotChangeBalance()
    {
        var f = CreateFixture();
        var ledger = new CulturalCapitalLedger(f.network);

        ledger.InitializeNode("A", int.MaxValue - 1);

        Assert.Throws<OverflowException>(() =>
            ledger.Credit("A", 2));

        Assert.AreEqual(
            int.MaxValue - 1,
            ledger.GetCapital("A"));
    }
}
