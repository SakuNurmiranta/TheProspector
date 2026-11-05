
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Seuraa vain erikseen rekisteröityjä
    /// yhteydenottoja, joihin odotetaan vastausta.
    ///
    /// Vastausaika päättyy DeadlineDay-päivänä.
    /// Skeptisyys kirjataan seuraavana päivänä.
    /// </summary>
    public sealed class ReplyWindowSkepticismMonitor
    {
        private sealed class WatchedRequest
        {
            public string PackageId;
            public string SenderId;
            public string RecipientId;
            public int DeadlineDay;
            public bool ReplyReceived;
        }

        private readonly SimulationClock _clock;
        private readonly DirectionalSkepticismLedger _ledger;

        private readonly Dictionary<string, WatchedRequest>
            _requests = new Dictionary<string, WatchedRequest>(
                StringComparer.Ordinal);

        public int Count => _requests.Count;

        public ReplyWindowSkepticismMonitor(
            SimulationClock clock,
            DirectionalSkepticismLedger ledger)
        {
            _clock = clock ??
                throw new ArgumentNullException(nameof(clock));

            _ledger = ledger ??
                throw new ArgumentNullException(nameof(ledger));
        }

        public void Track(
            string packageId,
            string senderId,
            string recipientId,
            int deadlineDay)
        {
            ValidateId(packageId, nameof(packageId));
            ValidateId(senderId, nameof(senderId));
            ValidateId(recipientId, nameof(recipientId));

            if (senderId == recipientId)
                throw new ArgumentException(
                    "Sender and recipient must differ.");

            if (deadlineDay < _clock.CurrentDay)
                throw new ArgumentOutOfRangeException(
                    nameof(deadlineDay));

            if (_requests.ContainsKey(packageId))
                throw new ArgumentException(
                    "Request already tracked.",
                    nameof(packageId));

            var request = new WatchedRequest
            {
                PackageId = packageId,
                SenderId = senderId,
                RecipientId = recipientId,
                DeadlineDay = deadlineDay
            };

            int delay =
                checked(deadlineDay - _clock.CurrentDay + 1);

            // Ajastus tehdään ennen rekisteröintiä,
            // jotta epäonnistunut ajastus ei jätä
            // rekisteriin keskeneräistä merkintää.
            _clock.ScheduleAfter(
                "SKEPTICISM-DEADLINE:" + packageId,
                delay,
                () =>
                {
                    if (!request.ReplyReceived)
                    {
                        _ledger.RecordUnanswered(
                            request.SenderId,
                            request.RecipientId,
                            request.PackageId);
                    }
                });

            _requests.Add(packageId, request);
        }

        /// <summary>
        /// Kutsutaan todellisen vastauksen
        /// saapuessa, ei pelkästään sen
        /// lähetyshetkellä.
        ///
        /// ReplyToPackageId yksilöi alkuperäisen
        /// yhteydenoton.
        /// </summary>
        public bool ReplyArrived(
            string replyToPackageId,
            string replySenderId,
            string replyRecipientId,
            bool compensates = false)
        {
            if (string.IsNullOrWhiteSpace(replyToPackageId) ||
                !_requests.TryGetValue(
                    replyToPackageId, out var request))
            {
                return false;
            }

            // Vastauksen suunnan pitää olla
            // alkuperäiseen lähetykseen nähden
            // päinvastainen.
            if (!string.Equals(
                    replySenderId,
                    request.RecipientId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    replyRecipientId,
                    request.SenderId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (request.ReplyReceived)
                return false;

            request.ReplyReceived = true;

            // Myöhäinen vastaus ei yksinään
            // hyvitä umpeutunutta odotusikkunaa.
            if (compensates &&
                _clock.CurrentDay > request.DeadlineDay)
            {
                _ledger.CompensateUnanswered(
                    request.SenderId,
                    request.RecipientId,
                    request.PackageId);
            }

            return true;
        }

        private static void ValidateId(
            string id,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(
                    "Identifier cannot be empty.",
                    parameterName);
        }
    }
}
