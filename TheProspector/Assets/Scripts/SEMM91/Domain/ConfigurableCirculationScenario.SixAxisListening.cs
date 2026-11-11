using System;

namespace SEMM91.Domain
{
    public sealed partial class ConfigurableCirculationScenario
    {
        private SixAxisListeningReactionSelector
            _listeningReactionSelector;

        public void EnableSixAxisListening(
            SixAxisProfileCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(
                    nameof(catalog));

            _listeningReactionSelector =
                new SixAxisListeningReactionSelector(
                    ReactionRules, catalog);
        }

        private ListeningReactionSelection SelectListeningReaction(
            string nodeId,
            string productId)
        {
            if (_listeningReactionSelector == null)
            {
                _listeningReactionSelector =
                    new SixAxisListeningReactionSelector(
                        ReactionRules);
            }

            return _listeningReactionSelector.Select(
                nodeId, productId);
        }
    }
}