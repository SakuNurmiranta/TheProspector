using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Immutable complete Canon Assimilation plan for
    /// one post-movement Canon candidate.
    ///
    /// The plan is calculated entirely from Canon_t
    /// before any authoritative pair activation changes.
    /// </summary>
    public sealed class
        SceneReleaseCanonAssimilationEvaluation
    {
        private readonly
            SceneReleaseCanonAssimilationPairEvaluation[]
            pairEvaluations;

        public SceneReleaseNexusBoundaryEvaluation
            NexusEvaluation { get; }

        public string SceneReleaseId =>
            NexusEvaluation.SceneReleaseId;

        public string SourceDemoTapeId =>
            NexusEvaluation.SourceDemoTapeId;

        public string SourceOwnerEntityId =>
            NexusEvaluation.SourceOwnerEntityId;

        public string SceneId =>
            NexusEvaluation.SceneId;

        public int SettledTurn =>
            NexusEvaluation.SettledTurn;

        public IReadOnlyList<
                SceneReleaseCanonAssimilationPairEvaluation>
            PairEvaluations =>
            pairEvaluations;

        public int RaisedPairCount { get; }

        public bool HasActivationChanges =>
            RaisedPairCount > 0;

        public SceneReleaseCanonAssimilationEvaluation(
            SceneReleaseNexusBoundaryEvaluation
                nexusEvaluation,
            IReadOnlyList<
                SceneReleaseCanonAssimilationPairEvaluation>
                sourcePairEvaluations)
        {
            NexusEvaluation =
                nexusEvaluation ??
                throw new ArgumentNullException(
                    nameof(nexusEvaluation)
                );

            if (!nexusEvaluation.IsCanonCandidate)
            {
                throw new ArgumentException(
                    "Canon Assimilation requires a " +
                    "Canon candidate.",
                    nameof(nexusEvaluation)
                );
            }

            if (sourcePairEvaluations == null)
            {
                throw new ArgumentNullException(
                    nameof(sourcePairEvaluations)
                );
            }

            pairEvaluations =
                new
                    SceneReleaseCanonAssimilationPairEvaluation[
                        sourcePairEvaluations.Count
                    ];

            HashSet<
                (
                    string TrackId,
                    string IdeaId,
                    int IdeaIndex
                )>
                seen =
                    new();

            int raised =
                0;

            for (int index = 0;
                 index < sourcePairEvaluations.Count;
                 index++)
            {
                SceneReleaseCanonAssimilationPairEvaluation
                    pair =
                        sourcePairEvaluations[index] ??
                        throw new ArgumentException(
                            "Canon Assimilation cannot " +
                            "contain null pair evaluation.",
                            nameof(sourcePairEvaluations)
                        );

                if (pair.SceneReleaseId !=
                        SceneReleaseId ||
                    pair.SourceDemoTapeId !=
                        SourceDemoTapeId ||
                    pair.SourceOwnerEntityId !=
                        SourceOwnerEntityId ||
                    pair.SceneId !=
                        SceneId ||
                    pair.SettledTurn !=
                        SettledTurn)
                {
                    throw new ArgumentException(
                        "Canon Assimilation pair " +
                        "provenance does not match the " +
                        "Canon candidate.",
                        nameof(sourcePairEvaluations)
                    );
                }

                var key =
                    (
                        pair.SourceTrackId,
                        pair.SourceIdeaId,
                        pair.IdeaIndex
                    );

                if (!seen.Add(key))
                {
                    throw new ArgumentException(
                        "Canon Assimilation contains a " +
                        "duplicate formal pair.",
                        nameof(sourcePairEvaluations)
                    );
                }

                pairEvaluations[index] =
                    pair;

                if (pair.RaisesActivation)
                {
                    raised++;
                }
            }

            Array.Sort(
                pairEvaluations,
                ComparePairs
            );

            RaisedPairCount =
                raised;
        }

        private static int ComparePairs(
            SceneReleaseCanonAssimilationPairEvaluation left,
            SceneReleaseCanonAssimilationPairEvaluation right)
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
    }
}