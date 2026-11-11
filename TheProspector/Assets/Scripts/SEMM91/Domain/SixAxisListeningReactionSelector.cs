using System;

namespace SEMM91.Domain
{
    public enum ListeningReactionSelectionSource
    {
        Legacy,
        DefaultRule,
        ExplicitRule,
        SixAxisProfile
    }

    public sealed class ListeningReactionSelection
    {
        public ListeningReaction Reaction { get; }
        public ListeningReactionSelectionSource Source { get; }
        public int? MatchingAxes { get; }

        public ListeningReactionSelection(
            ListeningReaction reaction,
            ListeningReactionSelectionSource source,
            int? matchingAxes = null)
        {
            Reaction = reaction;
            Source = source;
            MatchingAxes = matchingAxes;
        }
    }

    public sealed class SixAxisListeningReactionSelector
    {
        private readonly NodeReactionRules _rules;
        private readonly SixAxisProfileCatalog _catalog;

        public SixAxisListeningReactionSelector(
            NodeReactionRules rules,
            SixAxisProfileCatalog catalog = null)
        {
            _rules = rules ??
                throw new ArgumentNullException(nameof(rules));

            _catalog = catalog;
        }

        public static ListeningReaction FromMatchingAxes(
            int matches)
        {
            if (matches < 0 || matches > 6)
                throw new ArgumentOutOfRangeException(
                    nameof(matches));

            if (matches >= 4)
                return ListeningReaction.SupportAndCopy;

            if (matches >= 2)
                return ListeningReaction.OpposeAndTransfer;

            return ListeningReaction.OpposeAndShelve;
        }

        public ListeningReactionSelection Select(
            string nodeId,
            string productId)
        {
            // Explicit test and scenario settings win.
            if (_rules.TryGetExplicit(
                    nodeId, productId, out var explicitReaction))
            {
                return new ListeningReactionSelection(
                    explicitReaction,
                    ListeningReactionSelectionSource.ExplicitRule);
            }

            if (_catalog != null &&
                _catalog.TryGetProfile(
                    SixAxisProfileRole.PersonalPreference,
                    nodeId,
                    out var personal) &&
                _catalog.TryGetProfile(
                    SixAxisProfileRole.DemoAtmosphere,
                    productId,
                    out var demo))
            {
                int matches = personal.MatchingAxes(demo);

                return new ListeningReactionSelection(
                    FromMatchingAxes(matches),
                    ListeningReactionSelectionSource.SixAxisProfile,
                    matches);
            }

            return new ListeningReactionSelection(
                _rules.DefaultReaction,
                ListeningReactionSelectionSource.DefaultRule);
        }
    }
}