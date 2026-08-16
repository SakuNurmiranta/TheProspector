using System;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Finalizes the semantic state of one successful
    /// Canon candidate after NewCanon Assimilation.
    ///
    /// It reuses the production legitimacy evaluator,
    /// freezes final G_release, and permanently closes
    /// all SceneRelease activation mutation.
    ///
    /// CanonRetained lifecycle is intentionally handled
    /// by a later settlement step.
    /// </summary>
    public sealed class
        SceneReleaseCanonFreezeService
    {
        private const float
            PositionTolerance =
                0.0001f;

        private readonly
            SceneReleaseLegitimacyEvaluator
            legitimacyEvaluator =
                new();

        public SceneReleaseCanonFreezeApplication
            Apply(
                SceneRelease release,
                DemoTape demoTape,
                TrackEvaluationEnvironment environment,
                SceneReleaseCanonAssimilationApplication
                    assimilationApplication)
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

            if (environment == null)
            {
                throw new ArgumentNullException(
                    nameof(environment)
                );
            }

            if (assimilationApplication == null)
            {
                throw new ArgumentNullException(
                    nameof(assimilationApplication)
                );
            }

            if (release.IsCanonized)
            {
                throw new InvalidOperationException(
                    "SceneRelease Canon state has " +
                    "already been frozen."
                );
            }

            SceneReleaseCanonAssimilationEvaluation
                assimilation =
                    assimilationApplication.Evaluation;

            ValidateRelease(
                release,
                demoTape,
                environment,
                assimilation
            );

            /*
             * Prove that the authoritative activation
             * graph still equals the already-applied
             * NewCanon Assimilation plan.
             */
            foreach (
                SceneReleaseCanonAssimilationPairEvaluation
                    pair
                in assimilation.PairEvaluations)
            {
                TagDegree current =
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
                                "Canon freeze found " +
                                "recorded pair-degree " +
                                "drift after assimilation."
                            );
                    }

                    current =
                        state.CurrentActivationDegree;
                }

                if (current !=
                    pair.FinalActivationDegree)
                {
                    throw new InvalidOperationException(
                        "Canon freeze is stale: " +
                        "authoritative activation does " +
                        "not match the applied NewCanon " +
                        "Assimilation plan."
                    );
                }
            }

            /*
             * Production calculation.
             *
             * No TRVE or Gravity math is duplicated
             * here. The evaluator projects current
             * authoritative SceneRelease activation.
             */
            SceneReleaseLegitimacyEvaluation
                finalLegitimacy =
                    legitimacyEvaluator.Evaluate(
                        release,
                        demoTape,
                        environment
                    );

            if (finalLegitimacy.SourceReleaseId !=
                    release.ReleaseId ||
                finalLegitimacy.SourceDemoTapeId !=
                    demoTape.DemoTapeId ||
                finalLegitimacy.SourceOwnerEntityId !=
                    release.SourceOwnerEntityId ||
                finalLegitimacy.SettledTurn !=
                    assimilation.SettledTurn)
            {
                throw new InvalidOperationException(
                    "Final post-assimilation legitimacy " +
                    "has invalid provenance."
                );
            }

            /*
             * A successful Canon candidate must still
             * contain active TRVE after assimilation.
             */
            if (!finalLegitimacy.HasTrve)
            {
                throw new InvalidOperationException(
                    "Canon candidate lost all active " +
                    "TRVE before final freeze."
                );
            }

            if (float.IsNaN(
                    finalLegitimacy.Gravity) ||
                float.IsInfinity(
                    finalLegitimacy.Gravity) ||
                finalLegitimacy.Gravity < 0f)
            {
                throw new InvalidOperationException(
                    "Final post-assimilation Gravity " +
                    "is not a valid finite magnitude."
                );
            }

            if (!release.TryFreezeCanonization(
                    assimilation.SettledTurn,
                    assimilation.KeeperTenureId,
                    finalLegitimacy.Gravity,
                    out
                        SceneReleaseCanonizationFreezeState
                        freezeState))
            {
                throw new InvalidOperationException(
                    "Validated Canon candidate could " +
                    "not freeze final canonical state."
                );
            }

            return new
                SceneReleaseCanonFreezeApplication(
                    assimilationApplication,
                    finalLegitimacy,
                    freezeState
                );
        }

        private static void ValidateRelease(
            SceneRelease release,
            DemoTape demoTape,
            TrackEvaluationEnvironment environment,
            SceneReleaseCanonAssimilationEvaluation
                assimilation)
        {
            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new InvalidOperationException(
                    "Canon freeze currently requires " +
                    "the candidate to remain Field."
                );
            }

            if (!release.HasFieldPosition ||
                release.FieldPositionState == null)
            {
                throw new InvalidOperationException(
                    "Canon freeze requires established " +
                    "Field Position."
                );
            }

            if (release.ReleaseId !=
                    assimilation.SceneReleaseId ||
                release.SourceDemoTapeId !=
                    assimilation.SourceDemoTapeId ||
                release.SourceOwnerEntityId !=
                    assimilation.SourceOwnerEntityId ||
                release.HostedSceneNodeId !=
                    assimilation.SceneId)
            {
                throw new ArgumentException(
                    "Canon Assimilation belongs to " +
                    "another SceneRelease.",
                    nameof(assimilation)
                );
            }

            if (demoTape.DemoTapeId !=
                release.SourceDemoTapeId)
            {
                throw new ArgumentException(
                    "Canon freeze received the wrong " +
                    "DemoTape.",
                    nameof(demoTape)
                );
            }

            if (environment.SettledTurn !=
                assimilation.SettledTurn)
            {
                throw new ArgumentException(
                    "Canon freeze environment belongs " +
                    "to another settlement turn.",
                    nameof(environment)
                );
            }

            if (assimilation.NexusEvaluation
                    .RequiresSameTurnMovement)
            {
                if (release.FieldPositionState
                        .LastMovementTurn !=
                    assimilation.SettledTurn)
                {
                    throw new InvalidOperationException(
                        "Movement-backed Canon freeze " +
                        "requires same-turn settled movement."
                    );
                }
            }
            else if (
                release.FieldPositionState
                    .LastMovementTurn >
                assimilation.SettledTurn)
            {
                throw new InvalidOperationException(
                    "Post-Happening Canon freeze cannot " +
                    "use future Field movement."
                );
            }

            if (Math.Abs(
                    release.FieldPositionState
                        .CurrentPosition -
                    assimilation
                        .NexusEvaluation
                        .FieldPosition) >
                PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Canon candidate Field Position " +
                    "changed after Nexus evaluation."
                );
            }
        }
    }
}