
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Continues the configured world's initial zine
    /// with periodically published physical issues.
    ///
    /// Issue 1 already exists at simulation day zero.
    /// This service starts from issue 2.
    ///
    /// Editorial content is collected from opened
    /// correspondence, not from private node state.
    ///
    /// Initial copies use existing Layer 3 distributor
    /// routes. Further postal circulation of multiple
    /// issues is a separate integration step.
    /// </summary>
    public sealed class RecurringZineCirculation : IDisposable
    {
        public ZineEditorialCycle Editorial { get; }

        public ZineEditorialMailCollector MailCollector
        {
            get;
        }

        public ZinePublicationDistribution Distribution
        {
            get;
        }

        public IReadOnlyList<ZineEditorialIssue> Issues =>
            Editorial.Issues;

        private bool _disposed;

        private readonly ZineIssueKnowledgeRegistry _knowledge;
        private readonly string _editorId;
        
        public RecurringZineCirculation(
            ConfigurableCirculationScenario scenario,
            InitialZineCirculation initial,
            int publicationIntervalDays = 90,
            int nodesPerCopy = 4)
        {
            if (scenario == null)
                throw new ArgumentNullException(
                    nameof(scenario));

            if (initial == null)
                throw new ArgumentNullException(
                    nameof(initial));

            if (publicationIntervalDays < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(publicationIntervalDays));

            if (nodesPerCopy < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(nodesPerCopy));

            var first = initial.Issue;
            string editorId = first.EditorNodeId;

            _editorId = editorId;
            _knowledge = initial.Knowledge;

            Editorial = new ZineEditorialCycle(
                first.ZineId,
                editorId,
                first.Profile,
                scenario.Clock,
                publicationIntervalDays,
                first.Number + 1);

            Editorial.IssuePublished += OnIssuePublished;
            
            // Collects only information arriving in
            // actual correspondence opened by editor.
            MailCollector = new ZineEditorialMailCollector(
                Editorial,
                scenario.Exchange);

            // Reuse the same physical issue registry and
            // the same knowledge registry as issue 1.
            // Each issue has its own independent print run.
            Distribution = new ZinePublicationDistribution(
                Editorial,
                initial.Bulk,
                () => scenario.Network.GetOutgoingConnections(
                    editorId,
                    CommunicationLayer.Insert),
                scenario.Network.NodeCount,
                nodesPerCopy);
        }

        private void OnIssuePublished(ZineEditorialIssue issue)
        {
            _knowledge.ReceiveIssue(_editorId, issue);
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;

            Editorial.IssuePublished -= OnIssuePublished;
            
            Distribution.Dispose();
            MailCollector.Dispose();
            Editorial.Stop();

            _disposed = true;
        }
    }
}
