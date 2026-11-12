
using System;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// Runnable Domain session.
    /// Aesthetics, zine circulation and field demo
    /// production are optional subsystems.
    /// </summary>
    public sealed class DemoSession
    {
        public ConfigurableCirculationScenario Scenario { get; }

        public CirculationInitialWorld InitialWorld { get; }

        public CirculationResearchJournal ResearchJournal { get; }
        
        public InitialZineCirculation InitialZine { get; }

        public ZinePostalInsertCirculation ZinePostalInserts { get; }

        public SixAxisProfileCatalog AestheticProfiles { get; }

        public ZineSixAxisContactEligibility SixAxisContacts { get; }

        public ZineFirstContactPostalService FirstContactPostal { get; }

        public ZineAutonomousSixAxisExchangeCycle FirstContactCycle
        {
            get;
        }

        public FieldDemoProductionCycle FieldDemoProduction
        {
            get;
        }

        private DemoSession(
            ConfigurableCirculationScenario scenario,
            CirculationInitialWorld initialWorld = null,
            InitialZineCirculation initialZine = null,
            ZinePostalInsertCirculation zinePostalInserts = null,
            SixAxisProfileCatalog aestheticProfiles = null,
            ZineSixAxisContactEligibility sixAxisContacts = null,
            ZineFirstContactPostalService firstContactPostal = null,
            ZineAutonomousSixAxisExchangeCycle firstContactCycle = null,
            FieldDemoProductionCycle fieldDemoProduction = null,
            CirculationResearchJournal researchJournal = null)
        {
            Scenario = scenario ??
                throw new ArgumentNullException(nameof(scenario));

            InitialWorld = initialWorld;
            InitialZine = initialZine;
            ZinePostalInserts = zinePostalInserts;
            AestheticProfiles = aestheticProfiles;
            SixAxisContacts = sixAxisContacts;
            FirstContactPostal = firstContactPostal;
            FirstContactCycle = firstContactCycle;
            FieldDemoProduction = fieldDemoProduction;
            ResearchJournal = researchJournal;
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
            int listeningDays = 2,
            int fieldDemoIntervalDays = 14,
            bool enableInitialZineForwarding = false)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (listeningDays < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(listeningDays));

            if (fieldDemoIntervalDays < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(fieldDemoIntervalDays));

            var initialWorld =
                CirculationInitialWorldFactory.Create(parameters);

            var scenario =
                new ConfigurableCirculationScenario(
                    initialWorld.Definition,
                    listeningDays: listeningDays,
                    fieldDiscoveryRandom:
                        new Random(parameters.RandomSeed));
            
            scenario.ActivateLocalListening();

            var initialZine =
                new InitialZineCirculation(
                    initialWorld,
                    scenario,
                    enableLocalForwarding:
                    enableInitialZineForwarding);

            var postalInserts =
                new ZinePostalInsertCirculation(
                    initialZine.Issue,
                    initialZine.Bulk,
                    initialZine.Knowledge,
                    scenario.Exchange,
                    scenario.World);

            var aesthetics =
                SixAxisProfileCatalog.CreateInitialized(
                    scenario.NodeIds,
                    scenario.ProductIds,
                    initialWorld.EditorNodeId,
                    initialWorld.ZineListedNodeIds,
                    initialZine.AdvertisedDemoIds,
                    initialZine.Issue.ProductId,
                    parameters.RandomSeed);
            
            scenario.EnableSixAxisListening(aesthetics);

            var contacts =
                new ZineSixAxisContactEligibility(
                    scenario.Network,
                    initialZine.Knowledge,
                    initialZine.Issue,
                    aesthetics,
                    minimumMatchingAxes: 4);

            var legacyPolicy = new ZineContactOutreachPolicy(
                scenario.Network,
                initialZine.Knowledge);

            var firstPostal = new ZineFirstContactPostalService(
                scenario.Network,
                scenario.Clock,
                legacyPolicy,
                scenario.Exchange,
                (sender, target) =>
                    contacts.GetCandidates(sender)
                        .Any(c => c.TargetNodeId == target));

            var firstCycle =
                new ZineAutonomousSixAxisExchangeCycle(
                    scenario,
                    contacts,
                    aesthetics,
                    firstPostal,
                    new Random(
                        unchecked(
                            parameters.RandomSeed ^ 0x62A8B1)));

            FieldDemoProductionCycle production = null;

            if (fieldDemoIntervalDays > 0)
            {
                production = new FieldDemoProductionCycle(
                    scenario,
                    aesthetics,
                    fieldDemoIntervalDays,
                    parameters.RandomSeed);
            }

            
            var researchJournal =
                new CirculationResearchJournal(
                    scenario,
                    initialWorld);

            
            return new DemoSession(
                scenario,
                initialWorld,
                initialZine,
                postalInserts,
                aesthetics,
                contacts,
                firstPostal,
                firstCycle,
                production,
                researchJournal);
        }

        public int Day =>
            Scenario.World.CurrentDay;

        public int NodeCount =>
            Scenario.Network.NodeCount;

        public int AddressCount =>
            Scenario.Network.AddressCount;

        public int ProductCount =>
            Scenario.ProductIds.Count;

        public string PrimaryProductId =>
            Scenario.ProductIds[0];

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

        public ListeningPhase? GetListeningPhase(
            string nodeId)
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
