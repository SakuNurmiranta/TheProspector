
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// Read-only diagnostics for a configured circulation session.
    ///
    /// Measures observed activity without scheduling events,
    /// creating connections, or changing cultural capital.
    /// </summary>
    public sealed class AutonomousCirculationDiagnostics
    {
        public int Day { get; private set; }
        public int NodeCount { get; private set; }
        public int OpenedMessages { get; private set; }

        public int PersonalConnections { get; private set; }
        public int NewPersonalConnections { get; private set; }
        public int CrossCirclePersonalConnections { get; private set; }
        public int EstablishedFirstContactArcs { get; private set; }

        public int LayerThreeConnections { get; private set; }

        public int ZineReaders { get; private set; }
        public int ZineReadersCountryA { get; private set; }
        public int ZineReadersCountryB { get; private set; }

        public int ZinePrintedCopies { get; private set; }
        public int ZineCopiesHeld { get; private set; }
        public int ZineCopiesInTransit { get; private set; }

        public int FirstLettersSent { get; private set; }
        public int PositiveReplies { get; private set; }
        public int NegativeReplies { get; private set; }
        public int SilentDecisions { get; private set; }

        public int CrossCircleCopyDispatches { get; private set; }
        public int DemosBeyondOriginalCircle { get; private set; }
        public int MaximumDemoReach { get; private set; }

        public bool ZineCopiesConserved =>
            ZineCopiesHeld + ZineCopiesInTransit ==
            ZinePrintedCopies;

        private AutonomousCirculationDiagnostics()
        {
        }

        public static AutonomousCirculationDiagnostics Capture(
            DemoSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.InitialWorld == null ||
                session.InitialZine == null ||
                session.FirstContactCycle == null)
            {
                throw new InvalidOperationException(
                    "A configured zine circulation session is required.");
            }

            var scenario = session.Scenario;
            var initial = session.InitialWorld;
            var zine = session.InitialZine;

            var report = new AutonomousCirculationDiagnostics
            {
                Day = session.Day,
                NodeCount = session.NodeCount,
                OpenedMessages = session.OpenedMessageCount,
                ZinePrintedCopies = zine.PrintedCopies,
                ZineCopiesHeld =
                    zine.Bulk.GetTotalCopiesHeld(zine.Issue),
                ZineCopiesInTransit =
                    zine.Bulk.GetPostalInTransitCopies(zine.Issue),
                FirstLettersSent =
                    session.FirstContactCycle.SentCount,
                PositiveReplies =
                    session.FirstContactCycle.PositiveReplyCount,
                NegativeReplies =
                    session.FirstContactCycle.NegativeReplyCount,
                SilentDecisions =
                    session.FirstContactCycle.SilentDecisionCount
            };

            // Identify the originally separate friendship components.
            // Every ungrouped individual is their own component.
            var components = new Dictionary<string, string>(
                StringComparer.Ordinal);

            foreach (string id in scenario.NodeIds)
                components.Add(id, id);

            foreach (var circle in initial.FriendCircles)
            {
                string componentId = circle[0];

                foreach (string id in circle)
                    components[id] = componentId;
            }

            var originalConnectionIds = new HashSet<string>(
                initial.Definition.Connections.Select(c => c.Id),
                StringComparer.Ordinal);

            foreach (string senderId in scenario.NodeIds)
            {
                var personal = scenario.Network
                    .GetOutgoingConnections(
                        senderId,
                        CommunicationLayer.Interpersonal);

                foreach (var connection in personal)
                {
                    report.PersonalConnections++;

                    if (!originalConnectionIds.Contains(connection.Id))
                        report.NewPersonalConnections++;

                    if (components[senderId] !=
                        components[connection.TargetNodeId])
                    {
                        report.CrossCirclePersonalConnections++;
                    }

                    if (connection.Id.StartsWith(
                            "ESTABLISHED:",
                            StringComparison.Ordinal))
                    {
                        report.EstablishedFirstContactArcs++;
                    }
                }

                report.LayerThreeConnections +=
                    scenario.Network.GetOutgoingConnections(
                        senderId,
                        CommunicationLayer.Insert).Count;

                // These are ordinary copy-policy dispatches,
                // not merely first-contact letters.
                foreach (var shipment in
                         scenario.GetPolicy(senderId).Dispatched)
                {
                    if (components[senderId] !=
                        components[shipment.RecipientNodeId])
                    {
                        report.CrossCircleCopyDispatches++;
                    }
                }
            }

            var countries = initial.Definition.Nodes.ToDictionary(
                n => n.Id,
                n => n.CountryCode,
                StringComparer.Ordinal);

            foreach (string id in scenario.NodeIds)
            {
                if (!zine.Knowledge.HasReceived(
                        id, zine.Issue.ProductId))
                {
                    continue;
                }

                report.ZineReaders++;

                if (countries[id] == "A")
                    report.ZineReadersCountryA++;
                else if (countries[id] == "B")
                    report.ZineReadersCountryB++;
            }

            foreach (var seed in initial.Definition.Seeds)
            {
                int reach = 0;
                bool reachedOtherCircle = false;

                foreach (string id in scenario.NodeIds)
                {
                    if (!scenario.Inventory.HasEverHeldProduct(
                            id, seed.DemoId))
                    {
                        continue;
                    }

                    reach++;

                    if (components[id] !=
                        components[seed.SourceNodeId])
                    {
                        reachedOtherCircle = true;
                    }
                }

                report.MaximumDemoReach =
                    Math.Max(report.MaximumDemoReach, reach);

                if (reachedOtherCircle)
                    report.DemosBeyondOriginalCircle++;
            }

            return report;
        }

        public override string ToString()
        {
            return
                "Day=" + Day +
                " Nodes=" + NodeCount +
                " Opened=" + OpenedMessages +
                " PersonalArcs=" + PersonalConnections +
                " NewArcs=" + NewPersonalConnections +
                " CrossCircleArcs=" + CrossCirclePersonalConnections +
                " FirstContactArcs=" + EstablishedFirstContactArcs +
                " L3Arcs=" + LayerThreeConnections +
                " ZineReaders=" + ZineReaders +
                " (A=" + ZineReadersCountryA +
                ", B=" + ZineReadersCountryB + ")" +
                " ZineCopies=" + ZineCopiesHeld +
                "+" + ZineCopiesInTransit +
                "/" + ZinePrintedCopies +
                " FirstLetters=" + FirstLettersSent +
                " PositiveReplies=" + PositiveReplies +
                " NegativeReplies=" + NegativeReplies +
                " Silence=" + SilentDecisions +
                " CrossCircleCopyDispatches=" +
                CrossCircleCopyDispatches +
                " DemosBeyondCircle=" +
                DemosBeyondOriginalCircle +
                " MaxDemoReach=" + MaximumDemoReach;
        }
    }
}
