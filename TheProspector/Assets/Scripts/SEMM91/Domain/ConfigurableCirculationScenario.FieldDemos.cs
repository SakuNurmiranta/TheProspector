
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// Result of introducing one new field-origin demo
    /// into an already running simulation scenario.
    /// </summary>
    public sealed class FieldDemoRegistration
    {
        public Artist Artist { get; }
        public CulturalProduct Product { get; }
        public CassetteCopy OriginalCopy { get; }
        public string InitialHolderNodeId { get; }

        public IReadOnlyList<CopyDistributionShipment>
            InitialDispatches { get; }

        internal FieldDemoRegistration(
            Artist artist,
            CulturalProduct product,
            CassetteCopy originalCopy,
            string initialHolderNodeId,
            IReadOnlyList<CopyDistributionShipment> initialDispatches)
        {
            Artist = artist;
            Product = product;
            OriginalCopy = originalCopy;
            InitialHolderNodeId = initialHolderNodeId;
            InitialDispatches = initialDispatches;
        }
    }

    public sealed partial class ConfigurableCirculationScenario
    {
        /// <summary>
        /// Introduces a new cultural product into the live world.
        ///
        /// The artist remains in Field, not in Network.
        /// The field selects a primary contact according to
        /// currently accumulated cultural capital.
        ///
        /// Registers the new demo with existing:
        /// - cultural product and physical cassette inventory
        /// - SIR wave and positive-copy observer
        /// - wave quiescence monitor
        /// - ordinary copy distribution and shipment index
        /// - observation / timeline product IDs
        ///
        /// Does not create nodes or interpersonal arcs.
        /// </summary>
        public FieldDemoRegistration SpawnFieldDemo(
            string demoId,
            string artistId,
            Random random)
        {
            if (string.IsNullOrWhiteSpace(demoId))
                throw new ArgumentException(
                    "Demo ID is required.",
                    nameof(demoId));

            if (string.IsNullOrWhiteSpace(artistId))
                throw new ArgumentException(
                    "Artist ID is required.",
                    nameof(artistId));

            if (random == null)
                throw new ArgumentNullException(nameof(random));

            if (_demos.ContainsKey(demoId))
                throw new ArgumentException(
                    "Demo is already registered.",
                    nameof(demoId));

            // The abstract field uses the cultural-capital
            // values of its currently registered local nodes.
            var fieldCapital =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);

            foreach (string nodeId in NodeIds)
            {
                if (!Field.ContainsLocalNode(nodeId))
                    continue;

                fieldCapital.Add(
                    nodeId,
                    Capital.GetCapital(nodeId));
            }

            // Artist is abstract. Only its primary contact
            // is an explicit person node.
            var artist = Field.SpawnArtist(
                artistId,
                fieldCapital,
                random);

            var demo = Field.CreateDemo(
                demoId,
                artist.Id,
                true);

            string holder = artist.PrimaryContactNodeId;

            
            var originObservation = new DemoOriginAtBirth(
                demoId,
                holder,
                Clock.CurrentDay,
                Capital.GetCapital(holder),
                Network.GetOutgoingConnections(
                    holder,
                    CommunicationLayer.Interpersonal).Count,
                true);

            
            // The original cassette enters the possession
            // of the selected explicit field contact.
            var original = Inventory.RegisterExistingCopy(
                "TAPE-ORIGINAL:" + demoId,
                demo,
                holder);

            _demos.Add(demoId, demo);
            _originals.Add(demoId, original);

            // Make the product visible to observation,
            // timeline and other dynamically reading systems.
            var productIds = new List<string>(ProductIds)
            {
                demoId
            };

            ProductIds = productIds.AsReadOnly();

            // Use the same SIR implementation as initial demos.
            var wave = new FieldDemoCopyWave(
                Field,
                Network,
                demo,
                FieldSIR);

            _fieldWaves.Add(demoId, wave);

            _fieldObservers.Add(
                new FieldDemoCopyDispatchObserver(
                    Distribution,
                    wave));

            _fieldQuiescenceMonitors.Add(
                new FieldDemoWaveQuiescenceMonitor(
                    demoId,
                    wave,
                    World,
                    Exchange,
                    NodeIds,
                    recipient =>
                        _listeningPolicies[recipient]
                            .GetStatus(demoId)));

            // A field-born demo may immediately circulate
            // along existing usable interpersonal routes.
            // An isolated origin may have zero recipients.
            var routes = LiveInterpersonalRoutes.Resolve(
                Network,
                holder,
                _routes[holder]);

            var dispatches = Distribution.DispatchAll(
                "FIELD-SEED:" + demoId,
                holder,
                original,
                routes,
                _countries[holder],
                5,
                recipient =>
                    Stances.HasCommunicatedDislike(
                        holder,
                        recipient,
                        demoId),
                recipient => true);

            _seedWaves.Add(demoId, dispatches);

            foreach (var shipment in dispatches)
            {
                CopyIndex.Register(shipment);

                Referrals.Register(
                    shipment.Shipment.Id,
                    holder,
                    shipment.RecipientNodeId,
                    demoId,
                    shipment.Shipment.Package.Thumb);
            }

            RegisterLocalOriginalIfEnabled(original);
            
            RecordRuntimeOrigin(originObservation);
            
            return new FieldDemoRegistration(
                artist,
                demo,
                original,
                holder,
                dispatches);
        }
    }
}
