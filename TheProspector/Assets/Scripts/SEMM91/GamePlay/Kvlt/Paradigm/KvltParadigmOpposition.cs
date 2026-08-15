using System;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltParadigmOpposition
    {
        public string FirstHailAspectId { get; }
        public string SecondHailAspectId { get; }
        public string HailAspectAId => FirstHailAspectId;
        public string HailAspectBId => SecondHailAspectId;

        public KvltParadigmOpposition(
            string firstHailAspectId,
            string secondHailAspectId)
        {
            FirstHailAspectId = RequireText(
                firstHailAspectId,
                nameof(firstHailAspectId));

            SecondHailAspectId = RequireText(
                secondHailAspectId,
                nameof(secondHailAspectId));

            if (FirstHailAspectId == SecondHailAspectId)
            {
                throw new ArgumentException(
                    "A paradigm cannot oppose itself.",
                    nameof(secondHailAspectId));
            }
        }

        public bool Contains(string hailAspectId)
        {
            return hailAspectId == FirstHailAspectId ||
                   hailAspectId == SecondHailAspectId;
        }

        public bool Matches(
            string firstHailAspectId,
            string secondHailAspectId)
        {
            return
                firstHailAspectId == FirstHailAspectId &&
                secondHailAspectId == SecondHailAspectId ||
                firstHailAspectId == SecondHailAspectId &&
                secondHailAspectId == FirstHailAspectId;
        }

        public string GetOpposingAspectId(
            string hailAspectId)
        {
            if (hailAspectId == FirstHailAspectId)
                return SecondHailAspectId;

            if (hailAspectId == SecondHailAspectId)
                return FirstHailAspectId;

            throw new ArgumentException(
                "Aspect does not belong to this opposition.",
                nameof(hailAspectId));
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "Paradigm identity cannot be empty.",
                    parameterName);

            return value.Trim();
        }
    }
}
