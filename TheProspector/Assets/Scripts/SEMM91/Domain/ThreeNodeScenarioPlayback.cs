
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    // Yhden simulaatiopäivän muuttumaton tilannekuva.
    public sealed class ThreeNodeSnapshot
    {
        public int Day { get; }
        public int PendingEvents { get; }

        public int CapitalA { get; }
        public int CapitalB { get; }
        public int CapitalC { get; }

        public string OriginalHolder { get; }
        public string ABCopyHolder { get; }
        public string BCCopyHolder { get; }

        public PostalShipmentStatus ShipABStatus { get; }
        public PostalShipmentStatus? ShipBAStatus { get; }
        public PostalShipmentStatus? ShipBCStatus { get; }

        public int BacklogA { get; }
        public int BacklogB { get; }
        public int BacklogC { get; }

        public int AOpeningReward { get; }

        public IReadOnlyList<string> Trace { get; }

        internal ThreeNodeSnapshot(
            ThreeNodeScenario scenario)
        {
            if (scenario == null)
                throw new ArgumentNullException(
                    nameof(scenario));

            Day = scenario.Clock.CurrentDay;
            PendingEvents = scenario.Clock.PendingCount;

            CapitalA = scenario.Capital.GetCapital("A");
            CapitalB = scenario.Capital.GetCapital("B");
            CapitalC = scenario.Capital.GetCapital("C");

            OriginalHolder = scenario.Original.HolderNodeId;

            ABCopyHolder =
                scenario.ABCopy?.HolderNodeId;

            BCCopyHolder =
                scenario.BCCopy?.HolderNodeId;

            ShipABStatus = scenario.ShipAB.Status;

            ShipBAStatus =
                scenario.ShipBA?.Status;

            ShipBCStatus =
                scenario.ShipBC?.Status;

            BacklogA =
                scenario.Backlog.GetPendingCount("A");

            BacklogB =
                scenario.Backlog.GetPendingCount("B");

            BacklogC =
                scenario.Backlog.GetPendingCount("C");

            AOpeningReward = scenario.AOpeningReward;

            // Kopio tapahtumajäljestä.
            // Vanha tilannekuva ei muutu,
            // vaikka skenaarioon syntyy uusia rivejä.
            Trace = new List<string>(
                scenario.Trace).AsReadOnly();
        }
    }

    public sealed class ThreeNodeScenarioPlayback
    {
        public const int FinalDay = 4;

        private readonly ThreeNodeScenario _scenario;

        private readonly List<ThreeNodeSnapshot> _history =
            new List<ThreeNodeSnapshot>();

        private readonly IReadOnlyList<ThreeNodeSnapshot>
            _historyView;

        public ThreeNodeScenarioPlayback()
        {
            _scenario = new ThreeNodeScenario();

            _historyView = _history.AsReadOnly();

            // Alustamisen jälkeinen päivä 0
            // tallennetaan ensimmäiseksi havainnoksi.
            RecordSnapshot();
        }

        public IReadOnlyList<ThreeNodeSnapshot> History =>
            _historyView;

        public ThreeNodeSnapshot Current =>
            _history[_history.Count - 1];

        public bool IsComplete =>
            _scenario.Clock.CurrentDay >= FinalDay;

        public bool Step()
        {
            if (IsComplete)
                return false;

            // Täsmälleen yksi simulaatiopäivä.
            // Kello suorittaa kaikki kyseiseen
            // päivään mennessä ajastetut tapahtumat.
            _scenario.Clock.AdvanceDays(1);

            RecordSnapshot();

            return true;
        }

        public void RunToEnd()
        {
            while (Step())
            {
                // Step huolehtii etenemisestä
                // ja tilannekuvan tallentamisesta.
            }
        }

        private void RecordSnapshot()
        {
            _history.Add(
                new ThreeNodeSnapshot(_scenario));
        }
    }
}
