using System;
using System.Collections.Generic;
using SEMM91.Core.Tracks;
using System.Linq;

namespace SEMM91.Core.Recordings
{
    [Serializable]
    public class DemoTape
    {
        private readonly List<DemoTapeTrackSnapshot> trackSnapshots = new();
        
        public string DemoTapeId { get; }
        public string DisplayName { get; }
        public string SourceSetId { get; }
        public string SourceSetName { get; }
        public int RecordedTurn { get; }
        public int TakeCount { get; }
        public float RecordingInterest { get; }
        public float AverageConveyance { get; }
        
        public IReadOnlyList<DemoTapeTrackSnapshot> TrackSnapshots => trackSnapshots;

        public DemoTape(
            string demoTapeId,
            string displayName,
            string sourceSetId,
            string sourceSetName,
            int recordedTurn,
            int takeCount,
            float recordingInterest,
            IEnumerable<DemoTapeTrackSnapshot> snapshots)
        {
            DemoTapeId = demoTapeId;
            DisplayName = displayName;
            SourceSetId = sourceSetId;
            SourceSetName = sourceSetName;
            RecordedTurn = recordedTurn;
            TakeCount = takeCount;
            RecordingInterest = recordingInterest;
            
            if (snapshots != null)
                trackSnapshots.AddRange(snapshots);
            
            AverageConveyance = trackSnapshots.Count == 0 
                ? 0f 
                : trackSnapshots.Average(t => t.RecordedConveyance);
            
        }
        
    }
}