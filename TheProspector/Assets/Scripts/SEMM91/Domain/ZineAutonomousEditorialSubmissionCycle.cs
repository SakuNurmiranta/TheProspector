
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// Autonomous submissions of supported physical demos
    /// to a zine editor.
    ///
    /// The sender must:
    /// - have read a publication by the editor;
    /// - know the editor's published postal address;
    /// - hold a physical cassette;
    /// - have completed listening and supported the demo.
    ///
    /// Uses normal postal delivery without creating
    /// permanent interpersonal network arcs.
    /// </summary>
    public sealed class ZineAutonomousEditorialSubmissionCycle
        : IDisposable
    {
        private const int ReplyAllowanceDays = 5;

        private readonly ConfigurableCirculationScenario _scenario;
        private readonly ZineIssueKnowledgeRegistry _knowledge;
        private readonly ZineEditorialCycle _editorial;

        private readonly HashSet<(string Sender, string Demo)>
            _submitted =
                new HashSet<(string Sender, string Demo)>();

        private readonly int _maxSubmissionsPerDay;

        private int _serial;
        private bool _disposed;

        public int SubmittedCount { get; private set; }

        public int MaxSubmissionsPerDay =>
            _maxSubmissionsPerDay;

        public ZineAutonomousEditorialSubmissionCycle(
            ConfigurableCirculationScenario scenario,
            ZineIssueKnowledgeRegistry knowledge,
            ZineEditorialCycle editorial,
            int maxSubmissionsPerDay = 1)
        {
            _scenario = scenario ??
                throw new ArgumentNullException(
                    nameof(scenario));

            _knowledge = knowledge ??
                throw new ArgumentNullException(
                    nameof(knowledge));

            _editorial = editorial ??
                throw new ArgumentNullException(
                    nameof(editorial));

            if (maxSubmissionsPerDay < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxSubmissionsPerDay));
            }

            if (!_scenario.Network.TryGetNode(
                    _editorial.EditorNodeId, out _))
            {
                throw new ArgumentException(
                    "The editor must exist in the network.",
                    nameof(editorial));
            }

            _maxSubmissionsPerDay =
                maxSubmissionsPerDay;

            // The editor deliberately publishes their
            // own return address in each new issue.
            QueueEditorialReturnAddress();

            _editorial.IssuePublished +=
                OnIssuePublished;

            _scenario.World.DayCompleted +=
                OnDayCompleted;
        }

        private void QueueEditorialReturnAddress()
        {
            if (!_scenario.Network.TryGetNodeAddress(
                    _editorial.EditorNodeId,
                    out string editorAddress))
            {
                throw new InvalidOperationException(
                    "Editor has no current postal address.");
            }

            _editorial.RecordAddress(
                _editorial.EditorNodeId,
                editorAddress);
        }

        private void OnIssuePublished(ZineEditorialIssue issue)
        {
            if (_disposed)
                return;

            // The previous issue is already immutable.
            // This declaration belongs to the next issue.
            QueueEditorialReturnAddress();
        }

        private void OnDayCompleted(int day)
        {
            if (_disposed || day <= 1)
                return;

            int submittedToday = 0;

            foreach (string sender in _scenario.NodeIds)
            {
                if (submittedToday >= _maxSubmissionsPerDay)
                    break;

                if (TrySubmit(sender))
                    submittedToday++;
            }
        }

        public bool TrySubmit(string senderNodeId)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(
                        ZineAutonomousEditorialSubmissionCycle));
            }

            if (string.IsNullOrWhiteSpace(senderNodeId))
            {
                throw new ArgumentException(
                    "Sender ID is required.",
                    nameof(senderNodeId));
            }

            string editorId = _editorial.EditorNodeId;

            if (senderNodeId == editorId)
                return false;

            var network = _scenario.Network;

            if (!network.TryGetNode(senderNodeId, out _))
                return false;

            // A merely known address is not sufficient.
            // The sender must have read an issue from
            // this editor publishing the editorial address.
            string publishedAddress =
                _knowledge.GetLatestPublishedAddress(
                    senderNodeId,
                    editorId);

            if (string.IsNullOrWhiteSpace(publishedAddress))
                return false;

            bool hasReadEditorialAddress =
                _knowledge.GetClaims(senderNodeId).Any(
                    claim =>
                        claim.EditorNodeId == editorId &&
                        claim.Entry.Kind ==
                            ZineEditorialEntryKind.Address &&
                        claim.Entry.SubjectNodeId == editorId &&
                        claim.Entry.AddressId ==
                            publishedAddress);

            if (!hasReadEditorialAddress)
                return false;

            // Use the sender's knowledge, never the
            // editor's private current address.
            if (!network.TryGetKnownAddress(
                    senderNodeId,
                    editorId,
                    out string knownAddress) ||
                knownAddress != publishedAddress)
            {
                return false;
            }

            if (!network.TryGetNodeAddress(
                    senderNodeId,
                    out string senderAddressId))
            {
                return false;
            }

            if (!network.TryGetAddress(
                    senderAddressId,
                    out var senderAddress) ||
                !network.TryGetAddress(
                    knownAddress,
                    out var editorAddress))
            {
                return false;
            }

            // Only the sender's own completed,
            // positive listening decisions are eligible.
            var supported = new HashSet<string>(
                _scenario.GetReactionPolicy(senderNodeId)
                    .Decisions
                    .Where(d =>
                        d.Reaction ==
                        ListeningReaction.SupportAndCopy)
                    .Select(d => d.ProductId),
                StringComparer.Ordinal);

            foreach (var source in
                     _scenario.Inventory.GetHeldCopies(
                         senderNodeId))
            {
                if (!supported.Contains(source.ProductId))
                    continue;

                var status = _scenario.GetListeningStatus(
                    senderNodeId,
                    source.ProductId);

                if (status == null ||
                    status.Phase != ListeningPhase.Completed)
                {
                    continue;
                }

                var key = (
                    Sender: senderNodeId,
                    Demo: source.ProductId);

                if (_submitted.Contains(key))
                    continue;

                int nextSerial = _serial + 1;

                string copyId =
                    "ZINE-EDITORIAL-COPY:" + nextSerial;

                string packageId =
                    "ZINE-EDITORIAL-MSG:" + nextSerial;

                string shipmentId =
                    "ZINE-EDITORIAL-SHIP:" + nextSerial;

                // This is a temporary postal route.
                // It is NOT added to the directed
                // multigraph as a personal relationship.
                var route = new NetworkConnection(
                    "ZINE-EDITORIAL-ROUTE:" + nextSerial,
                    senderNodeId,
                    editorId,
                    CommunicationLayer.Interpersonal,
                    "editorial-submission");

                // Duplicate the physical cassette.
                // The sender retains the original.
                var copy = _scenario.Inventory.RecordCopy(
                    source.Id,
                    copyId,
                    senderNodeId,
                    senderNodeId);

                var package = new MessagePackage(
                    packageId,
                    route,
                    cassetteCopy: copy,
                    addressNoticeAddressId:
                        senderAddressId);

                var shipment = _scenario.Exchange.Send(
                    shipmentId,
                    package,
                    senderAddress.CountryId,
                    editorAddress.CountryId,
                    ReplyAllowanceDays);

                // Register the physical shipment for
                // the existing listening mechanism.
                _scenario.CopyIndex.RegisterTransfer(
                    shipment,
                    copy);

                _serial = nextSerial;
                _submitted.Add(key);
                SubmittedCount++;

                return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _editorial.IssuePublished -=
                OnIssuePublished;

            _scenario.World.DayCompleted -=
                OnDayCompleted;

            _disposed = true;
        }
    }
}
