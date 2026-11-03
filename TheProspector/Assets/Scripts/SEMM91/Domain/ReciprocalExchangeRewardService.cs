
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public enum ReplyTiming
    {
        Unknown,
        OnTime,
        Late
    }

    public sealed class ReciprocalExchangeRewardService
    {
        private sealed class OpenedMessage
        {
            public string Sender { get; }
            public string Recipient { get; }

            public OpenedMessage(
                string sender,
                string recipient)
            {
                Sender = sender;
                Recipient = recipient;
            }
        }

        private readonly MessageReceptionRegistry _reception;
        private readonly CulturalCapitalLedger _capital;

        // Vain todella avatut viestit.
        private readonly Dictionary<string, OpenedMessage>
            _opened =
                new Dictionary<string, OpenedMessage>();

        // Kukin alkuperäinen viesti voi tuottaa
        // vain yhden vastavuoroisuuspalkkion.
        private readonly HashSet<string> _rewardedRequests =
            new HashSet<string>();

        public ReciprocalExchangeRewardService(
            MessageReceptionRegistry reception,
            CulturalCapitalLedger capital)
        {
            _reception = reception ??
                throw new ArgumentNullException(
                    nameof(reception));

            _capital = capital ??
                throw new ArgumentNullException(
                    nameof(capital));
        }

        public int Open(
            BacklogItem item,
            ReplyTiming timing = ReplyTiming.Unknown)
        {
            return OpenCore(
                item, null, false, timing);
        }

        public int Open(
            BacklogItem item,
            Network network,
            ReplyTiming timing = ReplyTiming.Unknown)
        {
            if (network == null)
                throw new ArgumentNullException(
                    nameof(network));

            return OpenCore(
                item, network, true, timing);
        }

        private int OpenCore(
            BacklogItem item,
            Network network,
            bool useNetwork,
            ReplyTiming timing)
        {
            if (item == null)
                throw new ArgumentNullException(
                    nameof(item));

            if (timing != ReplyTiming.Unknown &&
                timing != ReplyTiming.OnTime &&
                timing != ReplyTiming.Late)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timing));
            }

            var package = item.Package;

            if (_opened.ContainsKey(package.Id))
            {
                throw new InvalidOperationException(
                    "Message has already been opened.");
            }

            var replyTo = package.ReplyToPackageId;

            bool eligibleReply = false;
            string initiatorId = null;
            string responderId = null;

            if (replyTo != null &&
                _opened.TryGetValue(
                    replyTo, out var original) &&
                !_rewardedRequests.Contains(replyTo))
            {
                // Vastauksen suunnan täytyy olla
                // täsmälleen alkuperäisen vastainen.
                eligibleReply =
                    original.Sender ==
                        package.TargetNodeId &&
                    original.Recipient ==
                        package.SourceNodeId;

                if (eligibleReply)
                {
                    initiatorId = original.Sender;
                    responderId = package.SourceNodeId;
                }
            }

            if (eligibleReply)
            {
                if (timing == ReplyTiming.Unknown)
                {
                    throw new InvalidOperationException(
                        "Reply timing must be resolved " +
                        "before awarding capital.");
                }

                // Tarkistetaan molemmat mahdolliset
                // saldomuutokset ennen viestin avaamista.
                // Näin ylivuoto ei johda tilanteeseen,
                // jossa vain toinen saa palkkionsa.
                _capital.EnsureCanCredit(initiatorId, 1);

                if (timing == ReplyTiming.OnTime)
                {
                    _capital.EnsureCanCredit(responderId, 1);
                }
            }

            // Varsinainen vastaanottorekisteri käsittelee
            // viestin, palautteen, kokoelmatiedon ja
            // mahdollisen osoiteilmoituksen.
            //
            // Epäonnistunutta avaamista ei merkitä.
            if (useNetwork)
                _reception.Open(item, network);
            else
                _reception.Open(item);

            _opened.Add(
                package.Id,
                new OpenedMessage(
                    package.SourceNodeId,
                    package.TargetNodeId));

            if (!eligibleReply)
                return 0;

            // Molempien kirjeiden avaaminen on nyt
            // vahvistettu.
            _rewardedRequests.Add(replyTo);

            // Alkuperäisen viestin lähettäjä saa
            // pisteen myös myöhäisestä vastauksesta.
            _capital.Credit(initiatorId, 1);

            if (timing == ReplyTiming.Late)
                return 1;

            // Myöhästynyt vastaaja menettää oman
            // ylläpitopalkkionsa.
            _capital.Credit(responderId, 1);

            return 2;
        }
    }
}
