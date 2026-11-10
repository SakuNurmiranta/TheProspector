using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public enum ListeningReaction
    {
        SupportAndCopy,
        OpposeAndTransfer,
        OpposeAndShelve
    }

    /// <summary>
    /// Kuuntelun jälkeisten fyysisten
    /// kontrollitoimenpiteiden rajapinta.
    ///
    /// TransferOne: sama kasetti siirtyy
    /// yhdelle vastaanottajalle, ei kopiota.
    ///
    /// Shelve: kasetti jää haltijalle,
    /// eikä sitä lähetetä edelleen.
    /// </summary>
    public interface IPhysicalControlActions
    {
        void TransferOne(
            string nodeId,
            CassetteCopy copy,
            int day);

        void Shelve(
            string nodeId,
            CassetteCopy copy,
            int day);
    }

    public sealed class ListeningReactionDecision
    {
        public string NodeId { get; }
        public string ProductId { get; }
        public string CopyId { get; }
        public int Day { get; }
        public ListeningReaction Reaction { get; }

        internal ListeningReactionDecision(
            string nodeId,
            CassetteCopy copy,
            int day,
            ListeningReaction reaction)
        {
            NodeId = nodeId;
            ProductId = copy.ProductId;
            CopyId = copy.Id;
            Day = day;
            Reaction = reaction;
        }
    }

    /// <summary>
    /// Ohjaa kuuntelun jälkeisen päätöksen
    /// täsmälleen yhteen kolmesta toiminnosta.
    ///
    /// Käytetään TimedListeningNodePolicy-luokan
    /// sisäisenä toimintapolitiikkana.
    /// </summary>
    public sealed class ListeningReactionNodePolicy
        : ISimulationNodePolicy
    {
        private readonly string _nodeId;

        private readonly PositiveCopyNodePolicy
            _positivePolicy;

        private readonly Func<BacklogItem, CassetteCopy>
            _resolveCopy;

        private readonly Func<string, string, ListeningReaction>
            _chooseReaction;

        private readonly IPhysicalControlActions _control;

        private readonly HashSet<string> _handledProducts =
            new(
                StringComparer.Ordinal);

        private readonly List<ListeningReactionDecision>
            _decisions = new();

        private readonly IReadOnlyList<ListeningReactionDecision>
            _decisionView;

        public IReadOnlyList<ListeningReactionDecision>
            Decisions => _decisionView;

        public ListeningReactionNodePolicy(
            string nodeId,
            PositiveCopyNodePolicy positivePolicy,
            Func<BacklogItem, CassetteCopy> resolveCopy,
            Func<string, string, ListeningReaction> chooseReaction,
            IPhysicalControlActions control)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException(
                    "Node ID cannot be empty.",
                    nameof(nodeId));

            _nodeId = nodeId;

            _positivePolicy = positivePolicy ??
                              throw new ArgumentNullException(
                                  nameof(positivePolicy));

            _resolveCopy = resolveCopy ??
                           throw new ArgumentNullException(
                               nameof(resolveCopy));

            _chooseReaction = chooseReaction ??
                              throw new ArgumentNullException(
                                  nameof(chooseReaction));

            _control = control ??
                       throw new ArgumentNullException(
                           nameof(control));

            _decisionView = _decisions.AsReadOnly();
        }

        public void OnDay(string nodeId, int day)
        {
            ValidateNode(nodeId);

            _positivePolicy.OnDay(nodeId, day);
        }


        public void OnMessageOpened(
            string nodeId,
            BacklogItem message,
            int capitalAward,
            int day)
        {
            ValidateNode(nodeId);

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var copy = _resolveCopy(message);

            if (copy == null)
            {
                // Preserve feedback-only postal behavior.
                _positivePolicy.OnMessageOpened(
                    nodeId,
                    message,
                    capitalAward,
                    day);

                return;
            }

            HandlePhysicalCopy(
                copy,
                day,
                () => _positivePolicy.OnMessageOpened(
                    nodeId,
                    message,
                    capitalAward,
                    day));
        }

        public void OnLocalListeningCompleted(
            string nodeId,
            CassetteCopy copy,
            int day)
        {
            ValidateNode(nodeId);

            if (copy == null)
                throw new ArgumentNullException(nameof(copy));

            HandlePhysicalCopy(
                copy,
                day,
                () => _positivePolicy.OnLocalListeningCompleted(
                    nodeId,
                    copy,
                    day));
        }

        private void HandlePhysicalCopy(
            CassetteCopy copy,
            int day,
            Action onPositive)
        {
            if (day < 0)
                throw new ArgumentOutOfRangeException(nameof(day));

            // One reaction per node and cultural product.
            if (_handledProducts.Contains(copy.ProductId))
                return;

            if (!string.Equals(
                    copy.HolderNodeId,
                    _nodeId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The node does not hold this cassette.");

            var reaction =
                _chooseReaction(_nodeId, copy.ProductId);

            if (!Enum.IsDefined(
                    typeof(ListeningReaction),
                    reaction))
                throw new InvalidOperationException(
                    "Undefined listening reaction.");

            switch (reaction)
            {
                case ListeningReaction.SupportAndCopy:
                    onPositive();
                    break;

                case ListeningReaction.OpposeAndTransfer:
                    _control.TransferOne(
                        _nodeId, copy, day);
                    break;

                case ListeningReaction.OpposeAndShelve:
                    _control.Shelve(
                        _nodeId, copy, day);
                    break;
            }

            // Commit the decision only after the selected
            // operation has completed successfully.
            _handledProducts.Add(copy.ProductId);

            _decisions.Add(
                new ListeningReactionDecision(
                    _nodeId,
                    copy,
                    day,
                    reaction));
        }


        private void ValidateNode(string nodeId)
        {
            if (!string.Equals(
                    _nodeId,
                    nodeId,
                    StringComparison.Ordinal))
                throw new ArgumentException(
                    "Reaction policy invoked for wrong node.",
                    nameof(nodeId));
        }
    }
}