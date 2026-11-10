using System;

namespace SEMM91.Domain
{
    public sealed partial class ConfigurableCirculationScenario
    {
        private bool _localListeningActivated;

        /// <summary>
        /// Enables local listening for original cassettes.
        /// Repeated activation is idempotent.
        /// </summary>
        public void ActivateLocalListening()
        {
            if (_localListeningActivated)
                return;

            foreach (string productId in ProductIds)
            {
                CassetteCopy original =
                    _originals[productId];

                _listeningPolicies[original.HolderNodeId]
                    .StartLocalListening(
                        original,
                        Clock.CurrentDay);
            }

            _localListeningActivated = true;
        }

        /// <summary>
        /// Registers an original created after initialization,
        /// provided local listening has been activated.
        /// </summary>
        private void RegisterLocalOriginalIfEnabled(
            CassetteCopy copy)
        {
            if (!_localListeningActivated)
                return;

            if (copy == null)
                throw new ArgumentNullException(nameof(copy));

            _listeningPolicies[copy.HolderNodeId]
                .StartLocalListening(
                    copy,
                    Clock.CurrentDay);
        }
    }
}