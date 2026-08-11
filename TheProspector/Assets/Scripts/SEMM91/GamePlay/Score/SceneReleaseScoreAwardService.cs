using System;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Score
{
    /// <summary>
    /// Converts already-resolved SceneRelease
    /// legitimacy evaluation into competitive Score.
    ///
    /// This service does not calculate semantic values
    /// and does not mutate release lifecycle.
    /// </summary>
    public sealed class
        SceneReleaseScoreAwardService
    {
        public bool TryAwardFirstFetterResonance(
            SceneRelease release,
            SceneReleaseLegitimacyEvaluation evaluation,
            ScoreLedger ledger,
            int globalTurn,
            out ScoreEvent scoreEvent)
        {
            scoreEvent = null;

            if (release == null ||
                evaluation == null ||
                ledger == null)
            {
                return false;
            }

            if (globalTurn < 0 ||
                globalTurn < release.ReleasedTurn)
            {
                return false;
            }

            /*
             * Resonance is awarded specifically for
             * successful first fettering.
             *
             * Merely being Field at some later point
             * is not enough to manufacture a delayed
             * first-fetter award.
             */
            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                return false;
            }

            if (!HasFetterTransitionAtTurn(
                    release,
                    globalTurn
                ))
            {
                return false;
            }

            /*
             * Fettering should only have happened
             * because the release possesses current
             * active TRVE.
             *
             * Keep this validation at the scoring
             * boundary so an inconsistent caller
             * cannot award R for inert material.
             */
            if (!evaluation.HasTrve)
            {
                return false;
            }

            /*
             * First-fetter Resonance belongs to the
             * DemoTape owner once per scene.
             *
             * A later SceneRelease of the same master
             * in the same scene cannot farm it again.
             */
            if (HasFirstFetterAward(
                    ledger,
                    release.SourceDemoTapeId,
                    release.HostedSceneNodeId
                ))
            {
                return false;
            }

            ScoreEvent proposed =
                new ScoreEvent(
                    BuildFirstFetterEventId(
                        release.SourceDemoTapeId,
                        release.HostedSceneNodeId
                    ),
                    release.SourceOwnerEntityId,
                    release.SourceOwnerEntityId,
                    release.HostedSceneNodeId,
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    ScoreEventKind
                        .FirstFetterResonance,
                    evaluation.Resonance,
                    globalTurn
                );

            if (!ledger.TryRecord(
                    proposed))
            {
                return false;
            }

            scoreEvent =
                proposed;

            return true;
        }

        public bool TryAwardFieldGravity(
            SceneRelease release,
            SceneReleaseLegitimacyEvaluation evaluation,
            ScoreLedger ledger,
            int globalTurn,
            out ScoreEvent scoreEvent)
        {
            scoreEvent = null;

            if (release == null ||
                evaluation == null ||
                ledger == null)
            {
                return false;
            }

            if (globalTurn < 0 ||
                globalTurn < release.ReleasedTurn)
            {
                return false;
            }

            /*
             * Fringe produces no recurring Gravity
             * Score. Failed-to-Fetter is likewise
             * outside the Field.
             */
            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                return false;
            }

            ScoreEvent proposed =
                new ScoreEvent(
                    BuildFieldGravityEventId(
                        release.ReleaseId,
                        globalTurn
                    ),
                    release.SourceOwnerEntityId,
                    release.SourceOwnerEntityId,
                    release.HostedSceneNodeId,
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    ScoreEventKind.FieldGravity,
                    evaluation.Gravity,
                    globalTurn
                );

            /*
             * Stable identity makes repeated
             * settlement of the same release/turn
             * idempotent at the Score boundary.
             */
            if (!ledger.TryRecord(
                    proposed))
            {
                return false;
            }

            scoreEvent =
                proposed;

            return true;
        }

        private static bool
            HasFetterTransitionAtTurn(
                SceneRelease release,
                int globalTurn)
        {
            foreach (
                SceneReleaseLifecycleTransition transition
                in release.LifecycleTransitions)
            {
                if (transition.ToState ==
                        SceneReleaseLifecycleState.Field &&
                    transition.GlobalTurn ==
                        globalTurn)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasFirstFetterAward(
            ScoreLedger ledger,
            string demoTapeId,
            string sceneId)
        {
            var priorEvents =
                ledger.GetForDemoTapeInScene(
                    demoTapeId,
                    sceneId
                );

            foreach (
                ScoreEvent scoreEvent
                in priorEvents)
            {
                if (scoreEvent.Kind ==
                    ScoreEventKind
                        .FirstFetterResonance)
                {
                    return true;
                }
            }

            return false;
        }

        private static string
            BuildFirstFetterEventId(
                string demoTapeId,
                string sceneId)
        {
            return
                "SCORE|FIRST_FETTER|" +
                Encode(sceneId) +
                "|" +
                Encode(demoTapeId);
        }

        private static string
            BuildFieldGravityEventId(
                string sceneReleaseId,
                int globalTurn)
        {
            return
                "SCORE|FIELD_GRAVITY|" +
                Encode(sceneReleaseId) +
                "|" +
                globalTurn;
        }

        private static string Encode(
            string value)
        {
            return
                value.Length +
                ":" +
                value;
        }
    }
}