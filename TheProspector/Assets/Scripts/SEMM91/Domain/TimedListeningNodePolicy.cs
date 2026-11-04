
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public enum ListeningPhase
    {
        Queued,
        Listening,
        Completed
    }

    /// <summary>
    /// Muuttumaton havainto yhden solmun
    /// yhden demon kuuntelutilanteesta.
    /// </summary>
    public sealed class ListeningProgressSnapshot
    {
        public string ProductId { get; }
        public ListeningPhase Phase { get; }
        public float Progress { get; }
        public int? StartedDay { get; }
        public int? CompletedDay { get; }

        internal ListeningProgressSnapshot(
            string productId,
            ListeningPhase phase,
            float progress,
            int? startedDay,
            int? completedDay)
        {
            ProductId = productId;
            Phase = phase;
            Progress = progress;
            StartedDay = startedDay;
            CompletedDay = completedDay;
        }
    }

    /// <summary>
    /// Lisää ajallisen kuunteluvaiheen olemassa
    /// olevan solmupolitiikan eteen.
    ///
    /// Avattu kasettiviesti käynnistää kuuntelun.
    /// Varsinainen päätöksenteko siirtyy
    /// kuuntelun valmistumiseen.
    ///
    /// Palautteet, joissa ei ole fyysistä kasettia,
    /// välitetään suoraan sisäiselle politiikalle.
    /// </summary>
    public sealed class TimedListeningNodePolicy
        : ISimulationNodePolicy
    {
        private sealed class PendingListening
        {
            public BacklogItem Message;
            public string ProductId;
            public int CapitalAward;
            public int StartedDay;
        }

        private readonly string _nodeId;
        private readonly ISimulationNodePolicy _inner;

        private readonly Func<BacklogItem, CassetteCopy>
            _resolveCopy;

        private readonly int _durationDays;

        private readonly Queue<PendingListening> _queue =
            new Queue<PendingListening>();

        private readonly Dictionary<
            string, ListeningProgressSnapshot> _statuses =
                new Dictionary<
                    string, ListeningProgressSnapshot>(
                        StringComparer.Ordinal);

        private PendingListening _active;

        public TimedListeningNodePolicy(
            string nodeId,
            ISimulationNodePolicy inner,
            Func<BacklogItem, CassetteCopy> resolveCopy,
            int durationDays)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException(
                    "Node ID cannot be empty.",
                    nameof(nodeId));

            if (durationDays < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(durationDays),
                    "Listening duration must be positive.");

            _nodeId = nodeId;

            _inner = inner ??
                throw new ArgumentNullException(
                    nameof(inner));

            _resolveCopy = resolveCopy ??
                throw new ArgumentNullException(
                    nameof(resolveCopy));

            _durationDays = durationDays;
        }

        public ListeningProgressSnapshot GetStatus(
            string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException(
                    "Product ID cannot be empty.",
                    nameof(productId));

            return _statuses.TryGetValue(
                productId, out var status)
                ? status
                : null;
        }

        public void OnMessageOpened(
            string nodeId,
            BacklogItem message,
            int capitalAward,
            int day)
        {
            ValidateNode(nodeId);

            if (message == null)
                throw new ArgumentNullException(
                    nameof(message));

            CassetteCopy copy = _resolveCopy(message);

            // Pelkkää palautetta ei tarvitse
            // käsitellä fyysisenä kuuntelujonona.
            if (copy == null)
            {
                _inner.OnMessageOpened(
                    nodeId,
                    message,
                    capitalAward,
                    day);

                return;
            }

            string productId = copy.ProductId;

            // Sama demo kuunnellaan tässä
            // yksinkertaistetussa mallissa kerran.
            // Toistuva fyysinen kopio ei aloita
            // kuuntelua alusta.
            if (_statuses.ContainsKey(productId))
                return;

            _queue.Enqueue(new PendingListening
            {
                Message = message,
                ProductId = productId,
                CapitalAward = capitalAward
            });

            _statuses[productId] =
                new ListeningProgressSnapshot(
                    productId,
                    ListeningPhase.Queued,
                    0f,
                    null,
                    null);

            if (_active == null)
                StartNext(day);
        }

        public void OnDay(string nodeId, int day)
        {
            ValidateNode(nodeId);

            _inner.OnDay(nodeId, day);

            if (_active == null)
                return;

            int elapsed = Math.Max(
                0,
                day - _active.StartedDay);

            float progress = Math.Max(
                0f,
                Math.Min(
                    1f,
                    elapsed / (float)_durationDays));

            if (elapsed < _durationDays)
            {
                _statuses[_active.ProductId] =
                    new ListeningProgressSnapshot(
                        _active.ProductId,
                        ListeningPhase.Listening,
                        progress,
                        _active.StartedDay,
                        null);

                return;
            }

            var completed = _active;

            _statuses[completed.ProductId] =
                new ListeningProgressSnapshot(
                    completed.ProductId,
                    ListeningPhase.Completed,
                    1f,
                    completed.StartedDay,
                    day);

            _active = null;

            // Vasta kuuntelun päätyttyä
            // suoritetaan kopiointipolitiikka.
            _inner.OnMessageOpened(
                nodeId,
                completed.Message,
                completed.CapitalAward,
                day);

            // Seuraava odottava demo voidaan
            // ottaa käsittelyyn samana päivänä.
            // Sen edistyminen alkaa kuitenkin
            // nollasta.
            StartNext(day);
        }

        private void StartNext(int day)
        {
            if (_active != null || _queue.Count == 0)
                return;

            _active = _queue.Dequeue();
            _active.StartedDay = day;

            _statuses[_active.ProductId] =
                new ListeningProgressSnapshot(
                    _active.ProductId,
                    ListeningPhase.Listening,
                    0f,
                    day,
                    null);
        }

        private void ValidateNode(string nodeId)
        {
            if (!string.Equals(
                    nodeId, _nodeId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Listening policy invoked for wrong node.",
                    nameof(nodeId));
            }
        }
    }
}
