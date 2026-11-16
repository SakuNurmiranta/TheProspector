
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class ZineRuntimeSnapshot
    {
        public int Day { get; private set; }
        public bool RecurringEnabled { get; private set; }

        public int PublishedIssueCount { get; private set; }
        public int LaterIssueCount { get; private set; }

        // Unique people who know at least one later issue.
        public int LaterIssueReaders { get; private set; }

        // Total reader/issue pairs.
        public int LaterIssueReadEvents { get; private set; }

        public int LaterIssuePrintedCopies { get; private set; }
        public int LaterIssuePostalAttachments { get; private set; }
        public int LaterIssueOpenedInserts { get; private set; }

        public int PublishedAddresses { get; private set; }
        public int PublishedContactDescriptions { get; private set; }
        public int PublishedDemoAdvertisements { get; private set; }

        public int LaterIssueContactsSent { get; private set; }

        private ZineRuntimeSnapshot()
        {
        }

        public static ZineRuntimeSnapshot Capture(
            DemoSession session)
        {
            if (session == null)
                throw new ArgumentNullException(
                    nameof(session));

            var result = new ZineRuntimeSnapshot
            {
                Day = session.Day,
                RecurringEnabled =
                    session.RecurringZines != null,
                PublishedIssueCount = 1
            };

            if (session.RecurringZines == null)
                return result;

            var issues = session.RecurringZines.Issues;

            result.LaterIssueCount = issues.Count;
            result.PublishedIssueCount += issues.Count;

            var readers = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (var issue in issues)
            {
                result.LaterIssuePrintedCopies +=
                    session.InitialZine.Bulk
                        .GetPrintedCopies(issue);

                foreach (var entry in issue.Entries)
                {
                    switch (entry.Kind)
                    {
                        case ZineEditorialEntryKind.Address:
                            result.PublishedAddresses++;
                            break;

                        case ZineEditorialEntryKind
                            .ContactSixAxisDescription:
                            result.PublishedContactDescriptions++;
                            break;

                        case ZineEditorialEntryKind
                            .DemoAdvertisement:
                            result.PublishedDemoAdvertisements++;
                            break;
                    }
                }

                foreach (string nodeId in
                         session.Scenario.NodeIds)
                {
                    if (!session.InitialZine.Knowledge
                            .HasReceived(
                                nodeId, issue.ProductId))
                    {
                        continue;
                    }

                    readers.Add(nodeId);
                    result.LaterIssueReadEvents++;
                }
            }

            result.LaterIssueReaders = readers.Count;

            if (session.MultiIssueZinePostalInserts != null)
            {
                result.LaterIssuePostalAttachments =
                    session.MultiIssueZinePostalInserts
                        .AttachedCount;

                result.LaterIssueOpenedInserts =
                    session.MultiIssueZinePostalInserts
                        .OpenedCount;
            }

            if (session.FirstContactCycle != null)
            {
                result.LaterIssueContactsSent =
                    session.FirstContactCycle
                        .LaterIssueContactSentCount;
            }

            return result;
        }
    }
}
