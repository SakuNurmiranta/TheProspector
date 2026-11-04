
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// Kolmen solmun skenaario, jossa
    /// kopioiden vastaanottajat valitaan
    /// solmun tietämyksen perusteella.
    ///
    /// Kielteinen kanta välittyy C:ltä B:lle
    /// todellisena henkilökohtaisena viestinä.
    ///
    /// Kopiointihalukkuus ja viestien avaaminen
    /// ovat edelleen skenaarion ohjaamia.
    /// </summary>
    public sealed class KnowledgeDrivenScenario
    {
        private readonly TimedPostalExchange _exchange;

        private readonly PositiveCopyDistributionService
            _distribution;

        private readonly CommunicatedStanceRegistry
            _stances;

        private readonly NetworkConnection _ab;
        private readonly NetworkConnection _ba;
        private readonly NetworkConnection _bc;
        private readonly NetworkConnection _cb;

        private readonly CulturalProduct _demo;

        private readonly bool _cCommunicatedDislike;

        private readonly List<string> _trace =
            new List<string>();
        
        
        private readonly int _cDislikeSentDay;


        public Network Network { get; }
        public SimulationClock Clock { get; }
        public CassetteInventory Inventory { get; }
        public KnownCollectionRegistry Known { get; }
        public RecipientBacklog Backlog { get; }
        public CulturalCapitalLedger Capital { get; }

        public CommunicatedStanceRegistry CommunicatedStances
            => _stances;

        public CassetteCopy Original { get; }
        public CassetteCopy ABCopy { get; private set; }
        public CassetteCopy BCCopy { get; private set; }

        public PostalShipment ShipAB { get; private set; }
        public PostalShipment ShipBA { get; private set; }
        public PostalShipment ShipBC { get; private set; }

        // C:n kielteisen viestin postilähetys.
        // Null, jos kielteistä kantaa ei viestitä.
        public PostalShipment ShipCB { get; private set; }

        public IReadOnlyList<CopyDistributionShipment>
            FirstWave { get; private set; }

        public IReadOnlyList<CopyDistributionShipment>
            SecondWave { get; private set; }

        public int AOpeningReward { get; private set; }

        public IReadOnlyList<string> Trace =>
            _trace.AsReadOnly();


        public KnowledgeDrivenScenario(
            bool bKnowsCIsMissing = true,
            bool cCommunicatedDislike = false,
            int cDislikeSentDay = 0)
        {
            if (cDislikeSentDay < 0 ||
                cDislikeSentDay > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cDislikeSentDay),
                    "This scenario supports days 0 and 1.");
            }

            _cCommunicatedDislike = cCommunicatedDislike;
            _cDislikeSentDay = cDislikeSentDay;

            // Säilytä tästä eteenpäin kaikki nykyinen
            // konstruktorin alustuskoodi ennallaan.


            Network = new Network();

            foreach (string id in new[] { "A", "B", "C" })
                Network.AddNode(new Node(id));

            Network.AddAddress("ADDR-A", "FI");
            Network.AddAddress("ADDR-B", "FI");
            Network.AddAddress("ADDR-C", "FI");

            Network.PlaceNode("A", "ADDR-A");
            Network.PlaceNode("B", "ADDR-B");
            Network.PlaceNode("C", "ADDR-C");

            Network.LearnAddress("A", "B", "ADDR-B");
            Network.LearnAddress("B", "A", "ADDR-A");
            Network.LearnAddress("B", "C", "ADDR-C");

            // C tarvitsee B:n osoitteen voidakseen
            // lähettää tälle oman kannanottonsa.
            Network.LearnAddress("C", "B", "ADDR-B");

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

            Network.AddConnection(
                "CONN-CB",
                "C", "B",
                CommunicationLayer.Interpersonal,
                "personal");

            _ab = GetConnection("A", "CONN-AB");
            _ba = GetConnection("B", "CONN-BA");
            _bc = GetConnection("B", "CONN-BC");
            _cb = GetConnection("C", "CONN-CB");

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

            Known = new KnownCollectionRegistry(Network);
            Backlog = new RecipientBacklog();
            Clock = new SimulationClock();

            Capital = new CulturalCapitalLedger(Network);

            foreach (string id in new[] { "A", "B", "C" })
                Capital.InitializeNode(id, 10);

            // A tietää, ettei B ole vastaanottanut D1:tä.
            Known.RecordSnapshot(
                "A", "B", Array.Empty<string>());

            // B:n tieto C:n kokoelmasta voidaan
            // jättää pois vaihtoehtoisessa skenaariossa.
            if (bKnowsCIsMissing)
            {
                Known.RecordSnapshot(
                    "B", "C", Array.Empty<string>());
            }

            var reception =
                new MessageReceptionRegistry(Known);

            var copyRewards =
                new CopyDeliveryRewardService(
                    Network, Inventory, Capital);

            var reciprocalRewards =
                new ReciprocalExchangeRewardService(
                    reception, Capital);

            // Yhteinen rekisteri kaikille avattujen
            // viestien välittämille kannanotoille.
            _stances = new CommunicatedStanceRegistry();

            _exchange = new TimedPostalExchange(
                Clock,
                Network,
                Inventory,
                Backlog,
                new ReplyWindowRegistry(),
                copyRewards,
                reciprocalRewards,
                _stances);

            _distribution =
                new PositiveCopyDistributionService(
                    Inventory,
                    Known,
                    _exchange);

            FirstWave =
                Array.Empty<CopyDistributionShipment>();

            SecondWave =
                Array.Empty<CopyDistributionShipment>();

            StartFirstWave();
        }

        private NetworkConnection GetConnection(
            string senderId,
            string connectionId)
        {
            return Network.GetOutgoingConnections(
                    senderId,
                    CommunicationLayer.Interpersonal)
                .Single(c => c.Id == connectionId);
        }

        private void StartFirstWave()
        {
            // A:n myönteinen kopiointipäätös
            // on skenaarion lähtöehto.
            var routes = new[]
            {
                new CopyDistributionRoute(
                    "B", _ab, "FI")
            };

            FirstWave = _distribution.DispatchAll(
                "BATCH-A-DAY0",
                "A",
                Original,
                routes,
                "FI",
                5,
                _ => false,
                _ => true);

            if (FirstWave.Count != 1)
                throw new InvalidOperationException(
                    "First wave must reach B.");

            ABCopy = FirstWave[0].Copy;
            ShipAB = FirstWave[0].Shipment;

            Log("A sends D1 to B");


            if (_cCommunicatedDislike)
            {
                if (_cDislikeSentDay == 0)
                {
                    SendCRejection();
                }
                else
                {
                    Clock.ScheduleAfter(
                        "AUTO-SEND-C-DISLIKE",
                        1,
                        SendCRejection);
                }
            }


            // Molempien lähetysten saapumistapahtumat
            // rekisteröitiin ennen tätä käsittelyä.
            //
            // Samana päivänä järjestys on siis:
            // A:n viesti, C:n mahdollinen viesti,
            // sitten B:n avaamis- ja levityspäätös.
            Clock.ScheduleAfter(
                "AUTO-PROCESS-DAY2",
                2,
                ProcessDayTwo);
        }

        private void SendCRejection()
        {
            // C:n kielteinen julkinen kanta voi
            // edeltää fyysisen kasetin saamista.
            //
            // Viesti sisältää eksplisiittisen
            // kielteisen palautteen D1:stä.
            var rejection = new MessagePackage(
                "MSG-CB-DISLIKE",
                _cb,
                feedbackProduct: _demo,
                thumb: -1);

            ShipCB = _exchange.Send(
                "SHIP-CB-DISLIKE",
                rejection,
                "FI",
                "FI",
                5);

            Log("C sends dislike of D1 to B");
            
            if (_cDislikeSentDay == 1)
            {
                // Lähetys lähtee päivänä 1.
                // Kotimaan posti saapuu päivänä 3.
                //
                // Saapumistapahtuma rekisteröitiin
                // ennen tätä avaamistapahtumaa.
                Clock.ScheduleAfter(
                    "AUTO-OPEN-C-DISLIKE",
                    2,
                    ProcessLateCRejection);
            }

            
        }

        private void ProcessDayTwo()
        {
            // Ensimmäisenä B avaa A:n viestin.
            if (!_exchange.TryOpenNext(
                    "B", out _))
            {
                throw new InvalidOperationException(
                    "B has no letter from A.");
            }

            Log("B opens A's letter");

            if (ShipCB != null &&
                ShipCB.Status == PostalShipmentStatus.Delivered)
            {
                // C:n kannanotto on nyt perillä.
                // B ei tiedä sen sisällöstä ennen
                // kuin avaa viestin.
                if (!_exchange.TryOpenNext(
                        "B", out _))
                {
                    throw new InvalidOperationException(
                        "B has no letter from C.");
                }

                Log("B opens C's rejection");
            }

            // B:llä on edelleen kaksi kontaktia,
            // A ja C. Kumpaakaan vastaanottajaa
            // ei ole lukittu valmiiksi valintaan.
            var routes = new[]
            {
                new CopyDistributionRoute(
                    "A", _ba, "FI"),

                new CopyDistributionRoute(
                    "C", _bc, "FI")
            };

            // UUSI 5.10:
            //
            // Ei enää alustettua bool-tarkistusta.
            // Kielteinen kanta tulee rekisteristä,
            // jota avatut viestit ovat päivittäneet.
            SecondWave = _distribution.DispatchAll(
                "BATCH-B-DAY2",
                "B",
                ABCopy,
                routes,
                "FI",
                5,
                recipient =>
                    _stances.HasCommunicatedDislike(
                        "B",
                        recipient,
                        "D1"),
                _ => true);

            foreach (var item in SecondWave)
            {
                if (item.RecipientNodeId != "C")
                {
                    throw new InvalidOperationException(
                        "Unexpected recipient: " +
                        item.RecipientNodeId);
                }

                BCCopy = item.Copy;
                ShipBC = item.Shipment;

                Log("B copies D1 to C");
            }

            // B:n vastaaminen A:lle on edelleen
            // skenaarioon määritelty tapahtuma.
            var reply = new MessagePackage(
                "MSG-BA-REPLY",
                _ba,
                feedbackProduct: _demo,
                thumb: +1,
                replyToPackageId: ShipAB.Package.Id);

            ShipBA = _exchange.Send(
                "SHIP-BA-REPLY",
                reply,
                "FI",
                "FI",
                5);

            Log("B replies to A");

            Clock.ScheduleAfter(
                "AUTO-PROCESS-DAY4",
                2,
                ProcessDayFour);
        }

        
        private void ProcessLateCRejection()
        {
            if (ShipCB == null ||
                ShipCB.Status != PostalShipmentStatus.Delivered)
            {
                throw new InvalidOperationException(
                    "C's rejection has not been delivered.");
            }

            if (!_exchange.TryOpenNext("B", out _))
            {
                throw new InvalidOperationException(
                    "B has no rejection letter to open.");
            }

            // B:n tieto päivittyy vasta tässä.
            // Päivänä 2 lähetettyä kopiota
            // ei käsitellä uudelleen.
            Log("B opens C's rejection");
        }

        
        private void ProcessDayFour()
        {
            if (ShipBC != null)
            {
                if (!_exchange.TryOpenNext(
                        "C", out _))
                {
                    throw new InvalidOperationException(
                        "C has no letter to open.");
                }

                Log("C opens B's letter");
            }

            if (!_exchange.TryOpenNext(
                    "A", out int reward))
            {
                throw new InvalidOperationException(
                    "A has no reply to open.");
            }

            AOpeningReward = reward;

            Log("A opens B's reply");
        }

        private void Log(string description)
        {
            _trace.Add(
                Clock.CurrentDay + ":" + description);
        }

        public void Run()
        {
            Clock.AdvanceTo(4);
        }
    }
}
