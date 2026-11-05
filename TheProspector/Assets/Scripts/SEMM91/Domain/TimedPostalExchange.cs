
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class TimedPostalExchange
    {
        private readonly SimulationClock _clock;
        private readonly Network _network;
        private readonly CassetteInventory _inventory;
        private readonly RecipientBacklog _backlog;
        private readonly ReplyWindowRegistry _windows;

        private readonly CopyDeliveryRewardService
            _copyRewards;

        private readonly ReciprocalExchangeRewardService
            _reciprocalRewards;

        private readonly CommunicatedStanceRegistry
            _stances;

        private readonly ReplyWindowSkepticismMonitor
            _replySkepticism;
        
        private readonly Dictionary<string, PostalShipment>
            _shipments =
                new Dictionary<string, PostalShipment>(
                    StringComparer.Ordinal);

        private readonly HashSet<string> _usedPackageIds =
            new HashSet<string>(StringComparer.Ordinal);

        private readonly Dictionary<string, ReplyTiming>
            _arrivalTiming =
                new Dictionary<string, ReplyTiming>(
                    StringComparer.Ordinal);

        public CommunicatedStanceRegistry CommunicatedStances
            => _stances;

        public TimedPostalExchange(
            SimulationClock clock,
            Network network,
            CassetteInventory inventory,
            RecipientBacklog backlog,
            ReplyWindowRegistry windows,
            CopyDeliveryRewardService copyRewards,
            ReciprocalExchangeRewardService reciprocalRewards,
            CommunicatedStanceRegistry communicatedStances = null,
            ReplyWindowSkepticismMonitor replySkepticism = null)
            
        {
            _clock = clock ??
                throw new ArgumentNullException(
                    nameof(clock));

            _network = network ??
                throw new ArgumentNullException(
                    nameof(network));

            _inventory = inventory ??
                throw new ArgumentNullException(
                    nameof(inventory));

            _backlog = backlog ??
                throw new ArgumentNullException(
                    nameof(backlog));

            _windows = windows ??
                throw new ArgumentNullException(
                    nameof(windows));

            _copyRewards = copyRewards ??
                throw new ArgumentNullException(
                    nameof(copyRewards));

            _reciprocalRewards = reciprocalRewards ??
                throw new ArgumentNullException(
                    nameof(reciprocalRewards));

            // Valinnainen parametri säilyttää
            // aiemmat konstruktorikutsut toimivina.
            _stances = communicatedStances ??
                new CommunicatedStanceRegistry();
            _replySkepticism = replySkepticism;
        }


        public PostalShipment Send(
            string shipmentId,
            MessagePackage package,
            string senderCountryCode,
            string recipientCountryCode,
            int replyAllowanceDays,
            bool expectsReplyForSkepticism = false,
            bool compensatesReply = false)

        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException(
                    "Shipment ID cannot be empty.",
                    nameof(shipmentId));

            if (package == null)
                throw new ArgumentNullException(
                    nameof(package));

            if (_shipments.ContainsKey(shipmentId))
                throw new ArgumentException(
                    "Shipment ID already used.",
                    nameof(shipmentId));

            if (_usedPackageIds.Contains(package.Id))
                throw new InvalidOperationException(
                    "Message package was already sent.");
            
            
            if ((expectsReplyForSkepticism || compensatesReply) &&
                _replySkepticism == null)
            {
                throw new InvalidOperationException(
                    "Reply skepticism monitor is not configured.");
            }

            if (compensatesReply &&
                string.IsNullOrWhiteSpace(package.ReplyToPackageId))
            {
                throw new ArgumentException(
                    "Compensation requires an explicit reply reference.",
                    nameof(compensatesReply));
            }


            int transitDays =
                PostalTimingRules.GetTransitDays(
                    senderCountryCode,
                    recipientCountryCode);

            _ = checked(_clock.CurrentDay + transitDays);

            _ = PostalTimingRules.CalculateDeadline(
                _clock.CurrentDay,
                transitDays,
                replyAllowanceDays);

            var shipment = PostalShipment.Dispatch(
                shipmentId,
                _network,
                _inventory,
                package);


            var window = _windows.Register(
                package.Id,
                package.SourceNodeId,
                package.TargetNodeId,
                _clock.CurrentDay,
                senderCountryCode,
                recipientCountryCode,
                replyAllowanceDays);

            if (expectsReplyForSkepticism)
            {
                _replySkepticism.Track(
                    package.Id,
                    package.SourceNodeId,
                    package.TargetNodeId,
                    window.DeadlineDay);
            }


            _clock.ScheduleAfter(
                "POSTAL-ARRIVAL:" + shipmentId,
                transitDays,

                () => ProcessArrival(
                    shipment,
                    transitDays,
                    compensatesReply));


            _shipments.Add(shipmentId, shipment);
            _usedPackageIds.Add(package.Id);

            return shipment;
        }


        private void ProcessArrival(
            PostalShipment shipment,
            int transitDays,
            bool compensatesReply)

        {
            _copyRewards.ProcessArrivalAndAward(shipment);

            if (shipment.Status ==
                PostalShipmentStatus.Returning)
            {
                _clock.ScheduleAfter(
                    "POSTAL-RETURN:" + shipment.Id,
                    transitDays,
                    () => shipment.ProcessReturn());

                return;
            }

            if (shipment.Status !=
                PostalShipmentStatus.Delivered)
            {
                return;
            }

            var package = shipment.Package;

            ReplyTiming timing = _windows.Resolve(
                package.ReplyToPackageId,
                package.SourceNodeId,
                package.TargetNodeId,
                _clock.CurrentDay);

            _arrivalTiming.Add(shipment.Id, timing);
            
            
// Vain toimitettu, oikean suuntainen
// ja tunnettu vastaus voi sulkea
// skeptisyyden odotusikkunan.
            if (_replySkepticism != null &&
                timing != ReplyTiming.Unknown)
            {
                _replySkepticism.ReplyArrived(
                    package.ReplyToPackageId,
                    package.SourceNodeId,
                    package.TargetNodeId,
                    compensates: compensatesReply);
            }


            // Postin saapuminen ei vielä muuta
            // vastaanottajan tietoa kannanotoista.
            _backlog.ReceiveDelivered(shipment);
        }

        
        public bool TryOpenNext(
            string recipientNodeId,
            out int awardedCapital)
        {
            // Vanha rajapinta säilytetään, jotta
            // aiemmat testit ja skenaariot toimivat.
            return TryOpenNext(
                recipientNodeId,
                out awardedCapital,
                out _);
        }

        public bool TryOpenNext(
            string recipientNodeId,
            out int awardedCapital,
            out BacklogItem openedItem)
        {
            awardedCapital = 0;
            openedItem = null;

            if (!_backlog.TryTakeNext(
                    recipientNodeId, out var item))
            {
                return false;
            }

            ReplyTiming timing = ReplyTiming.Unknown;

            if (_arrivalTiming.TryGetValue(
                    item.ShipmentId,
                    out var resolved))
            {
                timing = resolved;
            }

            awardedCapital = _reciprocalRewards.Open(
                item,
                _network,
                timing);

            // Avattu viesti päivittää myös vastaanottajan
            // tiedon lähettäjän viestimästä kannasta.
            _stances.RecordOpened(item.Package);

            // Vasta onnistuneen käsittelyn jälkeen
            // palautetaan avattu viesti kutsujalle.
            openedItem = item;

            return true;
        }

    }
}
