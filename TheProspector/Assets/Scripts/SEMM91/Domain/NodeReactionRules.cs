
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Solmu- ja kulttuurituotekohtaiset
    /// kuuntelureaktiot.
    ///
    /// Tämä on konfiguroitava päätössäännöstö,
    /// ei esteettisen arvioinnin toteutus.
    /// </summary>
    public sealed class NodeReactionRules
    {
        private readonly Dictionary<
            (string NodeId, string ProductId),
            ListeningReaction> _rules =
                new Dictionary<
                    (string NodeId, string ProductId),
                    ListeningReaction>();

        public ListeningReaction DefaultReaction { get; }

        public int Count => _rules.Count;

        public NodeReactionRules(
            ListeningReaction defaultReaction =
                ListeningReaction.SupportAndCopy)
        {
            ValidateReaction(defaultReaction);
            DefaultReaction = defaultReaction;
        }

        public void Set(
            string nodeId,
            string productId,
            ListeningReaction reaction)
        {
            ValidateId(nodeId, nameof(nodeId));
            ValidateId(productId, nameof(productId));
            ValidateReaction(reaction);

            _rules[(nodeId, productId)] = reaction;
        }

        public ListeningReaction Choose(
            string nodeId,
            string productId)
        {
            ValidateId(nodeId, nameof(nodeId));
            ValidateId(productId, nameof(productId));

            return _rules.TryGetValue(
                (nodeId, productId),
                out var reaction)
                ? reaction
                : DefaultReaction;
        }

        private static void ValidateReaction(
            ListeningReaction reaction)
        {
            if (!Enum.IsDefined(
                    typeof(ListeningReaction),
                    reaction))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reaction));
            }
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
