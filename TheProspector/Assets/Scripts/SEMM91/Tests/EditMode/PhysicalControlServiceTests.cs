
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public class PhysicalControlServiceTests
{
    private sealed class Fixture
    {
        public AutonomousCirculationScenario Scenario;
        public PhysicalControlService Control;
        public CassetteCopy Source;
    }

    private static Fixture Create()
    {
        // B:n kokoelmatieto C:stä on aluksi
        // tuntematon, joten vanha myönteinen
        // kopiointipolitiikka ei levitä D1:tä.
        var s = new AutonomousCirculationScenario(
            bKnowsCIsMissing: false,
            cKnowsDIsMissing: true);

        s.World.AdvanceDays(2);

        // A -> B saapui. B pitää nyt kasettia.
        var source = s.SeedWave[0].Copy;

        Assert.AreEqual("B", source.HolderNodeId);
        Assert.AreEqual(10, s.Capital.GetCapital("B"));

        // Tämän jälkeen B:n tieto C:n kokoelmasta
        // on riittävä siirtoreitin valintaan.
        s.Known.RecordSnapshot(
            "B", "C", Array.Empty<string>());

        var bc = s.Network.GetOutgoingConnections(
                "B", CommunicationLayer.Interpersonal)
            .Single(c => c.Id == "BC");

        var cd = s.Network.GetOutgoingConnections(
                "C", CommunicationLayer.Interpersonal)
            .Single(c => c.Id == "CD");

        IEnumerable<CopyDistributionRoute> Routes(string nodeId)
        {
            if (nodeId == "B")
            {
                return new[]
                {
                    new CopyDistributionRoute(
                        "C", bc, "FI")
                };
            }

            if (nodeId == "C")
            {
                return new[]
                {
                    new CopyDistributionRoute(
                        "D", cd, "FI")
                };
            }

            return Array.Empty<CopyDistributionRoute>();
        }

        var control = new PhysicalControlService(
            s.Exchange,
            Routes,
            productId =>
                productId == "D1" ? s.Demo : null,
            nodeId => "FI",
            (senderId, recipientId, productId) =>
                s.Known.IsKnownToHold(
                    senderId,
                    recipientId,
                    productId) == false,
            (shipment, copy) =>
                s.CopyIndex.RegisterTransfer(
                    shipment, copy));

        return new Fixture
        {
            Scenario = s,
            Control = control,
            Source = source
        };
    }

    [Test]
    public void Control_ShelvingPreservesPhysicalHolder()
    {
        var f = Create();

        f.Control.Shelve("B", f.Source, 2);

        Assert.AreEqual("B", f.Source.HolderNodeId);

        Assert.AreEqual(
            PhysicalControlDisposition.Shelved,
            f.Control.GetDisposition("B", f.Source.Id));

        Assert.AreEqual(1, f.Control.Shelves.Count);
        Assert.AreEqual(0, f.Control.Transfers.Count);

        Assert.AreEqual(0, f.Scenario.Clock.PendingCount);
        Assert.AreEqual(1, f.Scenario.CopyIndex.Count);
    }

    [Test]
    public void Control_ShelvingDoesNotAwardCapital()
    {
        var f = Create();

        f.Control.Shelve("B", f.Source, 2);

        f.Scenario.Clock.AdvanceDays(4);

        Assert.AreEqual(14,
            f.Scenario.Capital.GetCapital("A"));

        Assert.AreEqual(10,
            f.Scenario.Capital.GetCapital("B"));

        Assert.IsFalse(
            f.Scenario.Inventory.HasEverHeldProduct(
                "C", "D1"));
    }

    [Test]
    public void Control_TransferUsesSamePhysicalCassette()
    {
        var f = Create();

        f.Control.TransferOne("B", f.Source, 2);

        Assert.AreEqual(1, f.Control.Transfers.Count);

        var transfer = f.Control.Transfers[0];

        Assert.AreEqual("B", transfer.SenderNodeId);
        Assert.AreEqual("C", transfer.RecipientNodeId);

        Assert.AreSame(f.Source, transfer.Copy);

        Assert.AreEqual(
            PostalShipmentStatus.InTransit,
            transfer.Shipment.Status);

        // Indexin kaksi lähetysriviä viittaavat
        // samaan B:lle tuotettuun kasettiin.
        Assert.AreEqual(2, f.Scenario.CopyIndex.Count);
    }

    [Test]
    public void Control_TransferCarriesNegativeFeedback()
    {
        var f = Create();

        f.Control.TransferOne("B", f.Source, 2);

        var package =
            f.Control.Transfers[0].Shipment.Package;

        Assert.AreEqual(-1, package.Thumb);
        Assert.AreEqual("D1", package.FeedbackProductId);

        // Lähetys ei vielä merkitse sitä, että
        // C olisi avannut kirjeen.
        Assert.IsNull(
            f.Scenario.Stances.GetLastThumb(
                "C", "B", "D1"));

        f.Scenario.Clock.AdvanceDays(2);

        Assert.IsTrue(
            f.Scenario.Exchange.TryOpenNext(
                "C", out _));

        Assert.AreEqual(-1,
            f.Scenario.Stances.GetLastThumb(
                "C", "B", "D1"));
    }

    [Test]
    public void Control_TransferArrivesWithoutCopyReward()
    {
        var f = Create();

        f.Control.TransferOne("B", f.Source, 2);

        f.Scenario.Clock.AdvanceDays(2);

        Assert.AreEqual(
            PostalShipmentStatus.Delivered,
            f.Control.Transfers[0].Shipment.Status);

        Assert.AreEqual("C", f.Source.HolderNodeId);

        Assert.IsTrue(
            f.Scenario.Inventory.HasEverHeldProduct(
                "C", "D1"));

        Assert.AreEqual(14,
            f.Scenario.Capital.GetCapital("A"));

        Assert.AreEqual(10,
            f.Scenario.Capital.GetCapital("B"));
    }

    [Test]
    public void Control_TransferCanContinueThroughNextNode()
    {
        var f = Create();

        f.Control.TransferOne("B", f.Source, 2);

        f.Scenario.Clock.AdvanceDays(2);

        Assert.AreEqual("C", f.Source.HolderNodeId);

        // C voi lähettää saman kasetin D:lle.
        f.Control.TransferOne("C", f.Source, 4);

        Assert.AreEqual(2, f.Control.Transfers.Count);

        Assert.AreSame(
            f.Control.Transfers[0].Copy,
            f.Control.Transfers[1].Copy);

        f.Scenario.Clock.AdvanceDays(2);

        Assert.AreEqual("D", f.Source.HolderNodeId);

        Assert.IsTrue(
            f.Scenario.Inventory.HasEverHeldProduct(
                "D", "D1"));

        Assert.AreEqual(3, f.Scenario.CopyIndex.Count);

        Assert.AreEqual(10,
            f.Scenario.Capital.GetCapital("B"));

        Assert.AreEqual(10,
            f.Scenario.Capital.GetCapital("C"));
    }

    [Test]
    public void Control_ShelvedCassetteCannotBeTransferredBySameNode()
    {
        var f = Create();

        f.Control.Shelve("B", f.Source, 2);

        Assert.Throws<InvalidOperationException>(() =>
            f.Control.TransferOne("B", f.Source, 2));

        Assert.AreEqual(0, f.Control.Transfers.Count);
        Assert.AreEqual(0, f.Scenario.Clock.PendingCount);
    }

    [Test]
    public void Control_TransferredCassetteCannotBeShelvedBySender()
    {
        var f = Create();

        f.Control.TransferOne("B", f.Source, 2);

        Assert.Throws<InvalidOperationException>(() =>
            f.Control.Shelve("B", f.Source, 2));

        Assert.AreEqual(
            PhysicalControlDisposition.Transferred,
            f.Control.GetDisposition("B", f.Source.Id));
    }

    [Test]
    public void Control_RejectsOperationsByWrongHolder()
    {
        var f = Create();

        Assert.Throws<InvalidOperationException>(() =>
            f.Control.Shelve("C", f.Source, 2));

        Assert.Throws<InvalidOperationException>(() =>
            f.Control.TransferOne("C", f.Source, 2));

        Assert.AreEqual(0, f.Control.Transfers.Count);
        Assert.AreEqual(0, f.Control.Shelves.Count);
    }

    [Test]
    public void Control_RejectsTransferWithoutEligibleRecipient()
    {
        var f = Create();

        // B saa tietää, että C:llä on jo D1.
        f.Scenario.Known.RecordSnapshot(
            "B", "C", new[] { "D1" });

        Assert.Throws<InvalidOperationException>(() =>
            f.Control.TransferOne("B", f.Source, 2));

        Assert.IsNull(
            f.Control.GetDisposition("B", f.Source.Id));

        Assert.AreEqual(0, f.Control.Transfers.Count);
        Assert.AreEqual(0, f.Scenario.Clock.PendingCount);
    }
}
