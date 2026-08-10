using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class
        ActivationLegitimacyCrisisGroup
    {
        private readonly
            ActivationLegitimacyAssessment[]
                assessments;

        public AllegianceCrisisQuestion
            Question { get; }

        public IReadOnlyList<
            ActivationLegitimacyAssessment>
            Assessments =>
                assessments;

        public ActivationLegitimacyCrisisGroup(
            AllegianceCrisisQuestion question,
            IReadOnlyList<
                ActivationLegitimacyAssessment>
                assessments)
        {
            Question =
                question ??
                throw new ArgumentNullException(
                    nameof(question)
                );

            if (assessments == null ||
                assessments.Count == 0)
            {
                throw new ArgumentException(
                    "Crisis group requires at least " +
                    "one uncovered assessment.",
                    nameof(assessments)
                );
            }

            this.assessments =
                new ActivationLegitimacyAssessment[
                    assessments.Count
                ];

            for (int index = 0;
                 index < assessments.Count;
                 index++)
            {
                ActivationLegitimacyAssessment
                    assessment =
                        assessments[index] ??
                        throw new ArgumentException(
                            "Crisis group cannot contain " +
                            "a null assessment.",
                            nameof(assessments)
                        );

                if (assessment.Disposition !=
                    ActivationLegitimacyDisposition
                        .RequiresAllegianceCrisis)
                {
                    throw new ArgumentException(
                        "Crisis group may contain only " +
                        "uncovered assessments.",
                        nameof(assessments)
                    );
                }

                ActivationLegitimacyCandidate
                    candidate =
                        assessment.Candidate;

                if (candidate.HappeningId !=
                        question.HappeningId ||
                    candidate.SourceIntentId !=
                        question.SourceIntentId ||
                    candidate.ActorEntityId !=
                        question.TriggeringActorEntityId ||
                    candidate.BehaviorTypeId !=
                        question.BehaviorTypeId ||
                    candidate.Axis !=
                        question.Axis ||
                    candidate.Pole !=
                        question.Pole ||
                    candidate.PraxisDegree !=
                        question.PraxisDegree)
                {
                    throw new ArgumentException(
                        "Assessment does not belong to " +
                        "this Allegiance Crisis question.",
                        nameof(assessments)
                    );
                }

                this.assessments[index] =
                    assessment;
            }
        }
    }
}