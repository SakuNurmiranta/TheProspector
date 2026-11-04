
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// Deterministinen kolmen solmun testiskenaario.
    ///
    /// A -> B: ensimmäinen kasettikopio
    /// B -> C: uusi kasettikopio
    /// B -> A: vastaus alkuperäiseen viestiin
    ///
    /// Ei vielä itsenäistä solmujen päätöksentekoa.
    /// </summary>
    public sealed class ThreeNodeScenario
    {
        private readonly List<string> _trace =
            new List<string>();

        private readonly TimedPostalExchange _exchange;

        private readonly NetworkConnection _ab;
        private readonly NetworkConnection _ba;
        private readonly NetworkConnection _bc;

        private readonly CulturalProduct _demo;

        public Network Network { get; }
        public SimulationClock Clock { get; }
        public CassetteInventory Inventory { get; }
        public RecipientBacklog Backlog { get; }
        public KnownCollectionRegistry Known { get; }
        public CulturalCapitalLedger Capital { get; }

        public CassetteCopy Original { get; }
        public CassetteCopy ABCopy { get; private set; }
        public CassetteCopy BCCopy { get; private set; }

        public PostalShipment ShipAB { get; private set; }
        public PostalShipment ShipBA { get; private set; }
        public PostalShipment ShipBC { get; private set; }

        public int AOpeningReward { get; private set; }

        public IReadOnlyList<string> Trace =>
            _trace.AsReadOnly();

        public ThreeNodeScenario()
        {
            Network = new Network();

            Network.AddNode(new Node("A"));
            Network.AddNode(new Node("B"));
            Network.AddNode(new Node("C"));

            // Kaikki kolme toimijaa ovat Suomessa.
            Network.AddAddress("ADDR-A", "FI");
            Network.AddAddress("ADDR-B", "FI");
            Network.AddAddress("ADDR-C", "FI");

            Network.PlaceNode("A", "ADDR-A");
            Network.PlaceNode("B", "ADDR-B");
            Network.PlaceNode("C", "ADDR-C");

            // Osoitetietämys on rajallinen.
            Network.LearnAddress("A", "B", "ADDR-B");
            Network.LearnAddress("B", "A", "ADDR-A");
            Network.LearnAddress("B", "C", "ADDR-C");

            Network.AddConnection(
                "CONN-AB",
                "A", "B",
                CommunicationLayer.Interpersonal,
                "personal");

            Network.AddConnection(
                "CONN-BA",
                "B", "A",
                CommunicationLayer.Interpersonal,
                "personal");

            Network.AddConnection(
                "CONN-BC",
                "B", "C",
                CommunicationLayer.Interpersonal,
                "personal");

            _ab = GetConnection("A", "CONN-AB");
            _ba = GetConnection("B", "CONN-BA");
            _bc = GetConnection("B", "CONN-BC");

            // Artisti on kentässä, ei eksplisiittinen
            // verkostosolmu.
            var field = new Field("F1", Network);
            field.RegisterLocalNode("A");

            field.SpawnArtist(
                "ART1",
                new Dictionary<string, int>
                {
                    ["A"] = 10
                },
                new Random(123));

            _demo = field.CreateDemo(
                "D1", "ART1", true);

            Inventory = new CassetteInventory(Network);

            Original = Inventory.RegisterExistingCopy(
                "TAPE-ORIGINAL",
                _demo,
                "A");

            Backlog = new RecipientBacklog();
            Known = new KnownCollectionRegistry(Network);

            Capital = new CulturalCapitalLedger(Network);
            Capital.InitializeNode("A", 10);
            Capital.InitializeNode("B", 10);
            Capital.InitializeNode("C", 10);

            Clock = new SimulationClock();

            var reception =
                new MessageReceptionRegistry(Known);

            var copyRewards =
                new CopyDeliveryRewardService(
                    Network,
                    Inventory,
                    Capital);

            var reciprocalRewards =
                new ReciprocalExchangeRewardService(
                    reception,
                    Capital);

            var windows = new ReplyWindowRegistry();

            _exchange = new TimedPostalExchange(
                Clock,
                Network,
                Inventory,
                Backlog,
                windows,
                copyRewards,
                reciprocalRewards);

            PrepareInitialShipment();
        }

        private NetworkConnection GetConnection(
            string sourceNodeId,
            string connectionId)
        {
            return Network.GetOutgoingConnections(
                    sourceNodeId,
                    CommunicationLayer.Interpersonal)
                .Single(c => c.Id == connectionId);
        }

        private void PrepareInitialShipment()
        {
            // A pitää alkuperäisen kasetin itsellään
            // ja valmistaa B:lle oman kopion.
            ABCopy = Inventory.RecordCopy(
                Original.Id,
                "TAPE-AB",
                "A",
                "A");

            var package = new MessagePackage(
                "MSG-AB",
                _ab,
                cassetteCopy: ABCopy);

            ShipAB = _exchange.Send(
                "SHIP-AB",
                package,
                "FI",
                "FI",
                5);

            Log("A sends D1 to B");

            // Päivän 2 tapahtumat suoritetaan
            // postin saapumisen jälkeen.
            //
            // SimulationClock säilyttää saman
            // päivän rekisteröintijärjestyksen.
            Clock.ScheduleAfter(
                "SCENARIO-DAY-2",
                2,
                ProcessDayTwo);
        }

        private void ProcessDayTwo()
        {
            // B avaa A:n lähettämän kirjeen.
            OpenRequired("B");

            Log("B opens A's letter");

            // B tekee oman kopion C:lle.
            BCCopy = Inventory.RecordCopy(
                ABCopy.Id,
                "TAPE-BC",
                "B",
                "B");

            var forwardPackage = new MessagePackage(
                "MSG-BC",
                _bc,
                cassetteCopy: BCCopy);

            ShipBC = _exchange.Send(
                "SHIP-BC",
                forwardPackage,
                "FI",
                "FI",
                5);

            Log("B copies D1 to C");

            // B vastaa A:n alkuperäiseen
            // viestiin ilman kasettikopiota.
            var replyPackage = new MessagePackage(
                "MSG-BA",
                _ba,
                feedbackProduct: _demo,
                thumb: +1,
                replyToPackageId: "MSG-AB");

            ShipBA = _exchange.Send(
                "SHIP-BA",
                replyPackage,
                "FI",
                "FI",
                5);

            Log("B replies to A");

            // Päivänä 4 sekä C:n kasetti että
            // A:n vastauskirje ovat saapuneet.
            Clock.ScheduleAfter(
                "SCENARIO-DAY-4",
                2,
                ProcessDayFour);
        }

        private void ProcessDayFour()
        {
            // C avaa B:n lähettämän kasetin.
            OpenRequired("C");

            Log("C opens B's letter");

            // A avaa B:n vastauksen.
            // Tästä syntyy +1 molemmille.
            AOpeningReward = OpenRequired("A");

            Log("A opens B's reply");
        }

        private int OpenRequired(string recipientNodeId)
        {
            if (!_exchange.TryOpenNext(
                    recipientNodeId,
                    out int reward))
            {
                throw new InvalidOperationException(
                    "Expected message missing from backlog: " +
                    recipientNodeId);
            }

            return reward;
        }

        private void Log(string description)
        {
            _trace.Add(
                Clock.CurrentDay + ":" + description);
        }

        public void Run()
        {
            // Suorittaa koko neljän päivän tapahtumaketjun.
            //
            // Uusi kutsu ei toista jo suoritettuja
            // tapahtumia.
            Clock.AdvanceTo(4);
        }
    }
}
