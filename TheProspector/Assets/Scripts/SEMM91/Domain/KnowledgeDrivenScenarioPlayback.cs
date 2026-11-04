using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class KnowledgeDrivenSnapshot
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

        public int FirstWaveCount { get; }
        public int SecondWaveCount { get; }

        public int AOpeningReward { get; }

        // Kunkin solmun oma käsitys toisen
        // solmun D1-vastaanottohistoriasta.
        //
        // true  = vastaanottanut D1:n
        // false = tunnetusti ei vastaanottanut
        // null  = tieto puuttuu
        public bool? BThinksAHasD1 { get; }
        public bool? BThinksCHasD1 { get; }
        public bool? CThinksBHasD1 { get; }
        public bool? AThinksBHasD1 { get; }

        // Todellinen vastaanottohistoria,
        // erillään muiden solmujen tietämyksestä.
        public bool CHasEverHeldD1 { get; }

        // B:n tieto C:n viestimästä kielteisestä kannasta.
        public bool BKnowsCDislikesD1 { get; }

// C:n B:lle lähettämän kirjeen tila.
        public PostalShipmentStatus? ShipCBStatus { get; }

        public IReadOnlyList<string> Trace { get; }

        internal KnowledgeDrivenSnapshot(
            KnowledgeDrivenScenario scenario)
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

            ShipBAStatus = scenario.ShipBA?.Status;
            ShipBCStatus = scenario.ShipBC?.Status;

            BacklogA =
                scenario.Backlog.GetPendingCount("A");

            BacklogB =
                scenario.Backlog.GetPendingCount("B");

            BacklogC =
                scenario.Backlog.GetPendingCount("C");

            FirstWaveCount = scenario.FirstWave.Count;
            SecondWaveCount = scenario.SecondWave.Count;

            AOpeningReward = scenario.AOpeningReward;

            BThinksAHasD1 =
                scenario.Known.IsKnownToHold(
                    "B", "A", "D1");

            BThinksCHasD1 =
                scenario.Known.IsKnownToHold(
                    "B", "C", "D1");

            CThinksBHasD1 =
                scenario.Known.IsKnownToHold(
                    "C", "B", "D1");

            AThinksBHasD1 =
                scenario.Known.IsKnownToHold(
                    "A", "B", "D1");

            CHasEverHeldD1 =
                scenario.Inventory.HasEverHeldProduct(
                    "C", "D1");

            BKnowsCDislikesD1 =
                scenario.CommunicatedStances
                    .HasCommunicatedDislike("B", "C", "D1");

            ShipCBStatus = scenario.ShipCB?.Status;

            Trace = new List<string>(
                scenario.Trace).AsReadOnly();
        }
    }

    public sealed class KnowledgeDrivenScenarioPlayback
    {
        public const int FinalDay = 4;

        private readonly KnowledgeDrivenScenario _scenario;

        private readonly List<KnowledgeDrivenSnapshot>
            _history = new();

        private readonly IReadOnlyList<KnowledgeDrivenSnapshot>
            _historyView;

        public bool BKnowsCIsMissing { get; }
        public bool CCommunicatedDislike { get; }

        public KnowledgeDrivenScenarioPlayback(
            bool bKnowsCIsMissing = true,
            bool cCommunicatedDislike = false)
        {
            BKnowsCIsMissing = bKnowsCIsMissing;
            CCommunicatedDislike = cCommunicatedDislike;

            _scenario = new KnowledgeDrivenScenario(
                bKnowsCIsMissing,
                cCommunicatedDislike);

            _historyView = _history.AsReadOnly();

            RecordSnapshot();
        }

        public IReadOnlyList<KnowledgeDrivenSnapshot>
            History => _historyView;

        public KnowledgeDrivenSnapshot Current =>
            _history[_history.Count - 1];

        public bool IsComplete =>
            _scenario.Clock.CurrentDay >= FinalDay;

        public bool Step()
        {
            if (IsComplete)
                return false;

            _scenario.Clock.AdvanceDays(1);

            RecordSnapshot();

            return true;
        }

        public void RunToEnd()
        {
            while (Step())
            {
                // Yksi havainto jokaiselta päivältä.
            }
        }

        private void RecordSnapshot()
        {
            _history.Add(
                new KnowledgeDrivenSnapshot(_scenario));
        }
    }
}