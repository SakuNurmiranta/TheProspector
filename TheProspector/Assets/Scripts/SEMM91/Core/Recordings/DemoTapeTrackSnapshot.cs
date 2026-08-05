using System;
using System.Collections.Generic;

namespace SEMM91.Core.Recordings
{
    [Serializable]
    public sealed class DemoTapeTrackSnapshot
    {
        private readonly DemoTapeIdeaSnapshot[]
            _ideaSnapshots;

        public string SourceTrackId { get; }

        public string DisplayName { get; }

        public float SourceConveyance { get; }

        public float RecordedConveyance { get; }

        public IReadOnlyList<DemoTapeIdeaSnapshot>
            IdeaSnapshots =>
                _ideaSnapshots;

        public int IdeaCount =>
            _ideaSnapshots.Length;

        /// <summary>
        /// Backward-compatible constructor for existing
        /// pre-session placeholder DemoTapes.
        /// </summary>
        public DemoTapeTrackSnapshot(
            string sourceTrackId,
            string displayName,
            float sourceConveyance,
            float recordedConveyance)
            : this(
                sourceTrackId,
                displayName,
                sourceConveyance,
                recordedConveyance,
                Array.Empty<DemoTapeIdeaSnapshot>()
            )
        {
        }

        public DemoTapeTrackSnapshot(
            string sourceTrackId,
            string displayName,
            float sourceConveyance,
            float recordedConveyance,
            IReadOnlyList<DemoTapeIdeaSnapshot>
                ideaSnapshots)
        {
            SourceTrackId = RequireText(
                sourceTrackId,
                nameof(sourceTrackId)
            );

            DisplayName = RequireText(
                displayName,
                nameof(displayName)
            );

            SourceConveyance =
                sourceConveyance;

            RecordedConveyance =
                recordedConveyance;

            _ideaSnapshots =
                CopyIdeaSnapshots(
                    ideaSnapshots
                );
        }

        private static DemoTapeIdeaSnapshot[]
            CopyIdeaSnapshots(
                IReadOnlyList<DemoTapeIdeaSnapshot>
                    snapshots)
        {
            if (snapshots == null)
            {
                throw new ArgumentNullException(
                    nameof(snapshots)
                );
            }

            DemoTapeIdeaSnapshot[] copy =
                new DemoTapeIdeaSnapshot[
                    snapshots.Count
                ];

            for (int index = 0;
                 index < snapshots.Count;
                 index++)
            {
                if (snapshots[index] == null)
                {
                    throw new ArgumentException(
                        "Track snapshot cannot contain a " +
                        "null Idea snapshot.",
                        nameof(snapshots)
                    );
                }

                copy[index] = snapshots[index];
            }

            return copy;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Snapshot text cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}