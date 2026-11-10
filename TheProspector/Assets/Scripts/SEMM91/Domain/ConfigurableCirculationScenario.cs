using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    public sealed class CirculationNodeDefinition
    {
        public string Id { get; }
        public string CountryCode { get; }

        internal CirculationNodeDefinition(
            string id,
            string countryCode)
        {
            Id = id;
            CountryCode = countryCode;
        }
    }

    public sealed class CirculationAddressDefinition
    {
        public string Id { get; }
        public string CountryCode { get; }

        internal CirculationAddressDefinition(string id, string countryCode)
        {
            Id = id;
            CountryCode = countryCode;
        }
    }

    public sealed class CirculationNodePlacementDefinition
    {
        public string NodeId { get; }
        public string AddressId { get; }

        internal CirculationNodePlacementDefinition(string nodeId, string addressId)
        {
            NodeId = nodeId;
            AddressId = addressId;
        }
    }

    public sealed class CirculationConnectionDefinition
    {
        public string Id { get; }
        public string SenderId { get; }
        public string RecipientId { get; }

        internal CirculationConnectionDefinition(
            string id,
            string senderId,
            string recipientId)
        {
            Id = id;
            SenderId = senderId;
            RecipientId = recipientId;
        }
    }


    public sealed class CirculationSeedDefinition
    {
        public string SourceNodeId { get; }
        public string DemoId { get; }

        internal CirculationSeedDefinition(
            string sourceNodeId,
            string demoId)
        {
            SourceNodeId = sourceNodeId;
            DemoId = demoId;
        }
    }


    public sealed class CirculationWorldDefinition
    {
        private readonly List<CirculationNodeDefinition>
            _nodes = new();

        private readonly List<CirculationAddressDefinition>
            _addresses = new();

        private readonly List<CirculationNodePlacementDefinition>
            _placements = new();

        private readonly Dictionary<string, string> _nodeCountryCodes =
            new(StringComparer.Ordinal);

        private readonly Dictionary<string, string> _addressCountryCodes =
            new(StringComparer.Ordinal);

        private readonly HashSet<string> _placedNodeIds =
            new(StringComparer.Ordinal);

        private readonly HashSet<string> _occupiedAddressIds =
            new(StringComparer.Ordinal);

        private readonly List<CirculationConnectionDefinition>
            _connections = new();

        private readonly List<(string Observer, string Subject)>
            _knownMissing = new();


        private readonly List<CirculationSeedDefinition>
            _seeds = new();

        private readonly HashSet<string> _seedProductIds = new(StringComparer.Ordinal);


        private readonly HashSet<string> _nodeIds = new(StringComparer.Ordinal);

        private readonly HashSet<string> _connectionIds = new(StringComparer.Ordinal);

        private readonly HashSet<(string, string)>
            _knownPairs = new();

        public IReadOnlyList<CirculationNodeDefinition>
            Nodes => _nodes.AsReadOnly();

        public IReadOnlyList<CirculationAddressDefinition>
            Addresses => _addresses.AsReadOnly();

        public IReadOnlyList<CirculationNodePlacementDefinition>
            Placements => _placements.AsReadOnly();

        public IReadOnlyList<CirculationConnectionDefinition>
            Connections => _connections.AsReadOnly();

        public IReadOnlyList<(string Observer, string Subject)>
            KnownMissing => _knownMissing.AsReadOnly();


        public IReadOnlyList<CirculationSeedDefinition>
            Seeds => _seeds.AsReadOnly();

        public string SeedNodeId =>
            _seeds.Count > 0 ? _seeds[0].SourceNodeId : null;

        public string DemoId =>
            _seeds.Count > 0 ? _seeds[0].DemoId : null;


        public void AddNode(
            string id,
            string countryCode = "FI")
        {
            ValidateId(id, nameof(id));
            ValidateId(countryCode, nameof(countryCode));

            if (!_nodeIds.Add(id))
                throw new ArgumentException(
                    "Duplicate node: " + id,
                    nameof(id));

            _nodes.Add(
                new CirculationNodeDefinition(
                    id, countryCode));

            _nodeCountryCodes.Add(id, countryCode);
        }

        /// <summary>
        /// Explicit addresses enable multi-slot initialization. If no addresses
        /// are configured, legacy ADDR-{nodeId} addresses are used instead.
        /// </summary>
        public void AddAddress(string id, string countryCode)
        {
            ValidateId(id, nameof(id));
            ValidateId(countryCode, nameof(countryCode));

            if (_addressCountryCodes.ContainsKey(id))
                throw new ArgumentException("Duplicate address: " + id,
                    nameof(id));

            _addressCountryCodes.Add(id, countryCode);
            _addresses.Add(new CirculationAddressDefinition(id, countryCode));
        }

        public void PlaceNode(string nodeId, string addressId)
        {
            RequireNode(nodeId);
            ValidateId(addressId, nameof(addressId));

            if (!_addressCountryCodes.TryGetValue(addressId, out var country))
                throw new ArgumentException("Unknown address: " + addressId,
                    nameof(addressId));

            if (!string.Equals(_nodeCountryCodes[nodeId], country,
                    StringComparison.Ordinal))
                throw new ArgumentException(
                    "Node and initial address must belong to the same country.",
                    nameof(addressId));

            if (!_placedNodeIds.Add(nodeId))
                throw new ArgumentException("Node already placed: " + nodeId,
                    nameof(nodeId));

            if (!_occupiedAddressIds.Add(addressId))
            {
                _placedNodeIds.Remove(nodeId);
                throw new ArgumentException("Address already occupied: " + addressId,
                    nameof(addressId));
            }

            _placements.Add(
                new CirculationNodePlacementDefinition(nodeId, addressId));
        }

        public void AddConnection(
            string id,
            string senderId,
            string recipientId)
        {
            ValidateId(id, nameof(id));
            RequireNode(senderId);
            RequireNode(recipientId);

            if (!_connectionIds.Add(id))
                throw new ArgumentException(
                    "Duplicate connection: " + id,
                    nameof(id));

            // Sama lähettäjä ja vastaanottaja
            // voivat muodostaa rinnakkaisia kaaria,
            // kunhan kaarien tunnisteet eroavat.
            _connections.Add(
                new CirculationConnectionDefinition(
                    id, senderId, recipientId));
        }

        public void MarkKnownMissing(
            string observerId,
            string subjectId)
        {
            RequireNode(observerId);
            RequireNode(subjectId);

            var key = (observerId, subjectId);

            if (_knownPairs.Add(key))
                _knownMissing.Add(key);
        }


        public void SetSeed(
            string senderId,
            string demoId)
        {
            RequireNode(senderId);
            ValidateId(demoId, nameof(demoId));

            // Vanha SetSeed tarkoittaa edelleen:
            // korvaa aikaisempi alustus.
            _seeds.Clear();
            _seedProductIds.Clear();

            AddSeed(senderId, demoId);
        }

        public void AddSeed(
            string senderId,
            string demoId)
        {
            RequireNode(senderId);
            ValidateId(demoId, nameof(demoId));

            if (!_seedProductIds.Add(demoId))
                throw new ArgumentException(
                    "Demo already seeded: " + demoId,
                    nameof(demoId));

            _seeds.Add(
                new CirculationSeedDefinition(
                    senderId, demoId));
        }


        private void RequireNode(string id)
        {
            ValidateId(id, nameof(id));

            if (!_nodeIds.Contains(id))
                throw new ArgumentException(
                    "Unknown node: " + id,
                    nameof(id));
        }

        private static void ValidateId(
            string id,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(
                    "Identifier cannot be empty.",
                    parameterName);
        }
    }

    /// <summary>
    /// Alustaa mielivaltaisen kokoisen eksplisiittisen
    /// suunnatun verkoston ja käynnistää yhden
    /// kulttuurituotteen autonomisen levityksen.
    ///
    /// Yhtään solmujen toimintapäivää ei
    /// määritellä etukäteen.
    /// </summary>
    public sealed partial class ConfigurableCirculationScenario
    {
        private readonly Dictionary<string, string>
            _countries =
                new(
                    StringComparer.Ordinal);

        private readonly Dictionary<
                string, List<CopyDistributionRoute>>
            _routes =
                new(
                    StringComparer.Ordinal);

        private readonly Dictionary<
                string, PositiveCopyNodePolicy>
            _positivePolicies =
                new(
                    StringComparer.Ordinal);

        private readonly Dictionary<
                string, ListeningReactionNodePolicy>
            _reactionPolicies =
                new(
                    StringComparer.Ordinal);

        private readonly Dictionary<
                string, TimedListeningNodePolicy>
            _listeningPolicies =
                new(
                    StringComparer.Ordinal);


        private readonly Dictionary<string, CulturalProduct>
            _demos =
                new(
                    StringComparer.Ordinal);

        private readonly Dictionary<string, CassetteCopy>
            _originals =
                new(
                    StringComparer.Ordinal);

        private readonly Dictionary<
                string, IReadOnlyList<CopyDistributionShipment>>
            _seedWaves =
                new(
                    StringComparer.Ordinal);


        private readonly Dictionary<string, FieldDemoCopyWave>
            _fieldWaves =
                new(
                    StringComparer.Ordinal);

        private readonly List<FieldDemoCopyDispatchObserver>
            _fieldObservers = new();


        private readonly List<FieldDemoWaveQuiescenceMonitor>
            _fieldQuiescenceMonitors = new();


        public Field Field { get; }
        public SIRStateRegistry FieldSIR { get; }
        public PositiveCopyDistributionService Distribution { get; }


        public IReadOnlyList<string> NodeIds { get; }
        public IReadOnlyList<string> ProductIds { get; private set; }
        public Network Network { get; }
        public SimulationClock Clock { get; }
        public SimulationWorld World { get; }

        public CirculationObservationReader Observation { get; }
        public CirculationTimelineReader Timeline { get; }

        public CassetteInventory Inventory { get; }
        public KnownCollectionRegistry Known { get; }
        public RecipientBacklog Backlog { get; }
        public CulturalCapitalLedger Capital { get; }


        public SkepticismReferralRegistry Referrals { get; }
        public DirectionalSkepticismLedger Skepticism { get; }

        public ReplyWindowSkepticismMonitor ReplySkepticism { get; }


        public CommunicatedStanceRegistry Stances { get; }
        public TimedPostalExchange Exchange { get; }
        public CopyShipmentIndex CopyIndex { get; }
        public PhysicalControlService Control { get; }

        public NodeReactionRules ReactionRules { get; }

        public CulturalProduct Demo { get; }
        public CassetteCopy Original { get; }

        public HardControlCommunicationActions HardControl { get; }

        public IReadOnlyList<CopyDistributionShipment>
            SeedWave { get; }

        public FieldDemoPassiveDiscoveryCycle PassiveFieldDiscovery { get; }

        public ConfigurableCirculationScenario(
            CirculationWorldDefinition definition,
            int listeningDays = 2,
            NodeReactionRules reactionRules = null,
            bool explicitPositiveReferrals = false,
            Random fieldDiscoveryRandom = null)

        {
            if (definition == null)
                throw new ArgumentNullException(
                    nameof(definition));

            if (listeningDays < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(listeningDays));

            if (string.IsNullOrWhiteSpace(
                    definition.SeedNodeId))
                throw new InvalidOperationException(
                    "A seed node must be configured.");

            ReactionRules =
                reactionRules ?? new NodeReactionRules();

            Network = new Network();

            // Backward compatible: old definitions without explicit address
            // slots still receive exactly one ADDR-{nodeId} per person.
            bool explicitAddresses = definition.Addresses.Count > 0;

            if (explicitAddresses &&
                definition.Placements.Count != definition.Nodes.Count)
                throw new InvalidOperationException(
                    "Every node requires an initial placement when explicit " +
                    "address slots are configured.");

            if (!explicitAddresses && definition.Placements.Count != 0)
                throw new InvalidOperationException(
                    "Placements require explicit address slots.");

            var addressesByNode = new Dictionary<string, string>(
                StringComparer.Ordinal);

            if (explicitAddresses)
            {
                foreach (var address in definition.Addresses)
                    Network.AddAddress(address.Id, address.CountryCode);
            }

            var nodeIds = new List<string>();

            foreach (var node in definition.Nodes)
            {
                Network.AddNode(new Node(node.Id));

                if (!explicitAddresses)
                {
                    var addressId = "ADDR-" + node.Id;
                    Network.AddAddress(addressId, node.CountryCode);
                    Network.PlaceNode(node.Id, addressId);
                    addressesByNode.Add(node.Id, addressId);
                }

                _countries.Add(node.Id, node.CountryCode);
                _routes.Add(node.Id, new List<CopyDistributionRoute>());
                nodeIds.Add(node.Id);
            }

            if (explicitAddresses)
            {
                foreach (var placement in definition.Placements)
                {
                    Network.PlaceNode(placement.NodeId, placement.AddressId);
                    addressesByNode.Add(
                        placement.NodeId, placement.AddressId);
                }
            }

            NodeIds = nodeIds.AsReadOnly();

            var learnedAddresses =
                new HashSet<(string, string)>();

            foreach (var edge in definition.Connections)
            {
                var addressPair = (
                    edge.SenderId,
                    edge.RecipientId);

                if (learnedAddresses.Add(addressPair))
                    Network.LearnAddress(
                        edge.SenderId,
                        edge.RecipientId,
                        addressesByNode[edge.RecipientId]);

                Network.AddConnection(
                    edge.Id,
                    edge.SenderId,
                    edge.RecipientId,
                    CommunicationLayer.Interpersonal,
                    "personal");
            }

            foreach (var edge in definition.Connections)
            {
                var connection =
                    Network.GetOutgoingConnections(
                            edge.SenderId,
                            CommunicationLayer.Interpersonal)
                        .Single(c => c.Id == edge.Id);

                _routes[edge.SenderId].Add(
                    new CopyDistributionRoute(
                        edge.RecipientId,
                        connection,
                        _countries[edge.RecipientId]));
            }


            var field = new Field("F1", Network);

            var registeredLocalNodes =
                new HashSet<string>(StringComparer.Ordinal);

            var productIds = new List<string>();

// Jokaiselle demolle alustetaan oma
// kentän artisti. Artistit eivät ole
// eksplisiittisiä verkostosolmuja.
            for (var i = 0; i < definition.Seeds.Count; i++)
            {
                var seed = definition.Seeds[i];

                if (registeredLocalNodes.Add(seed.SourceNodeId)) field.RegisterLocalNode(seed.SourceNodeId);

                var artistId = "ART" + (i + 1);

                // Field.CalculateShares tarvitsee paatoman jokaiselle
                // tahan mennessa rekisteroidylle paikalliselle solmulle.
                // Seedin lahtosolmu maarittaa kasetin alustetun haltijan
                // erikseen RegisterExistingCopy-kutsussa.
                var fieldCapital = registeredLocalNodes.ToDictionary(
                    nodeId => nodeId,
                    nodeId => 10,
                    StringComparer.Ordinal);

                field.SpawnArtist(
                    artistId,
                    fieldCapital,
                    new Random(123 + i));

                var demo = field.CreateDemo(
                    seed.DemoId,
                    artistId,
                    true);

                _demos.Add(seed.DemoId, demo);
                productIds.Add(seed.DemoId);
            }


// Ensimmäinen skenaarioversio käyttää yhtä
// koko eksplisiittisen verkoston kattavaa kenttää.
//
// Rekisteröinti tehdään artistien alustamisen
// JÄLKEEN, jotta niiden alkusijoittelu säilyy.
            foreach (var nodeId in NodeIds)
                if (!field.ContainsLocalNode(nodeId))
                    field.RegisterLocalNode(nodeId);

            Field = field;


            ProductIds = productIds.AsReadOnly();

            Inventory = new CassetteInventory(Network);

            foreach (var seed in definition.Seeds)
            {
                var original = Inventory.RegisterExistingCopy(
                    "TAPE-ORIGINAL:" + seed.DemoId,
                    _demos[seed.DemoId],
                    seed.SourceNodeId);

                _originals.Add(seed.DemoId, original);
            }

// Vanhan yksittäisen demon rajapinnan
// yhteensopivuus.
            Demo = _demos[definition.DemoId];
            Original = _originals[definition.DemoId];


            Known = new KnownCollectionRegistry(Network);

            foreach (var pair in definition.KnownMissing)
                Known.RecordSnapshot(
                    pair.Observer,
                    pair.Subject,
                    Array.Empty<string>());

            Clock = new SimulationClock();
            Backlog = new RecipientBacklog();
            Stances = new CommunicatedStanceRegistry();

            CopyIndex = new CopyShipmentIndex();


            Referrals = new SkepticismReferralRegistry();
            Skepticism = new DirectionalSkepticismLedger();

            ReplySkepticism = new ReplyWindowSkepticismMonitor(
                Clock,
                Skepticism);

            Capital = new CulturalCapitalLedger(Network);

            foreach (var nodeId in NodeIds) Capital.InitializeNode(nodeId, 10);

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
                Stances,
                ReplySkepticism);


            Func<string, CulturalProduct> referralResolver = null;

            if (explicitPositiveReferrals) referralResolver = productId => _demos[productId];


            Distribution = new PositiveCopyDistributionService(
                Inventory,
                Known,
                Exchange,
                referralResolver);

// Vanha paikallinen muuttuja säilyttää
// konstruktorin myöhemmät käyttökohdat.
            var distribution = Distribution;

            FieldSIR = new SIRStateRegistry();

// Jokainen kulttuurituote saa oman
// SIR-aaltonsa mutta käyttää samaa
// kenttää ja samaa jakelupalvelua.
            foreach (var productId in ProductIds)
            {
                var wave = new FieldDemoCopyWave(
                    Field,
                    Network,
                    _demos[productId],
                    FieldSIR);

                _fieldWaves.Add(productId, wave);

                // Tilaus tehdään ennen varsinaisten
                // alustuslähetysten käynnistämistä.
                _fieldObservers.Add(
                    new FieldDemoCopyDispatchObserver(
                        Distribution,
                        wave));
            }



            Control = new PhysicalControlService(
                Exchange,
                nodeId => LiveInterpersonalRoutes.Resolve(
                    Network,
                    nodeId,
                    _routes[nodeId]),

                productId =>
                    _demos.TryGetValue(
                        productId, out var product)
                        ? product
                        : null,
                nodeId => _countries[nodeId],
                (sender, recipient, productId) =>
                    Known.IsKnownToHold(
                        sender, recipient, productId) == false
                    &&
                    !Stances.HasCommunicatedDislike(
                        sender, recipient, productId),
                (shipment, copy) =>
                    CopyIndex.RegisterTransfer(
                        shipment, copy));



            HardControl = new HardControlCommunicationActions(
                Control,
                Exchange,
                nodeId => LiveInterpersonalRoutes.Resolve(
                    Network,
                    nodeId,
                    _routes[nodeId]),

                productId =>
                    _demos.TryGetValue(
                        productId, out var product)
                        ? product
                        : null,
                nodeId => _countries[nodeId]);


            World = new SimulationWorld(
                Clock, Network, Exchange);

            foreach (var nodeId in NodeIds)
            {
                // Suljetaan foreach-muuttuja kunkin
                // toimintapolitiikan omaan muuttujaan.
                var id = nodeId;

                var positivePolicy =
                    new PositiveCopyNodePolicy(
                        id,
                        distribution,
                        Stances,
                       
                        CopyIndex.Resolve,
                        () => LiveInterpersonalRoutes.Resolve(
                            Network,
                            id,
                            _routes[id]),

                        node => _countries[node],
                        (node, product) => true,
                        (sender, recipient) => true,
                        onDispatched: shipment =>
                        {
                            CopyIndex.Register(shipment);

                            Referrals.Register(
                                shipment.Shipment.Id,
                                id,
                                shipment.RecipientNodeId,
                                shipment.Copy.ProductId,
                                shipment.Shipment.Package.Thumb);
                        });


                var reactionPolicy =
                    new ListeningReactionNodePolicy(
                        id,
                        positivePolicy,
                        CopyIndex.Resolve,
                        ReactionRules.Choose,
                        HardControl);


                var listeningPolicy =
                    new TimedListeningNodePolicy(
                        id,
                        reactionPolicy,
                        CopyIndex.Resolve,
                        listeningDays);

                _positivePolicies.Add(
                    id, positivePolicy);

                _reactionPolicies.Add(
                    id, reactionPolicy);

                _listeningPolicies.Add(
                    id, listeningPolicy);


                var skepticismPolicy = new SkepticismNodePolicy(
                    id,
                    listeningPolicy,
                    reactionPolicy,
                    CopyIndex.Resolve,
                    Referrals,
                    Skepticism);

                World.RegisterNode(
                    id, skepticismPolicy);
            }


// Kuuntelupolitiikat ovat nyt olemassa
// kaikille solmuille.
//
// Kuuntelijat asennetaan ennen seedien
// postittamista, jotta myös alustuksen
// fyysiset lähetykset tulevat seuratuiksi.
            foreach (var productId in ProductIds)
            {
                var id = productId;

                _fieldQuiescenceMonitors.Add(
                    new FieldDemoWaveQuiescenceMonitor(
                        id,
                        _fieldWaves[id],
                        World,
                        Exchange,
                        NodeIds,
                        recipient =>
                            _listeningPolicies[recipient]
                                .GetStatus(id)));
            }

            
// Sulkemismonitorit on jo rekisteröity.
// Passiivinen löytö käsitellään niiden jälkeen,
// saman päivän viimeisessä vaiheessa.
            PassiveFieldDiscovery =
                new FieldDemoPassiveDiscoveryCycle(
                    World,
                    Field,
                    NodeIds,
                    _fieldWaves,
                    nodeId => Capital.GetCapital(nodeId),
                    (nodeId, productId) =>
                        Inventory.HasEverHeldProduct(
                            nodeId, productId),
                    fieldDiscoveryRandom ?? new Random(12345));


            // Alustetut viestintätapahtumat: yksi lähetysaalto per demo.
            // Kaikki myöhemmät päätökset syntyvät solmujen toimintakierrosta.
            foreach (var seed in definition.Seeds)
            {
                // Jokaisella demolla on oma alustuslähetys.
                // Tunnisteet ovat kulttuurituotekohtaisia.
                var wave = distribution.DispatchAll(
                    "SEED:" + seed.DemoId,
                    seed.SourceNodeId,
                    _originals[seed.DemoId],
                    _routes[seed.SourceNodeId],
                    _countries[seed.SourceNodeId],
                    5,
                    recipient =>
                        Stances.HasCommunicatedDislike(
                            seed.SourceNodeId,
                            recipient,
                            seed.DemoId),
                    recipient => true);

                _seedWaves.Add(seed.DemoId, wave);


                foreach (var shipment in wave)
                {
                    CopyIndex.Register(shipment);

                    Referrals.Register(
                        shipment.Shipment.Id,
                        seed.SourceNodeId,
                        shipment.RecipientNodeId,
                        seed.DemoId,
                        shipment.Shipment.Package.Thumb);
                }
            }

            // Vanha SeedWave-rajapinta osoittaa ensimmäiseen demoon.
            SeedWave = _seedWaves[definition.DemoId];

            Observation = new CirculationObservationReader(
                this, definition);

            Timeline = new CirculationTimelineReader(this);
        }

        
        public bool HasDiscoveredFromField(
            string nodeId,
            string productId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException(
                    "Node ID cannot be empty.",
                    nameof(nodeId));

            if (!Network.TryGetNode(nodeId, out _))
                throw new ArgumentException(
                    "Unknown node: " + nodeId,
                    nameof(nodeId));

            return GetFieldWave(productId)
                .HasPassiveExposure(nodeId);
        }

        public PositiveCopyNodePolicy GetPolicy(
            string nodeId)
        {
            if (!_positivePolicies.TryGetValue(
                    nodeId, out var policy))
                throw new ArgumentException(
                    "Unknown node: " + nodeId,
                    nameof(nodeId));

            return policy;
        }

        public ListeningReactionNodePolicy
            GetReactionPolicy(string nodeId)
        {
            if (!_reactionPolicies.TryGetValue(
                    nodeId, out var policy))
                throw new ArgumentException(
                    "Unknown node: " + nodeId,
                    nameof(nodeId));

            return policy;
        }

        public ListeningProgressSnapshot
            GetListeningStatus(
                string nodeId,
                string productId)
        {
            if (!_listeningPolicies.TryGetValue(
                    nodeId, out var policy))
                throw new ArgumentException(
                    "Unknown node: " + nodeId,
                    nameof(nodeId));

            return policy.GetStatus(productId);
        }


        public CulturalProduct GetDemo(string productId)
        {
            if (!_demos.TryGetValue(productId, out var demo))
                throw new ArgumentException(
                    "Unknown demo: " + productId,
                    nameof(productId));

            return demo;
        }

        public CassetteCopy GetOriginal(string productId)
        {
            if (!_originals.TryGetValue(productId, out var copy))
                throw new ArgumentException(
                    "Unknown demo: " + productId,
                    nameof(productId));

            return copy;
        }


        public FieldDemoCopyWave GetFieldWave(
            string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException(
                    "Product ID cannot be empty.",
                    nameof(productId));

            if (!_fieldWaves.TryGetValue(
                    productId, out var wave))
                throw new ArgumentException(
                    "Unknown product: " + productId,
                    nameof(productId));

            return wave;
        }

        public IReadOnlyList<CopyDistributionShipment>
            GetSeedWave(string productId)
        {
            if (!_seedWaves.TryGetValue(
                    productId, out var wave))
                throw new ArgumentException(
                    "Unknown demo: " + productId,
                    nameof(productId));

            return wave;
        }
    }
}