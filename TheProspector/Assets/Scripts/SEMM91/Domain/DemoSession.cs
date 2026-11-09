
using System;

namespace SEMM91.Domain
{
    /// <summary>
    /// Runnable Domain session for Unity.
    /// Supports the legacy demonstration and configured worlds.
    /// </summary>
    public sealed class DemoSession
    {
        public ConfigurableCirculationScenario Scenario { get; }

        public CirculationInitialWorld InitialWorld { get; }

        public InitialZineCirculation InitialZine { get; }

        public ZinePostalInsertCirculation ZinePostalInserts { get; }

        private DemoSession(
            ConfigurableCirculationScenario scenario,
            CirculationInitialWorld initialWorld = null,
            InitialZineCirculation initialZine = null,
            ZinePostalInsertCirculation zinePostalInserts = null)
        {
            Scenario = scenario ??
                throw new ArgumentNullException(nameof(scenario));

            InitialWorld = initialWorld;
            InitialZine = initialZine;
            ZinePostalInserts = zinePostalInserts;
        }

        public static DemoSession CreateDefault(
            Random random = null)
        {
            var definition = new CirculationWorldDefinition();

            definition.AddNode("A", "FI");
            definition.AddNode("B", "FI");
            definition.AddNode("C", "FI");
            definition.AddNode("D", "FI");

            definition.AddConnection("AB", "A", "B");
            definition.AddConnection("BC", "B", "C");

            definition.MarkKnownMissing("A", "B");
            definition.MarkKnownMissing("B", "C");

            definition.SetSeed("A", "D1");

            var scenario =
                new ConfigurableCirculationScenario(
                    definition,
                    listeningDays: 2,
                    fieldDiscoveryRandom: random);

            return new DemoSession(scenario);
        }

        public static DemoSession CreateConfigured(
            CirculationInitializationParameters parameters,
            int listeningDays = 2)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (listeningDays < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(listeningDays));

            var initialWorld =
                CirculationInitialWorldFactory.Create(parameters);

            var scenario =
                new ConfigurableCirculationScenario(
                    initialWorld.Definition,
                    listeningDays: listeningDays,
                    fieldDiscoveryRandom:
                        new Random(parameters.RandomSeed));

            var initialZine =
                new InitialZineCirculation(
                    initialWorld,
                    scenario);

            var postalInserts =
                new ZinePostalInsertCirculation(
                    initialZine.Issue,
                    initialZine.Bulk,
                    initialZine.Knowledge,
                    scenario.Exchange,
                    scenario.World);

            return new DemoSession(
                scenario,
                initialWorld,
                initialZine,
                postalInserts);
        }

        public int Day => Scenario.World.CurrentDay;

        public int NodeCount => Scenario.Network.NodeCount;

        public int AddressCount => Scenario.Network.AddressCount;

        public int ProductCount => Scenario.ProductIds.Count;

        public string PrimaryProductId => Scenario.ProductIds[0];

        public int OpenedMessageCount =>
            Scenario.World.OpenedMessages.Count;

        public SIRState FieldState =>
            Scenario.FieldSIR.GetFieldState(
                Scenario.Field,
                Scenario.GetDemo(PrimaryProductId));

        public double DeliveredReach =>
            Scenario.GetFieldWave(PrimaryProductId)
                .ObservedDeliveredReach;

        public int ActiveCopiers =>
            Scenario.GetFieldWave(PrimaryProductId)
                .ActiveCopierCount;

        // Legacy four-node indicators.

        public int BCopyShipments
        {
            get
            {
                if (!Scenario.Network.TryGetNode("B", out _))
                    return 0;

                return Scenario.GetPolicy("B")
                    .Dispatched.Count;
            }
        }

        public bool DHasDiscovered
        {
            get
            {
                if (!Scenario.Network.TryGetNode("D", out _))
                    return false;

                return Scenario.HasDiscoveredFromField(
                    "D",
                    PrimaryProductId);
            }
        }

        public ListeningPhase? GetListeningPhase(string nodeId)
        {
            return Scenario.GetListeningStatus(
                nodeId,
                PrimaryProductId)?.Phase;
        }

        public void AdvanceDay()
        {
            Scenario.World.AdvanceDay();
        }

        public void AdvanceDays(int days)
        {
            Scenario.World.AdvanceDays(days);
        }
    }
}
