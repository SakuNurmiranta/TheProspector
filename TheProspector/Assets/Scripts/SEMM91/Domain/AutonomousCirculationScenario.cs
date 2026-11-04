
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// Yhdistää postilähetyksen fyysiseen kasettiin.
    /// Kaikki uudet kopiot rekisteröidään
    /// lähettämisen yhteydessä.
    /// </summary>
    public sealed class CopyShipmentIndex
    {
        private readonly Dictionary<string, CassetteCopy>
            _copies = new Dictionary<string, CassetteCopy>(
                StringComparer.Ordinal);

        public int Count => _copies.Count;

        public void Register(CopyDistributionShipment item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!_copies.TryAdd(item.Shipment.Id, item.Copy))
            {
                throw new InvalidOperationException(
                    "Shipment was already indexed: " +
                    item.Shipment.Id);
            }
        }

        
        public void RegisterTransfer(
            PostalShipment shipment,
            CassetteCopy copy)
        {
            if (shipment == null)
                throw new ArgumentNullException(
                    nameof(shipment));

            if (copy == null)
                throw new ArgumentNullException(
                    nameof(copy));

            if (_copies.ContainsKey(shipment.Id))
                throw new InvalidOperationException(
                    "Shipment was already indexed: " +
                    shipment.Id);

            // Uusi postilähetys, mutta ei uusi kasetti.
            _copies.Add(shipment.Id, copy);
        }

        
        public CassetteCopy Resolve(BacklogItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return _copies.TryGetValue(
                item.ShipmentId,
                out var copy)
                ? copy
                : null;
        }
    }

    /// <summary>
    /// Neljän solmun autonominen kasettilevityksen
    /// integraatio.
    ///
    /// Yksi alkulähetys alustetaan kerran.
    /// Sen jälkeen solmut toimivat
    /// SimulationWorldin kautta.
    /// </summary>
    public sealed class AutonomousCirculationScenario
    {
        private readonly Dictionary<
            string, PositiveCopyNodePolicy> _policies =
                new Dictionary<string, PositiveCopyNodePolicy>(
                    StringComparer.Ordinal);
        
        
        private readonly Dictionary<string, TimedListeningNodePolicy>
            _listeningPolicies =
                new Dictionary<string, TimedListeningNodePolicy>(
                    StringComparer.Ordinal);

        
        private readonly Dictionary<string, ListeningReactionNodePolicy>
            _reactionPolicies =
                new Dictionary<string, ListeningReactionNodePolicy>(
                    StringComparer.Ordinal);

        

        private readonly Dictionary<
            string, List<CopyDistributionRoute>> _routes =
                new Dictionary<
                    string, List<CopyDistributionRoute>>(
                        StringComparer.Ordinal);

        public Network Network { get; }
        public SimulationClock Clock { get; }
        public SimulationWorld World { get; }

        public PhysicalControlService Control { get; private set; }
        public CassetteInventory Inventory { get; }
        public KnownCollectionRegistry Known { get; }
        public RecipientBacklog Backlog { get; }
        public CulturalCapitalLedger Capital { get; }

        public CommunicatedStanceRegistry Stances { get; }
        public TimedPostalExchange Exchange { get; }

        public CopyShipmentIndex CopyIndex { get; }

        public CulturalProduct Demo { get; }
        public CassetteCopy Original { get; }

        public IReadOnlyList<CopyDistributionShipment>
            SeedWave { get; }

        public int ListeningDays { get; }
        
       
        public AutonomousCirculationScenario(
            bool bKnowsCIsMissing = true,
            bool cKnowsDIsMissing = true,
            bool cCopies = true,
            int listeningDays = 0,
            ListeningReaction? bReaction = null,
            ListeningReaction? cReaction = null,
            NodeReactionRules reactionRules = null)

        {
            
            if (listeningDays == 0 &&
                (bReaction.HasValue ||
                 cReaction.HasValue ||
                 reactionRules != null))
            {
                throw new ArgumentException(
                    "Reaction choices require timed listening.",
                    nameof(listeningDays));
            }

            if (reactionRules != null &&
                (bReaction.HasValue || cReaction.HasValue))
            {
                throw new ArgumentException(
                    "Use either node reaction rules or " +
                    "legacy B/C reaction settings.");
            }


            if (bReaction.HasValue &&
                !Enum.IsDefined(
                    typeof(ListeningReaction),
                    bReaction.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bReaction));
            }

            if (cReaction.HasValue &&
                !Enum.IsDefined(
                    typeof(ListeningReaction),
                    cReaction.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cReaction));
            }

            ListeningDays = listeningDays;
            
            Network = new Network();

            foreach (string id in new[] { "A", "B", "C", "D" })
            {
                Network.AddNode(new Node(id));

                Network.AddAddress("ADDR-" + id, "FI");
                Network.PlaceNode(id, "ADDR-" + id);
            }

            Network.LearnAddress("A", "B", "ADDR-B");
            Network.LearnAddress("B", "C", "ADDR-C");
            Network.LearnAddress("C", "D", "ADDR-D");

            AddConnection("AB", "A", "B");
            AddConnection("BC", "B", "C");
            AddConnection("CD", "C", "D");

            var field = new Field("F1", Network);
            field.RegisterLocalNode("A");

            field.SpawnArtist(
                "ART1",
                new Dictionary<string, int>
                {
                    ["A"] = 10
                },
                new Random(123));

            Demo = field.CreateDemo(
                "D1", "ART1", true);

            Inventory = new CassetteInventory(Network);

            Original = Inventory.RegisterExistingCopy(
                "TAPE-ORIGINAL",
                Demo,
                "A");

            Known = new KnownCollectionRegistry(Network);

            // Tieto on tässä alkuasetelmaa.
            // Kopioiden lähettäminen ei ole.
            Known.RecordSnapshot(
                "A", "B", Array.Empty<string>());

            if (bKnowsCIsMissing)
            {
                Known.RecordSnapshot(
                    "B", "C", Array.Empty<string>());
            }

            if (cKnowsDIsMissing)
            {
                Known.RecordSnapshot(
                    "C", "D", Array.Empty<string>());
            }

            Clock = new SimulationClock();
            Backlog = new RecipientBacklog();
            Stances = new CommunicatedStanceRegistry();
            CopyIndex = new CopyShipmentIndex();

            Capital = new CulturalCapitalLedger(Network);

            foreach (string id in new[] { "A", "B", "C", "D" })
                Capital.InitializeNode(id, 10);

            var reception =
                new MessageReceptionRegistry(Known);

            Exchange = new TimedPostalExchange(
                Clock,
                Network,
                Inventory,
                Backlog,
                new ReplyWindowRegistry(),
                new CopyDeliveryRewardService(
                    Network, Inventory, Capital),
                new ReciprocalExchangeRewardService(
                    reception, Capital),
                Stances);

            var distribution =
                new PositiveCopyDistributionService(
                    Inventory, Known, Exchange);

            // Osoitekerroksen kontaktit.
            // Nämä ovat verkoston alustettua rakennetta,
            // eivät aikataulutettuja toimintakäskyjä.
            _routes["A"] = new List<CopyDistributionRoute>
            {
                Route("A", "AB", "B")
            };

            _routes["B"] = new List<CopyDistributionRoute>
            {
                Route("B", "BC", "C")
            };

            _routes["C"] = new List<CopyDistributionRoute>
            {
                Route("C", "CD", "D")
            };

            _routes["D"] =
                new List<CopyDistributionRoute>();

            
            Control = new PhysicalControlService(
                Exchange,

                // Käytetään solmun nykyisiä reittejä.
                nodeId => _routes[nodeId],

                // Tämän koeajon ainoa kulttuurituote.
                productId =>
                    productId == "D1" ? Demo : null,

                nodeId => "FI",

                // Siirtoreitti on kelvollinen vain,
                // kun vastaanottajan tiedetään
                // olevan ilman demoa eikä tämä ole
                // viestinyt torjuntaa.
                (senderId, recipientId, productId) =>
                    Known.IsKnownToHold(
                        senderId, recipientId, productId) == false
                    &&
                    !Stances.HasCommunicatedDislike(
                        senderId, recipientId, productId),

                // Siirto kirjataan kasetti-indeksiin.
                (shipment, copy) =>
                    CopyIndex.RegisterTransfer(shipment, copy));

            
            World = new SimulationWorld(
                Clock, Network, Exchange);

            // Kaikki neljä solmua saavat samanlaisen
            // tapahtumapohjaisen toimintapolitiikan.
            foreach (string nodeId in
                     new[] { "A", "B", "C", "D" })
            {
                string id = nodeId;

                var policy = new PositiveCopyNodePolicy(
                    id,
                    distribution,
                    Stances,
                    CopyIndex.Resolve,
                    () => _routes[id],
                    _ => "FI",
                 

                    (senderId, productId) =>
                        senderId != "C" ||
                        cCopies ||
                        cReaction == ListeningReaction.SupportAndCopy ||
                        reactionRules != null,


                    (senderId, recipientId) => true,
                    onDispatched: CopyIndex.Register);

                
                
                _policies.Add(id, policy);

                if (ListeningDays == 0)
                {
                    // Vanha, välitön kopiointipolitiikka.
                    World.RegisterNode(id, policy);
                }
                else
                {
                    ISimulationNodePolicy completedPolicy = policy;

                    // Vain erikseen määritellyt solmut
                    // käyttävät tässä vaiheessa kolmijakoa.
                    ListeningReaction? configuredReaction =
                        id == "B" ? bReaction :
                        id == "C" ? cReaction :
                        (ListeningReaction?)null;

                    
                    if (reactionRules != null)
                    {
                        // Kaikki solmut käyttävät samaa
                        // yleiskäyttöistä säännöstöä.
                        //
                        // Reaktio ratkaistaan vasta kuuntelun
                        // valmistuessa. Sääntöä ei lukita
                        // konstruktorissa.
                        var reactionPolicy =
                            new ListeningReactionNodePolicy(
                                id,
                                policy,
                                CopyIndex.Resolve,
                                reactionRules.Choose,
                                Control);

                        _reactionPolicies.Add(id, reactionPolicy);
                        completedPolicy = reactionPolicy;
                    }
                    else if (configuredReaction.HasValue)
                    {
                        // Vanha Crucible 6.6 -vaihtoehto
                        // säilytetään yhteensopivuuden vuoksi.
                        ListeningReaction selectedReaction =
                            configuredReaction.Value;

                        var reactionPolicy =
                            new ListeningReactionNodePolicy(
                                id,
                                policy,
                                CopyIndex.Resolve,
                                (nodeId, productId) =>
                                    selectedReaction,
                                Control);

                        _reactionPolicies.Add(id, reactionPolicy);
                        completedPolicy = reactionPolicy;
                    }


                    // Molemmissa tapauksissa kuuntelu
                    // tapahtuu ennen päätöstä.
                    var listening = new TimedListeningNodePolicy(
                        id,
                        completedPolicy,
                        CopyIndex.Resolve,
                        ListeningDays);

                    _listeningPolicies.Add(id, listening);

                    World.RegisterNode(id, listening);
                }


            }

            // Ainoa käsin alustettu lähetys.
            // Se käynnistää myöhemmän ketjun.
            SeedWave = distribution.DispatchAll(
                "SEED-D1",
                "A",
                Original,
                _routes["A"],
                "FI",
                5,
                recipientId =>
                    Stances.HasCommunicatedDislike(
                        "A", recipientId, "D1"),
                recipientId => true);

            if (SeedWave.Count != 1)
                throw new InvalidOperationException(
                    "Expected one seed shipment.");

            foreach (var item in SeedWave)
                CopyIndex.Register(item);
        }

        public PositiveCopyNodePolicy GetPolicy(
            string nodeId)
        {
            if (!_policies.TryGetValue(nodeId, out var policy))
                throw new ArgumentException(
                    "Unknown simulation policy: " + nodeId,
                    nameof(nodeId));

            return policy;
        }
        
        
        public ListeningProgressSnapshot GetListeningStatus(
            string nodeId,
            string productId)
        {
            if (!_listeningPolicies.TryGetValue(
                    nodeId, out var listening))
            {
                return null;
            }

            return listening.GetStatus(productId);
        }


        private void AddConnection(
            string connectionId,
            string sourceId,
            string targetId)
        {
            Network.AddConnection(
                connectionId,
                sourceId,
                targetId,
                CommunicationLayer.Interpersonal,
                "personal");
        }

        private CopyDistributionRoute Route(
            string senderId,
            string connectionId,
            string recipientId)
        {
            var connection =
                Network.GetOutgoingConnections(
                    senderId,
                    CommunicationLayer.Interpersonal)
                    .Single(c => c.Id == connectionId);

            return new CopyDistributionRoute(
                recipientId,
                connection,
                "FI");
        }
        
        
        public ListeningReactionNodePolicy GetReactionPolicy(
            string nodeId)
        {
            if (!_policies.ContainsKey(nodeId))
                throw new ArgumentException(
                    "Unknown node: " + nodeId,
                    nameof(nodeId));

            return _reactionPolicies.TryGetValue(
                nodeId, out var policy)
                ? policy
                : null;
        }

    }
}
