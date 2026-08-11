using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Immutable complete Canon-breakthrough
    /// evaluation for one current Field SceneRelease.
    ///
    /// Claims remain separate deliberately.
    /// The design currently defines breakthrough
    /// differential per dominant claim but does not
    /// specify how multiple simultaneous claims combine
    /// into one movement magnitude.
    /// </summary>
    public sealed class
        SceneReleaseCanonBreakthroughEvaluation
    {
        private readonly
            SceneReleaseCanonBreakthroughClaim[]
            claims;

        private readonly
            SceneReleaseCanonBreakthroughClaim[]
            qualifyingClaims;

        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int SettledTurn { get; }

        public float StartFieldPosition { get; }

        public IReadOnlyList<
                SceneReleaseCanonBreakthroughClaim>
            Claims =>
            claims;

        public IReadOnlyList<
                SceneReleaseCanonBreakthroughClaim>
            QualifyingClaims =>
            qualifyingClaims;

        public bool HasQualifyingBreakthrough =>
            qualifyingClaims.Length > 0;

        public SceneReleaseCanonBreakthroughEvaluation(
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            int settledTurn,
            float startFieldPosition,
            IReadOnlyList<
                SceneReleaseCanonBreakthroughClaim>
                sourceClaims)
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

            if (float.IsNaN(startFieldPosition) ||
                float.IsInfinity(startFieldPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startFieldPosition)
                );
            }

            if (sourceClaims == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceClaims)
                );
            }

            SettledTurn =
                settledTurn;

            StartFieldPosition =
                startFieldPosition;

            claims =
                new SceneReleaseCanonBreakthroughClaim[
                    sourceClaims.Count
                ];

            List<
                SceneReleaseCanonBreakthroughClaim>
                qualifying =
                    new();

            HashSet<
                (
                    string TrackId,
                    string IdeaId,
                    int IdeaIndex
                )>
                seenClaims =
                    new();

            for (int index = 0;
                 index < sourceClaims.Count;
                 index++)
            {
                SceneReleaseCanonBreakthroughClaim claim =
                    sourceClaims[index] ??
                    throw new ArgumentException(
                        "Canon breakthrough evaluation " +
                        "cannot contain null claim.",
                        nameof(sourceClaims)
                    );

                if (claim.SceneReleaseId !=
                        SceneReleaseId ||
                    claim.SourceDemoTapeId !=
                        SourceDemoTapeId ||
                    claim.SourceOwnerEntityId !=
                        SourceOwnerEntityId ||
                    claim.SceneId !=
                        SceneId)
                {
                    throw new ArgumentException(
                        "Canon breakthrough claim " +
                        "provenance does not match release.",
                        nameof(sourceClaims)
                    );
                }

                if (claim.SettledTurn !=
                    SettledTurn)
                {
                    throw new ArgumentException(
                        "Canon breakthrough evaluation " +
                        "cannot mix settled turns.",
                        nameof(sourceClaims)
                    );
                }

                var key =
                    (
                        claim.SourceTrackId,
                        claim.SourceIdeaId,
                        claim.IdeaIndex
                    );

                if (!seenClaims.Add(key))
                {
                    throw new ArgumentException(
                        "Canon breakthrough evaluation " +
                        "contains duplicate pair claim.",
                        nameof(sourceClaims)
                    );
                }

                claims[index] =
                    claim;

                if (claim.IsQualifyingBreakthrough)
                {
                    qualifying.Add(
                        claim
                    );
                }
            }

            Array.Sort(
                claims,
                CompareClaims
            );

            qualifyingClaims =
                qualifying.ToArray();

            Array.Sort(
                qualifyingClaims,
                CompareClaims
            );
        }

        private static int CompareClaims(
            SceneReleaseCanonBreakthroughClaim left,
            SceneReleaseCanonBreakthroughClaim right)
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
                    "Canon breakthrough evaluation " +
                    "identity cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}