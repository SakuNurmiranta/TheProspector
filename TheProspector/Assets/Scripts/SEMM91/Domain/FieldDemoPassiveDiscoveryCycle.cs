
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Automatisoi sulkeutuneiden SIR-aaltojen
    /// passiiviset löydöt maailman päivän lopussa.
    ///
    /// Ei luo kasetteja, verkkoyhteyksiä
    /// eikä uusia SIR-aaltoja.
    /// </summary>
    public sealed class FieldDemoPassiveDiscoveryCycle
        : IDisposable
    {
        private readonly SimulationWorld _world;
        private readonly Field _field;

        private readonly IReadOnlyDictionary<
            string, FieldDemoCopyWave> _waves;

        private readonly List<string> _nodeIds;

        private readonly Func<string, int> _capitalForNode;

        private readonly Func<string, string, bool>
            _hasEverHeldProduct;

        private readonly Random _random;

        private readonly HashSet<(string Product, string Node)>
            _attempted =
                new HashSet<(string Product, string Node)>();

        private bool _disposed;

        public int AttemptCount { get; private set; }
        public int DiscoveryCount { get; private set; }

        public FieldDemoPassiveDiscoveryCycle(
            SimulationWorld world,
            Field field,
            IEnumerable<string> nodeIds,
            IReadOnlyDictionary<string, FieldDemoCopyWave> waves,
            Func<string, int> capitalForNode,
            Func<string, string, bool> hasEverHeldProduct,
            Random random)
        {
            _world = world ??
                throw new ArgumentNullException(nameof(world));

            _field = field ??
                throw new ArgumentNullException(nameof(field));

            if (nodeIds == null)
                throw new ArgumentNullException(
                    nameof(nodeIds));

            _nodeIds = new List<string>(nodeIds);

            _waves = waves ??
                throw new ArgumentNullException(nameof(waves));

            _capitalForNode = capitalForNode ??
                throw new ArgumentNullException(
                    nameof(capitalForNode));

            _hasEverHeldProduct = hasEverHeldProduct ??
                throw new ArgumentNullException(
                    nameof(hasEverHeldProduct));

            _random = random ??
                throw new ArgumentNullException(nameof(random));

            _world.DayCompleted += OnDayCompleted;
        }

        private void OnDayCompleted(int day)
        {
            if (_disposed)
                return;

            foreach (var pair in _waves)
            {
                string productId = pair.Key;
                FieldDemoCopyWave wave = pair.Value;

                if (!wave.IsPassivelyDiscoverable)
                    continue;

                if (wave.ObservedDeliveredReach <= 0.0)
                    continue;

                // Kenttäosuudet lasketaan sen
                // hetkisen pääoman perusteella.
                var capital =
                    new Dictionary<string, int>(
                        StringComparer.Ordinal);

                foreach (string nodeId in _nodeIds)
                {
                    if (_field.ContainsLocalNode(nodeId))
                    {
                        capital.Add(
                            nodeId,
                            _capitalForNode(nodeId));
                    }
                }

                foreach (string nodeId in _nodeIds)
                {
                    if (!_field.ContainsLocalNode(nodeId))
                        continue;

                    var key = (productId, nodeId);

                    if (_attempted.Contains(key))
                        continue;

                    // Oma aiempi fyysinen kasetti
                    // tekee passiivisen löydön
                    // tämän mallin kannalta tarpeettomaksi.
                    if (_hasEverHeldProduct(
                            nodeId, productId))
                    {
                        continue;
                    }

                    // Wave omistaa itse löytötiedon.
                    // Tämä sovitin ainoastaan ajastaa
                    // yhden löytöyrityksen.
                    bool discovered =
                        wave.TrySamplePassiveExposure(
                            nodeId,
                            capital,
                            _random);

                    _attempted.Add(key);
                    AttemptCount++;

                    if (discovered)
                        DiscoveryCount++;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _world.DayCompleted -= OnDayCompleted;
            _disposed = true;
        }
    }
}
