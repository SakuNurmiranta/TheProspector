
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class ZineContactCandidate
    {
        public string TargetNodeId { get; }
        public string AddressId { get; }

        internal ZineContactCandidate(
            string targetNodeId,
            string addressId)
        {
            TargetNodeId = targetNodeId;
            AddressId = addressId;
        }
    }

    /// <summary>
    /// Uusien henkilökohtaisten kontaktien
    /// valintapolitiikka.
    ///
    /// Käyttää zinessä julkaistua, solmun
    /// itsensä ilmoittamaa julkista profiilia.
    ///
    /// Ei lähetä postia eikä luo yhteyksiä.
    /// </summary>
    public sealed class ZineContactOutreachPolicy
    {
        private readonly Network _network;

        private readonly ZineIssueKnowledgeRegistry
            _knowledge;

        private readonly Dictionary<string, string>
            _pending =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

        public ZineContactOutreachPolicy(
            Network network,
            ZineIssueKnowledgeRegistry knowledge)
        {
            _network = network ??
                throw new ArgumentNullException(
                    nameof(network));

            _knowledge = knowledge ??
                throw new ArgumentNullException(
                    nameof(knowledge));
        }

        public IReadOnlyList<ZineContactCandidate>
            GetCandidates(
                string senderNodeId,
                bool senderPublicProfile)
        {
            ValidateId(senderNodeId, nameof(senderNodeId));

            if (!_network.TryGetNode(senderNodeId, out _))
                throw new ArgumentException(
                    "Unknown sender node.",
                    nameof(senderNodeId));

            // Samasta henkilöstä saatetaan saada
            // useita eri numeroiden tietoja.
            // Käytetään uusinta julkaisupäivää.
            var newest = new Dictionary<
                string, ZineKnowledgeClaim>(
                StringComparer.Ordinal);

            foreach (var claim in
                     _knowledge.GetClaims(senderNodeId))
            {
                var entry = claim.Entry;

                if (entry.Kind !=
                    ZineEditorialEntryKind.Address)
                    continue;

                string targetId = entry.SubjectNodeId;

                if (targetId == senderNodeId)
                    continue;

                if (!newest.TryGetValue(
                        targetId, out var previous) ||
                    claim.PublishedDay >=
                        previous.PublishedDay)
                {
                    newest[targetId] = claim;
                }
            }

            var existingContacts =
                new HashSet<string>(
                    StringComparer.Ordinal);

            foreach (var connection in
                     _network.GetOutgoingConnections(
                         senderNodeId,
                         CommunicationLayer.Interpersonal))
            {
                existingContacts.Add(
                    connection.TargetNodeId);
            }

            var candidates =
                new List<ZineContactCandidate>();

            foreach (var claim in newest.Values)
            {
                var entry = claim.Entry;

                // Ilman itse ilmoitettua profiilia
                // kohdetta ei vielä valita.
                if (!entry.SelfDeclaredPublicProfile.HasValue ||
                    entry.SelfDeclaredPublicProfile.Value !=
                        senderPublicProfile)
                {
                    continue;
                }

                string targetId = entry.SubjectNodeId;

                if (!_network.TryGetNode(targetId, out _))
                    continue;

                if (existingContacts.Contains(targetId))
                    continue;

                if (!_network.TryGetKnownAddress(
                        senderNodeId,
                        targetId,
                        out var knownAddress))
                {
                    continue;
                }

                // Jos osoitekirja ja julkaistu
                // profiilitieto eivät enää viittaa
                // samaan osoitetietoon, emme tee
                // automaattista kontaktivalintaa.
                if (!string.Equals(
                        knownAddress,
                        entry.AddressId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                candidates.Add(
                    new ZineContactCandidate(
                        targetId,
                        knownAddress));
            }

            // Deterministinen järjestys testeihin.
            candidates.Sort((a, b) =>
                string.CompareOrdinal(
                    a.TargetNodeId,
                    b.TargetNodeId));

            return candidates.AsReadOnly();
        }

        public bool TryStartOutreach(
            string senderNodeId,
            bool senderPublicProfile,
            string targetNodeId)
        {
            ValidateId(senderNodeId, nameof(senderNodeId));
            ValidateId(targetNodeId, nameof(targetNodeId));

            if (_pending.ContainsKey(senderNodeId))
                return false;

            foreach (var candidate in GetCandidates(
                         senderNodeId,
                         senderPublicProfile))
            {
                if (candidate.TargetNodeId != targetNodeId)
                    continue;

                _pending.Add(senderNodeId, targetNodeId);
                return true;
            }

            return false;
        }

        public bool HasPendingOutreach(string senderNodeId)
        {
            ValidateId(senderNodeId, nameof(senderNodeId));

            return _pending.ContainsKey(senderNodeId);
        }

        public bool CompleteOutreach(
            string senderNodeId,
            string targetNodeId)
        {
            ValidateId(senderNodeId, nameof(senderNodeId));
            ValidateId(targetNodeId, nameof(targetNodeId));

            if (!_pending.TryGetValue(
                    senderNodeId, out var pendingTarget))
                return false;

            if (pendingTarget != targetNodeId)
                return false;

            _pending.Remove(senderNodeId);
            return true;
        }

        private static void ValidateId(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "Identifier cannot be empty.",
                    parameterName);
        }
    }
}
