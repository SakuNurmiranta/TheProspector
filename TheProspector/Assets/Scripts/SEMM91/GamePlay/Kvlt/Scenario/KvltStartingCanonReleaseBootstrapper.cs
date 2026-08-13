using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    /// <summary>
    /// Establishes one SceneRelease that is already
    /// CanonRetained in the configured turn-0 state.
    ///
    /// It does not replay promotion, Field movement,
    /// activation attempts, Allegiance Crisis or
    /// canonization events.
    /// </summary>
    public sealed class
        KvltStartingCanonReleaseBootstrapper
    {
        private readonly
            SceneReleaseLegitimacyEvaluator
            legitimacyEvaluator =
                new();

        private readonly
            CanonicalNormativeCentreDeriver
            normativeCentreDeriver =
                new();

        public KvltStartingCanonReleaseBootstrapResult
            Apply(
                SeededWorldState worldState,
                DemoTape demoTape,
                string ownerEntityId,
                string sceneId,
                string keeperTenureId,
                int startingTurn)
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

            RequireText(
                ownerEntityId,
                nameof(ownerEntityId)
            );

            RequireText(
                sceneId,
                nameof(sceneId)
            );

            RequireText(
                keeperTenureId,
                nameof(keeperTenureId)
            );

            if (startingTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingTurn)
                );
            }

            if (demoTape.RecordedTurn !=
                startingTurn)
            {
                throw new InvalidOperationException(
                    "Starting canonical DemoTape must " +
                    "belong to the starting turn."
                );
            }

            if (demoTape.IsReleased)
            {
                throw new InvalidOperationException(
                    "Starting canonical DemoTape has " +
                    "already entered Scene state."
                );
            }

            foreach (
                SceneRelease existing
                in worldState.SceneReleases)
            {
                if (existing.SourceDemoTapeId ==
                    demoTape.DemoTapeId)
                {
                    throw new InvalidOperationException(
                        "Starting canonical DemoTape " +
                        "already has a SceneRelease."
                    );
                }
            }

            SceneRelease release =
                new(
                    displayName:
                        $"{demoTape.DisplayName} - KVLT Release",

                    sourceDemoTapeId:
                        demoTape.DemoTapeId,

                    sourceOwnerEntityId:
                        ownerEntityId,

                    hostedSceneNodeId:
                        sceneId,

                    releasedTurn:
                        startingTurn,

                    sourceConveyance:
                        demoTape.AverageConveyance
                );

            List<
                SceneReleaseFrozenPairActivation>
                frozenActivations =
                    ResolveStartingActivations(
                        worldState,
                        demoTape
                    );

            IReadOnlyList<
                    TrackActivationEvaluationSnapshot>
                evaluationActivations =
                    BuildEvaluationActivations(
                        release,
                        demoTape,
                        frozenActivations
                    );

            /*
             * Current Normative Centre was already
             * installed by Entry 6.
             *
             * Canon-only interpretation is derived
             * independently for Influence Potential.
             */
            TrackEvaluationEnvironment environment =
                new(
                    startingTurn,
                    worldState.KvltNormativeCentre,
                    normativeCentreDeriver.Derive(
                        worldState.KvltCanon
                    ),
                    worldState.SocietyNorms
                );

            /*
             * Evaluate against the proposed activation
             * snapshot before mutating SceneRelease.
             *
             * This avoids temporarily installing
             * activation just to discover final G.
             */
            SceneReleaseLegitimacyEvaluation
                finalLegitimacy =
                    legitimacyEvaluator.Evaluate(
                        release,
                        demoTape,
                        environment,
                        evaluationActivations
                    );

            if (!finalLegitimacy.HasTrve)
            {
                throw new InvalidOperationException(
                    "Starting canonical release has no " +
                    "active TRVE."
                );
            }

            if (float.IsNaN(
                    finalLegitimacy.Gravity) ||
                float.IsInfinity(
                    finalLegitimacy.Gravity) ||
                finalLegitimacy.Gravity < 0f)
            {
                throw new InvalidOperationException(
                    "Starting canonical release produced " +
                    "invalid Gravity."
                );
            }

            if (!release
                    .TrySeedInitialCanonRetainedState(
                        startingTurn,
                        keeperTenureId,
                        finalLegitimacy.Gravity,
                        frozenActivations,
                        out
                            SceneReleaseCanonizationFreezeState
                            freezeState))
            {
                throw new InvalidOperationException(
                    "Validated starting canonical release " +
                    "could not enter CanonRetained state."
                );
            }

            /*
             * The DemoTape is publicly hosted because
             * its SceneRelease now exists.
             */
            demoTape.MarkHosted();

            worldState.AddSceneRelease(
                release
            );

            return new
                KvltStartingCanonReleaseBootstrapResult(
                    release,
                    finalLegitimacy,
                    freezeState
                );
        }

        private static List<
                SceneReleaseFrozenPairActivation>
            ResolveStartingActivations(
                SeededWorldState worldState,
                DemoTape demoTape)
        {
            List<
                SceneReleaseFrozenPairActivation>
                result =
                    new();

            HashSet<
                SceneReleasePairActivationKey>
                seen =
                    new();

            foreach (
                CanonPrecedentRecord canon
                in worldState.KvltCanon.Records)
            {
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
                        "Starting Canon lacks exact " +
                        "Track/Idea provenance."
                    );
                }

                ResolvePair(
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
                        "Starting Canon degree exceeds " +
                        "the recorded dominant degree."
                    );
                }

                if (canon.Degree ==
                    TagDegree.Neutral)
                {
                    throw new InvalidOperationException(
                        "Starting canonical activation " +
                        "cannot be neutral."
                    );
                }

                SceneReleasePairActivationKey key =
                    new(
                        track.SourceTrackId,
                        idea.SourceIdeaId,
                        ideaIndex
                    );

                if (!seen.Add(key))
                {
                    throw new InvalidOperationException(
                        "Starting Canon resolves more than " +
                        "once to the same formal pair."
                    );
                }

                result.Add(
                    new
                        SceneReleaseFrozenPairActivation(
                            key,
                            dominant.Degree,
                            canon.Degree
                        )
                );
            }

            if (result.Count == 0)
            {
                throw new InvalidOperationException(
                    "Founding DemoTape establishes no " +
                    "starting canonical pair activation."
                );
            }

            return result;
        }

        private static void ResolvePair(
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
                            "Starting Canon provenance " +
                            "does not identify a formal pair."
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
                            "Starting canonical Idea does " +
                            "not preserve an intact pair."
                        );
                    }

                    if (dominant.Axis !=
                            canon.Axis ||
                        dominant.Pole !=
                            canon.Pole)
                    {
                        throw new InvalidOperationException(
                            "Starting Canon provenance and " +
                            "recorded dominant Tag disagree."
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
                "Starting Canon provenance could not be " +
                "resolved from the founding DemoTape | " +
                $"track={canon.SourceTrackId} | " +
                $"idea={canon.SourceIdeaId}"
            );
        }

        private static IReadOnlyList<
                TrackActivationEvaluationSnapshot>
            BuildEvaluationActivations(
                SceneRelease release,
                DemoTape demoTape,
                IReadOnlyList<
                    SceneReleaseFrozenPairActivation>
                    frozenActivations)
        {
            Dictionary<
                string,
                List<IdeaActivationEvaluationSnapshot>>
                byTrack =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                SceneReleaseFrozenPairActivation frozen
                in frozenActivations)
            {
                if (!byTrack.TryGetValue(
                        frozen.SourceTrackId,
                        out
                            List<
                                IdeaActivationEvaluationSnapshot>
                            ideas))
                {
                    ideas =
                        new List<
                            IdeaActivationEvaluationSnapshot>();

                    byTrack.Add(
                        frozen.SourceTrackId,
                        ideas
                    );
                }

                ideas.Add(
                    new
                        IdeaActivationEvaluationSnapshot(
                            frozen.SourceIdeaId,
                            (int)frozen
                                .FinalActivationDegree
                        )
                );
            }

            List<
                TrackActivationEvaluationSnapshot>
                result =
                    new();

            foreach (
                DemoTapeTrackSnapshot track
                in demoTape.TrackSnapshots)
            {
                if (!byTrack.TryGetValue(
                        track.SourceTrackId,
                        out
                            List<
                                IdeaActivationEvaluationSnapshot>
                            ideas))
                {
                    continue;
                }

                result.Add(
                    new
                        TrackActivationEvaluationSnapshot(
                            release.ReleaseId,
                            demoTape.DemoTapeId,
                            track.SourceTrackId,
                            ideas
                        )
                );
            }

            return result;
        }

        private static void RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Starting canonical release " +
                    "provenance cannot be empty.",
                    parameterName
                );
            }
        }
    }
}