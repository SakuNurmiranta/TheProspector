using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
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

        private readonly Func<string, string, bool> _qualifiedContact;

        private readonly Dictionary<string, string> _qualifiedPending = new(StringComparer.Ordinal);

        private readonly Dictionary<string, Attempt> _attempts = new(StringComparer.Ordinal);

        private readonly HashSet<(string Sender, string Recipient)>
            _attemptedPairs = new();

        private readonly HashSet<string> _repliesSent = new(StringComparer.Ordinal);

        private bool _disposed;

        public ZineFirstContactPostalService(
            Network network,
            SimulationClock clock,
            ZineContactOutreachPolicy outreach,
            TimedPostalExchange exchange,
            Func<string, string, bool> qualifiedContact = null)
        {
            _network = network ??
                       throw new ArgumentNullException(nameof(network));

            _clock = clock ??
                     throw new ArgumentNullException(nameof(clock));

            _outreach = outreach ??
                        throw new ArgumentNullException(nameof(outreach));

            _exchange = exchange ??
                        throw new ArgumentNullException(nameof(exchange));

            _qualifiedContact = qualifiedContact;

            _exchange.ShipmentDelivered += OnShipmentDelivered;
        }

        public bool TrySend(
            string senderNodeId,
            bool senderPublicProfile,
            string targetNodeId,
            CassetteCopy copy,
            string packageId,
            out PostalShipment shipment,
            string requestedDemoId = null)
        {
            ThrowIfDisposed();

            ValidateId(senderNodeId, nameof(senderNodeId));
            ValidateId(targetNodeId, nameof(targetNodeId));
            ValidateId(packageId, nameof(packageId));

            shipment = null;

            // Either an offered cassette or a demo request
            // must be present, but never both.
            if (copy == null && requestedDemoId == null) throw new ArgumentNullException(nameof(copy));

            if (copy != null && requestedDemoId != null)
                throw new ArgumentException(
                    "First contact cannot offer and request " +
                    "a cassette simultaneously.",
                    nameof(requestedDemoId));

            if (requestedDemoId != null)
                ValidateId(
                    requestedDemoId,
                    nameof(requestedDemoId));

            if (copy != null &&
                copy.HolderNodeId != senderNodeId)
                throw new InvalidOperationException(
                    "Sender must hold the cassette.");

            if (_attempts.ContainsKey(packageId))
                throw new ArgumentException(
                    "Package ID already used.",
                    nameof(packageId));

            var pair = (senderNodeId, targetNodeId);

            if (_attemptedPairs.Contains(pair))
                return false;

            if (!_network.TryGetNodeAddress(
                    senderNodeId, out var senderAddressId))
                throw new InvalidOperationException(
                    "Sender has no current address.");

            if (!_network.TryGetAddress(
                    senderAddressId, out var senderAddress))
                throw new InvalidOperationException(
                    "Sender address is unknown.");

            if (!_network.TryGetKnownAddress(
                    senderNodeId,
                    targetNodeId,
                    out var recipientAddressId))
                return false;

            if (!_network.TryGetAddress(
                    recipientAddressId, out var recipientAddress))
                return false;

            var senderCountry = senderAddress.CountryId;
            var recipientCountry = recipientAddress.CountryId;

            var transitDays = PostalTimingRules.GetTransitDays(
                senderCountry,
                recipientCountry);

            var deadline = PostalTimingRules.CalculateDeadline(
                _clock.CurrentDay,
                transitDays,
                ReplyAllowanceDays);

            if (!StartOutreach(
                    senderNodeId,
                    senderPublicProfile,
                    targetNodeId))
                return false;

            try
            {
                // Temporary first-contact route.
                // No permanent personal relationship yet.
                var route = new NetworkConnection(
                    "FIRST-ROUTE:" + packageId,
                    senderNodeId,
                    targetNodeId,
                    CommunicationLayer.Interpersonal,
                    "first-contact");

                var package = new MessagePackage(
                    packageId,
                    route,
                    copy,
                    addressNoticeAddressId: senderAddressId,
                    requestedDemoId: requestedDemoId);

                shipment = _exchange.Send(
                    "FIRST-SHIP:" + packageId,
                    package,
                    senderCountry,
                    recipientCountry,
                    ReplyAllowanceDays,
                    true);

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
                CompleteOutreach(
                    senderNodeId,
                    targetNodeId);

                throw;
            }
        }

        /// <summary>
        /// A reply can contain the responder's own cassette,
        /// positive/negative feedback, or only an address notice.
        /// Existing two-argument calls remain valid.
        /// </summary>
        public PostalShipment ReplyWithAddress(
            BacklogItem openedItem,
            string replyPackageId,
            CassetteCopy replyCassette = null,
            CulturalProduct feedbackProduct = null,
            int? thumb = null)
        {
            ThrowIfDisposed();

            if (openedItem == null)
                throw new ArgumentNullException(nameof(openedItem));

            ValidateId(replyPackageId, nameof(replyPackageId));

            var original = openedItem.Package;

            if (!_attempts.TryGetValue(
                    original.Id, out var attempt))
                throw new InvalidOperationException(
                    "Unknown first-contact request.");

            if (original.SourceNodeId != attempt.Sender ||
                original.TargetNodeId != attempt.Recipient)
                throw new InvalidOperationException(
                    "Request endpoints do not match.");

            if (_repliesSent.Contains(original.Id))
                throw new InvalidOperationException(
                    "Request was already answered.");

            var responder = attempt.Recipient;
            var recipient = attempt.Sender;

            if (!_network.TryGetNodeAddress(
                    responder, out var responderAddressId))
                throw new InvalidOperationException(
                    "Responder has no address.");

            if (!_network.TryGetKnownAddress(
                    responder,
                    recipient,
                    out var knownRecipientAddressId))
                throw new InvalidOperationException(
                    "Return address has not been learned.");

            if (!_network.TryGetAddress(
                    responderAddressId, out var responderAddress) ||
                !_network.TryGetAddress(
                    knownRecipientAddressId, out var recipientAddress))
                throw new InvalidOperationException(
                    "Reply address is unknown.");

            var route = new NetworkConnection(
                "FIRST-REPLY-ROUTE:" + replyPackageId,
                responder,
                recipient,
                CommunicationLayer.Interpersonal,
                "first-reply");

            var package = new MessagePackage(
                replyPackageId,
                route,
                replyCassette,
                feedbackProduct,
                thumb,
                responderAddressId,
                original.Id);

            var shipment = _exchange.Send(
                "FIRST-REPLY-SHIP:" + replyPackageId,
                package,
                responderAddress.CountryId,
                recipientAddress.CountryId,
                ReplyAllowanceDays);

            _repliesSent.Add(original.Id);

            return shipment;
        }

        private void OnShipmentDelivered(PostalShipment shipment)
        {
            var package = shipment.Package;
            var originalId = package.ReplyToPackageId;

            if (string.IsNullOrWhiteSpace(originalId))
                return;

            if (!_attempts.TryGetValue(
                    originalId, out var attempt))
                return;

            if (package.SourceNodeId != attempt.Recipient ||
                package.TargetNodeId != attempt.Sender)
                return;

            if (attempt.Replied)
                return;

            attempt.Replied = true;

            CompleteOutreach(attempt.Sender, attempt.Recipient);

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

            CompleteOutreach(
                attempt.Sender,
                attempt.Recipient);
        }

        private void EstablishDirection(
            string sender,
            string recipient,
            string originalPackageId)
        {
            foreach (var connection in
                     _network.GetOutgoingConnections(
                         sender,
                         CommunicationLayer.Interpersonal))
                if (connection.TargetNodeId == recipient)
                    return;

            _network.AddConnection(
                "ESTABLISHED:" + originalPackageId +
                ":" + sender + ":" + recipient,
                sender,
                recipient,
                CommunicationLayer.Interpersonal,
                "personal");
        }

        public bool HasPendingOutreach(string senderNodeId)
        {
            ThrowIfDisposed();
            ValidateId(senderNodeId, nameof(senderNodeId));

            return _qualifiedContact != null
                ? _qualifiedPending.ContainsKey(senderNodeId)
                : _outreach.HasPendingOutreach(senderNodeId);
        }

        private bool StartOutreach(
            string sender,
            bool legacyProfile,
            string target)
        {
            // Preserve the existing one-boolean behavior.
            if (_qualifiedContact == null)
                return _outreach.TryStartOutreach(
                    sender, legacyProfile, target);

            // The new six-axis gate is supplied by the caller.
            if (_qualifiedPending.ContainsKey(sender) ||
                !_qualifiedContact(sender, target))
                return false;

            _qualifiedPending.Add(sender, target);
            return true;
        }

        private void CompleteOutreach(
            string sender,
            string target)
        {
            if (_qualifiedContact == null)
            {
                _outreach.CompleteOutreach(sender, target);
                return;
            }

            if (_qualifiedPending.TryGetValue(
                    sender, out var pending) &&
                pending == target)
                _qualifiedPending.Remove(sender);
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