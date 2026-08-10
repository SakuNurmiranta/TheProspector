using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// One exact recorded pair whose prospective
    /// activation has reached institutional
    /// legitimacy classification.
    ///
    /// This is not yet an activation result.
    /// </summary>
    public sealed class ActivationLegitimacyCandidate
    {
        public ActivationAttemptRoute Route { get; }

        public string HappeningId { get; }

        public string SourceIntentId { get; }

        public string ActorEntityId { get; }

        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceTrackId { get; }

        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public string BehaviorTypeId { get; }

        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        /// <summary>
        /// Degree presented to Accepted Transgression.
        ///
        /// Hail:
        /// factual Behavior demonstrated degree.
        ///
        /// Performance:
        /// PUBLIC_PERFORMANCE activation degree.
        /// </summary>
        public TagDegree PraxisDegree { get; }

        /// <summary>
        /// Degree this occurrence could apply to this
        /// recorded dominant Tag after legitimacy.
        /// </summary>
        public TagDegree AppliedActivationDegree { get; }

        public TagDegree RecordedDominantDegree { get; }

        public string BehaviorOccurrenceId { get; }

        public string HailOccurrenceId { get; }

        public string HailedAspectId { get; }

        public bool HasHail =>
            Route == ActivationAttemptRoute.Hail;

        public ActivationLegitimacyCandidate(
            ActivationAttemptRoute route,
            string happeningId,
            string sourceIntentId,
            string actorEntityId,
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex,
            string behaviorTypeId,
            TagAxis axis,
            TagPole pole,
            TagDegree praxisDegree,
            TagDegree appliedActivationDegree,
            TagDegree recordedDominantDegree,
            string behaviorOccurrenceId = null,
            string hailOccurrenceId = null,
            string hailedAspectId = null)
        {
            if (!Enum.IsDefined(
                    typeof(ActivationAttemptRoute),
                    route))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(route)
                );
            }

            Route = route;

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

            SourceTrackId =
                RequireText(
                    sourceTrackId,
                    nameof(sourceTrackId)
                );

            SourceIdeaId =
                RequireText(
                    sourceIdeaId,
                    nameof(sourceIdeaId)
                );

            if (ideaIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ideaIndex)
                );
            }

            IdeaIndex = ideaIndex;

            BehaviorTypeId =
                RequireText(
                    behaviorTypeId,
                    nameof(behaviorTypeId)
                );

            ValidateDegree(
                praxisDegree,
                nameof(praxisDegree)
            );

            ValidateDegree(
                appliedActivationDegree,
                nameof(appliedActivationDegree)
            );

            ValidateDegree(
                recordedDominantDegree,
                nameof(recordedDominantDegree)
            );

            Axis = axis;
            Pole = pole;
            PraxisDegree = praxisDegree;
            AppliedActivationDegree =
                appliedActivationDegree;
            RecordedDominantDegree =
                recordedDominantDegree;

            if (route ==
                ActivationAttemptRoute.Performance)
            {
                if (!string.IsNullOrWhiteSpace(
                        behaviorOccurrenceId) ||
                    !string.IsNullOrWhiteSpace(
                        hailOccurrenceId) ||
                    !string.IsNullOrWhiteSpace(
                        hailedAspectId))
                {
                    throw new ArgumentException(
                        "Performance legitimacy candidate " +
                        "cannot carry Hail provenance."
                    );
                }

                BehaviorOccurrenceId = null;
                HailOccurrenceId = null;
                HailedAspectId = null;
                return;
            }

            BehaviorOccurrenceId =
                RequireText(
                    behaviorOccurrenceId,
                    nameof(behaviorOccurrenceId)
                );

            HailOccurrenceId =
                RequireText(
                    hailOccurrenceId,
                    nameof(hailOccurrenceId)
                );

            HailedAspectId =
                RequireText(
                    hailedAspectId,
                    nameof(hailedAspectId)
                );
        }

        private static void ValidateDegree(
            TagDegree degree,
            string parameterName)
        {
            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    degree))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Activation legitimacy provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}