using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Events;

namespace SEMM91.GamePlay.Gestation.Questing
{
    /// <summary>
    /// Adapts authoritative world-event axis signals into
    /// character-specific Questing composite contributions.
    ///
    /// The event already carries its signed semantic signal.
    /// This provider performs no additional weighting.
    /// </summary>
    public sealed class WorldEventQuestingContributionProvider :
        IQuestingCompositeContributionProvider
    {
        private readonly WorldEventRegistry _registry;

        public WorldEventQuestingContributionProvider(
            WorldEventRegistry registry)
        {
            _registry = registry ??
                throw new ArgumentNullException(nameof(registry));
        }

        public IReadOnlyList<QuestingCompositeContribution>
            BuildContributions(
                QuestingCompositeContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context)
                );
            }

            IReadOnlyList<WorldEventRecord> events =
                _registry.GetRecent(context.CharacterId);

            List<QuestingCompositeContribution>
                contributions = new();

            int oldestRetainedTurn =
                Math.Max(
                    0,
                    context.CurrentGlobalTurn -
                    WorldEventRegistry.RetainedTurnCount +
                    1
                );

            foreach (WorldEventRecord worldEvent in events)
            {
                if (worldEvent.GlobalTurn < oldestRetainedTurn)
                    continue;

                if (worldEvent.GlobalTurn >
                    context.CurrentGlobalTurn)
                {
                    continue;
                }

                for (int signalIndex = 0;
                     signalIndex < worldEvent.AxisSignals.Count;
                     signalIndex++)
                {
                    WorldEventAxisSignal signal =
                        worldEvent.AxisSignals[signalIndex];

                    if (signal.Axis != context.ActiveAxis)
                        continue;

                    string description =
                        string.IsNullOrWhiteSpace(
                            signal.Description)
                            ? $"World event {worldEvent.EventId}"
                            : signal.Description;

                    contributions.Add(
                        new QuestingCompositeContribution(
                            sourceId:
                                $"world-event:" +
                                $"{worldEvent.EventId}:" +
                                $"signal:{signalIndex}",
                            axis: signal.Axis,
                            value: signal.SignedIntensity,
                            description: description
                        )
                    );
                }
            }

            return contributions.ToArray();
        }
    }
}