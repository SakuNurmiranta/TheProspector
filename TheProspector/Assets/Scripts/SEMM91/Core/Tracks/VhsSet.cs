using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    public class VhsSet
    {
        public string VhsSetId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<VhsTrack> VhsTracks => _vhsTracks;
        
        public int CreatedTurn { get; }
        public int LastRehearsedTurn { get; private set; }
        
        private readonly List<VhsTrack> _vhsTracks = new();

        public VhsSet(string vhsSetId, string displayName, int createdTurn)
        {
            VhsSetId = vhsSetId;
            DisplayName = displayName;
            CreatedTurn = createdTurn;
            LastRehearsedTurn = createdTurn;
        }

        public void AddTrack(VhsTrack vhsTrack)
        {
            if (vhsTrack == null) return;
            _vhsTracks.Add(vhsTrack);
        }

        public void RehearseAll(float conveyanceGain, int currentTurn)
        {
            foreach (VhsTrack vhsTrack in _vhsTracks)
            {
                vhsTrack.Rehearse(conveyanceGain, currentTurn);
            }
            
            LastRehearsedTurn = currentTurn;
            
        }

        public void ApplyForgetfulness(float multiplier, float minimumConveyance)
        {
            foreach (VhsTrack vhsTrack in _vhsTracks)
            {
                vhsTrack.ApplyConveyanceMultiplier(multiplier, minimumConveyance);
            }
        }

        public VhsTrack GetLatestVhsTrack()
        {
            if (_vhsTracks.Count == 0) return null;

            return _vhsTracks[_vhsTracks.Count - 1];
        }
    }
}