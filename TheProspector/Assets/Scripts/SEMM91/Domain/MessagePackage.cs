using System;

namespace SEMM91.Domain
{
    public sealed class MessagePackage
    {
        public bool? DeclaredPublicProfile { get; }
        public string SkepticismTargetNodeId { get; }
        public string AddressNoticeAddressId { get; }

        public string Id { get; }
        public string ConnectionId { get; }
        public string ReplyToPackageId { get; }

        public string SourceNodeId { get; }
        public string TargetNodeId { get; }

        public string CassetteCopyId { get; }
        public string PayloadProductId { get; }

        public string FeedbackProductId { get; }
        public int? Thumb { get; }

        // Crucible 9.2.18.3
        public string RequestedDemoId { get; }

        public MessagePackage(
            string id,
            NetworkConnection connection,
            CassetteCopy cassetteCopy = null,
            CulturalProduct feedbackProduct = null,
            int? thumb = null,
            string addressNoticeAddressId = null,
            string replyToPackageId = null,
            bool? declaredPublicProfile = null,
            string skepticismTargetNodeId = null,
            string requestedDemoId = null)
        {
            // 1. Package identity.
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "Message ID cannot be empty.",
                    nameof(id));
            }

            // 2. A valid Layer 2 route is required.
            if (connection == null)
            {
                throw new ArgumentNullException(
                    nameof(connection));
            }

            if (connection.Layer !=
                CommunicationLayer.Interpersonal)
            {
                throw new ArgumentException(
                    "Message requires an interpersonal connection.",
                    nameof(connection));
            }

            // 3. Feedback must have both product and thumb.
            if ((feedbackProduct == null) != !thumb.HasValue)
            {
                throw new ArgumentException(
                    "Feedback product and thumb must " +
                    "be provided together.");
            }

            if (thumb.HasValue &&
                thumb.Value != -1 &&
                thumb.Value != +1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(thumb),
                    "Thumb must be -1 or +1.");
            }

            // 4. Address notice validation.
            // Preserve the pre-9.2.18.3 contract.
            if (addressNoticeAddressId != null &&
                string.IsNullOrWhiteSpace(
                    addressNoticeAddressId))
            {
                throw new ArgumentException(
                    "Address notice cannot contain an empty address.",
                    nameof(addressNoticeAddressId));
            }

            // 5. Skepticism target validation.
            if (skepticismTargetNodeId != null &&
                string.IsNullOrWhiteSpace(
                    skepticismTargetNodeId))
            {
                throw new ArgumentException(
                    "Skepticism target cannot be empty.",
                    nameof(skepticismTargetNodeId));
            }

            // 6. Reply reference validation.
            if (replyToPackageId != null)
            {
                if (string.IsNullOrWhiteSpace(
                        replyToPackageId))
                {
                    throw new ArgumentException(
                        "Reply reference cannot be empty.",
                        nameof(replyToPackageId));
                }

                if (replyToPackageId == id)
                {
                    throw new ArgumentException(
                        "Message cannot reply to itself.",
                        nameof(replyToPackageId));
                }
            }

            // 7. A public profile declaration requires
            // an address notice.
            if (declaredPublicProfile.HasValue &&
                addressNoticeAddressId == null)
            {
                throw new ArgumentException(
                    "Public profile declaration requires an address.",
                    nameof(declaredPublicProfile));
            }

            // 8. New demo request validation.
            if (requestedDemoId != null)
            {
                if (string.IsNullOrWhiteSpace(
                        requestedDemoId))
                {
                    throw new ArgumentException(
                        "Requested demo ID cannot be empty.",
                        nameof(requestedDemoId));
                }

                if (cassetteCopy != null)
                {
                    throw new ArgumentException(
                        "A package cannot simultaneously " +
                        "offer a cassette and request a demo.",
                        nameof(requestedDemoId));
                }
            }

            // 9. Empty packages remain forbidden.
            // A demo request now counts as valid content.
            if (cassetteCopy == null &&
                feedbackProduct == null &&
                addressNoticeAddressId == null &&
                skepticismTargetNodeId == null &&
                requestedDemoId == null)
            {
                throw new ArgumentException(
                    "Message must contain cassette, feedback, " +
                    "address notice, skepticism statement " +
                    "or demo request.");
            }

            // 10. The cassette must belong to the sender.
            if (cassetteCopy != null &&
                cassetteCopy.HolderNodeId !=
                    connection.SourceNodeId)
            {
                throw new InvalidOperationException(
                    "Sender does not hold the cassette.");
            }

            // 11. Assign immutable package contents.
            Id = id;

            ConnectionId = connection.Id;

            SourceNodeId = connection.SourceNodeId;
            TargetNodeId = connection.TargetNodeId;

            CassetteCopyId = cassetteCopy?.Id;
            PayloadProductId = cassetteCopy?.ProductId;

            FeedbackProductId = feedbackProduct?.Id;
            Thumb = thumb;

            AddressNoticeAddressId =
                addressNoticeAddressId;

            DeclaredPublicProfile =
                declaredPublicProfile;

            SkepticismTargetNodeId =
                skepticismTargetNodeId;

            ReplyToPackageId =
                replyToPackageId;

            RequestedDemoId =
                requestedDemoId;
        }
    }
}