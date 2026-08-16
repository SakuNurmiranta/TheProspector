using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Transgression
{
    public sealed class AllegianceCrisisRegistry
    {
        private readonly
            List<AllegianceCrisis>
                crisesInOrder =
                    new();

        private readonly
            Dictionary<string, AllegianceCrisis>
                crisesByQuestionId =
                    new(
                        StringComparer.Ordinal
                    );

        public int Count =>
            crisesInOrder.Count;

        public void Record(
            AllegianceCrisis crisis)
        {
            if (crisis == null)
            {
                throw new ArgumentNullException(
                    nameof(crisis)
                );
            }

            string questionId =
                crisis.Question.QuestionId;

            if (crisesByQuestionId.ContainsKey(
                    questionId))
            {
                throw new InvalidOperationException(
                    "Allegiance Crisis already exists " +
                    $"for question {questionId}."
                );
            }

            crisesInOrder.Add(crisis);

            crisesByQuestionId.Add(
                questionId,
                crisis
            );
        }

        public bool TryGet(
            string questionId,
            out AllegianceCrisis crisis)
        {
            if (string.IsNullOrWhiteSpace(
                    questionId))
            {
                crisis = null;
                return false;
            }

            return crisesByQuestionId.TryGetValue(
                questionId,
                out crisis
            );
        }

        public IReadOnlyList<AllegianceCrisis>
            GetAll()
        {
            return crisesInOrder.ToArray();
        }

        public IReadOnlyList<AllegianceCrisis>
            GetOpen()
        {
            List<AllegianceCrisis> open =
                new();

            foreach (
                AllegianceCrisis crisis
                in crisesInOrder)
            {
                if (crisis.IsOpen)
                {
                    open.Add(crisis);
                }
            }

            return open.ToArray();
        }
    }
}