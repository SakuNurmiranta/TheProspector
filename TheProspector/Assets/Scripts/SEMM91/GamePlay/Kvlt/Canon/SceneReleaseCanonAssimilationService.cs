using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Validates the complete frozen Canon
    /// Assimilation plan before changing any pair
    /// activation state.
    /// </summary>
    public sealed class
        SceneReleaseCanonAssimilationService
    {
        private const float PositionTolerance =
            0.0001f;

        public SceneReleaseCanonAssimilationApplication
            Apply(
                SceneRelease release,
                SceneReleaseCanonAssimilationEvaluation
                    evaluation)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (evaluation == null)
            {
                throw new ArgumentNullException(
                    nameof(evaluation)
                );
            }

            if (release.IsActivationFrozen)
            {
                throw new InvalidOperationException(
                    "Canon Assimilation cannot modify an " +
                    "already-frozen canonical SceneRelease."
                );
            }

            ValidateRelease(
                release,
                evaluation
            );

            /*
             * PASS 1:
             * Validate every pair against authoritative
             * current A before mutating any pair.
             */
            foreach (
                SceneReleaseCanonAssimilationPairEvaluation
                    pair
                in evaluation.PairEvaluations)
            {
                TagDegree actual =
                    TagDegree.Neutral;

                if (release.TryGetPairActivationState(
                        pair.SourceTrackId,
                        pair.SourceIdeaId,
                        pair.IdeaIndex,
                        out
                            SceneReleasePairActivationState
                            state))
                {
                    if (state.RecordedDominantDegree !=
                        pair.RecordedDominantDegree)
                    {
                        throw new
                            InvalidOperationException(
                                "Canon Assimilation plan is " +
                                "stale: recorded pair degree " +
                                "changed."
                            );
                    }

                    actual =
                        state.CurrentActivationDegree;
                }

                if (actual !=
                    pair.ExistingActivationDegree)
                {
                    throw new InvalidOperationException(
                        "Canon Assimilation plan is stale: " +
                        "authoritative activation changed " +
                        "after evaluation."
                    );
                }
            }

            List<
                SceneReleaseCanonAssimilationActivationRecord>
                applied =
                    new();

            /*
             * PASS 2:
             * Authoritative mutation.
             */
            foreach (
                SceneReleaseCanonAssimilationPairEvaluation
                    pair
                in evaluation.PairEvaluations)
            {
                if (!pair.RaisesActivation)
                {
                    continue;
                }

                if (!release.TryApplyCanonAssimilation(
                        pair.SourceTrackId,
                        pair.SourceIdeaId,
                        pair.IdeaIndex,
                        pair.DominantAxis,
                        pair.DominantPole,
                        pair.RecordedDominantDegree,
                        pair.ExistingCanonicalDegree,
                        pair.CanonicalPrecedents,
                        pair.SettledTurn,
                        out
                            SceneReleaseCanonAssimilationActivationRecord
                            record))
                {
                    throw new InvalidOperationException(
                        "Validated Canon Assimilation " +
                        "could not be applied | " +
                        $"release={release.ReleaseId} | " +
                        $"track={pair.SourceTrackId} | " +
                        $"idea={pair.SourceIdeaId}"
                    );
                }

                if (record.NewActivationDegree !=
                    pair.FinalActivationDegree)
                {
                    throw new InvalidOperationException(
                        "Applied Canon Assimilation does " +
                        "not match frozen evaluation."
                    );
                }

                applied.Add(
                    record
                );
            }

            return new
                SceneReleaseCanonAssimilationApplication(
                    evaluation,
                    applied
                );
        }

        private static void ValidateRelease(
            SceneRelease release,
            SceneReleaseCanonAssimilationEvaluation
                evaluation)
        {
            if (release.ReleaseId !=
                    evaluation.SceneReleaseId ||
                release.SourceDemoTapeId !=
                    evaluation.SourceDemoTapeId ||
                release.SourceOwnerEntityId !=
                    evaluation.SourceOwnerEntityId ||
                release.HostedSceneNodeId !=
                    evaluation.SceneId)
            {
                throw new ArgumentException(
                    "Canon Assimilation evaluation " +
                    "belongs to another SceneRelease.",
                    nameof(evaluation)
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new InvalidOperationException(
                    "Canon candidate must still be " +
                    "Field during assimilation."
                );
            }

            if (!release.HasFieldPosition ||
                release.FieldPositionState == null)
            {
                throw new InvalidOperationException(
                    "Canon Assimilation requires Field " +
                    "Position."
                );
            }

            if (evaluation.NexusEvaluation
                    .RequiresSameTurnMovement)
            {
                if (release.FieldPositionState
                        .LastMovementTurn !=
                    evaluation.SettledTurn)
                {
                    throw new InvalidOperationException(
                        "Movement-backed Canon Assimilation " +
                        "requires same-turn Field movement."
                    );
                }
            }
            else if (
                release.FieldPositionState
                    .LastMovementTurn >
                evaluation.SettledTurn)
            {
                throw new InvalidOperationException(
                    "Post-Happening Canon Assimilation " +
                    "cannot use future Field movement."
                );
            }

            if (Math.Abs(
                    release.FieldPositionState
                        .CurrentPosition -
                    evaluation.NexusEvaluation
                        .FieldPosition) >
                PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Canon Assimilation evaluation is " +
                    "stale: Field Position changed."
                );
            }
        }
    }
}