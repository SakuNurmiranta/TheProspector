
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Solmun toimintapolitiikka.
    ///
    /// Käynnistyy päivittäin ja silloin,
    /// kun solmu avaa vastaanottamansa viestin.
    ///
    /// Päätöksenteko on erillään simulaatiokellosta.
    /// </summary>
    public interface ISimulationNodePolicy
    {
        void OnDay(
            string nodeId,
            int day);

        void OnMessageOpened(
            string nodeId,
            BacklogItem message,
            int capitalAward,
            int day);
    }

    /// <summary>
    /// Yhden avatun viestin havaintotietue.
    /// </summary>
    public sealed class WorldOpenedMessage
    {
        public int Day { get; }
        public string NodeId { get; }
        public string PackageId { get; }
        public string ShipmentId { get; }
        public int CapitalAward { get; }

        internal WorldOpenedMessage(
            int day,
            string nodeId,
            BacklogItem message,
            int capitalAward)
        {
            Day = day;
            NodeId = nodeId;
            PackageId = message.Package.Id;
            ShipmentId = message.ShipmentId;
            CapitalAward = capitalAward;
        }
    }

    /// <summary>
    /// SEMM91:n autonomisen simulaation
    /// päivittäinen suoritusympäristö.
    ///
    /// Ei sisällä skenaarioon kovakoodattuja
    /// tapahtumapäiviä tai solmujen päätöksiä.
    /// </summary>
    public sealed class SimulationWorld
    {
        private sealed class RegisteredNode
        {
            public string Id { get; }
            public ISimulationNodePolicy Policy { get; }

            public RegisteredNode(
                string id,
                ISimulationNodePolicy policy)
            {
                Id = id;
                Policy = policy;
            }
        }

        private readonly SimulationClock _clock;
        private readonly Network _network;
        private readonly TimedPostalExchange _exchange;

        private readonly List<RegisteredNode> _nodes =
            new List<RegisteredNode>();

        private readonly HashSet<string> _registeredIds =
            new HashSet<string>(
                StringComparer.Ordinal);

        private readonly List<WorldOpenedMessage>
            _openedMessages =
                new List<WorldOpenedMessage>();

        private readonly IReadOnlyList<WorldOpenedMessage>
            _openedMessagesView;

        private bool _advancing;

        public int CurrentDay => _clock.CurrentDay;

        public int RegisteredNodeCount => _nodes.Count;

        public IReadOnlyList<WorldOpenedMessage>
            OpenedMessages => _openedMessagesView;

        public SimulationWorld(
            SimulationClock clock,
            Network network,
            TimedPostalExchange exchange)
        {
            _clock = clock ??
                throw new ArgumentNullException(
                    nameof(clock));

            _network = network ??
                throw new ArgumentNullException(
                    nameof(network));

            _exchange = exchange ??
                throw new ArgumentNullException(
                    nameof(exchange));

            _openedMessagesView =
                _openedMessages.AsReadOnly();
        }

        public void RegisterNode(
            string nodeId,
            ISimulationNodePolicy policy = null)
        {
            if (_advancing)
                throw new InvalidOperationException(
                    "Cannot register nodes during a day.");

            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException(
                    "Node ID cannot be empty.",
                    nameof(nodeId));

            if (!_network.TryGetNode(nodeId, out _))
                throw new ArgumentException(
                    "Node does not exist in the network.",
                    nameof(nodeId));

            if (!_registeredIds.Add(nodeId))
                throw new ArgumentException(
                    "Node is already registered.",
                    nameof(nodeId));

            // Rekisteröintijärjestys on myös
            // päivittäinen käsittelyjärjestys.
            _nodes.Add(
                new RegisteredNode(nodeId, policy));
        }

        public void AdvanceDay()
        {
            if (_advancing)
                throw new InvalidOperationException(
                    "World advancement is already running.");

            _advancing = true;

            try
            {
                // 1. Aika etenee yhden päivän.
                //
                // Kaikki päivälle ajastettu posti
                // ja muut tapahtumat suoritetaan.
                _clock.AdvanceDays(1);

                int day = _clock.CurrentDay;

                // 2. Solmut käsittelevät päivän.
                foreach (var node in _nodes)
                {
                    // Ensimmäinen autonominen
                    // käsittelysääntö:
                    //
                    // Yksi backlog-viesti
                    // per solmu per päivä.
                    if (_exchange.TryOpenNext(
                            node.Id,
                            out int reward,
                            out BacklogItem opened))
                    {
                        // Viesti on jo avattu ja sen
                        // vastaanottovaikutukset kirjattu.
                        var observation =
                            new WorldOpenedMessage(
                                day,
                                node.Id,
                                opened,
                                reward);

                        _openedMessages.Add(observation);

                        // 3. Solmun toimintapolitiikka
                        // saa tiedon avatusta viestistä.
                        //
                        // Se voi käynnistää uusia
                        // lähetyksiä ja päätöksiä.
                        node.Policy?.OnMessageOpened(
                            node.Id,
                            opened,
                            reward,
                            day);
                    }

                    // 4. Päivittäinen toimintapäätös
                    // suoritetaan myös silloin, kun
                    // solmulla ei ole avattavaa postia.
                    node.Policy?.OnDay(
                        node.Id,
                        day);
                }
            }
            finally
            {
                _advancing = false;
            }
        }

        public void AdvanceDays(int days)
        {
            if (days < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(days));

            if (_advancing)
                throw new InvalidOperationException(
                    "World advancement is already running.");

            for (int i = 0; i < days; i++)
            {
                AdvanceDay();
            }
        }
    }
}
