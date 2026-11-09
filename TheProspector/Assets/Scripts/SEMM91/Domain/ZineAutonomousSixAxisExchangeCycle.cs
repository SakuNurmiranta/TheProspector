
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// Autonomous first cassette exchanges motivated by a zine.
    /// Uses the existing first-contact postal service and
    /// existing listening mechanism.
    /// </summary>
    public sealed class ZineAutonomousSixAxisExchangeCycle : IDisposable
    {
        private readonly ConfigurableCirculationScenario _scenario;
        private readonly ZineSixAxisContactEligibility _contacts;
        private readonly SixAxisProfileCatalog _profiles;
        private readonly ZineFirstContactPostalService _postal;
        private readonly Random _random;

        private readonly Dictionary<string, BacklogItem> _opened =
            new Dictionary<string, BacklogItem>(
                StringComparer.Ordinal);

        private readonly HashSet<string> _sentPackages =
            new HashSet<string>(StringComparer.Ordinal);

        private readonly HashSet<(string, string)> _attemptedPairs =
            new HashSet<(string, string)>();

        private int _serial;
        private bool _disposed;

        public int SentCount { get; private set; }
        public int PositiveReplyCount { get; private set; }
        public int NegativeReplyCount { get; private set; }
        public int SilentDecisionCount { get; private set; }

        public int MinimumPositiveMatchingAxes { get; }
        public double PositiveReplyProbability { get; }
        public double NegativeReplyProbability { get; }

        public ZineAutonomousSixAxisExchangeCycle(
            ConfigurableCirculationScenario scenario,
            ZineSixAxisContactEligibility contacts,
            SixAxisProfileCatalog profiles,
            ZineFirstContactPostalService postal,
            Random random,
            int minimumPositiveMatchingAxes = 4,
            double positiveReplyProbability = 0.80,
            double negativeReplyProbability = 0.35)
        {
            _scenario = scenario ??
                throw new ArgumentNullException(nameof(scenario));

            _contacts = contacts ??
                throw new ArgumentNullException(nameof(contacts));

            _profiles = profiles ??
                throw new ArgumentNullException(nameof(profiles));

            _postal = postal ??
                throw new ArgumentNullException(nameof(postal));

            _random = random ??
                throw new ArgumentNullException(nameof(random));

            if (minimumPositiveMatchingAxes < 0 ||
                minimumPositiveMatchingAxes > 6)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumPositiveMatchingAxes));
            }

            if (!ValidProbability(positiveReplyProbability))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(positiveReplyProbability));
            }

            if (!ValidProbability(negativeReplyProbability))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(negativeReplyProbability));
            }

            MinimumPositiveMatchingAxes =
                minimumPositiveMatchingAxes;

            PositiveReplyProbability =
                positiveReplyProbability;

            NegativeReplyProbability =
                negativeReplyProbability;

            _scenario.Exchange.MessageOpened += OnMessageOpened;
            _scenario.World.DayCompleted += OnDayCompleted;
        }

        private static bool ValidProbability(double value)
        {
            return !double.IsNaN(value) &&
                   value >= 0.0 &&
                   value <= 1.0;
        }

        private void OnMessageOpened(BacklogItem item)
        {
            if (_disposed || item == null)
                return;

            if (_sentPackages.Contains(item.Package.Id))
                _opened[item.Package.Id] = item;
        }

        private void OnDayCompleted(int day)
        {
            if (_disposed)
                return;

            ProcessCompletedListening();

            // Day 1 belongs to initial bulk distribution.
            if (day <= 1)
                return;

            foreach (var sender in _scenario.NodeIds)
                TryInitiate(sender);
        }

        /// <summary>
        /// A node can initiate one new first contact per day,
        /// provided it has a physical source cassette.
        /// </summary>
        public bool TryInitiate(string sender)
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    nameof(ZineAutonomousSixAxisExchangeCycle));

            if (_postal.HasPendingOutreach(sender))
                return false;

            var held = _scenario.Inventory.GetHeldCopies(sender);

            if (held.Count == 0)
                return false;

            foreach (var target in _contacts.GetCandidates(sender))
            {
                if (_attemptedPairs.Contains(
                        (sender, target.TargetNodeId)))
                    continue;

                // Use the actual published address snapshot.
                if (!_scenario.Network.TryGetKnownAddress(
                        sender,
                        target.TargetNodeId,
                        out string known) ||
                    known != target.PublishedAddressId)
                {
                    continue;
                }

                var source = held[0];
                var number = ++_serial;

                // Duplicate an existing cassette.
                // Retain the source copy with its owner.

                var cassette = _scenario.Inventory.RecordCopy(
                    source.Id,
                    "ZINE-FIRST-COPY:" + number,
                    sender,
                    sender);


                string packageId = "ZINE-FIRST:" + number;

                if (!_postal.TrySend(
                        sender,
                        false,
                        target.TargetNodeId,
                        cassette,
                        packageId,
                        out var shipment))
                {
                    continue;
                }

                // The listening policy resolves received
                // cassette copies through CopyShipmentIndex.
                _scenario.CopyIndex.Register(
                    new CopyDistributionShipment(
                        target.TargetNodeId,
                        cassette,
                        shipment));

                _attemptedPairs.Add(
                    (sender, target.TargetNodeId));

                _sentPackages.Add(packageId);

                SentCount++;
                return true;
            }

            return false;
        }

        private void ProcessCompletedListening()
        {
            if (_opened.Count == 0)
                return;

            var completedIds = new List<string>();

            foreach (var pair in _opened.OrderBy(
                         entry => entry.Key,
                         StringComparer.Ordinal))
            {
                var item = pair.Value;
                string productId =
                    item.Package.PayloadProductId;

                var status = _scenario.GetListeningStatus(
                    item.RecipientNodeId,
                    productId);

                if (status == null ||
                    status.Phase != ListeningPhase.Completed)
                {
                    continue;
                }

                var preference = _profiles.GetProfile(
                    SixAxisProfileRole.PersonalPreference,
                    item.RecipientNodeId);

                var demo = _profiles.GetProfile(
                    SixAxisProfileRole.DemoAtmosphere,
                    productId);

                bool positive =
                    preference.MatchingAxes(demo) >=
                    MinimumPositiveMatchingAxes;

                double probability = positive
                    ? PositiveReplyProbability
                    : NegativeReplyProbability;

                if (_random.NextDouble() < probability)
                {
                    CassetteCopy replyCassette = null;

                    if (positive)
                    {
                        // Prefer an owned demo distinct from
                        // the newly received cultural product.
                        var candidates =
                            _scenario.Inventory.GetHeldCopies(
                                item.RecipientNodeId);

                        var source = candidates.FirstOrDefault(
                            c => c.ProductId != productId);

                        if (source != null)
                        {

                            replyCassette = _scenario.Inventory.RecordCopy(
                                source.Id,
                                "ZINE-REPLY-COPY:" + (++_serial),
                                item.RecipientNodeId,
                                item.RecipientNodeId);

                        }

                        PositiveReplyCount++;
                    }
                    else
                    {
                        NegativeReplyCount++;
                    }

                    var reply = _postal.ReplyWithAddress(
                        item,
                        "ZINE-REPLY:" + (++_serial),
                        replyCassette,
                        _scenario.GetDemo(productId),
                        positive ? 1 : -1);

                    // A positive reply with cassette must also
                    // enter the normal listening mechanism.
                    if (replyCassette != null)
                    {
                        _scenario.CopyIndex.Register(
                            new CopyDistributionShipment(
                                item.Package.SourceNodeId,
                                replyCassette,
                                reply));
                    }
                }
                else
                {
                    SilentDecisionCount++;
                }

                completedIds.Add(pair.Key);
            }

            foreach (string id in completedIds)
                _opened.Remove(id);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _scenario.Exchange.MessageOpened -= OnMessageOpened;
            _scenario.World.DayCompleted -= OnDayCompleted;

            _disposed = true;
        }
    }
}
