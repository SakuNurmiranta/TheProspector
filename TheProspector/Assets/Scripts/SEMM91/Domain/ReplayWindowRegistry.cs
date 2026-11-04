
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class ReplyWindow
    {
        public string PackageId { get; }
        public string SenderNodeId { get; }
        public string RecipientNodeId { get; }

        public int SentDay { get; }
        public int TransitDays { get; }
        public int DeadlineDay { get; }

        internal ReplyWindow(
            string packageId,
            string senderNodeId,
            string recipientNodeId,
            int sentDay,
            int transitDays,
            int deadlineDay)
        {
            PackageId = packageId;
            SenderNodeId = senderNodeId;
            RecipientNodeId = recipientNodeId;
            SentDay = sentDay;
            TransitDays = transitDays;
            DeadlineDay = deadlineDay;
        }
    }

    public sealed class ReplyWindowRegistry
    {
        private readonly Dictionary<string, ReplyWindow>
            _windows =
                new Dictionary<string, ReplyWindow>(
                    StringComparer.Ordinal);

        public int Count => _windows.Count;

        public ReplyWindow Register(
            string packageId,
            string senderNodeId,
            string recipientNodeId,
            int sentDay,
            string senderCountryCode,
            string recipientCountryCode,
            int allowanceDays)
        {
            ValidateId(packageId, nameof(packageId));
            ValidateId(senderNodeId, nameof(senderNodeId));
            ValidateId(recipientNodeId, nameof(recipientNodeId));

            if (senderNodeId == recipientNodeId)
                throw new ArgumentException(
                    "Sender and recipient must differ.");

            if (_windows.ContainsKey(packageId))
                throw new ArgumentException(
                    "Reply window already registered.",
                    nameof(packageId));

            int transitDays =
                PostalTimingRules.GetTransitDays(
                    senderCountryCode,
                    recipientCountryCode);

            int deadlineDay =
                PostalTimingRules.CalculateDeadline(
                    sentDay,
                    transitDays,
                    allowanceDays);

            // Kaikki tarkistukset suoritetaan ennen
            // rekisterin muuttamista.
            var window = new ReplyWindow(
                packageId,
                senderNodeId,
                recipientNodeId,
                sentDay,
                transitDays,
                deadlineDay);

            _windows.Add(packageId, window);

            return window;
        }

        public ReplyTiming Resolve(
            string replyToPackageId,
            string replySenderNodeId,
            string replyRecipientNodeId,
            int replyArrivalDay)
        {
            if (replyArrivalDay < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(replyArrivalDay));

            ValidateId(
                replySenderNodeId,
                nameof(replySenderNodeId));

            ValidateId(
                replyRecipientNodeId,
                nameof(replyRecipientNodeId));

            // Ilman viittausta alkuperäiseen viestiin
            // vastaussuhdetta ei voida ratkaista.
            if (string.IsNullOrWhiteSpace(replyToPackageId))
                return ReplyTiming.Unknown;

            if (!_windows.TryGetValue(
                    replyToPackageId,
                    out var original))
            {
                return ReplyTiming.Unknown;
            }

            // Vastauksen suunnan pitää olla
            // alkuperäisen viestin vastainen.
            if (original.SenderNodeId !=
                    replyRecipientNodeId ||
                original.RecipientNodeId !=
                    replySenderNodeId)
            {
                return ReplyTiming.Unknown;
            }

            if (replyArrivalDay < original.SentDay)
                throw new ArgumentOutOfRangeException(
                    nameof(replyArrivalDay),
                    "Reply cannot arrive before the request.");

            return replyArrivalDay <= original.DeadlineDay
                ? ReplyTiming.OnTime
                : ReplyTiming.Late;
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
