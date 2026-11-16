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
            new(
                StringComparer.Ordinal);

        private readonly HashSet<string> _sentPackages = new(StringComparer.Ordinal);

        private readonly HashSet<(string, string)> _attemptedPairs = new();

        private readonly List<ZineFirstContactAssessment> _assessments = new();

        public IReadOnlyList<ZineFirstContactAssessment> Assessments =>
            _assessments.AsReadOnly();

        private int _serial;
        private bool _disposed;

        public int SentCount { get; private set; }
        public int PositiveReplyCount { get; private set; }
        public int NegativeReplyCount { get; private set; }
        public int SilentDecisionCount { get; private set; }

        public int OfferSentCount { get; private set; }
        public int RequestSentCount { get; private set; }
        public int RequestFulfilledCount { get; private set; }
        public int RequestDeclinedCount { get; private set; }
        
        public int LaterIssueContactSentCount
        {
            get;
            private set;
        }

        public int MinimumPositiveMatchingAxes { get; }
        public double PositiveReplyProbability { get; }
        public double NegativeReplyProbability { get; }

        public int RequestUnansweredCount { get; private set; }

        public MultiIssueZineContactSelection
            MultiIssueSelection { get; set; }

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
                throw new ArgumentOutOfRangeException(
                    nameof(minimumPositiveMatchingAxes));

            if (!ValidProbability(positiveReplyProbability))
                throw new ArgumentOutOfRangeException(
                    nameof(positiveReplyProbability));

            if (!ValidProbability(negativeReplyProbability))
                throw new ArgumentOutOfRangeException(
                    nameof(negativeReplyProbability));

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
        /// Initiates a first contact for one of two reasons:
        ///
        /// OfferDemo:
        /// A supported held demo matches the target's published
        /// preference description.
        ///
        /// RequestDemo:
        /// The target's published advertised demo matches
        /// the reader's personal preference.
        ///
        /// Both paths require at least MinimumPositiveMatchingAxes
        /// matching axes. Neither creates a personal network arc.
        /// </summary>
        public bool TryInitiate(string sender)
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    nameof(ZineAutonomousSixAxisExchangeCycle));

            if (_postal.HasPendingOutreach(sender))
                return false;

            // Later publications may offer new contacts.
            // The original issue-1 behavior remains the
            // fallback if no later-issue contact is selected.
            if (TryInitiateFromLaterIssue(sender))
                return true;

            var held = _scenario.Inventory.GetHeldCopies(sender);

            var supported = new HashSet<string>(
                _scenario.GetReactionPolicy(sender)
                    .Decisions
                    .Where(d =>
                        d.Reaction ==
                        ListeningReaction.SupportAndCopy)
                    .Select(d => d.ProductId),
                StringComparer.Ordinal);

            var personal = _contacts.GetPersonalProfile(sender);

            foreach (var target in
                     _contacts.GetDirectoryContacts(sender))
            {
                var recipient = target.TargetNodeId;

                if (_attemptedPairs.Contains((sender, recipient)))
                    continue;

                // Do not replace the published address with
                // the recipient's current physical address.
                if (!_scenario.Network.TryGetKnownAddress(
                        sender,
                        recipient,
                        out var known) ||
                    known != target.PublishedAddressId)
                    continue;

                var targetDescription =
                    _contacts.GetEditorialDescription(recipient);

                // First possibility: offer a supported cassette
                // that the published target might appreciate.
                var source = held.FirstOrDefault(copy =>
                {
                    if (!supported.Contains(copy.ProductId))
                        return false;

                    if (!_profiles.TryGetProfile(
                            SixAxisProfileRole.DemoAtmosphere,
                            copy.ProductId,
                            out var demoProfile))
                        return false;

                    return demoProfile.MatchingAxes(
                               targetDescription) >=
                           MinimumPositiveMatchingAxes;
                });

                if (source != null)
                {
                    var number = ++_serial;

                    var cassette = _scenario.Inventory.RecordCopy(
                        source.Id,
                        "ZINE-FIRST-COPY:" + number,
                        sender,
                        sender);

                    var packageId = "ZINE-FIRST:" + number;

                    if (!_postal.TrySend(
                            sender,
                            false,
                            recipient,
                            cassette,
                            packageId,
                            out var shipment))
                        continue;

                    _scenario.CopyIndex.Register(
                        new CopyDistributionShipment(
                            recipient,
                            cassette,
                            shipment));

                    _attemptedPairs.Add((sender, recipient));
                    _sentPackages.Add(packageId);

                    SentCount++;
                    OfferSentCount++;

                    return true;
                }

                // Second possibility: request the demo actually
                // advertised in this issue's published claims.
                if (!_contacts.TryGetAdvertisedDemo(
                        sender,
                        recipient,
                        out var requestedDemoId))
                    continue;

                var publishedDemo =
                    _contacts.GetPublishedDemoDescription(
                        requestedDemoId);

                if (personal.MatchingAxes(publishedDemo) <
                    MinimumPositiveMatchingAxes)
                    continue;

                var requestNumber = ++_serial;

                var requestId =
                    "ZINE-DEMO-REQUEST:" + requestNumber;

                if (!_postal.TrySend(
                        sender,
                        false,
                        recipient,
                        null,
                        requestId,
                        out _,
                        requestedDemoId))
                    continue;

                _attemptedPairs.Add((sender, recipient));
                _sentPackages.Add(requestId);

                SentCount++;
                RequestSentCount++;

                return true;
            }

            return false;
        }


        private bool TryInitiateFromLaterIssue(string sender)
        {
            if (MultiIssueSelection == null)
                return false;

            foreach (var opportunity in
                     MultiIssueSelection.GetOpportunities(sender))
            {
                var recipient = opportunity.TargetNodeId;

                if (_attemptedPairs.Contains((sender, recipient)))
                    continue;

                if (!_scenario.Network.TryGetKnownAddress(
                        sender,
                        recipient,
                        out var knownAddress) ||
                    knownAddress !=
                    opportunity.PublishedAddressId)
                    continue;

                CassetteCopy outgoingCopy = null;
                string requestedDemoId = null;

                if (opportunity.Intent ==
                    MultiIssueContactIntent.OfferDemo)
                {
                    var source = _scenario.Inventory
                        .GetHeldCopies(sender)
                        .FirstOrDefault(c =>
                            c.ProductId == opportunity.DemoId);

                    if (source == null)
                        continue;

                    outgoingCopy = _scenario.Inventory.RecordCopy(
                        source.Id,
                        "ZINE-MULTI-COPY:" + ++_serial,
                        sender,
                        sender);
                }
                else
                {
                    requestedDemoId = opportunity.DemoId;
                }

                var packageId =
                    "ZINE-MULTI-FIRST:" + ++_serial;

                if (!_postal.TrySend(
                        sender,
                        false,
                        recipient,
                        outgoingCopy,
                        packageId,
                        out var shipment,
                        requestedDemoId))
                    continue;

                if (outgoingCopy != null)
                {
                    _scenario.CopyIndex.Register(
                        new CopyDistributionShipment(
                            recipient,
                            outgoingCopy,
                            shipment));

                    OfferSentCount++;
                }
                else
                {
                    RequestSentCount++;
                }

                _attemptedPairs.Add((sender, recipient));
                _sentPackages.Add(packageId);

                SentCount++;
                LaterIssueContactSentCount++;
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

// A cassette request does not require the recipient
// to listen to anything before answering.
                if (item.Package.RequestedDemoId != null)
                {
                    ProcessOpenedDemoRequest(item);
                    completedIds.Add(pair.Key);
                    continue;
                }

                var productId =
                    item.Package.PayloadProductId;

                var status = _scenario.GetListeningStatus(
                    item.RecipientNodeId,
                    productId);

                if (status == null ||
                    status.Phase != ListeningPhase.Completed)
                    continue;

                var preference = _profiles.GetProfile(
                    SixAxisProfileRole.PersonalPreference,
                    item.RecipientNodeId);

                var demo = _profiles.GetProfile(
                    SixAxisProfileRole.DemoAtmosphere,
                    productId);

                var matchingAxes = preference.MatchingAxes(demo);
                var positive =
                    matchingAxes >= MinimumPositiveMatchingAxes;

                var probability = positive
                    ? PositiveReplyProbability
                    : NegativeReplyProbability;

                ZineFirstContactOutcome outcome;

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

                        var source = candidates.FirstOrDefault(c =>
                            c.ProductId != productId &&
                            HasSupportedProduct(
                                item.RecipientNodeId,
                                c.ProductId));

                        if (source != null)
                            replyCassette = _scenario.Inventory.RecordCopy(
                                source.Id,
                                "ZINE-REPLY-COPY:" + ++_serial,
                                item.RecipientNodeId,
                                item.RecipientNodeId);

                        PositiveReplyCount++;
                    }
                    else
                    {
                        NegativeReplyCount++;
                    }

                    var reply = _postal.ReplyWithAddress(
                        item,
                        "ZINE-REPLY:" + ++_serial,
                        replyCassette,
                        _scenario.GetDemo(productId),
                        positive ? 1 : -1);

                    outcome = positive
                        ? ZineFirstContactOutcome.PositiveReply
                        : ZineFirstContactOutcome.NegativeReply;

                    // A positive reply with cassette must also
                    // enter the normal listening mechanism.
                    if (replyCassette != null)
                        _scenario.CopyIndex.Register(
                            new CopyDistributionShipment(
                                item.Package.SourceNodeId,
                                replyCassette,
                                reply));
                }
                else
                {
                    SilentDecisionCount++;
                    outcome = ZineFirstContactOutcome.Silence;
                }

                _assessments.Add(new ZineFirstContactAssessment(
                    item.RecipientNodeId,
                    productId,
                    item.Package.Id,
                    _scenario.Clock.CurrentDay,
                    matchingAxes,
                    outcome));

                completedIds.Add(pair.Key);
            }

            foreach (var id in completedIds)
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

        private bool HasSupportedProduct(
            string nodeId,
            string productId)
        {
            return _scenario.GetReactionPolicy(nodeId)
                .Decisions.Any(d =>
                    d.ProductId == productId &&
                    d.Reaction == ListeningReaction.SupportAndCopy);
        }

        /// <summary>
        /// Handles a cassette request after its letter was opened.
        ///
        /// Availability means the requested physical recording
        /// is currently held by the recipient.
        /// A requested copy is never generated from a mere
        /// zine advertisement or historical collection record.
        ///
        /// Acceptance reuses existing copying and postal services.
        /// Declining may produce an address-only reply; silence
        /// creates no reciprocal personal connection.
        /// </summary>
        private void ProcessOpenedDemoRequest(BacklogItem item)
        {
            var requestedDemoId =
                item.Package.RequestedDemoId;

            var recipient = item.RecipientNodeId;

            var source = _scenario.Inventory
                .GetHeldCopies(recipient)
                .FirstOrDefault(copy =>
                    copy.ProductId == requestedDemoId);

            var accepted =
                source != null &&
                _random.NextDouble() <
                PositiveReplyProbability;

            if (accepted)
            {
                var number = ++_serial;

                var replyCopy =
                    _scenario.Inventory.RecordCopy(
                        source.Id,
                        "ZINE-REQUEST-REPLY-COPY:" + number,
                        recipient,
                        recipient);

                var reply = _postal.ReplyWithAddress(
                    item,
                    "ZINE-REQUEST-REPLY:" + number,
                    replyCopy);

                _scenario.CopyIndex.Register(
                    new CopyDistributionShipment(
                        item.Package.SourceNodeId,
                        replyCopy,
                        reply));

                RequestFulfilledCount++;

                return;
            }

            if (_random.NextDouble() <
                NegativeReplyProbability)
            {
                // A polite refusal, not a negative opinion
                // about the requested music.
                _postal.ReplyWithAddress(
                    item,
                    "ZINE-REQUEST-DECLINE:" + ++_serial);

                RequestDeclinedCount++;
            }
            else
            {
                RequestUnansweredCount++;
            }
        }
    }
}