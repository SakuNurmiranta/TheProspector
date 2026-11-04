
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Tallentaa, mitä solmu on saanut tietää
    /// muiden solmujen viestimistä kannoista.
    ///
    /// Ei tallenna toimijoiden yksityisiä
    /// mielipiteitä eikä päätä, pitävätkö
    /// solmut kulttuurituotteista.
    /// </summary>
    public sealed class CommunicatedStanceRegistry
    {
        private readonly Dictionary<
            (string Observer, string Speaker, string Product),
            int> _lastThumb =
                new Dictionary<
                    (string Observer, string Speaker, string Product),
                    int>();

        public int Count => _lastThumb.Count;

        public void RecordOpened(MessagePackage package)
        {
            if (package == null)
                throw new ArgumentNullException(
                    nameof(package));

            if (string.IsNullOrWhiteSpace(
                    package.FeedbackProductId))
            {
                return;
            }

            // Vain eksplisiittinen positiivinen
            // tai negatiivinen palaute tallennetaan.
            if (package.Thumb != +1 &&
                package.Thumb != -1)
            {
                return;
            }

            var key = (
                package.TargetNodeId,
                package.SourceNodeId,
                package.FeedbackProductId);

            int stance = package.Thumb == -1 ? -1 : +1;

            // Uusin avattu kannanotto korvaa
            // saman henkilön aiemman viestityn
            // kannan kyseiselle vastaanottajalle.
            _lastThumb[key] = stance;
        }

        public int? GetLastThumb(
            string observerNodeId,
            string speakerNodeId,
            string productId)
        {
            ValidateId(observerNodeId,
                nameof(observerNodeId));

            ValidateId(speakerNodeId,
                nameof(speakerNodeId));

            ValidateId(productId,
                nameof(productId));

            return _lastThumb.TryGetValue(
                (observerNodeId, speakerNodeId, productId),
                out int thumb)
                ? thumb
                : (int?)null;
        }

        public bool HasCommunicatedDislike(
            string observerNodeId,
            string speakerNodeId,
            string productId)
        {
            return GetLastThumb(
                observerNodeId,
                speakerNodeId,
                productId) == -1;
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
