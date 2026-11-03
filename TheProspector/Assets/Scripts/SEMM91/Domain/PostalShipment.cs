
using System;

namespace SEMM91.Domain
{
    public sealed class PostalShipment
    {
        private readonly Network _network;
        private readonly CassetteInventory _inventory;

        public string Id { get; }
        public MessagePackage Package { get; }

        public string DestinationAddressId { get; }
        public string ReturnAddressId { get; }

        public PostalDistance Distance { get; }

        public PostalShipmentStatus Status { get; private set; }

        private PostalShipment(
            string id,
            Network network,
            CassetteInventory inventory,
            MessagePackage package,
            string destinationAddressId,
            string returnAddressId,
            PostalDistance distance)
        {
            Id = id;
            _network = network;
            _inventory = inventory;
            Package = package;

            DestinationAddressId = destinationAddressId;
            ReturnAddressId = returnAddressId;
            Distance = distance;

            Status = PostalShipmentStatus.InTransit;
        }

        public static PostalShipment Dispatch(
            string shipmentId,
            Network network,
            CassetteInventory inventory,
            MessagePackage package)
        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException(
                    "Shipment ID cannot be empty.",
                    nameof(shipmentId));

            if (network == null)
                throw new ArgumentNullException(nameof(network));

            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            if (package == null)
                throw new ArgumentNullException(nameof(package));

            if (!network.TryGetNode(
                    package.SourceNodeId, out _) ||
                !network.TryGetNode(
                    package.TargetNodeId, out _))
                throw new ArgumentException(
                    "Package endpoints do not exist.");

            // Lähettäjä ei voi käyttää vastaanottajan
            // todellista osoitetta automaattisesti.
            if (!network.TryGetKnownAddress(
                    package.SourceNodeId,
                    package.TargetNodeId,
                    out var destinationId))
                throw new InvalidOperationException(
                    "Sender does not know destination address.");

            if (!network.TryGetNodeAddress(
                    package.SourceNodeId,
                    out var returnId))
                throw new InvalidOperationException(
                    "Sender has no return address.");

            if (!network.TryGetAddress(destinationId, out _))
                throw new InvalidOperationException(
                    "Destination address is not registered.");

            var distance = network.GetPostalDistance(
                returnId,
                destinationId);

            var shipment = new PostalShipment(
                shipmentId,
                network,
                inventory,
                package,
                destinationId,
                returnId,
                distance);

            // Vasta onnistuneen alustuksen jälkeen
            // fyysinen kasetti siirtyy kuljetukseen.
            if (package.CassetteCopyId != null)
            {
                inventory.BeginShipment(
                    package.CassetteCopyId,
                    package.SourceNodeId,
                    shipmentId);
            }

            return shipment;
        }

        public void ProcessArrival()
        {
            if (Status != PostalShipmentStatus.InTransit)
                throw new InvalidOperationException(
                    "Shipment is not travelling to recipient.");

            bool recipientPresent =
                _network.TryGetOccupant(
                    DestinationAddressId,
                    out var occupant) &&
                occupant.Id == Package.TargetNodeId;

            if (!recipientPresent)
            {
                Status = PostalShipmentStatus.Returning;
                return;
            }

            if (Package.CassetteCopyId != null)
            {
                _inventory.CompleteShipment(
                    Package.CassetteCopyId,
                    Id,
                    Package.TargetNodeId);
            }

            Status = PostalShipmentStatus.Delivered;
        }

        public void ProcessReturn()
        {
            if (Status != PostalShipmentStatus.Returning)
                throw new InvalidOperationException(
                    "Shipment is not returning.");

            bool senderPresent =
                _network.TryGetOccupant(
                    ReturnAddressId,
                    out var occupant) &&
                occupant.Id == Package.SourceNodeId;

            if (!senderPresent)
                throw new InvalidOperationException(
                    "Sender is not at the return address.");

            if (Package.CassetteCopyId != null)
            {
                _inventory.CompleteShipment(
                    Package.CassetteCopyId,
                    Id,
                    Package.SourceNodeId);
            }

            Status = PostalShipmentStatus.Returned;
        }
    }
}
