
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Solmun toimintapolitiikka, joka voi
    /// kopioida vastaanotetun kasetin kaikille
    /// ehdot täyttäville kontakteille.
    ///
    /// Päätöksen ajankohta määräytyy viestin
    /// avaamisesta, ei käsikirjoitetusta päivästä.
    ///
    /// Esteettinen arviointi annetaan erillisenä
    /// päätösfunktiona.
    /// </summary>
    public sealed class PositiveCopyNodePolicy
        : ISimulationNodePolicy
    {
        private readonly string _nodeId;

        private readonly Action<CopyDistributionShipment> _onDispatched;
        
        private readonly PositiveCopyDistributionService
            _distribution;

        private readonly CommunicatedStanceRegistry
            _stances;

        private readonly Func<BacklogItem, CassetteCopy>
            _resolveIncomingCopy;

        private readonly Func<IEnumerable<CopyDistributionRoute>>
            _routes;

        private readonly Func<string, string>
            _countryForNode;

        private readonly Func<string, string, bool>
            _shouldCopy;

        private readonly Func<string, string, bool>
            _canOffer;

        private readonly int _replyAllowanceDays;

        private readonly HashSet<string>
            _processedPackages =
                new HashSet<string>(
                    StringComparer.Ordinal);

        private readonly List<CopyDistributionShipment>
            _dispatched =
                new List<CopyDistributionShipment>();

        private readonly IReadOnlyList<CopyDistributionShipment>
            _dispatchedView;

        private int _batchSequence;
        
        private readonly HashSet<string> _processedLocalProducts =
            new HashSet<string>(StringComparer.Ordinal);

        public int CopyDecisions { get; private set; }

        public IReadOnlyList<CopyDistributionShipment>
            Dispatched => _dispatchedView;

        public PositiveCopyNodePolicy(
            string nodeId,
            PositiveCopyDistributionService distribution,
            CommunicatedStanceRegistry stances,
            Func<BacklogItem, CassetteCopy> resolveIncomingCopy,
            Func<IEnumerable<CopyDistributionRoute>> routes,
            Func<string, string> countryForNode,
            Func<string, string, bool> shouldCopy,
            Func<string, string, bool> canOffer = null,
            int replyAllowanceDays = 5,
            Action<CopyDistributionShipment> onDispatched = null)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException(
                    "Node ID cannot be empty.",
                    nameof(nodeId));

            if (replyAllowanceDays < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(replyAllowanceDays));

            _onDispatched = onDispatched;
            
            _nodeId = nodeId;

            _distribution = distribution ??
                throw new ArgumentNullException(
                    nameof(distribution));

            _stances = stances ??
                throw new ArgumentNullException(
                    nameof(stances));

            _resolveIncomingCopy = resolveIncomingCopy ??
                throw new ArgumentNullException(
                    nameof(resolveIncomingCopy));

            _routes = routes ??
                throw new ArgumentNullException(
                    nameof(routes));

            _countryForNode = countryForNode ??
                throw new ArgumentNullException(
                    nameof(countryForNode));

            _shouldCopy = shouldCopy ??
                throw new ArgumentNullException(
                    nameof(shouldCopy));

            _canOffer = canOffer ??
                ((sender, recipient) => true);

            _replyAllowanceDays = replyAllowanceDays;

            _dispatchedView = _dispatched.AsReadOnly();
        }

        public void OnDay(string nodeId, int day)
        {
            ValidateNode(nodeId);

            // Ei päivämäärään sidottuja toimintoja.
            //
            // Tulevaisuudessa täällä voidaan
            // käsitellä esimerkiksi kuunteluaikaa,
            // stressiä ja keskeneräisiä päätöksiä.
        }

        
public void OnMessageOpened(
    string nodeId,
    BacklogItem message,
    int capitalAward,
    int day)
{
    ValidateNode(nodeId);

    if (message == null)
        throw new ArgumentNullException(nameof(message));

    if (!_processedPackages.Add(message.Package.Id))
        return;

    CassetteCopy sourceCopy =
        _resolveIncomingCopy(message);

    if (sourceCopy == null)
        return;

    DispatchFromCopy(sourceCopy);
}

public void OnLocalListeningCompleted(
    string nodeId,
    CassetteCopy sourceCopy,
    int day)
{
    ValidateNode(nodeId);

    if (sourceCopy == null)
        throw new ArgumentNullException(
            nameof(sourceCopy));

    if (day < 0)
        throw new ArgumentOutOfRangeException(nameof(day));

    if (_processedLocalProducts.Contains(
            sourceCopy.ProductId))
    {
        return;
    }

    DispatchFromCopy(sourceCopy);

    _processedLocalProducts.Add(
        sourceCopy.ProductId);
}

private void DispatchFromCopy(
    CassetteCopy sourceCopy)
{
    if (!string.Equals(
            sourceCopy.HolderNodeId,
            _nodeId,
            StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "Node does not hold the received copy.");
    }

    CopyDecisions++;

    if (!_shouldCopy(
            _nodeId,
            sourceCopy.ProductId))
    {
        return;
    }

    var availableRoutes = _routes();

    if (availableRoutes == null)
        throw new InvalidOperationException(
            "Route provider returned null.");

    var routeSnapshot =
        new List<CopyDistributionRoute>(
            availableRoutes);

    string batchId =
        "AUTO-COPY:" +
        _nodeId + ":" +
        (++_batchSequence);

    var shipments = _distribution.DispatchAll(
        batchId,
        _nodeId,
        sourceCopy,
        routeSnapshot,
        _countryForNode(_nodeId),
        _replyAllowanceDays,
        recipientId =>
            _stances.HasCommunicatedDislike(
                _nodeId,
                recipientId,
                sourceCopy.ProductId),
        recipientId =>
            _canOffer(_nodeId, recipientId));

    foreach (var shipment in shipments)
    {
        _dispatched.Add(shipment);
        _onDispatched?.Invoke(shipment);
    }
}


        private void ValidateNode(string nodeId)
        {
            if (!string.Equals(
                    nodeId,
                    _nodeId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Policy was invoked for the wrong node.",
                    nameof(nodeId));
            }
        }
    }
}
