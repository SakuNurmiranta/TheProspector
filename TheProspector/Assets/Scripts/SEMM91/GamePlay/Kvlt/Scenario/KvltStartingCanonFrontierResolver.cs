using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    /// <summary>
    /// Resolves Scenario-seeded Canon frontier
    /// provenance from the exact founding DemoTape,
    /// starting Canon and frozen CanonRetained release.
    ///
    /// This service creates provenance facts only.
    /// It performs no score or Gravity mutation.
    /// </summary>
    public sealed class
        KvltStartingCanonFrontierResolver
    {
        public IReadOnlyList<
                SceneReleaseCanonFrontierClaim>
            Resolve(
                SeededWorldState worldState,
                DemoTape demoTape,
                SceneRelease release,
                string canonicalActivatorPlayerId)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(
                    nameof(worldState)
                );
            }

            if (demoTape == null)
            {
                throw new ArgumentNullException(
                    nameof(demoTape)
                );
            }

            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    canonicalActivatorPlayerId))
            {
                throw new ArgumentException(
                    "Starting frontier requires player " +
                    "activator identity.",
                    nameof(canonicalActivatorPlayerId)
                );
            }

            if (!release.IsCanonized ||
                release.CanonizationFreezeState == null ||
                release.LifecycleState !=
                    SceneReleaseLifecycleState
                        .CanonRetained)
            {
                throw new InvalidOperationException(
                    "Starting frontier resolution " +
                    "requires a frozen CanonRetained " +
                    "release."
                );
            }

            if (release.SourceDemoTapeId !=
                demoTape.DemoTapeId)
            {
                throw new InvalidOperationException(
                    "Starting frontier DemoTape does not " +
                    "match SceneRelease."
                );
            }

            List<
                SceneReleaseCanonFrontierClaim>
                claims =
                    new();

            foreach (
                CanonPrecedentRecord canon
                in worldState.KvltCanon.Records)
            {
                /*
                 * Other starting or future Canon records
                 * are irrelevant to this artifact.
                 */
                if (canon.ProvenanceKind !=
                        CanonProvenanceKind
                            .ScenarioSeed ||
                    canon.SourceArtifactId !=
                        demoTape.DemoTapeId)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                        canon.SourceTrackId) ||
                    string.IsNullOrWhiteSpace(
                        canon.SourceIdeaId))
                {
                    throw new InvalidOperationException(
                        "Scenario Canon lacks exact " +
                        "Track/Idea provenance."
                    );
                }

                ResolveFormalPair(
                    demoTape,
                    canon,
                    out DemoTapeTrackSnapshot track,
                    out DemoTapeIdeaSnapshot idea,
                    out int ideaIndex,
                    out
                        DemoTapeTagOccurrenceSnapshot
                        dominant
                );

                if ((int)canon.Degree >
                    (int)dominant.Degree)
                {
                    throw new InvalidOperationException(
                        "Scenario Canon degree exceeds " +
                        "recorded pair degree."
                    );
                }

                if (!release.TryGetPairActivationState(
                        track.SourceTrackId,
                        idea.SourceIdeaId,
                        ideaIndex,
                        out
                            SceneReleasePairActivationState
                            activation))
                {
                    throw new InvalidOperationException(
                        "Scenario Canon pair has no frozen " +
                        "activation on the release."
                    );
                }

                if ((int)activation
                        .CurrentActivationDegree <
                    (int)canon.Degree)
                {
                    throw new InvalidOperationException(
                        "Scenario Canon exceeds frozen " +
                        "release activation."
                    );
                }

                claims.Add(
                    new
                        SceneReleaseCanonFrontierClaim(
                            release,
                            track.SourceTrackId,
                            idea.SourceIdeaId,
                            ideaIndex,
                            canon.Axis,
                            canon.Pole,
                            canon.Degree,
                            canonicalActivatorPlayerId,
                            release
                                .CanonizedUnderKeeperTenureId,
                            release.CanonizedTurn
                        )
                );
            }

            if (claims.Count == 0)
            {
                throw new InvalidOperationException(
                    "Founding canonical release resolved " +
                    "no Scenario frontier claims."
                );
            }

            claims.Sort(
                CompareClaims
            );

            return claims;
        }

        private static void ResolveFormalPair(
            DemoTape demoTape,
            CanonPrecedentRecord canon,
            out DemoTapeTrackSnapshot resolvedTrack,
            out DemoTapeIdeaSnapshot resolvedIdea,
            out int resolvedIdeaIndex,
            out DemoTapeTagOccurrenceSnapshot
                resolvedDominant)
        {
            resolvedTrack =
                null;

            resolvedIdea =
                null;

            resolvedIdeaIndex =
                -1;

            resolvedDominant =
                null;

            foreach (
                DemoTapeTrackSnapshot track
                in demoTape.TrackSnapshots)
            {
                if (track.SourceTrackId !=
                    canon.SourceTrackId)
                {
                    continue;
                }

                for (int ideaIndex = 0;
                     ideaIndex <
                     track.IdeaSnapshots.Count;
                     ideaIndex++)
                {
                    DemoTapeIdeaSnapshot idea =
                        track.IdeaSnapshots[
                            ideaIndex
                        ];

                    if (idea.SourceIdeaId !=
                        canon.SourceIdeaId)
                    {
                        continue;
                    }

                    if (idea.PayloadType !=
                        IdeaPayloadType.TagPair)
                    {
                        throw new InvalidOperationException(
                            "Scenario Canon provenance " +
                            "does not identify a TagPair."
                        );
                    }

                    DemoTapeTagOccurrenceSnapshot
                        dominant =
                            null;

                    DemoTapeTagOccurrenceSnapshot
                        submissive =
                            null;

                    foreach (
                        DemoTapeTagOccurrenceSnapshot tag
                        in idea.TagOccurrences)
                    {
                        if (tag.Role ==
                            DemoTapeTagOccurrenceRole
                                .PairDominant)
                        {
                            dominant =
                                tag;
                        }
                        else if (
                            tag.Role ==
                            DemoTapeTagOccurrenceRole
                                .PairSubmissive)
                        {
                            submissive =
                                tag;
                        }
                    }

                    if (dominant == null ||
                        submissive == null)
                    {
                        throw new InvalidOperationException(
                            "Scenario Canon pair is not " +
                            "structurally intact."
                        );
                    }

                    if (dominant.Axis !=
                            canon.Axis ||
                        dominant.Pole !=
                            canon.Pole ||
                        dominant.Degree !=
                            canon.Degree)
                    {
                        throw new InvalidOperationException(
                            "Scenario Canon declaration " +
                            "does not match the authored " +
                            "dominant Tag."
                        );
                    }

                    resolvedTrack =
                        track;

                    resolvedIdea =
                        idea;

                    resolvedIdeaIndex =
                        ideaIndex;

                    resolvedDominant =
                        dominant;

                    return;
                }
            }

            throw new InvalidOperationException(
                "Scenario Canon provenance could not " +
                "be resolved from founding DemoTape | " +
                $"track={canon.SourceTrackId} | " +
                $"idea={canon.SourceIdeaId}"
            );
        }

        private static int CompareClaims(
            SceneReleaseCanonFrontierClaim left,
            SceneReleaseCanonFrontierClaim right)
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

            return ideaIndex != 0
                ? ideaIndex
                : string.CompareOrdinal(
                    left.SourceIdeaId,
                    right.SourceIdeaId
                );
        }
    }
}