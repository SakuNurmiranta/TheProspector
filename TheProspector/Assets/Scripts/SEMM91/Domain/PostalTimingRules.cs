using System;

namespace SEMM91.Domain
{
    public static class PostalTimingRules
    {
        public const int DomesticDays = 2;
        public const int InternationalDays = 5;

        public const int MinReplyAllowanceDays = 5;
        public const int MaxReplyAllowanceDays = 7;

        public static int GetTransitDays(
            string sourceCountryCode,
            string targetCountryCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCountryCode))
                throw new ArgumentException(
                    "Source country cannot be empty.",
                    nameof(sourceCountryCode));

            if (string.IsNullOrWhiteSpace(targetCountryCode))
                throw new ArgumentException(
                    "Target country cannot be empty.",
                    nameof(targetCountryCode));

            bool sameCountry = string.Equals(
                sourceCountryCode.Trim(),
                targetCountryCode.Trim(),
                StringComparison.OrdinalIgnoreCase);

            return sameCountry
                ? DomesticDays
                : InternationalDays;
        }

        public static int CalculateDeadline(
            int sentDay,
            int transitDays,
            int allowanceDays)
        {
            if (sentDay < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(sentDay));

            if (transitDays < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(transitDays));

            if (allowanceDays < MinReplyAllowanceDays ||
                allowanceDays > MaxReplyAllowanceDays)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(allowanceDays),
                    "Reply allowance must be 5–7 days.");
            }

            return checked(
                sentDay + transitDays + allowanceDays);
        }
    }
}