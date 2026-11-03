
using System;

namespace SEMM91.Domain
{
    public sealed class MessagePackage
    {
        
        public string AddressNoticeAddressId { get; }

        public string Id { get; }

        public string ConnectionId { get; }

        public string SourceNodeId { get; }

        public string TargetNodeId { get; }

        public string CassetteCopyId { get; }

        public string PayloadProductId { get; }

        public string FeedbackProductId { get; }

        public int? Thumb { get; }

        public MessagePackage(
            string id,
            NetworkConnection connection,
            CassetteCopy cassetteCopy = null,
            CulturalProduct feedbackProduct = null,
            int? thumb = null,
            string addressNoticeAddressId = null
            )
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(
                    "Message ID cannot be empty.",
                    nameof(id));

            if (connection == null)
                throw new ArgumentNullException(
                    nameof(connection));

            if (connection.Layer !=
                CommunicationLayer.Interpersonal)
                throw new ArgumentException(
                    "Message requires an interpersonal connection.",
                    nameof(connection));

            // Feedback requires both a target product
            // and an explicit positive/negative thumb.
            if ((feedbackProduct == null) != !thumb.HasValue)
                throw new ArgumentException(
                    "Feedback product and thumb must " +
                    "be provided together.");

            if (thumb.HasValue &&
                thumb.Value != -1 &&
                thumb.Value != +1)
                throw new ArgumentOutOfRangeException(
                    nameof(thumb),
                    "Thumb must be -1 or +1.");

            
            if (cassetteCopy == null &&
                feedbackProduct == null &&
                addressNoticeAddressId == null)
            {
                throw new ArgumentException(
                    "Message must contain cassette, " +
                    "feedback or address notice.");
            }


            if (cassetteCopy != null &&
                cassetteCopy.HolderNodeId !=
                connection.SourceNodeId)
                throw new InvalidOperationException(
                    "Sender does not hold the cassette.");

            
            if (addressNoticeAddressId != null &&
                string.IsNullOrWhiteSpace(addressNoticeAddressId))
            {
                throw new ArgumentException(
                    "Address notice cannot contain an empty address.",
                    nameof(addressNoticeAddressId));
            }

            
            Id = id;

            ConnectionId = connection.Id;

            SourceNodeId = connection.SourceNodeId;
            TargetNodeId = connection.TargetNodeId;

            CassetteCopyId = cassetteCopy?.Id;
            PayloadProductId = cassetteCopy?.ProductId;

            FeedbackProductId = feedbackProduct?.Id;
            Thumb = thumb;
            
            AddressNoticeAddressId = addressNoticeAddressId;

        }
    }
}
