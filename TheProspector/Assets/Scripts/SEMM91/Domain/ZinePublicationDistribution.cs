
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Yhdistää toimituksen ajastetut julkaisut
    /// olemassa olevaan Insert-jakeluun.
    ///
    /// Ei omista julkaisua, kellota numeroita
    /// eikä muuta toimituksellista sisältöä.
    /// </summary>
    public sealed class ZinePublicationDistribution : IDisposable
    {
        private readonly ZineEditorialCycle _editorial;
        private readonly ZineIssueInsertDelivery _delivery;

        private readonly Func<
            IEnumerable<NetworkConnection>> _routes;

        private readonly Func<string, bool>
            _recipientPrefersTrue;

        private readonly ZinePhysicalBulkDistribution _physical;
        private readonly int _population;
        private readonly int _nodesPerCopy;

        private bool _disposed;

        public ZinePublicationDistribution(
            ZineEditorialCycle editorial,
            ZineIssueInsertDelivery delivery,
            Func<IEnumerable<NetworkConnection>> routes,
            Func<string, bool> recipientPrefersTrue)
        {
            _editorial = editorial ??
                throw new ArgumentNullException(
                    nameof(editorial));

            _delivery = delivery ??
                throw new ArgumentNullException(
                    nameof(delivery));

            _routes = routes ??
                throw new ArgumentNullException(
                    nameof(routes));

            _recipientPrefersTrue =
                recipientPrefersTrue ??
                throw new ArgumentNullException(
                    nameof(recipientPrefersTrue));

            _editorial.IssuePublished += OnIssuePublished;
        }

        /// <summary>
        /// Opt-in finite physical circulation. Legacy constructor remains
        /// unchanged for existing tests and older demonstration scenarios.
        /// </summary>
        public ZinePublicationDistribution(
            ZineEditorialCycle editorial,
            ZinePhysicalBulkDistribution physical,
            Func<IEnumerable<NetworkConnection>> routes,
            int population,
            int nodesPerCopy = 4)
        {
            _editorial = editorial ??
                throw new ArgumentNullException(nameof(editorial));
            _physical = physical ??
                throw new ArgumentNullException(nameof(physical));
            _routes = routes ??
                throw new ArgumentNullException(nameof(routes));
            if (population < 1)
                throw new ArgumentOutOfRangeException(nameof(population));
            if (nodesPerCopy < 1)
                throw new ArgumentOutOfRangeException(nameof(nodesPerCopy));
            _population = population;
            _nodesPerCopy = nodesPerCopy;
            _editorial.IssuePublished += OnIssuePublished;
        }

        private void OnIssuePublished(
            ZineEditorialIssue issue)
        {
            var selectedRoutes = _routes();

            if (selectedRoutes == null)
                throw new InvalidOperationException(
                    "Zine distribution routes are missing.");

            // Otetaan tilannekuva julkaisuhetkellä.
            // Myöhemmät verkostomuutokset eivät
            // vaikuta jo käsiteltyyn julkaisuun.
            var routes = new List<NetworkConnection>(
                selectedRoutes);

            foreach (var route in routes)
            {
                if (route == null)
                    throw new InvalidOperationException(
                        "Zine distribution route is null.");

                if (!string.Equals(
                        route.SourceNodeId,
                        _editorial.EditorNodeId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Initial distribution must originate " +
                        "from the zine editor.");
                }
            }

            if (_physical != null)
            {
                _physical.PrintIssue(
                    issue, _population, _nodesPerCopy);
                _physical.DistributeInitial(issue, routes);
            }
            else
            {
                _delivery.Distribute(
                    issue, routes, _recipientPrefersTrue);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _editorial.IssuePublished -= OnIssuePublished;

            _disposed = true;
        }
    }
}
