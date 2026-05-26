using System;

namespace SEMM91.Core.Recordings
{
    [Serializable]
    public class DemoTapeTrackSnapshot
    {
        public string SourceTrackId { get; }
        public string DisplayName { get; }
        public float SourceConveyance { get; }
        public float RecordedConveyance { get; }

        public DemoTapeTrackSnapshot(
            string sourceTrackId,
            string displayName,
            float sourceConveyance,
            float recordedConveyance)
        {
            SourceTrackId = sourceTrackId;
            DisplayName = displayName;
            SourceConveyance = sourceConveyance;
            RecordedConveyance = recordedConveyance;
        }
    }
}