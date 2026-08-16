using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Immutable claim-level decomposition of one
    /// canonized release's frozen post-assimilation
    /// Gravity.
    ///
    /// Every active pair participates in the complete
    /// decomposition. Only actual frontier claims are
    /// separately retained for later institutional
    /// payout.
    /// </summary>
    public sealed class
        SceneReleaseCanonGravityDecompositionState
    {
        private const float Tolerance =
            0.0001f;

        private readonly
            SceneReleaseActivePairGravityContribution[]
            activePairContributions;

        private readonly
            SceneReleaseCanonFrontierGravityContribution[]
            frontierContributions;

        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int CanonizedTurn { get; }

        public string KeeperTenureId { get; }

        public float
            FrozenPostAssimilationGravity { get; }

        public IReadOnlyList<
                SceneReleaseActivePairGravityContribution>
            ActivePairContributions =>
            activePairContributions;

        public IReadOnlyList<
                SceneReleaseCanonFrontierGravityContribution>
            FrontierContributions =>
            frontierContributions;

        public float
            ActivePairContributionTotal { get; }

        public float
            FrontierContributionTotal { get; }

        public SceneReleaseCanonGravityDecompositionState(
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            int canonizedTurn,
            string keeperTenureId,
            float frozenPostAssimilationGravity,
            IReadOnlyList<
                SceneReleaseActivePairGravityContribution>
                sourceActivePairContributions,
            IReadOnlyList<
                SceneReleaseCanonFrontierGravityContribution>
                sourceFrontierContributions)
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

            KeeperTenureId =
                RequireText(
                    keeperTenureId,
                    nameof(keeperTenureId)
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

            CanonizedTurn =
                canonizedTurn;

            FrozenPostAssimilationGravity =
                frozenPostAssimilationGravity;
            
            if (sourceActivePairContributions == null)
            {
                throw new ArgumentNullException(
                    nameof(
                        sourceActivePairContributions
                    )
                );
            }

            if (sourceFrontierContributions == null)
            {
                throw new ArgumentNullException(
                    nameof(
                        sourceFrontierContributions
                    )
                );
            }

            activePairContributions =
                new
                    SceneReleaseActivePairGravityContribution[
                        sourceActivePairContributions.Count
                    ];

            HashSet<
                (
                    string TrackId,
                    string IdeaId,
                    int IdeaIndex
                )>
                activeKeys =
                    new();

            float activeTotal =
                0f;

            for (int index = 0;
                 index <
                 sourceActivePairContributions.Count;
                 index++)
            {
                SceneReleaseActivePairGravityContribution
                    contribution =
                        sourceActivePairContributions[index] ??
                        throw new ArgumentException(
                            "Active-pair Gravity " +
                            "decomposition cannot contain " +
                            "null.",
                            nameof(
                                sourceActivePairContributions
                            )
                        );

                ValidateContributionIdentity(
                    contribution
                );

                var key =
                    (
                        contribution.SourceTrackId,
                        contribution.SourceIdeaId,
                        contribution.IdeaIndex
                    );

                if (!activeKeys.Add(key))
                {
                    throw new ArgumentException(
                        "Active-pair Gravity " +
                        "decomposition contains duplicate " +
                        "pair identity.",
                        nameof(
                            sourceActivePairContributions
                        )
                    );
                }

                activePairContributions[index] =
                    contribution;

                activeTotal +=
                    contribution
                        .ClaimReleaseGravityContribution;
            }

            if (Math.Abs(
                    activeTotal -
                    frozenPostAssimilationGravity) >
                Tolerance)
            {
                throw new ArgumentException(
                    "Active pair claim contributions " +
                    "do not sum to frozen G_release.",
                    nameof(
                        sourceActivePairContributions
                    )
                );
            }

            frontierContributions =
                new
                    SceneReleaseCanonFrontierGravityContribution[
                        sourceFrontierContributions.Count
                    ];

            HashSet<
                (
                    string TrackId,
                    string IdeaId,
                    int IdeaIndex
                )>
                frontierKeys =
                    new();

            float frontierTotal =
                0f;

            for (int index = 0;
                 index <
                 sourceFrontierContributions.Count;
                 index++)
            {
                SceneReleaseCanonFrontierGravityContribution
                    contribution =
                        sourceFrontierContributions[index] ??
                        throw new ArgumentException(
                            "Frontier Gravity " +
                            "decomposition cannot contain " +
                            "null.",
                            nameof(
                                sourceFrontierContributions
                            )
                        );

                SceneReleaseActivePairGravityContribution
                    pair =
                        contribution.PairContribution;

                ValidateContributionIdentity(
                    pair
                );

                var key =
                    (
                        contribution.SourceTrackId,
                        contribution.SourceIdeaId,
                        contribution.IdeaIndex
                    );

                if (!activeKeys.Contains(key))
                {
                    throw new ArgumentException(
                        "Frontier Gravity contribution " +
                        "has no matching active pair.",
                        nameof(
                            sourceFrontierContributions
                        )
                    );
                }

                if (!frontierKeys.Add(key))
                {
                    throw new ArgumentException(
                        "Frontier Gravity decomposition " +
                        "contains duplicate pair identity.",
                        nameof(
                            sourceFrontierContributions
                        )
                    );
                }

                frontierContributions[index] =
                    contribution;

                frontierTotal +=
                    contribution
                        .ClaimReleaseGravityContribution;
            }

            Array.Sort(
                activePairContributions,
                CompareActive
            );

            Array.Sort(
                frontierContributions,
                CompareFrontier
            );

            ActivePairContributionTotal =
                activeTotal;

            FrontierContributionTotal =
                frontierTotal;
        }

        public bool TryGetActivePairContribution(
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex,
            out
                SceneReleaseActivePairGravityContribution
                contribution)
        {
            contribution =
                null;

            foreach (
                SceneReleaseActivePairGravityContribution
                    candidate
                in activePairContributions)
            {
                if (candidate.SourceTrackId ==
                        sourceTrackId &&
                    candidate.SourceIdeaId ==
                        sourceIdeaId &&
                    candidate.IdeaIndex ==
                        ideaIndex)
                {
                    contribution =
                        candidate;

                    return true;
                }
            }

            return false;
        }

        public bool TryGetFrontierContribution(
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex,
            out
                SceneReleaseCanonFrontierGravityContribution
                contribution)
        {
            contribution =
                null;

            foreach (
                SceneReleaseCanonFrontierGravityContribution
                    candidate
                in frontierContributions)
            {
                if (candidate.SourceTrackId ==
                        sourceTrackId &&
                    candidate.SourceIdeaId ==
                        sourceIdeaId &&
                    candidate.IdeaIndex ==
                        ideaIndex)
                {
                    contribution =
                        candidate;

                    return true;
                }
            }

            return false;
        }

        private void ValidateContributionIdentity(
            SceneReleaseActivePairGravityContribution
                contribution)
        {
            if (contribution.SceneReleaseId !=
                    SceneReleaseId ||
                contribution.SourceDemoTapeId !=
                    SourceDemoTapeId ||
                contribution.SourceOwnerEntityId !=
                    SourceOwnerEntityId ||
                contribution.SceneId !=
                    SceneId ||
                contribution.CanonizedTurn !=
                    CanonizedTurn)
            {
                throw new ArgumentException(
                    "Claim Gravity contribution belongs " +
                    "to another canonized release."
                );
            }
        }

        private static int CompareActive(
            SceneReleaseActivePairGravityContribution left,
            SceneReleaseActivePairGravityContribution right)
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

            int idea =
                left.IdeaIndex.CompareTo(
                    right.IdeaIndex
                );

            return idea != 0
                ? idea
                : string.CompareOrdinal(
                    left.SourceIdeaId,
                    right.SourceIdeaId
                );
        }

        private static int CompareFrontier(
            SceneReleaseCanonFrontierGravityContribution left,
            SceneReleaseCanonFrontierGravityContribution right)
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

            int idea =
                left.IdeaIndex.CompareTo(
                    right.IdeaIndex
                );

            return idea != 0
                ? idea
                : string.CompareOrdinal(
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
                    "Canon Gravity decomposition " +
                    "provenance cannot be empty.",
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