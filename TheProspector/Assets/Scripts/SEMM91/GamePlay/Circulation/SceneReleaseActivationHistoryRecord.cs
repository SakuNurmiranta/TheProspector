using System;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class SceneReleaseActivationHistoryRecord
    {
        public SceneReleaseActivationHistoryKind
            Kind { get; }

        public ActivationLegitimacyCandidate
            Candidate { get; }

        public TagDegree
            PreviousActivationDegree { get; }

        public TagDegree
            NewActivationDegree { get; }

        public int OccurredTurn { get; }

        public AcceptedTransgressionRecord
            CoveringPrecedent { get; }

        public string
            AllegianceCrisisQuestionId { get; }

        public string PendingActivationId { get; }

        public bool RaisedActivation =>
            (int)NewActivationDegree >
            (int)PreviousActivationDegree;

        public SceneReleaseActivationHistoryRecord(
            SceneReleaseActivationHistoryKind kind,
            ActivationLegitimacyCandidate candidate,
            TagDegree previousActivationDegree,
            TagDegree newActivationDegree,
            int occurredTurn,
            AcceptedTransgressionRecord
                coveringPrecedent = null,
            string allegianceCrisisQuestionId = null,
            string pendingActivationId = null)
        {
            if (!Enum.IsDefined(
                    typeof(
                        SceneReleaseActivationHistoryKind
                    ),
                    kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind)
                );
            }

            Candidate =
                candidate ??
                throw new ArgumentNullException(
                    nameof(candidate)
                );

            ValidateDegree(
                previousActivationDegree
            );

            ValidateDegree(
                newActivationDegree
            );

            if (occurredTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(occurredTurn)
                );
            }

            bool hasCrisis =
                !string.IsNullOrWhiteSpace(
                    allegianceCrisisQuestionId
                );

            bool hasPending =
                !string.IsNullOrWhiteSpace(
                    pendingActivationId
                );

            switch (kind)
            {
                case SceneReleaseActivationHistoryKind
                    .CoveredApplied:

                    if (coveringPrecedent == null ||
                        hasCrisis ||
                        hasPending)
                    {
                        throw new ArgumentException(
                            "Direct covered activation " +
                            "requires precedent only."
                        );
                    }

                    break;

                case SceneReleaseActivationHistoryKind
                    .CrisisAcceptedApplied:

                    if (coveringPrecedent == null ||
                        !hasCrisis ||
                        hasPending)
                    {
                        throw new ArgumentException(
                            "Crisis-accepted activation " +
                            "requires precedent and " +
                            "crisis provenance."
                        );
                    }

                    break;

                case SceneReleaseActivationHistoryKind
                    .PendingStored:

                    if (coveringPrecedent != null ||
                        !hasCrisis ||
                        !hasPending)
                    {
                        throw new ArgumentException(
                            "Pending storage requires " +
                            "crisis and Pending provenance " +
                            "without accepted precedent."
                        );
                    }

                    break;

                case SceneReleaseActivationHistoryKind
                    .PendingRedeemed:

                    if (coveringPrecedent == null ||
                        !hasCrisis ||
                        !hasPending)
                    {
                        throw new ArgumentException(
                            "Pending redemption requires " +
                            "precedent, crisis and Pending " +
                            "provenance."
                        );
                    }

                    break;
            }

            Kind =
                kind;

            PreviousActivationDegree =
                previousActivationDegree;

            NewActivationDegree =
                newActivationDegree;

            OccurredTurn =
                occurredTurn;

            CoveringPrecedent =
                coveringPrecedent;

            AllegianceCrisisQuestionId =
                hasCrisis
                    ? allegianceCrisisQuestionId.Trim()
                    : null;

            PendingActivationId =
                hasPending
                    ? pendingActivationId.Trim()
                    : null;
        }

        private static void ValidateDegree(
            TagDegree degree)
        {
            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    degree))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(degree)
                );
            }
        }
    }
}