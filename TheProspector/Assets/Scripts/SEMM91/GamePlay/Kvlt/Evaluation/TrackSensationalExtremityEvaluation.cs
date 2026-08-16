using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackSensationalExtremityEvaluation
    {
        private readonly
            TagSensationalExtremityEvaluation[]
                occurrenceEvaluations;

        public string SourceTrackId { get; }

        public int SensationalExtremity { get; }

        public bool HasSocietyTransgressiveOccurrence
        {
            get;
        }

        public IReadOnlyList<
            TagSensationalExtremityEvaluation>
            OccurrenceEvaluations =>
                occurrenceEvaluations;

        public TrackSensationalExtremityEvaluation(
            string sourceTrackId,
            int sensationalExtremity,
            IReadOnlyList<
                TagSensationalExtremityEvaluation>
                occurrenceEvaluations)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceTrackId))
            {
                throw new ArgumentException(
                    "Sensational Extremity evaluation " +
                    "requires a source Track ID.",
                    nameof(sourceTrackId)
                );
            }

            if (occurrenceEvaluations == null)
            {
                throw new ArgumentNullException(
                    nameof(occurrenceEvaluations)
                );
            }

            SourceTrackId =
                sourceTrackId.Trim();

            SensationalExtremity =
                sensationalExtremity;

            this.occurrenceEvaluations =
                new TagSensationalExtremityEvaluation[
                    occurrenceEvaluations.Count
                ];

            bool hasTransgressiveOccurrence =
                false;

            for (int index = 0;
                 index < occurrenceEvaluations.Count;
                 index++)
            {
                TagSensationalExtremityEvaluation
                    evaluation =
                        occurrenceEvaluations[index] ??
                        throw new ArgumentException(
                            "Sensational Extremity evaluation " +
                            "cannot contain a null occurrence result.",
                            nameof(occurrenceEvaluations)
                        );

                this.occurrenceEvaluations[index] =
                    evaluation;

                if (evaluation.IsSocietyTransgressive)
                {
                    hasTransgressiveOccurrence =
                        true;
                }
            }

            HasSocietyTransgressiveOccurrence =
                hasTransgressiveOccurrence;
        }
    }
}