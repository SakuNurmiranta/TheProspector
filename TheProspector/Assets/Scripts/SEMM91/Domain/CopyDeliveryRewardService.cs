
using System;

namespace SEMM91.Domain
{
    public sealed class CopyDeliveryRewardService
    {
        private const int RewardAmount = 4;

        private readonly Network _network;
        private readonly CassetteInventory _inventory;
        private readonly CulturalCapitalLedger _capital;

        public CopyDeliveryRewardService(
            Network network,
            CassetteInventory inventory,
            CulturalCapitalLedger capital)
        {
            _network = network ??
                throw new ArgumentNullException(
                    nameof(network));

            _inventory = inventory ??
                throw new ArgumentNullException(
                    nameof(inventory));

            _capital = capital ??
                throw new ArgumentNullException(
                    nameof(capital));
        }

        public int ProcessArrivalAndAward(
            PostalShipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException(
                    nameof(shipment));

            if (shipment.Status !=
                PostalShipmentStatus.InTransit)
            {
                throw new InvalidOperationException(
                    "Shipment is not awaiting arrival.");
            }

            var package = shipment.Package;

            bool rewardEligible = false;

            if (package.CassetteCopyId != null)
            {
                if (!_inventory.TryGetCopy(
                        package.CassetteCopyId,
                        out var copy))
                {
                    throw new InvalidOperationException(
                        "Shipment cassette is not registered.");
                }

                if (copy.InTransitShipmentId != shipment.Id)
                {
                    throw new InvalidOperationException(
                        "Cassette is not in this shipment.");
                }

                // Todellinen toimituskohde tarkistetaan
                // vasta saapumistapahtumassa.
                //
                // Tämä on simulaattorin sisäinen tarkistus,
                // ei lähettävän solmun käytettävissä oleva
                // kaikkitietävä tieto.
                bool recipientPresent =
                    _network.TryGetOccupant(
                        shipment.DestinationAddressId,
                        out var occupant) &&
                    occupant.Id == package.TargetNodeId;

                // Palkkio vaatii:
                // 1. Onnistuneen toimituksen.
                // 2. Kopioijan oman valmistaman kopion.
                // 3. Uuden vastaanottajan.
                // 4. Eri lähettäjän ja vastaanottajan.
                rewardEligible =
                    recipientPresent &&
                    package.SourceNodeId !=
                        package.TargetNodeId &&
                    copy.CopiedByNodeId ==
                        package.SourceNodeId &&
                    !_inventory.HasEverHeldProduct(
                        package.TargetNodeId,
                        copy.ProductId);


                if (rewardEligible)
                {
                    // Tarkistetaan ylivuoto ennen toimitusta.
                    // Tulosta ei tarvitse tallentaa.
                    _ = checked(
                        _capital.GetCapital(package.SourceNodeId)
                        + RewardAmount);
                }

            }

            // Postijärjestelmä suorittaa varsinaisen
            // toimituksen tai käynnistää palautuksen.
            shipment.ProcessArrival();

            if (shipment.Status !=
                PostalShipmentStatus.Delivered)
            {
                return 0;
            }

            if (!rewardEligible)
            {
                return 0;
            }

            // Tässä vaiheessa kasetti on toimitettu
            // ja vastaanottajan omistushistoria
            // on päivitetty.
            _capital.Credit(
                package.SourceNodeId,
                RewardAmount);

            return RewardAmount;
        }
    }
}
