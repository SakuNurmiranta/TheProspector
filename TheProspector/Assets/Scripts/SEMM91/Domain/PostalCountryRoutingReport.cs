using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// Immutable observation of one postal shipment.
    ///
    /// Country information comes from the original
    /// dispatch and destination address slots,
    /// not the nodes' later residences.
    /// </summary>
    public sealed class PostalCountryRouteObservation
    {
        public string ShipmentId { get; }
        public string PackageId { get; }

        public string SenderNodeId { get; }
        public string RecipientNodeId { get; }

        public string SourceCountry { get; }
        public string DestinationCountry { get; }

        public PostalDistance Distance { get; }
        public PostalShipmentStatus Status { get; }

        public bool IsPhysical { get; }
        public bool IsFirstContactRequest { get; }
        public bool IsFirstContactReply { get; }

        public bool IsInternational =>
            Distance == PostalDistance.Long;

        internal PostalCountryRouteObservation(
            PostalShipment shipment,
            Network network)
        {
            if (shipment == null)
                throw new ArgumentNullException(
                    nameof(shipment));

            if (network == null)
                throw new ArgumentNullException(
                    nameof(network));

            if (!network.TryGetAddress(
                    shipment.ReturnAddressId,
                    out var sourceAddress))
            {
                throw new InvalidOperationException(
                    "Source address is not registered.");
            }

            if (!network.TryGetAddress(
                    shipment.DestinationAddressId,
                    out var destinationAddress))
            {
                throw new InvalidOperationException(
                    "Destination address is not registered.");
            }

            var package = shipment.Package;

            ShipmentId = shipment.Id;
            PackageId = package.Id;

            SenderNodeId = package.SourceNodeId;
            RecipientNodeId = package.TargetNodeId;

            SourceCountry = sourceAddress.CountryId;
            DestinationCountry = destinationAddress.CountryId;

            Distance = shipment.Distance;
            Status = shipment.Status;

            IsPhysical =
                package.CassetteCopyId != null;

            IsFirstContactRequest =
                package.ConnectionId.StartsWith(
                    "FIRST-ROUTE:",
                    StringComparison.Ordinal);

            IsFirstContactReply =
                package.ConnectionId.StartsWith(
                    "FIRST-REPLY-ROUTE:",
                    StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return
                "Shipment=" + ShipmentId +
                " Package=" + PackageId +
                " Route=" + SourceCountry +
                "->" + DestinationCountry +
                " Nodes=" + SenderNodeId +
                "->" + RecipientNodeId +
                " Distance=" + Distance +
                " Status=" + Status +
                " Physical=" + IsPhysical +
                " FirstRequest=" + IsFirstContactRequest +
                " FirstReply=" + IsFirstContactReply;
        }
    }

    /// <summary>
    /// Counts all postal traffic between two countries.
    /// </summary>
    public sealed class PostalCountryRouteSummary
    {
        public string SourceCountry { get; }
        public string DestinationCountry { get; }

        public int ShipmentCount { get; }
        public int DeliveredCount { get; }
        public int PhysicalCount { get; }

        internal PostalCountryRouteSummary(
            string sourceCountry,
            string destinationCountry,
            IReadOnlyList<PostalCountryRouteObservation>
                observations)
        {
            SourceCountry = sourceCountry;
            DestinationCountry = destinationCountry;

            ShipmentCount = observations.Count;

            DeliveredCount = observations.Count(s =>
                s.Status ==
                PostalShipmentStatus.Delivered);

            PhysicalCount = observations.Count(s =>
                s.IsPhysical);
        }

        public override string ToString()
        {
            return
                SourceCountry + "->" + DestinationCountry +
                " Shipments=" + ShipmentCount +
                " Delivered=" + DeliveredCount +
                " Physical=" + PhysicalCount;
        }
    }

    /// <summary>
    /// Read-only routing diagnostics for all postal traffic.
    ///
    /// Unlike CirculationResearchReport, this includes
    /// letters without a physical cassette.
    ///
    /// Capturing the report does not advance the clock,
    /// open mail or change the network.
    /// </summary>
    public sealed class PostalCountryRoutingReport
    {
        public int Day { get; }

        public IReadOnlyList<PostalCountryRouteObservation>
            Shipments { get; }

        public IReadOnlyList<PostalCountryRouteSummary>
            Routes { get; }

        public int TotalShipments => Shipments.Count;

        public int PhysicalShipments =>
            Shipments.Count(s => s.IsPhysical);

        public int NonPhysicalShipments =>
            Shipments.Count(s => !s.IsPhysical);

        public int InternationalShipments =>
            Shipments.Count(s => s.IsInternational);

        public int DomesticShipments =>
            Shipments.Count(s => !s.IsInternational);

        public int DeliveredShipments =>
            Shipments.Count(s =>
                s.Status == PostalShipmentStatus.Delivered);

        public int InternationalDelivered =>
            Shipments.Count(s =>
                s.IsInternational &&
                s.Status == PostalShipmentStatus.Delivered);

        public int FirstContactRequests =>
            Shipments.Count(s =>
                s.IsFirstContactRequest);

        public int FirstContactReplies =>
            Shipments.Count(s =>
                s.IsFirstContactReply);

        private PostalCountryRoutingReport(
            int day,
            IReadOnlyList<PostalCountryRouteObservation>
                shipments,
            IReadOnlyList<PostalCountryRouteSummary>
                routes)
        {
            Day = day;
            Shipments = shipments;
            Routes = routes;
        }

        public int CountRoute(
            string sourceCountry,
            string destinationCountry)
        {
            return Shipments.Count(s =>
                string.Equals(
                    s.SourceCountry,
                    sourceCountry,
                    StringComparison.Ordinal) &&
                string.Equals(
                    s.DestinationCountry,
                    destinationCountry,
                    StringComparison.Ordinal));
        }

        public static PostalCountryRoutingReport Capture(
            DemoSession session)
        {
            if (session == null)
                throw new ArgumentNullException(
                    nameof(session));

            var network = session.Scenario.Network;

            var observations = session.Scenario.Exchange
                .GetShipments()
                .Select(shipment =>
                    new PostalCountryRouteObservation(
                        shipment,
                        network))
                .OrderBy(
                    s => s.ShipmentId,
                    StringComparer.Ordinal)
                .ToList();

            var routes = new List<PostalCountryRouteSummary>();

            foreach (var group in observations
                         .GroupBy(s => new
                         {
                             s.SourceCountry,
                             s.DestinationCountry
                         })
                         .OrderBy(
                             g => g.Key.SourceCountry,
                             StringComparer.Ordinal)
                         .ThenBy(
                             g => g.Key.DestinationCountry,
                             StringComparer.Ordinal))
            {
                routes.Add(
                    new PostalCountryRouteSummary(
                        group.Key.SourceCountry,
                        group.Key.DestinationCountry,
                        group.ToList().AsReadOnly()));
            }

            return new PostalCountryRoutingReport(
                session.Day,
                observations.AsReadOnly(),
                routes.AsReadOnly());
        }

        public override string ToString()
        {
            return
                "Day=" + Day +
                " PostalShipments=" + TotalShipments +
                " Physical=" + PhysicalShipments +
                " NonPhysical=" + NonPhysicalShipments +
                " Domestic=" + DomesticShipments +
                " International=" + InternationalShipments +
                " Delivered=" + DeliveredShipments +
                " InternationalDelivered=" +
                    InternationalDelivered +
                " FirstContactRequests=" +
                    FirstContactRequests +
                " FirstContactReplies=" +
                    FirstContactReplies;
        }
    }
}