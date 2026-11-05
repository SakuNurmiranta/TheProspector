
using System;

namespace SEMM91.Domain
{
    /// <summary>
    /// Ensimmäisen Unity-demon ajettava istunto.
    /// Ei sisällä esityskerrokseen sidottua koodia.
    /// </summary>
    public sealed class DemoSession
    {
        public ConfigurableCirculationScenario Scenario
        {
            get;
        }

        private DemoSession(
            ConfigurableCirculationScenario scenario)
        {
            Scenario = scenario ??
                throw new ArgumentNullException(
                    nameof(scenario));
        }

        public static DemoSession CreateDefault(
            Random random = null)
        {
            var definition =
                new CirculationWorldDefinition();

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

        public int Day => Scenario.World.CurrentDay;

        public SIRState FieldState =>
            Scenario.FieldSIR.GetFieldState(
                Scenario.Field,
                Scenario.Demo);

        public double DeliveredReach =>
            Scenario.GetFieldWave("D1")
                .ObservedDeliveredReach;

        public int ActiveCopiers =>
            Scenario.GetFieldWave("D1")
                .ActiveCopierCount;

        public int OpenedMessageCount =>
            Scenario.World.OpenedMessages.Count;

        public int BCopyShipments =>
            Scenario.GetPolicy("B")
                .Dispatched.Count;

        public bool DHasDiscovered =>
            Scenario.HasDiscoveredFromField(
                "D", "D1");

        public ListeningPhase? GetListeningPhase(
            string nodeId)
        {
            return Scenario.GetListeningStatus(
                nodeId, "D1")?.Phase;
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
