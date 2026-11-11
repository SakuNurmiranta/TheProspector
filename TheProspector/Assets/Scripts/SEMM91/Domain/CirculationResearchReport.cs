
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    public sealed class CirculationDemoResearchObservation
    {
        public string ProductId { get; }
        public string InitialHolderNodeId { get; }
        public bool IsFieldBorn { get; }

        public int BirthDay { get; }
        public int CapitalAtBirth { get; }
        public int OutgoingArcsAtBirth { get; }

        public int EverHeldCount { get; }
        public int OutsideOriginComponentEverHeld { get; }
        public int InitialComponentsReached { get; }
        public int PassiveExposureCount { get; }

        public int PhysicalShipments { get; }
        public int DeliveredPhysicalShipments { get; }
        public int CopyServiceShipments { get; }
        public int ViaEstablishedFirstContactArcs { get; }

        internal CirculationDemoResearchObservation(
            DemoOriginAtBirth origin,
            int everHeldCount,
            int outsideOriginComponentEverHeld,
            int initialComponentsReached,
            int passiveExposureCount,
            IReadOnlyList<ResearchShipmentObservation> shipments)
        {
            ProductId = origin.ProductId;
            InitialHolderNodeId = origin.InitialHolderNodeId;
            IsFieldBorn = origin.IsFieldBorn;
            BirthDay = origin.BirthDay;
            CapitalAtBirth = origin.CapitalAtBirth;
            OutgoingArcsAtBirth = origin.OutgoingArcsAtBirth;

            EverHeldCount = everHeldCount;
            OutsideOriginComponentEverHeld =
                outsideOriginComponentEverHeld;
            InitialComponentsReached =
                initialComponentsReached;
            PassiveExposureCount = passiveExposureCount;

            PhysicalShipments = shipments.Count;
            DeliveredPhysicalShipments =
                shipments.Count(s =>
                    s.Status == PostalShipmentStatus.Delivered);
            CopyServiceShipments =
                shipments.Count(s => s.IsCopyDistribution);
            ViaEstablishedFirstContactArcs =
                shipments.Count(s =>
                    s.ViaEstablishedFirstContactArc);
        }

        public override string ToString()
        {
            return
                ProductId +
                " BornDay=" + BirthDay +
                " FieldBorn=" + IsFieldBorn +
                " Origin=" + InitialHolderNodeId +
                " CapitalAtBirth=" + CapitalAtBirth +
                " ArcsAtBirth=" + OutgoingArcsAtBirth +
                " EverHeld=" + EverHeldCount +
                " OutsideCircle=" +
                    OutsideOriginComponentEverHeld +
                " Components=" + InitialComponentsReached +
                " PassiveExposure=" + PassiveExposureCount +
                " PhysicalShipments=" + PhysicalShipments +
                " Delivered=" + DeliveredPhysicalShipments +
                " CopyService=" + CopyServiceShipments +
                " ViaFirstContactArc=" +
                    ViaEstablishedFirstContactArcs;
        }
    }

    /// <summary>
    /// Immutable observation at one simulation day.
    ///
    /// No random draws, clock advancement, postal sends
    /// or capital modifications occur during capture.
    /// </summary>
    public sealed class CirculationResearchReport
    {
        public int Day { get; }
        public int NodeCount { get; }

        public IReadOnlyList<CirculationDemoResearchObservation>
            Demos { get; }

        public IReadOnlyList<ResearchShipmentObservation>
            Shipments { get; }

        public int DemoCount => Demos.Count;

        public int InitialDemoCount =>
            Demos.Count(d => !d.IsFieldBorn);

        public int FieldBornDemoCount =>
            Demos.Count(d => d.IsFieldBorn);

        public int MaximumDemoReach =>
            Demos.Count == 0
                ? 0
                : Demos.Max(d => d.EverHeldCount);

        public int DemosBeyondOriginalCircle =>
            Demos.Count(d =>
                d.OutsideOriginComponentEverHeld > 0);

        public int FieldDemosBeyondOriginalCircle =>
            Demos.Count(d =>
                d.IsFieldBorn &&
                d.OutsideOriginComponentEverHeld > 0);

        public int TotalPhysicalShipments =>
            Shipments.Count;

        public int DeliveredPhysicalShipments =>
            Shipments.Count(s =>
                s.Status == PostalShipmentStatus.Delivered);

        public int ShipmentsViaEstablishedFirstContactArcs =>
            Shipments.Count(s =>
                s.ViaEstablishedFirstContactArc);

        private CirculationResearchReport(
            int day,
            int nodeCount,
            IReadOnlyList<CirculationDemoResearchObservation> demos,
            IReadOnlyList<ResearchShipmentObservation> shipments)
        {
            Day = day;
            NodeCount = nodeCount;
            Demos = demos;
            Shipments = shipments;
        }

        public static CirculationResearchReport Capture(
            DemoSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.InitialWorld == null ||
                session.ResearchJournal == null)
            {
                throw new InvalidOperationException(
                    "A configured session with research journal " +
                    "is required.");
            }

            var scenario = session.Scenario;
            var initial = session.InitialWorld;
            var journal = session.ResearchJournal;

            // Every initially isolated person has their own
            // component. Friend-circle members share one.
            var components = new Dictionary<string, string>(
                StringComparer.Ordinal);

            foreach (string id in scenario.NodeIds)
                components.Add(id, id);

            foreach (var circle in initial.FriendCircles)
            {
                if (circle.Count == 0)
                    continue;

                string componentId = circle[0];

                foreach (string id in circle)
                    components[id] = componentId;
            }

            var shipments = journal.CaptureShipments();

            var shipmentsByProduct = shipments
                .GroupBy(s => s.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<ResearchShipmentObservation>)
                        g.ToList().AsReadOnly(),
                    StringComparer.Ordinal);

            var rows =
                new List<CirculationDemoResearchObservation>();

            foreach (string productId in scenario.ProductIds)
            {
                DemoOriginAtBirth origin;

                if (!journal.TryGetInitialOrigin(
                        productId, out origin) &&
                    !scenario.TryGetRuntimeOrigin(
                        productId, out origin))
                {
                    throw new InvalidOperationException(
                        "No origin recorded for demo: " +
                        productId);
                }

                string originalComponent =
                    components[origin.InitialHolderNodeId];

                var reachedComponents = new HashSet<string>(
                    StringComparer.Ordinal);

                int everHeld = 0;
                int outside = 0;
                int passive = 0;

                var wave = scenario.GetFieldWave(productId);

                foreach (string nodeId in scenario.NodeIds)
                {
                    if (scenario.Inventory.HasEverHeldProduct(
                            nodeId, productId))
                    {
                        everHeld++;

                        string component = components[nodeId];
                        reachedComponents.Add(component);

                        if (component != originalComponent)
                            outside++;
                    }

                    // Passive field exposure is awareness,
                    // not proof of physical cassette ownership.
                    if (wave.HasPassiveExposure(nodeId))
                        passive++;
                }

                if (!shipmentsByProduct.TryGetValue(
                        productId, out var productShipments))
                {
                    productShipments =
                        Array.Empty<ResearchShipmentObservation>();
                }

                rows.Add(
                    new CirculationDemoResearchObservation(
                        origin,
                        everHeld,
                        outside,
                        reachedComponents.Count,
                        passive,
                        productShipments));
            }

            return new CirculationResearchReport(
                session.Day,
                session.NodeCount,
                rows.AsReadOnly(),
                shipments);
        }

        public override string ToString()
        {
            return
                "Day=" + Day +
                " Nodes=" + NodeCount +
                " Demos=" + DemoCount +
                " Initial=" + InitialDemoCount +
                " FieldBorn=" + FieldBornDemoCount +
                " BeyondCircle=" + DemosBeyondOriginalCircle +
                " FieldBeyondCircle=" +
                    FieldDemosBeyondOriginalCircle +
                " MaxReach=" + MaximumDemoReach +
                " PhysicalShipments=" + TotalPhysicalShipments +
                " Delivered=" + DeliveredPhysicalShipments +
                " ViaFirstContactArc=" +
                    ShipmentsViaEstablishedFirstContactArcs;
        }
    }
}
