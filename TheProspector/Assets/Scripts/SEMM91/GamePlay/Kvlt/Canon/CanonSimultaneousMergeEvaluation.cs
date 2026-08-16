using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Immutable result of merging one settlement's
    /// successful Canon breakthroughs against the same
    /// previously settled Canon.
    ///
    /// Successful candidate identity is retained so
    /// later NewCanon Assimilation cannot be applied to
    /// a release that did not participate in this batch.
    /// </summary>
    public sealed class
        CanonSimultaneousMergeEvaluation
    {
        private readonly
            SceneReleaseCanonFrontierClaim[]
            frontierClaims;

        private readonly
            string[]
            successfulCandidateReleaseIds;

        private readonly
            HashSet<string>
            successfulCandidateLookup;

        public CanonState PreviousCanon { get; }

        public CanonState NewCanon { get; }

        public int SettledTurn { get; }

        public string KeeperTenureId { get; }

        public IReadOnlyList<string>
            SuccessfulCandidateReleaseIds =>
            successfulCandidateReleaseIds;

        public int SuccessfulCandidateCount =>
            successfulCandidateReleaseIds.Length;

        public int QualifyingBreakthroughClaimCount
        {
            get;
        }

        public IReadOnlyList<
                SceneReleaseCanonFrontierClaim>
            FrontierClaims =>
            frontierClaims;

        public int FrontierClaimCount =>
            frontierClaims.Length;

        public CanonSimultaneousMergeEvaluation(
            CanonState previousCanon,
            CanonState newCanon,
            int settledTurn,
            string keeperTenureId,
            IReadOnlyList<string>
                sourceSuccessfulCandidateReleaseIds,
            int qualifyingBreakthroughClaimCount,
            IReadOnlyList<
                SceneReleaseCanonFrontierClaim>
                sourceFrontierClaims)
        {
            PreviousCanon =
                previousCanon ??
                throw new ArgumentNullException(
                    nameof(previousCanon)
                );

            NewCanon =
                newCanon ??
                throw new ArgumentNullException(
                    nameof(newCanon)
                );

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    keeperTenureId))
            {
                throw new ArgumentException(
                    "Simultaneous Canon merge requires " +
                    "Keeper tenure identity.",
                    nameof(keeperTenureId)
                );
            }

            if (sourceSuccessfulCandidateReleaseIds ==
                null)
            {
                throw new ArgumentNullException(
                    nameof(
                        sourceSuccessfulCandidateReleaseIds
                    )
                );
            }

            if (sourceSuccessfulCandidateReleaseIds.Count ==
                0)
            {
                throw new ArgumentException(
                    "Canon merge requires at least one " +
                    "successful candidate.",
                    nameof(
                        sourceSuccessfulCandidateReleaseIds
                    )
                );
            }

            if (qualifyingBreakthroughClaimCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(
                        qualifyingBreakthroughClaimCount
                    )
                );
            }

            if (sourceFrontierClaims == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceFrontierClaims)
                );
            }

            if (sourceFrontierClaims.Count == 0)
            {
                throw new ArgumentException(
                    "A successful Canon merge must " +
                    "establish at least one frontier.",
                    nameof(sourceFrontierClaims)
                );
            }

            successfulCandidateReleaseIds =
                new string[
                    sourceSuccessfulCandidateReleaseIds
                        .Count
                ];

            successfulCandidateLookup =
                new HashSet<string>(
                    StringComparer.Ordinal
                );

            for (int index = 0;
                 index <
                 sourceSuccessfulCandidateReleaseIds.Count;
                 index++)
            {
                string releaseId =
                    sourceSuccessfulCandidateReleaseIds[
                        index
                    ];

                if (string.IsNullOrWhiteSpace(
                        releaseId))
                {
                    throw new ArgumentException(
                        "Successful Canon candidate " +
                        "identity cannot be empty.",
                        nameof(
                            sourceSuccessfulCandidateReleaseIds
                        )
                    );
                }

                releaseId =
                    releaseId.Trim();

                if (!successfulCandidateLookup.Add(
                        releaseId))
                {
                    throw new ArgumentException(
                        "Successful Canon candidate " +
                        "identity is duplicated.",
                        nameof(
                            sourceSuccessfulCandidateReleaseIds
                        )
                    );
                }

                successfulCandidateReleaseIds[index] =
                    releaseId;
            }

            Array.Sort(
                successfulCandidateReleaseIds,
                StringComparer.Ordinal
            );

            frontierClaims =
                new SceneReleaseCanonFrontierClaim[
                    sourceFrontierClaims.Count
                ];

            for (int index = 0;
                 index < sourceFrontierClaims.Count;
                 index++)
            {
                SceneReleaseCanonFrontierClaim claim =
                    sourceFrontierClaims[index] ??
                    throw new ArgumentException(
                        "Canon frontier cannot contain " +
                        "null claim.",
                        nameof(sourceFrontierClaims)
                    );

                if (claim.CanonizedTurn !=
                        settledTurn ||
                    claim.KeeperTenureId !=
                        keeperTenureId)
                {
                    throw new ArgumentException(
                        "Canon frontier claim belongs " +
                        "to another settlement or " +
                        "Keeper tenure.",
                        nameof(sourceFrontierClaims)
                    );
                }

                if (!successfulCandidateLookup.Contains(
                        claim.SceneReleaseId))
                {
                    throw new ArgumentException(
                        "Canon frontier claim belongs " +
                        "to a release outside the " +
                        "successful candidate batch.",
                        nameof(sourceFrontierClaims)
                    );
                }

                frontierClaims[index] =
                    claim;
            }

            SettledTurn =
                settledTurn;

            KeeperTenureId =
                keeperTenureId.Trim();

            QualifyingBreakthroughClaimCount =
                qualifyingBreakthroughClaimCount;
        }

        public bool ContainsSuccessfulCandidate(
            string sceneReleaseId)
        {
            if (string.IsNullOrWhiteSpace(
                    sceneReleaseId))
            {
                return false;
            }

            return successfulCandidateLookup.Contains(
                sceneReleaseId.Trim()
            );
        }
    }
}