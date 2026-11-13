using System;

namespace SEMM91.Domain
{
    /// <summary>
    /// Visual aging of a directional communication.
    /// Zero means freshly used; one means 365 days old.
    ///
    /// This does not affect communication history,
    /// network availability or social state.
    /// </summary>
    public static class CommunicationRecency
    {
        public const int FadeDays = 365;

        public static float Age01(
            int lastSentDay,
            double visualDay)
        {
            if (lastSentDay < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lastSentDay));
            }

            if (double.IsNaN(visualDay) ||
                double.IsInfinity(visualDay) ||
                visualDay < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(visualDay));
            }

            double elapsed = visualDay - lastSentDay;

            return (float)Math.Max(
                0.0,
                Math.Min(1.0, elapsed / FadeDays));
        }
    }
}