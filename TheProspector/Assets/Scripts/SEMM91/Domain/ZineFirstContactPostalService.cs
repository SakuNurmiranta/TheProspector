
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Kytkee zinen perusteella valitun uuden
    /// kontaktin varsinaiseen postitukseen.
    ///
    /// Ensimmäinen viesti käyttää yksittäistä
    /// reittiä, ei pysyvää verkkokaarta.
    ///
    /// Vastauksen saavuttua rekisteröidään
    /// molemmat henkilökohtaiset yhteydet.
    /// </summary>
    public sealed class ZineFirstContactPostalService : IDisposable
    {
        private sealed class Attempt
        {
            public string Sender;
            public string Recipient;
            public string PackageId;
            public bool Replied;
        }

        private const int ReplyAllowanceDays = 5;

        private readonly Network _network;
        private readonly SimulationClock _clock;
        private readonly ZineContactOutreachPolicy _outreach;
        private readonly TimedPostalExchange _exchange;

        private readonly Dictionary<string, Attempt>
            _attempts = new Dictionary<string, Attempt>(
                StringComparer.Ordinal);

        private readonly HashSet<(string Sender, string Recipient)>
            _attemptedPairs =
                new HashSet<(string Sender, string Recipient)>();

        private readonly HashSet<string>
            _repliesSent = new HashSet<string>(
                StringComparer.Ordinal);

        private bool _disposed;

        public ZineFirstContactPostalService(
            Network network,
            SimulationClock clock,
            ZineContactOutreachPolicy outreach,
            TimedPostalExchange exchange)
        {
            _network = network ??
                throw new ArgumentNullException(
                    nameof(network));

            _clock = clock ??
                throw new ArgumentNullException(
                    nameof(clock));

            _outreach = outreach ??
                throw new ArgumentNullException(
                    nameof(outreach));

            _exchange = exchange ??
                throw new ArgumentNullException(
                    nameof(exchange));

            _exchange.ShipmentDelivered += OnShipmentDelivered;
        }

        public bool TrySend(
            string senderNodeId,
            bool senderPublicProfile,
            string targetNodeId,
            CassetteCopy copy,
            string packageId,
            out PostalShipment shipment)
        {
            ThrowIfDisposed();

            ValidateId(senderNodeId, nameof(senderNodeId));
            ValidateId(targetNodeId, nameof(targetNodeId));
            ValidateId(packageId, nameof(packageId));

            shipment = null;

            if (copy == null)
                throw new ArgumentNullException(nameof(copy));

            if (copy.HolderNodeId != senderNodeId)
                throw new InvalidOperationException(
                    "Sender must hold the cassette.");

            if (_attempts.ContainsKey(packageId))
                throw new ArgumentException(
                    "Package ID already used.",
                    nameof(packageId));

            var pair = (senderNodeId, targetNodeId);

            if (_attemptedPairs.Contains(pair))
                return false;

            // Tarkistetaan todellinen lähetysosoite
            // sekä vastaanottajan TUNNETTU osoite.
            if (!_network.TryGetNodeAddress(
                    senderNodeId, out var senderAddressId))
            {
                throw new InvalidOperationException(
                    "Sender has no current address.");
            }

            if (!_network.TryGetAddress(
                    senderAddressId, out var senderAddress))
            {
                throw new InvalidOperationException(
                    "Sender address is unknown.");
            }

            if (!_network.TryGetKnownAddress(
                    senderNodeId,
                    targetNodeId,
                    out var recipientAddressId))
            {
                return false;
            }

            if (!_network.TryGetAddress(
                    recipientAddressId,
                    out var recipientAddress))
            {
                return false;
            }

            string senderCountry = senderAddress.CountryId;
            string recipientCountry = recipientAddress.CountryId;

            int transitDays = PostalTimingRules.GetTransitDays(
                senderCountry, recipientCountry);

            int deadline = PostalTimingRules.CalculateDeadline(
                _clock.CurrentDay,
                transitDays,
                ReplyAllowanceDays);

            // Poliittinen valinta ja yhden aktiivisen
            // tunnustelun rajoitus tulevat 7.6:sta.
            if (!_outreach.TryStartOutreach(
                    senderNodeId,
                    senderPublicProfile,
                    targetNodeId))
            {
                return false;
            }

            try
            {
                // Reitti EI rekisteröidy Networkiin.
                // Tunteminen ja yhteyden muodostuminen
                // ovat eri asioita.
                var route = new NetworkConnection(
                    "FIRST-ROUTE:" + packageId,
                    senderNodeId,
                    targetNodeId,
                    CommunicationLayer.Interpersonal,
                    "first-contact");

                var package = new MessagePackage(
                    packageId,
                    route,
                    cassetteCopy: copy,
                    addressNoticeAddressId: senderAddressId);

                shipment = _exchange.Send(
                    "FIRST-SHIP:" + packageId,
                    package,
                    senderCountry,
                    recipientCountry,
                    ReplyAllowanceDays,
                    expectsReplyForSkepticism: true);

                _attempts.Add(
                    packageId,
                    new Attempt
                    {
                        Sender = senderNodeId,
                        Recipient = targetNodeId,
                        PackageId = packageId
                    });

                _attemptedPairs.Add(pair);

                _clock.ScheduleAfter(
                    "FIRST-OUTREACH-END:" + packageId,
                    deadline - _clock.CurrentDay + 1,
                    () => OnDeadline(packageId));

                return true;
            }
            catch
            {
                _outreach.CompleteOutreach(
                    senderNodeId, targetNodeId);

                throw;
            }
        }

        /// <summary>
        /// Lähettää vastauksen vastaanottajan
        /// avaamaan ensimmäiseen yhteydenottoon.
        ///
        /// Ensimmäisessä versiossa vastaus
        /// sisältää solmun oman osoitetiedon.
        /// </summary>
        public PostalShipment ReplyWithAddress(
            BacklogItem openedItem,
            string replyPackageId)
        {
            ThrowIfDisposed();

            if (openedItem == null)
                throw new ArgumentNullException(
                    nameof(openedItem));

            ValidateId(
                replyPackageId,
                nameof(replyPackageId));

            var original = openedItem.Package;

            if (!_attempts.TryGetValue(
                    original.Id, out var attempt))
            {
                throw new InvalidOperationException(
                    "Unknown first-contact request.");
            }

            if (original.SourceNodeId != attempt.Sender ||
                original.TargetNodeId != attempt.Recipient)
            {
                throw new InvalidOperationException(
                    "Request endpoints do not match.");
            }

            if (_repliesSent.Contains(original.Id))
                throw new InvalidOperationException(
                    "Request was already answered.");
            
            string responder = attempt.Recipient;
            string recipient = attempt.Sender;

            if (!_network.TryGetNodeAddress(
                    responder, out var responderAddressId))
            {
                throw new InvalidOperationException(
                    "Responder has no address.");
            }

            if (!_network.TryGetKnownAddress(
                    responder,
                    recipient,
                    out var knownRecipientAddressId))
            {
                throw new InvalidOperationException(
                    "Return address has not been learned.");
            }

            if (!_network.TryGetAddress(
                    responderAddressId,
                    out var responderAddress) ||
                !_network.TryGetAddress(
                    knownRecipientAddressId,
                    out var recipientAddress))
            {
                throw new InvalidOperationException(
                    "Reply address is unknown.");
            }

            var route = new NetworkConnection(
                "FIRST-REPLY-ROUTE:" + replyPackageId,
                responder,
                recipient,
                CommunicationLayer.Interpersonal,
                "first-reply");

            var package = new MessagePackage(
                replyPackageId,
                route,
                addressNoticeAddressId: responderAddressId,
                replyToPackageId: original.Id);

            var shipment = _exchange.Send(
                "FIRST-REPLY-SHIP:" + replyPackageId,
                package,
                responderAddress.CountryId,
                recipientAddress.CountryId,
                ReplyAllowanceDays);

            _repliesSent.Add(original.Id);

            return shipment;
        }

        private void OnShipmentDelivered(
            PostalShipment shipment)
        {
            var package = shipment.Package;
            string originalId = package.ReplyToPackageId;

            if (string.IsNullOrWhiteSpace(originalId))
                return;

            if (!_attempts.TryGetValue(
                    originalId, out var attempt))
                return;

            // Vain oikean henkilön vastakkaiseen
            // suuntaan toimittama vastaus hyväksytään.
            if (package.SourceNodeId != attempt.Recipient ||
                package.TargetNodeId != attempt.Sender)
            {
                return;
            }

            if (attempt.Replied)
                return;

            attempt.Replied = true;

            _outreach.CompleteOutreach(
                attempt.Sender,
                attempt.Recipient);

            EstablishDirection(
                attempt.Sender,
                attempt.Recipient,
                originalId);

            EstablishDirection(
                attempt.Recipient,
                attempt.Sender,
                originalId);
        }

        private void OnDeadline(string packageId)
        {
            if (!_attempts.TryGetValue(
                    packageId, out var attempt))
                return;

            if (attempt.Replied)
                return;

            // Vastausta ei saapunut määräajassa.
            // Verkkokaarta ei ole muodostettu,
            // joten mitään ei tarvitse poistaa.
            _outreach.CompleteOutreach(
                attempt.Sender,
                attempt.Recipient);
        }

        private void EstablishDirection(
            string sender,
            string recipient,
            string originalPackageId)
        {
            // Jos suhde on syntynyt muuta kautta,
            // sitä ei monisteta.
            foreach (var connection in
                     _network.GetOutgoingConnections(
                         sender,
                         CommunicationLayer.Interpersonal))
            {
                if (connection.TargetNodeId == recipient)
                    return;
            }

            _network.AddConnection(
                "ESTABLISHED:" + originalPackageId +
                ":" + sender + ":" + recipient,
                sender,
                recipient,
                CommunicationLayer.Interpersonal,
                "personal");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _exchange.ShipmentDelivered -= OnShipmentDelivered;
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    nameof(ZineFirstContactPostalService));
        }

        private static void ValidateId(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "Identifier cannot be empty.",
                    parameterName);
        }
    }
}
