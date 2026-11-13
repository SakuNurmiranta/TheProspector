using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// One visible directional reply expectation.
    /// The direction is the expected reply direction,
    /// not the direction of the original request.
    /// </summary>
    public sealed class ReplyGuide
    {
        public string PackageId { get; }
        public string SourceNodeId { get; }
        public string TargetNodeId { get; }

        public float Progress01 { get; }
        public bool IsOverdue { get; }

        internal ReplyGuide(
            string packageId,
            string sourceNodeId,
            string targetNodeId,
            float progress01,
            bool isOverdue)
        {
            PackageId = packageId;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
            Progress01 = progress01;
            IsOverdue = isOverdue;
        }
    }

    /// <summary>
    /// Read-only visual projection.
    ///
    /// One guide per direction:
    ///  - overdue expectations take priority
    ///  - otherwise the earliest deadline is shown
    ///  - settled expectations are not displayed
    /// </summary>
    public static class ReplyExpectationProjection
    {
        public static IReadOnlyList<ReplyGuide> Build(
            IReadOnlyList<TrackedReplyExpectation>
                expectations,
            double currentDay)
        {
            if (expectations == null)
                throw new ArgumentNullException(
                    nameof(expectations));

            if (double.IsNaN(currentDay) ||
                double.IsInfinity(currentDay) ||
                currentDay < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentDay));
            }

            var chosen = new Dictionary<
                (string Source, string Target),
                TrackedReplyExpectation>();

            foreach (var expectation in expectations)
            {
                if (expectation.IsSettled)
                    continue;

                // A sent to B; the expected reply is B->A.
                var direction = (
                    expectation.RecipientNodeId,
                    expectation.SenderNodeId);

                if (!chosen.TryGetValue(
                        direction, out var current))
                {
                    chosen.Add(direction, expectation);
                    continue;
                }

                bool candidateOverdue =
                    currentDay > expectation.DeadlineDay;

                bool currentOverdue =
                    currentDay > current.DeadlineDay;

                bool replace =
                    candidateOverdue && !currentOverdue;

                if (candidateOverdue == currentOverdue)
                {
                    replace =
                        expectation.DeadlineDay <
                        current.DeadlineDay;

                    if (expectation.DeadlineDay ==
                        current.DeadlineDay)
                    {
                        replace = string.CompareOrdinal(
                            expectation.PackageId,
                            current.PackageId) < 0;
                    }
                }

                if (replace)
                    chosen[direction] = expectation;
            }

            var result = new List<ReplyGuide>();

            foreach (var pair in chosen)
            {
                var e = pair.Value;

                double duration = Math.Max(
                    1.0,
                    e.DeadlineDay - e.SentDay);

                double elapsed =
                    currentDay - e.SentDay;

                float progress = (float)Math.Min(
                    1.0,
                    Math.Max(0.0, elapsed / duration));

                result.Add(new ReplyGuide(
                    e.PackageId,
                    e.RecipientNodeId,
                    e.SenderNodeId,
                    progress,
                    currentDay > e.DeadlineDay));
            }

            result.Sort((a, b) =>
            {
                int source = string.CompareOrdinal(
                    a.SourceNodeId,
                    b.SourceNodeId);

                return source != 0
                    ? source
                    : string.CompareOrdinal(
                        a.TargetNodeId,
                        b.TargetNodeId);
            });

            return result.AsReadOnly();
        }
    }
}