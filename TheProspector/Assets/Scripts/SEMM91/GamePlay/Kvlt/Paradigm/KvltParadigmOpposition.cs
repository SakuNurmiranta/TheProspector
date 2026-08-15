using System;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltParadigmOpposition
    {
        public string FirstHailAspectId { get; }
        public string SecondHailAspectId { get; }

        public KvltParadigmOpposition(
            string firstHailAspectId,
            string secondHailAspectId)
        {
            FirstHailAspectId =
                RequireText(
                    firstHailAspectId,
                    nameof(firstHailAspectId)
                );

            SecondHailAspectId =
                RequireText(
                    secondHailAspectId,
                    nameof(secondHailAspectId)
                );

            if (FirstHailAspectId ==
                SecondHailAspectId)
            {
                throw new ArgumentException(
                    "Opposed paradigms must be distinct."
                );
            }
        }

        public bool Contains(string hailAspectId)
        {
            return
                hailAspectId ==
                FirstHailAspectId ||
                hailAspectId ==
                SecondHailAspectId;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Paradigm identity cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}