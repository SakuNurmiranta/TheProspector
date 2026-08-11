using System;
using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Builds the complete Canon_t Assimilation plan
    /// for one post-movement Canon candidate.
    ///
    /// All formal recorded pairs are inspected.
    /// Solitary Tags can never receive TRVE activation.
    /// </summary>
    public sealed class
        SceneReleaseCanonAssimilationEvaluator
    {
        private const float PositionTolerance =
            0.0001f;

        public SceneReleaseCanonAssimilationEvaluation
            Evaluate(
                SceneRelease release,
                DemoTape demoTape,
                SceneReleaseNexusBoundaryEvaluation
                    nexusEvaluation,
                CanonState frozenCanon)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (demoTape == null)
            {
                throw new ArgumentNullException(
                    nameof(demoTape)
                );
            }

            if (nexusEvaluation == null)
            {
                throw new ArgumentNullException(
                    nameof(nexusEvaluation)
                );
            }

            if (frozenCanon == null)
            {
                throw new ArgumentNullException(
                    nameof(frozenCanon)
                );
            }

            if (!nexusEvaluation.IsCanonCandidate)
            {
                throw new ArgumentException(
                    "Canon Assimilation requires a " +
                    "Canon candidate.",
                    nameof(nexusEvaluation)
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new ArgumentException(
                    "Canon candidate must still be an " +
                    "active Field release.",
                    nameof(release)
                );
            }

            if (!release.HasFieldPosition ||
                release.FieldPositionState == null)
            {
                throw new ArgumentException(
                    "Canon Assimilation requires Field " +
                    "Position.",
                    nameof(release)
                );
            }

            ValidateIdentity(
                release,
                demoTape,
                nexusEvaluation
            );

            if (!NearlyEqual(
                    release.FieldPositionState
                        .CurrentPosition,
                    nexusEvaluation.FieldPosition))
            {
                throw new InvalidOperationException(
                    "Canon candidate position changed " +
                    "after Nexus-boundary evaluation."
                );
            }

            if (release.FieldPositionState
                    .LastMovementTurn !=
                nexusEvaluation.SettledTurn)
            {
                throw new InvalidOperationException(
                    "Canon Assimilation requires the " +
                    "same settled movement turn as the " +
                    "Nexus candidate."
                );
            }

            List<
                SceneReleaseCanonAssimilationPairEvaluation>
                results =
                    new();

            foreach (
                DemoTapeTrackSnapshot track
                in demoTape.TrackSnapshots)
            {
                foreach (
                    DemoTapeIdeaSnapshot idea
                    in track.IdeaSnapshots)
                {
                    if (idea.PayloadType ==
                        IdeaPayloadType.SingleTag)
                    {
                        continue;
                    }

                    if (idea.PayloadType !=
                        IdeaPayloadType.TagPair)
                    {
                        throw new InvalidOperationException(
                            "Unsupported Idea payload in " +
                            "Canon Assimilation."
                        );
                    }

                    DemoTapeTagOccurrenceSnapshot dominant =
                        FindDominant(
                            idea
                        );

                    TagDegree existingActivation =
                        TagDegree.Neutral;

                    if (release.TryGetPairActivationState(
                            track.SourceTrackId,
                            idea.SourceIdeaId,
                            idea.IdeaIndex,
                            out
                                SceneReleasePairActivationState
                                state))
                    {
                        if (state.RecordedDominantDegree !=
                            dominant.Degree)
                        {
                            throw new InvalidOperationException(
                                "SceneRelease activation " +
                                "state disagrees with " +
                                "immutable DemoTape."
                            );
                        }

                        existingActivation =
                            state.CurrentActivationDegree;
                    }

                    bool hasCanon =
                        frozenCanon.TryGetCanonicalDegree(
                            dominant.Axis,
                            dominant.Pole,
                            out
                                TagDegree
                                canonicalDegree
                        );

                    List<CanonPrecedentRecord>
                        precedents =
                            new();

                    if (hasCanon)
                    {
                        foreach (
                            CanonPrecedentRecord record
                            in frozenCanon.Records)
                        {
                            if (record.Axis ==
                                    dominant.Axis &&
                                record.Pole ==
                                    dominant.Pole &&
                                record.Degree ==
                                    canonicalDegree)
                            {
                                precedents.Add(
                                    record
                                );
                            }
                        }

                        if (precedents.Count == 0)
                        {
                            throw new InvalidOperationException(
                                "Canon reports a degree " +
                                "without matching precedent " +
                                "provenance."
                            );
                        }
                    }
                    else
                    {
                        canonicalDegree =
                            TagDegree.Neutral;
                    }

                    results.Add(
                        new
                            SceneReleaseCanonAssimilationPairEvaluation(
                                release.ReleaseId,
                                demoTape.DemoTapeId,
                                release.SourceOwnerEntityId,
                                release.HostedSceneNodeId,
                                track.SourceTrackId,
                                idea.SourceIdeaId,
                                idea.IdeaIndex,
                                nexusEvaluation.SettledTurn,
                                dominant.Axis,
                                dominant.Pole,
                                dominant.Degree,
                                existingActivation,
                                hasCanon,
                                canonicalDegree,
                                precedents
                            )
                    );
                }
            }

            return new
                SceneReleaseCanonAssimilationEvaluation(
                    nexusEvaluation,
                    results
                );
        }

        private static void ValidateIdentity(
            SceneRelease release,
            DemoTape demoTape,
            SceneReleaseNexusBoundaryEvaluation nexus)
        {
            if (release.SourceDemoTapeId !=
                demoTape.DemoTapeId)
            {
                throw new ArgumentException(
                    "SceneRelease and DemoTape identity " +
                    "do not match.",
                    nameof(demoTape)
                );
            }

            if (nexus.SceneReleaseId !=
                    release.ReleaseId ||
                nexus.SourceDemoTapeId !=
                    release.SourceDemoTapeId ||
                nexus.SourceOwnerEntityId !=
                    release.SourceOwnerEntityId ||
                nexus.SceneId !=
                    release.HostedSceneNodeId)
            {
                throw new ArgumentException(
                    "Nexus candidate provenance does " +
                    "not match SceneRelease.",
                    nameof(nexus)
                );
            }
        }

        private static
            DemoTapeTagOccurrenceSnapshot
            FindDominant(
                DemoTapeIdeaSnapshot idea)
        {
            foreach (
                DemoTapeTagOccurrenceSnapshot occurrence
                in idea.TagOccurrences)
            {
                if (occurrence.Role ==
                    DemoTapeTagOccurrenceRole
                        .PairDominant)
                {
                    return occurrence;
                }
            }

            throw new InvalidOperationException(
                "Formal Tag-pair has no dominant " +
                "occurrence."
            );
        }

        private static bool NearlyEqual(
            float left,
            float right)
        {
            return
                Math.Abs(left - right) <=
                PositionTolerance;
        }
    }
}