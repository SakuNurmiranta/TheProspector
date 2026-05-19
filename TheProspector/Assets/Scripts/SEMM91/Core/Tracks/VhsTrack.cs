//VhsTrack is the track in mutable draft state, before being released on a record. When a band performs live,
//it uses the VhsTrack version of the track, as it represents the bands current handle on the track.

using System.Collections.Generic;
using SEMM91.Core.Ideas;

namespace SEMM91.Core.Tracks
{
    public class VhsTrack
    {
        private const int RawRemovalRehearsalThreshold = 3;
        private const int HonedRehearsalThreshold = 6;
        public string VhsTrackId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<Idea> Ideas => _ideas;
        public float Conveyance { get; private set; }
        public float ConveyanceMax { get; private set; } = 1.0f;
        
        public int LastRehearsedTurn { get; private set; }
        public int RehearsalCount { get; private set; }
        
        public bool IsRaw { get; private set; } = true;
        public bool IsHoned { get; private set; } 
        
        private readonly List<Idea> _ideas = new();

        public VhsTrack(
            string vhsTrackId, 
            string displayName, 
            float initialConveyance,
            int createdTurn)
        {
            VhsTrackId = vhsTrackId;
            DisplayName = displayName;
            Conveyance = Clamp01(initialConveyance);
            LastRehearsedTurn = createdTurn;

            RehearsalCount = 1;
            UpdateLifecycleFlags();

        }

        public void AddIdea(Idea idea)
        {
            if (idea == null) 
                return;
            
            _ideas.Add(idea);
        }

        public void Rehearse(float conveyanceGain, int currentTurn)
        {
            RehearsalCount++;
            
            Conveyance = Clamp01(Conveyance + conveyanceGain);

            if (Conveyance > ConveyanceMax) Conveyance = ConveyanceMax;

            LastRehearsedTurn = currentTurn;

            UpdateLifecycleFlags();
            
        }

        public void Erode(float conveyanceLoss)
        {
            Conveyance = Clamp01(Conveyance - conveyanceLoss);
            
        }
        
        private void UpdateLifecycleFlags()
        {
            IsRaw = RehearsalCount <= RawRemovalRehearsalThreshold;
            if (!IsHoned && RehearsalCount >= HonedRehearsalThreshold)
            {
                IsHoned = true;
                ConveyanceMax = Conveyance;
            }
        }

        private static float Clamp01(float value)
        {
            if (value < 0.0f) return 0.0f;
            if (value > 1.0f) return 1.0f;
            return value;
        }
        
        public void ApplyConveyanceMultiplier(float multiplier, float minimumConveyance = 0.0f)
        {
            Conveyance = Clamp01(Conveyance * multiplier);
            
            if (Conveyance < minimumConveyance) Conveyance = minimumConveyance;
        }
    }
}