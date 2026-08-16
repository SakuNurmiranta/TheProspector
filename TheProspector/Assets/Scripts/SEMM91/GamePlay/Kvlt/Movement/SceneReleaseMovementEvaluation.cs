using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Immutable complete movement decomposition for
    /// one SceneRelease during one settlement.
    ///
    /// All components are calculated from frozen
    /// start-of-movement state before any authoritative
    /// SceneRelease position is mutated.
    /// </summary>
    public sealed class SceneReleaseMovementEvaluation
    {
        private readonly
            SceneReleaseMovementComponent[]
            components;

        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int SettledTurn { get; }

        public float StartFieldPosition { get; }

        public IReadOnlyList<
                SceneReleaseMovementComponent>
            Components =>
            components;

        public float TotalDelta { get; }

        public float ProjectedFieldPosition { get; }

        public SceneReleaseMovementEvaluation(
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            int settledTurn,
            float startFieldPosition,
            IReadOnlyList<
                SceneReleaseMovementComponent>
                sourceComponents)
        {
            SceneReleaseId =
                RequireText(
                    sceneReleaseId,
                    nameof(sceneReleaseId)
                );

            SourceDemoTapeId =
                RequireText(
                    sourceDemoTapeId,
                    nameof(sourceDemoTapeId)
                );

            SourceOwnerEntityId =
                RequireText(
                    sourceOwnerEntityId,
                    nameof(sourceOwnerEntityId)
                );

            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            if (!IsFinite(startFieldPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startFieldPosition)
                );
            }

            if (sourceComponents == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceComponents)
                );
            }

            if (sourceComponents.Count == 0)
            {
                throw new ArgumentException(
                    "Movement evaluation requires at " +
                    "least one resolved component.",
                    nameof(sourceComponents)
                );
            }

            SettledTurn =
                settledTurn;

            StartFieldPosition =
                startFieldPosition;

            components =
                new SceneReleaseMovementComponent[
                    sourceComponents.Count
                ];

            HashSet<
                SceneReleaseMovementComponentKind>
                seenKinds =
                    new();

            for (int index = 0;
                 index < sourceComponents.Count;
                 index++)
            {
                SceneReleaseMovementComponent component =
                    sourceComponents[index] ??
                    throw new ArgumentException(
                        "Movement evaluation cannot " +
                        "contain a null component.",
                        nameof(sourceComponents)
                    );

                if (component.SceneReleaseId !=
                    SceneReleaseId)
                {
                    throw new ArgumentException(
                        "Movement component belongs to " +
                        "a different SceneRelease.",
                        nameof(sourceComponents)
                    );
                }

                if (component.SceneId !=
                    SceneId)
                {
                    throw new ArgumentException(
                        "Movement component belongs to " +
                        "a different scene.",
                        nameof(sourceComponents)
                    );
                }

                if (component.SettledTurn !=
                    SettledTurn)
                {
                    throw new ArgumentException(
                        "Movement component belongs to " +
                        "a different settled turn.",
                        nameof(sourceComponents)
                    );
                }

                /*
                 * Each movement policy contributes
                 * exactly once to the final movement.
                 *
                 * This prevents accidentally applying
                 * Natural Drift twice, for example.
                 */
                if (!seenKinds.Add(
                        component.Kind))
                {
                    throw new ArgumentException(
                        "Movement evaluation contains " +
                        "duplicate component kind.",
                        nameof(sourceComponents)
                    );
                }

                components[index] =
                    component;
            }

            Array.Sort(
                components,
                (
                    left,
                    right
                ) =>
                {
                    int kindComparison =
                        left.Kind.CompareTo(
                            right.Kind
                        );

                    if (kindComparison != 0)
                    {
                        return kindComparison;
                    }

                    return string.CompareOrdinal(
                        left.SourceId,
                        right.SourceId
                    );
                }
            );

            double total =
                0d;

            foreach (
                SceneReleaseMovementComponent component
                in components)
            {
                total +=
                    component.Delta;
            }

            float totalDelta =
                (float)total;

            if (!IsFinite(totalDelta))
            {
                throw new InvalidOperationException(
                    "Composed movement delta is not finite."
                );
            }

            float projectedPosition =
                startFieldPosition +
                totalDelta;

            if (!IsFinite(projectedPosition))
            {
                throw new InvalidOperationException(
                    "Projected Field Position is not finite."
                );
            }

            TotalDelta =
                totalDelta;

            ProjectedFieldPosition =
                projectedPosition;
        }

        public bool TryGetComponent(
            SceneReleaseMovementComponentKind kind,
            out SceneReleaseMovementComponent component)
        {
            foreach (
                SceneReleaseMovementComponent candidate
                in components)
            {
                if (candidate.Kind ==
                    kind)
                {
                    component =
                        candidate;

                    return true;
                }
            }

            component =
                null;

            return false;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Movement evaluation provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }

        private static bool IsFinite(
            float value)
        {
            return
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}