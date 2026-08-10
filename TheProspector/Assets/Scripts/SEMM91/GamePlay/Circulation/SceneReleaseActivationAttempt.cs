using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class SceneReleaseActivationAttempt
    {
        private readonly
            HailAnsweringPairCandidate[]
                answeringPairCandidates;

        public ActivationAttemptRoute Route { get; }

        public string HappeningId { get; }

        public string SourceIntentId { get; }

        public string ActorEntityId { get; }

        public int DeclaredTurn { get; }

        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string HailedAspectId { get; }

        public bool HasHailedAspect =>
            Route == ActivationAttemptRoute.Hail;

        public IReadOnlyList<
            HailAnsweringPairCandidate>
            AnsweringPairCandidates =>
                answeringPairCandidates;

        private SceneReleaseActivationAttempt(
            ActivationAttemptRoute route,
            string happeningId,
            string sourceIntentId,
            string actorEntityId,
            int declaredTurn,
            string sceneReleaseId,
            string sourceDemoTapeId,
            string hailedAspectId,
            IReadOnlyList<
                HailAnsweringPairCandidate>
                pairCandidates)
        {
            if (!Enum.IsDefined(
                    typeof(ActivationAttemptRoute),
                    route))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(route)
                );
            }

            Route =
                route;

            HappeningId =
                RequireText(
                    happeningId,
                    nameof(happeningId)
                );

            SourceIntentId =
                RequireText(
                    sourceIntentId,
                    nameof(sourceIntentId)
                );

            ActorEntityId =
                RequireText(
                    actorEntityId,
                    nameof(actorEntityId)
                );

            if (declaredTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(declaredTurn)
                );
            }

            DeclaredTurn =
                declaredTurn;

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

            if (pairCandidates == null)
            {
                throw new ArgumentNullException(
                    nameof(pairCandidates)
                );
            }

            answeringPairCandidates =
                new HailAnsweringPairCandidate[
                    pairCandidates.Count
                ];

            for (int index = 0;
                 index < pairCandidates.Count;
                 index++)
            {
                answeringPairCandidates[index] =
                    pairCandidates[index] ??
                    throw new ArgumentException(
                        "Activation Attempt cannot " +
                        "contain a null pair candidate.",
                        nameof(pairCandidates)
                    );
            }

            if (route ==
                ActivationAttemptRoute.Performance)
            {
                if (!string.IsNullOrWhiteSpace(
                        hailedAspectId))
                {
                    throw new ArgumentException(
                        "Performance attempt cannot " +
                        "carry a Hailed Aspect.",
                        nameof(hailedAspectId)
                    );
                }

                if (answeringPairCandidates.Length != 0)
                {
                    throw new ArgumentException(
                        "Performance attempt does not " +
                        "pre-resolve Hail-answering pairs.",
                        nameof(pairCandidates)
                    );
                }

                HailedAspectId = null;
                return;
            }

            HailedAspectId =
                RequireText(
                    hailedAspectId,
                    nameof(hailedAspectId)
                );

            if (answeringPairCandidates.Length == 0)
            {
                throw new ArgumentException(
                    "A release-specific Hail attempt " +
                    "requires at least one structurally " +
                    "compatible answering pair.",
                    nameof(pairCandidates)
                );
            }
        }

        public static SceneReleaseActivationAttempt
            ForPerformance(
                string happeningId,
                string sourceIntentId,
                string actorEntityId,
                int declaredTurn,
                string sceneReleaseId,
                string sourceDemoTapeId)
        {
            return new SceneReleaseActivationAttempt(
                ActivationAttemptRoute.Performance,
                happeningId,
                sourceIntentId,
                actorEntityId,
                declaredTurn,
                sceneReleaseId,
                sourceDemoTapeId,
                null,
                Array.Empty<
                    HailAnsweringPairCandidate>()
            );
        }

        public static SceneReleaseActivationAttempt
            ForHail(
                string happeningId,
                string sourceIntentId,
                string actorEntityId,
                int declaredTurn,
                string sceneReleaseId,
                string sourceDemoTapeId,
                string hailedAspectId,
                IReadOnlyList<
                    HailAnsweringPairCandidate>
                    pairCandidates)
        {
            return new SceneReleaseActivationAttempt(
                ActivationAttemptRoute.Hail,
                happeningId,
                sourceIntentId,
                actorEntityId,
                declaredTurn,
                sceneReleaseId,
                sourceDemoTapeId,
                hailedAspectId,
                pairCandidates
            );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Activation Attempt provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}