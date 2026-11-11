using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed partial class ConfigurableCirculationScenario
    {
        private readonly Dictionary<string, DemoOriginAtBirth>
            _runtimeOrigins =
                new Dictionary<string, DemoOriginAtBirth>(
                    StringComparer.Ordinal);

        public bool TryGetRuntimeOrigin(
            string productId,
            out DemoOriginAtBirth origin)
        {
            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException(
                    "Product ID is required.",
                    nameof(productId));

            return _runtimeOrigins.TryGetValue(
                productId, out origin);
        }

        private void RecordRuntimeOrigin(
            DemoOriginAtBirth origin)
        {
            if (origin == null)
                throw new ArgumentNullException(nameof(origin));

            _runtimeOrigins.Add(origin.ProductId, origin);
        }
    }
}