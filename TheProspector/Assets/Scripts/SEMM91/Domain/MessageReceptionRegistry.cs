using System;
using System.Collections.Generic;


/*
 * TODO: Relationship-driven address disclosure.
 *
 * Sending an address notice must be a voluntary
 * decision made by the sender's future behavior model.
 *
 * Moving must not automatically notify contacts.
 *
 * The recipient's future trust model may also decide
 * whether to accept the claimed address.
 *
 * For now, an explicitly sent and opened personal
 * address notice is accepted without evaluation.
 */


namespace SEMM91.Domain
{
    public sealed class MessageReceptionRegistry
    {
        private readonly Dictionary<
                (string, string), ProductReviewState>
            _reviewStates = new();

        private readonly Dictionary<
                (string, string), bool>
            _personalOpinions = new();

        private readonly Dictionary<
                string, List<ReceivedFeedback>>
            _feedback = new();

        private readonly HashSet<string> _openedShipments = new();


        private readonly KnownCollectionRegistry _knownCollections;

        public MessageReceptionRegistry(
            KnownCollectionRegistry knownCollections = null)
        {
            _knownCollections = knownCollections;
        }

        public void Open(BacklogItem item)
        {
            Open(item, null);
        }

        public void Open(BacklogItem item, Network network)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var package = item.Package;

            if (item.RecipientNodeId != package.TargetNodeId)
                throw new InvalidOperationException(
                    "Backlog recipient does not match package.");

            // Tarkistetaan muuttoilmoitus ennen kuin
            // lähetys merkitään käsitellyksi.
            if (package.AddressNoticeAddressId != null)
            {
                if (network == null)
                    throw new InvalidOperationException(
                        "Address notice requires a network.");

                if (!network.TryGetAddress(
                        package.AddressNoticeAddressId, out _))
                    throw new ArgumentException(
                        "Address notice references unknown address.");

                if (!network.TryGetNode(
                        package.SourceNodeId, out _) ||
                    !network.TryGetNode(
                        item.RecipientNodeId, out _))
                    throw new ArgumentException(
                        "Address notice references unknown node.");
            }

            if (!_openedShipments.Add(item.ShipmentId))
                throw new InvalidOperationException(
                    "Shipment has already been opened.");

            var recipientId = item.RecipientNodeId;

            // Kulttuurituote odottaa perehtymistä.
            if (package.PayloadProductId != null)
            {
                var key = (
                    recipientId,
                    package.PayloadProductId);

                if (!_reviewStates.ContainsKey(key))
                    _reviewStates[key] =
                        ProductReviewState.Pending;
            }

            // Muualta saatu palaute tallennetaan erikseen.
            if (package.FeedbackProductId != null)
            {
                if (!_feedback.TryGetValue(
                        recipientId, out var claims))
                {
                    claims = new List<ReceivedFeedback>();
                    _feedback.Add(recipientId, claims);
                }

                claims.Add(new ReceivedFeedback(
                    item.ShipmentId,
                    package.Id,
                    package.SourceNodeId,
                    recipientId,
                    package.FeedbackProductId,
                    package.Thumb.Value));
            }

            // Muuttoilmoitus päivittää vain vastaanottajan
            // tunteman osoitteen, ei solmun sijaintia.
            //
            // Henkilökohtaisessa ilmoituksessa
            // ilmoituksen kohde on lähettäjä itse.
            if (package.AddressNoticeAddressId != null)
                network.LearnAddress(
                    recipientId,
                    package.SourceNodeId,
                    package.AddressNoticeAddressId);


// Vastaanottaja saa tiedon lähettäjän
// kokoelmahistoriasta vasta viestin avattuaan.
            if (_knownCollections != null)
                _knownCollections.RecordSnapshot(
                    recipientId,
                    package.SourceNodeId,
                    item.SenderCollectionProductIds);
        }


        public ProductReviewState GetReviewState(
            string nodeId,
            string productId)
        {
            ValidateIds(nodeId, productId);

            return _reviewStates.TryGetValue(
                (nodeId, productId), out var state)
                ? state
                : ProductReviewState.NotReceived;
        }

        public bool Review(
            Node node,
            CulturalProduct product)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var key = (node.Id, product.Id);

            if (GetReviewState(node.Id, product.Id) !=
                ProductReviewState.Pending)
                throw new InvalidOperationException(
                    "Product is not awaiting review.");

            // Supports tarkistaa myös mieltymysten
            // alustamisen. Virhe ei muuta tilaa.
            var opinion = node.Supports(product);

            _personalOpinions[key] = opinion;
            _reviewStates[key] = ProductReviewState.Reviewed;

            return opinion;
        }

        public bool? GetPersonalOpinion(
            string nodeId,
            string productId)
        {
            ValidateIds(nodeId, productId);

            if (_personalOpinions.TryGetValue(
                    (nodeId, productId), out var opinion))
                return opinion;

            return null;
        }

        public IReadOnlyList<ReceivedFeedback> GetFeedback(
            string recipientNodeId)
        {
            if (string.IsNullOrWhiteSpace(recipientNodeId))
                throw new ArgumentException(
                    "Recipient ID cannot be empty.",
                    nameof(recipientNodeId));

            if (!_feedback.TryGetValue(
                    recipientNodeId, out var claims))
                return Array.Empty<ReceivedFeedback>();

            return new List<ReceivedFeedback>(claims)
                .AsReadOnly();
        }

        private static void ValidateIds(
            string nodeId,
            string productId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException(
                    "Node ID cannot be empty.",
                    nameof(nodeId));

            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException(
                    "Product ID cannot be empty.",
                    nameof(productId));
        }
    }
}