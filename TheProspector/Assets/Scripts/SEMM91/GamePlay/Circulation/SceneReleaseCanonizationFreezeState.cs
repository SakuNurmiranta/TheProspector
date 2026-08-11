using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Durable Peak-2 canonization state captured after
    /// NewCanon Assimilation and final legitimacy
    /// recalculation.
    ///
    /// This is distinct from the old SceneLegacyState
    /// canonization path.
    /// </summary>
    public sealed class
        SceneReleaseCanonizationFreezeState
    {
        private readonly
            SceneReleaseFrozenPairActivation[]
            frozenPairActivations;

        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int CanonizedTurn { get; }

        public string
            CanonizedUnderKeeperTenureId { get; }

        public float
            FrozenPostAssimilationGravity { get; }

        public IReadOnlyList<
                SceneReleaseFrozenPairActivation>
            FrozenPairActivations =>
            frozenPairActivations;

        public SceneReleaseCanonizationFreezeState(
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            int canonizedTurn,
            string canonizedUnderKeeperTenureId,
            float frozenPostAssimilationGravity,
            IReadOnlyList<
                SceneReleaseFrozenPairActivation>
                sourceFrozenPairActivations)
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

            CanonizedUnderKeeperTenureId =
                RequireText(
                    canonizedUnderKeeperTenureId,
                    nameof(
                        canonizedUnderKeeperTenureId
                    )
                );

            if (canonizedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(canonizedTurn)
                );
            }

            if (!IsFinite(
                    frozenPostAssimilationGravity) ||
                frozenPostAssimilationGravity < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(
                        frozenPostAssimilationGravity
                    )
                );
            }

            if (sourceFrozenPairActivations == null)
            {
                throw new ArgumentNullException(
                    nameof(
                        sourceFrozenPairActivations
                    )
                );
            }

            frozenPairActivations =
                new SceneReleaseFrozenPairActivation[
                    sourceFrozenPairActivations.Count
                ];

            HashSet<
                SceneReleasePairActivationKey>
                seen =
                    new();

            for (int index = 0;
                 index <
                 sourceFrozenPairActivations.Count;
                 index++)
            {
                SceneReleaseFrozenPairActivation
                    activation =
                        sourceFrozenPairActivations[index] ??
                        throw new ArgumentException(
                            "Frozen activation snapshot " +
                            "cannot contain null.",
                            nameof(
                                sourceFrozenPairActivations
                            )
                        );

                if (!seen.Add(
                        activation.Key))
                {
                    throw new ArgumentException(
                        "Frozen activation snapshot " +
                        "contains duplicate pair identity.",
                        nameof(
                            sourceFrozenPairActivations
                        )
                    );
                }

                frozenPairActivations[index] =
                    activation;
            }

            Array.Sort(
                frozenPairActivations,
                CompareActivations
            );

            CanonizedTurn =
                canonizedTurn;

            FrozenPostAssimilationGravity =
                frozenPostAssimilationGravity;
        }

        public bool TryGetFrozenPairActivation(
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex,
            out
                SceneReleaseFrozenPairActivation
                activation)
        {
            activation =
                null;

            if (string.IsNullOrWhiteSpace(
                    sourceTrackId) ||
                string.IsNullOrWhiteSpace(
                    sourceIdeaId) ||
                ideaIndex < 0)
            {
                return false;
            }

            foreach (
                SceneReleaseFrozenPairActivation candidate
                in frozenPairActivations)
            {
                if (candidate.SourceTrackId ==
                        sourceTrackId &&
                    candidate.SourceIdeaId ==
                        sourceIdeaId &&
                    candidate.IdeaIndex ==
                        ideaIndex)
                {
                    activation =
                        candidate;

                    return true;
                }
            }

            return false;
        }

        private static int CompareActivations(
            SceneReleaseFrozenPairActivation left,
            SceneReleaseFrozenPairActivation right)
        {
            int track =
                string.CompareOrdinal(
                    left.SourceTrackId,
                    right.SourceTrackId
                );

            if (track != 0)
            {
                return track;
            }

            int ideaIndex =
                left.IdeaIndex.CompareTo(
                    right.IdeaIndex
                );

            if (ideaIndex != 0)
            {
                return ideaIndex;
            }

            return string.CompareOrdinal(
                left.SourceIdeaId,
                right.SourceIdeaId
            );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Canonization freeze provenance " +
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