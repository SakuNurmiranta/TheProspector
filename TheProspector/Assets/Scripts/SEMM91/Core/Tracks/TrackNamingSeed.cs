using System;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Produces a deterministic naming seed from stable Track
    /// identity.
    ///
    /// Does not use string.GetHashCode because its result is not
    /// intended as a persistent cross-runtime identifier.
    /// </summary>
    public static class TrackNamingSeed
    {
        private const uint HashOffsetBasis =
            2166136261u;

        private const uint HashPrime =
            16777619u;

        public static int FromTrack(Track track)
        {
            if (track == null)
            {
                throw new ArgumentNullException(
                    nameof(track)
                );
            }

            return FromTrackId(
                track.VhsTrackId
            );
        }

        public static int FromTrackId(
            string trackId)
        {
            if (string.IsNullOrWhiteSpace(trackId))
            {
                throw new ArgumentException(
                    "Track identity cannot be empty.",
                    nameof(trackId)
                );
            }

            uint hash = HashOffsetBasis;

            /*
             * Mix both bytes of each UTF-16 character so the
             * result is deterministic without encoding,
             * culture, or runtime hash dependencies.
             */
            foreach (char character in trackId)
            {
                unchecked
                {
                    hash ^= (byte)(
                        character & 0x00FF
                    );

                    hash *= HashPrime;

                    hash ^= (byte)(
                        (character >> 8) & 0x00FF
                    );

                    hash *= HashPrime;
                }
            }

            /*
             * Naming APIs currently require a non-negative int.
             * Masking avoids the int.MinValue Math.Abs case.
             */
            return (int)(
                hash & 0x7FFFFFFFu
            );
        }
    }
}