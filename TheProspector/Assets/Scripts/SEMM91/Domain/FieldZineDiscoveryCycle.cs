
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Kytkee zinen kenttälöydöt simulaation
    /// päivittäiseen ajankulkuun.
    ///
    /// Kenttään tuotu numero arvotaan
    /// jokaiselle soveltuvalle lukijalle
    /// korkeintaan kerran.
    ///
    /// Ei luo henkilökohtaisia yhteyksiä
    /// eikä omaa rinnakkaista kenttärekisteriä.
    /// </summary>
    public sealed class FieldZineDiscoveryCycle : IDisposable
    {
        private readonly SimulationClock _clock;
        private readonly ZineEditorialCycle _editorial;
        private readonly FieldZineDiscovery _discovery;
        private readonly ZineIssueKnowledgeRegistry _knowledge;

        private readonly string _sourceNodeId;
        private readonly List<string> _readers;

        private readonly Func<string, bool> _readerPrefersTrue;

        private readonly Func<IReadOnlyDictionary<string, int>>
            _capitalByNode;

        private readonly Random _random;
        private readonly double _reach;

        private readonly HashSet<string> _introduced =
            new HashSet<string>(StringComparer.Ordinal);

        private readonly HashSet<(string Issue, string Reader)>
            _sampled =
                new HashSet<(string Issue, string Reader)>();

        private bool _disposed;

        public FieldZineDiscoveryCycle(
            SimulationClock clock,
            ZineEditorialCycle editorial,
            FieldZineDiscovery discovery,
            ZineIssueKnowledgeRegistry knowledge,
            string sourceNodeId,
            IEnumerable<string> readerNodeIds,
            Func<string, bool> readerPrefersTrue,
            Func<IReadOnlyDictionary<string, int>> capitalByNode,
            Random random,
            double reach)
        {
            _clock = clock ??
                throw new ArgumentNullException(nameof(clock));

            _editorial = editorial ??
                throw new ArgumentNullException(
                    nameof(editorial));

            _discovery = discovery ??
                throw new ArgumentNullException(
                    nameof(discovery));

            _knowledge = knowledge ??
                throw new ArgumentNullException(
                    nameof(knowledge));

            if (string.IsNullOrWhiteSpace(sourceNodeId))
                throw new ArgumentException(
                    "Source node is required.",
                    nameof(sourceNodeId));

            if (readerNodeIds == null)
                throw new ArgumentNullException(
                    nameof(readerNodeIds));

            _sourceNodeId = sourceNodeId;
            _readers = new List<string>(readerNodeIds);

            foreach (var reader in _readers)
            {
                if (string.IsNullOrWhiteSpace(reader))
                    throw new ArgumentException(
                        "Reader ID cannot be empty.",
                        nameof(readerNodeIds));
            }

            _readerPrefersTrue = readerPrefersTrue ??
                throw new ArgumentNullException(
                    nameof(readerPrefersTrue));

            _capitalByNode = capitalByNode ??
                throw new ArgumentNullException(
                    nameof(capitalByNode));

            _random = random ??
                throw new ArgumentNullException(nameof(random));

            if (double.IsNaN(reach) ||
                double.IsInfinity(reach) ||
                reach < 0.0 || reach > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reach));
            }

            _reach = reach;

            ScheduleNextDay();
        }

        private void ScheduleNextDay()
        {
            int nextDay = checked(_clock.CurrentDay + 1);

            _clock.ScheduleAfter(
                "FIELD-ZINE-SCAN:" +
                _editorial.ZineId + ":" +
                _sourceNodeId + ":" +
                nextDay,
                1,
                OnDay);
        }

        private void OnDay()
        {
            if (_disposed)
                return;

            foreach (var issue in _editorial.Issues)
            {
                // Lähteen on todella saatava numero.
                if (!_introduced.Contains(issue.ProductId))
                {
                    if (!_discovery.Introduce(
                            _sourceNodeId,
                            issue,
                            _reach))
                    {
                        continue;
                    }

                    _introduced.Add(issue.ProductId);
                }

                foreach (string reader in _readers)
                {
                    if (reader == _sourceNodeId)
                        continue;

                    if (_knowledge.HasReceived(
                            reader, issue.ProductId))
                    {
                        continue;
                    }

                    // Profiililtaan epäsopiva lukija ei
                    // kuluta tämän numeron otantaa.
                    if (_readerPrefersTrue(reader) != issue.Profile)
                        continue;

                    var key = (issue.ProductId, reader);

                    // Vain yksi yritys per numero ja lukija.
                    if (!_sampled.Add(key))
                        continue;

                    _discovery.TryDiscover(
                        reader,
                        issue.ProductId,
                        _readerPrefersTrue(reader),
                        _capitalByNode(),
                        _random);
                }
            }

            if (!_disposed)
                ScheduleNextDay();
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
