using System;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Immutable historical record of a
    /// successfully-fettered SceneRelease being
    /// pushed across the scene's outer boundary.
    ///
    /// This is distinct from Failed-to-Fetter:
    /// Rejected material entered and participated
    /// in the living Field before expulsion.
    /// </summary>
    public sealed class SceneReleaseRejectionState
    {
        public int RejectedTurn { get; }

        public float OuterBoundary { get; }

        public float RejectedPosition { get; }

        public float PeakInwardPosition { get; }

        public float OutwardOvershoot { get; }

        public SceneReleaseRejectionState(
            int rejectedTurn,
            float outerBoundary,
            float rejectedPosition,
            float peakInwardPosition)
        {
            if (rejectedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rejectedTurn)
                );
            }

            if (!IsFinite(outerBoundary))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(outerBoundary)
                );
            }

            if (!IsFinite(rejectedPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rejectedPosition)
                );
            }

            if (!IsFinite(peakInwardPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(peakInwardPosition)
                );
            }

            if (rejectedPosition >=
                outerBoundary)
            {
                throw new ArgumentException(
                    "Rejected position must be " +
                    "strictly beyond the outer boundary.",
                    nameof(rejectedPosition)
                );
            }

            if (peakInwardPosition <
                rejectedPosition)
            {
                throw new ArgumentException(
                    "Peak inward position cannot be " +
                    "outward of the final rejected position.",
                    nameof(peakInwardPosition)
                );
            }

            RejectedTurn =
                rejectedTurn;

            OuterBoundary =
                outerBoundary;

            RejectedPosition =
                rejectedPosition;

            PeakInwardPosition =
                peakInwardPosition;

            OutwardOvershoot =
                outerBoundary -
                rejectedPosition;
        }

        private static bool IsFinite(
            float value)
        {
            return
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}