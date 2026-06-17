using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Gestation.Questing
{
    /// <summary>
    /// Immutable factual context shared with every contribution
    /// provider participating in one composite build.
    /// </summary>
    public sealed class QuestingCompositeContext
    {
        public string CharacterId { get; }
        public TagAxis ActiveAxis { get; }
        public int CurrentGlobalTurn { get; }

        public QuestingCompositeContext(
            string characterId,
            TagAxis activeAxis,
            int currentGlobalTurn)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException(
                    "Questing character ID is required.",
                    nameof(characterId)
                );
            }

            if (currentGlobalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentGlobalTurn),
                    currentGlobalTurn,
                    "Global turn cannot be negative."
                );
            }

            CharacterId = characterId;
            ActiveAxis = activeAxis;
            CurrentGlobalTurn = currentGlobalTurn;
        }
    }

    /// <summary>
    /// Source-specific interpreter that converts factual domain
    /// state into signed Questing composite contributions.
    ///
    /// Implementations may later read action history, world events,
    /// scene state, stress, or other authoritative sources.
    /// </summary>
    public interface IQuestingCompositeContributionProvider
    {
        IReadOnlyList<QuestingCompositeContribution>
            BuildContributions(
                QuestingCompositeContext context);
    }

    /// <summary>
    /// Deterministically gathers source-specific contributions and
    /// delegates numeric aggregation to QuestingCompositeBuilder.
    /// </summary>
    public sealed class QuestingCompositeAssembler
    {
        private readonly QuestingCompositeBuilder _builder;

        private readonly IReadOnlyList
            <IQuestingCompositeContributionProvider> _providers;

        public QuestingCompositeAssembler(
            QuestingCompositeBuilder builder,
            IEnumerable<IQuestingCompositeContributionProvider>
                providers)
        {
            _builder = builder ??
                throw new ArgumentNullException(nameof(builder));

            if (providers == null)
            {
                throw new ArgumentNullException(
                    nameof(providers)
                );
            }

            List<IQuestingCompositeContributionProvider>
                providerList = new();

            foreach (
                IQuestingCompositeContributionProvider provider
                in providers)
            {
                if (provider == null)
                {
                    throw new ArgumentException(
                        "Contribution provider collection " +
                        "contains a null entry.",
                        nameof(providers)
                    );
                }

                providerList.Add(provider);
            }

            _providers = providerList.ToArray();
        }

        public QuestingCompositeBuildResult Build(
            QuestingCompositeContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context)
                );
            }

            List<QuestingCompositeContribution>
                contributions = new();

            foreach (
                IQuestingCompositeContributionProvider provider
                in _providers)
            {
                IReadOnlyList<QuestingCompositeContribution>
                    providerContributions =
                        provider.BuildContributions(context);

                if (providerContributions == null)
                {
                    throw new InvalidOperationException(
                        $"Contribution provider " +
                        $"{provider.GetType().Name} returned null."
                    );
                }

                for (int index = 0;
                     index < providerContributions.Count;
                     index++)
                {
                    QuestingCompositeContribution contribution =
                        providerContributions[index];

                    if (contribution == null)
                    {
                        throw new InvalidOperationException(
                            $"Contribution provider " +
                            $"{provider.GetType().Name} returned " +
                            $"a null contribution at index {index}."
                        );
                    }

                    contributions.Add(contribution);
                }
            }

            return _builder.Build(
                context.ActiveAxis,
                contributions
            );
        }
    }
}