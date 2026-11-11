using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    public sealed class CirculationSelectionDemoObservation
    {
        public string ProductId { get; }
        public int SupportCount { get; }
        public int TransferCount { get; }
        public int ShelveCount { get; }
        public int PositiveReplyCount { get; }
        public int NegativeReplyCount { get; }
        public int SilentAssessmentCount { get; }
        public int DivergenceCount { get; }

        internal CirculationSelectionDemoObservation(
            string productId,
            IReadOnlyList<ListeningReactionDecision> decisions,
            IReadOnlyList<ZineFirstContactAssessment> assessments,
            int divergenceCount)
        {
            ProductId = productId;

            SupportCount = decisions.Count(d =>
                d.Reaction == ListeningReaction.SupportAndCopy);

            TransferCount = decisions.Count(d =>
                d.Reaction == ListeningReaction.OpposeAndTransfer);

            ShelveCount = decisions.Count(d =>
                d.Reaction == ListeningReaction.OpposeAndShelve);

            PositiveReplyCount = assessments.Count(a =>
                a.Outcome == ZineFirstContactOutcome.PositiveReply);

            NegativeReplyCount = assessments.Count(a =>
                a.Outcome == ZineFirstContactOutcome.NegativeReply);

            SilentAssessmentCount = assessments.Count(a =>
                a.Outcome == ZineFirstContactOutcome.Silence);

            DivergenceCount = divergenceCount;
        }

        public override string ToString() =>
            ProductId +
            " Support=" + SupportCount +
            " Transfer=" + TransferCount +
            " Shelve=" + ShelveCount +
            " ZinePositive=" + PositiveReplyCount +
            " ZineNegative=" + NegativeReplyCount +
            " ZineSilent=" + SilentAssessmentCount +
            " DifferentVerdicts=" + DivergenceCount;
    }

    // Cross-channel difference, not proof of a broken rule.
    // The physical decision and publicly sent reply are distinct acts.
    public sealed class CirculationSelectionDivergence
    {
        public string NodeId { get; }
        public string ProductId { get; }
        public ListeningReaction PhysicalReaction { get; }
        public int PhysicalDay { get; }
        public ZineFirstContactOutcome ZineOutcome { get; }
        public int ReplyDay { get; }
        public string RequestPackageId { get; }

        internal CirculationSelectionDivergence(
            ListeningReactionDecision decision,
            ZineFirstContactAssessment assessment)
        {
            NodeId = decision.NodeId;
            ProductId = decision.ProductId;
            PhysicalReaction = decision.Reaction;
            PhysicalDay = decision.Day;
            ZineOutcome = assessment.Outcome;
            ReplyDay = assessment.Day;
            RequestPackageId = assessment.RequestPackageId;
        }

        public override string ToString() =>
            "Node=" + NodeId +
            " Product=" + ProductId +
            " Physical=" + PhysicalReaction +
            " Day=" + PhysicalDay +
            " Zine=" + ZineOutcome +
            " Day=" + ReplyDay +
            " Request=" + RequestPackageId;
    }

    /// <summary>
    /// Read-only research snapshot. No new listening, copying,
    /// transfer, shelving or postal behavior is initiated.
    /// Optional aesthetics remain optional for legacy sessions.
    /// </summary>
    public sealed class CirculationSelectionReport
    {
        public int Day { get; }

        public IReadOnlyList<ListeningReactionDecision>
            PhysicalDecisions { get; }

        public IReadOnlyList<ZineFirstContactAssessment>
            FirstContactAssessments { get; }

        public IReadOnlyList<CirculationSelectionDivergence>
            Divergences { get; }

        public IReadOnlyList<CirculationSelectionDemoObservation>
            Demos { get; }

        public int PendingTransferCount { get; }

        public int SixAxisDecisionCount =>
            PhysicalDecisions.Count(d =>
                d.Source ==
                ListeningReactionSelectionSource.SixAxisProfile);

        public int SupportCount =>
            Demos.Sum(d => d.SupportCount);

        public int TransferCount =>
            Demos.Sum(d => d.TransferCount);

        public int ShelveCount =>
            Demos.Sum(d => d.ShelveCount);

        public int PositiveReplyCount =>
            Demos.Sum(d => d.PositiveReplyCount);

        public int NegativeReplyCount =>
            Demos.Sum(d => d.NegativeReplyCount);

        public int SilentAssessmentCount =>
            Demos.Sum(d => d.SilentAssessmentCount);

        private CirculationSelectionReport(
            int day,
            int pendingTransferCount,
            IReadOnlyList<ListeningReactionDecision> decisions,
            IReadOnlyList<ZineFirstContactAssessment> assessments,
            IReadOnlyList<CirculationSelectionDivergence> divergences,
            IReadOnlyList<CirculationSelectionDemoObservation> demos)
        {
            Day = day;
            PendingTransferCount = pendingTransferCount;
            PhysicalDecisions = decisions;
            FirstContactAssessments = assessments;
            Divergences = divergences;
            Demos = demos;
        }

        public static bool AreVerdictsDifferent(
            ListeningReaction physical,
            ZineFirstContactOutcome zine)
        {
            if (zine == ZineFirstContactOutcome.Silence)
                return false;

            return
                (physical == ListeningReaction.SupportAndCopy &&
                 zine == ZineFirstContactOutcome.NegativeReply) ||

                (physical != ListeningReaction.SupportAndCopy &&
                 zine == ZineFirstContactOutcome.PositiveReply);
        }

        public static CirculationSelectionReport Capture(
            DemoSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var scenario = session.Scenario;

            var decisions = scenario.NodeIds
                .SelectMany(id =>
                    scenario.GetReactionPolicy(id).Decisions)
                .OrderBy(d => d.Day)
                .ThenBy(
                    d => d.NodeId,
                    StringComparer.Ordinal)
                .ThenBy(
                    d => d.ProductId,
                    StringComparer.Ordinal)
                .ToList();

            var assessments =
                (session.FirstContactCycle == null
                    ? Enumerable.Empty<ZineFirstContactAssessment>()
                    : session.FirstContactCycle.Assessments)
                .OrderBy(a => a.Day)
                .ThenBy(
                    a => a.RequestPackageId,
                    StringComparer.Ordinal)
                .ToList();

            var byNodeAndProduct = decisions.ToDictionary(
                d => (d.NodeId, d.ProductId),
                d => d);

            var divergences =
                new List<CirculationSelectionDivergence>();

            foreach (var assessment in assessments)
            {
                if (byNodeAndProduct.TryGetValue(
                        (assessment.NodeId, assessment.ProductId),
                        out var decision) &&
                    AreVerdictsDifferent(
                        decision.Reaction,
                        assessment.Outcome))
                {
                    divergences.Add(
                        new CirculationSelectionDivergence(
                            decision,
                            assessment));
                }
            }

            var rows =
                new List<CirculationSelectionDemoObservation>();

            foreach (string productId in scenario.ProductIds)
            {
                var forProductDecisions = decisions
                    .Where(d =>
                        d.ProductId == productId)
                    .ToList();

                var forProductAssessments = assessments
                    .Where(a =>
                        a.ProductId == productId)
                    .ToList();

                rows.Add(
                    new CirculationSelectionDemoObservation(
                        productId,
                        forProductDecisions,
                        forProductAssessments,
                        divergences.Count(d =>
                            d.ProductId == productId)));
            }

            // Count transfers still waiting for a valid route.
            // This does not modify any simulation state.
            int pendingTransfers = 0;

            foreach (string nodeId in scenario.NodeIds)
            {
                pendingTransfers += scenario
                    .GetReactionPolicy(nodeId)
                    .PendingTransferCount;
            }

            return new CirculationSelectionReport(
                session.Day,
                pendingTransfers,
                decisions.AsReadOnly(),
                assessments.AsReadOnly(),
                divergences.AsReadOnly(),
                rows.AsReadOnly());
        }

        public override string ToString() =>
            "Day=" + Day +
            " Support=" + SupportCount +
            " Transfer=" + TransferCount +
            " Shelve=" + ShelveCount +
            " PendingTransfer=" + PendingTransferCount +
            " SixAxisDecisions=" + SixAxisDecisionCount +
            " ZinePositive=" + PositiveReplyCount +
            " ZineNegative=" + NegativeReplyCount +
            " ZineSilent=" + SilentAssessmentCount +
            " DifferentVerdicts=" + Divergences.Count;
    }
}