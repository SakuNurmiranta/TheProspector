using System;
using System.Collections.Generic;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class TrackSensationalExtremityEvaluator
    {
        public TrackSensationalExtremityEvaluation Evaluate(
            DemoTapeTrackSnapshot track,
            TrackEvaluationEnvironment environment)
        {
            if (track == null)
            {
                throw new ArgumentNullException(
                    nameof(track)
                );
            }

            if (environment == null)
            {
                throw new ArgumentNullException(
                    nameof(environment)
                );
            }

            List<TagSensationalExtremityEvaluation>
                occurrenceEvaluations =
                    new();

            int trackEs = 0;

            foreach (DemoTapeIdeaSnapshot idea
                     in track.IdeaSnapshots)
            {
                for (int occurrenceIndex = 0;
                     occurrenceIndex <
                     idea.TagOccurrences.Count;
                     occurrenceIndex++)
                {
                    DemoTapeTagOccurrenceSnapshot
                        occurrence =
                            idea.TagOccurrences[
                                occurrenceIndex
                            ];

                    TagPole antiPole =
                        GetOppositePole(
                            occurrence.Pole
                        );

                    bool isTransgressive =
                        environment
                            .TryGetSocietyNormativeDegree(
                                occurrence.Axis,
                                antiPole,
                                out _
                            );

                    TagSensationalExtremityEvaluation
                        evaluation =
                            new TagSensationalExtremityEvaluation(
                                idea.SourceIdeaId,
                                idea.IdeaIndex,
                                occurrenceIndex,
                                occurrence.Role,
                                occurrence.Axis,
                                occurrence.Pole,
                                occurrence.Degree,
                                isTransgressive
                            );

                    occurrenceEvaluations.Add(
                        evaluation
                    );

                    if (isTransgressive)
                    {
                        trackEs =
                            Math.Max(
                                trackEs,
                                (int)occurrence.Degree
                            );
                    }
                }
            }

            return new TrackSensationalExtremityEvaluation(
                track.SourceTrackId,
                trackEs,
                occurrenceEvaluations
            );
        }

        private static TagPole GetOppositePole(
            TagPole pole)
        {
            return pole switch
            {
                TagPole.Negative =>
                    TagPole.Positive,

                TagPole.Positive =>
                    TagPole.Negative,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(pole),
                        pole,
                        "Unsupported Tag pole."
                    )
            };
        }
    }
}