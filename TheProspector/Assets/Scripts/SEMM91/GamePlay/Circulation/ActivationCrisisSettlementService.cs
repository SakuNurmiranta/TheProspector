using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Applies the institutional meaning of one
    /// resolved Allegiance Crisis.
    ///
    /// This service may raise Accepted Transgression
    /// precedent but does not mutate SceneRelease
    /// activation state.
    /// </summary>
    public sealed class ActivationCrisisSettlementService
    {
        public ActivationCrisisSettlement Settle(
            ActivationLegitimacyCrisisGroup group,
            AllegianceCrisis crisis,
            AcceptedTransgressionState
                acceptedTransgressions)
        {
            if (group == null)
            {
                throw new ArgumentNullException(
                    nameof(group)
                );
            }

            if (crisis == null)
            {
                throw new ArgumentNullException(
                    nameof(crisis)
                );
            }

            if (acceptedTransgressions == null)
            {
                throw new ArgumentNullException(
                    nameof(acceptedTransgressions)
                );
            }

            if (!crisis.IsResolved)
            {
                throw new InvalidOperationException(
                    "Cannot settle activation consequences " +
                    "from an unresolved Allegiance Crisis."
                );
            }

            if (group.Question.QuestionId !=
                crisis.Question.QuestionId)
            {
                throw new ArgumentException(
                    "Crisis group and resolved crisis " +
                    "refer to different questions."
                );
            }

            return crisis.Resolution.Outcome ==
                    AllegianceCrisisOutcome.Kvlt
                ? SettleKvltVictory(
                    group,
                    crisis,
                    acceptedTransgressions
                )
                : SettleSocietyVictory(
                    group,
                    crisis
                );
        }

        private static ActivationCrisisSettlement
            SettleKvltVictory(
                ActivationLegitimacyCrisisGroup group,
                AllegianceCrisis crisis,
                AcceptedTransgressionState state)
        {
            AllegianceCrisisQuestion question =
                crisis.Question;

            AcceptedTransgressionRecord precedent;

            bool raisedPrecedent = false;

            if (!state.TryGetCoveringPrecedent(
                    question.BehaviorTypeId,
                    question.Axis,
                    question.Pole,
                    question.PraxisDegree,
                    out precedent))
            {
                precedent =
                    new AcceptedTransgressionRecord(
                        BuildAcceptedTransgressionId(
                            question.QuestionId
                        ),
                        state.KvltEntityId,
                        question.BehaviorTypeId,
                        question.Axis,
                        question.Pole,
                        question.PraxisDegree,
                        crisis.Resolution.ResolvedTurn,
                        AcceptedTransgressionSourceKind
                            .AllegianceCrisis,
                        question.QuestionId
                    );

                if (!state.TryAccept(
                        precedent))
                {
                    throw new InvalidOperationException(
                        "KVLT won Allegiance Crisis but " +
                        "Accepted Transgression precedent " +
                        "could not be recorded."
                    );
                }

                raisedPrecedent = true;
            }

            List<LegitimizedActivationCandidate>
                legitimized =
                    new();

            foreach (
                ActivationLegitimacyAssessment assessment
                in group.Assessments)
            {
                legitimized.Add(
                    new LegitimizedActivationCandidate(
                        assessment.Candidate,
                        question.QuestionId,
                        precedent,
                        crisis.Resolution.ResolvedTurn
                    )
                );
            }

            return new ActivationCrisisSettlement(
                question,
                crisis.Resolution,
                precedent,
                raisedPrecedent,
                legitimized,
                Array.Empty<
                    PendingActivationCandidate>()
            );
        }

        private static ActivationCrisisSettlement
            SettleSocietyVictory(
                ActivationLegitimacyCrisisGroup group,
                AllegianceCrisis crisis)
        {
            AllegianceCrisisQuestion question =
                crisis.Question;

            List<PendingActivationCandidate>
                pending =
                    new();

            foreach (
                ActivationLegitimacyAssessment assessment
                in group.Assessments)
            {
                pending.Add(
                    new PendingActivationCandidate(
                        assessment.Candidate,
                        question.QuestionId,
                        crisis.Resolution.ResolvedTurn
                    )
                );
            }

            return new ActivationCrisisSettlement(
                question,
                crisis.Resolution,
                null,
                false,
                Array.Empty<
                    LegitimizedActivationCandidate>(),
                pending
            );
        }

        private static string
            BuildAcceptedTransgressionId(
                string questionId)
        {
            return
                "AT|" +
                questionId;
        }
    }
}