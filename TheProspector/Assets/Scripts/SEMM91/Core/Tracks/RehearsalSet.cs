using System.Collections.Generic;

namespace SEMM91.Core.Tracks
{
    public class RehearsalSet
    {
        public string VhsSetId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<Track> VhsTracks => _vhsTracks;
        
        public int CreatedTurn { get; }
        public int LastRehearsedTurn { get; private set; }
        
        private readonly List<Track> _vhsTracks = new();

        public RehearsalSet(string vhsSetId, string displayName, int createdTurn)
        {
            VhsSetId = vhsSetId;
            DisplayName = displayName;
            CreatedTurn = createdTurn;
            LastRehearsedTurn = createdTurn;
        }

        public void AddTrack(Track track)
        {
            if (track == null) return;
            _vhsTracks.Add(track);
        }

        public void RehearseAll(float conveyanceGain, int currentTurn)
        {
            foreach (Track vhsTrack in _vhsTracks)
            {
                vhsTrack.Rehearse(conveyanceGain, currentTurn);
            }
            
            LastRehearsedTurn = currentTurn;
            
        }

        public void ApplyForgetfulness(float multiplier, float minimumConveyance)
        {
            foreach (Track vhsTrack in _vhsTracks)
            {
                vhsTrack.ApplyConveyanceMultiplier(multiplier, minimumConveyance);
            }
        }

        public Track GetLatestVhsTrack()
        {
            if (_vhsTracks.Count == 0) return null;

            return _vhsTracks[_vhsTracks.Count - 1];
        }
        
        public bool HasEmptyTrack
        {
            get
            {
                foreach (Track track in _vhsTracks)
                {
                    if (track != null &&
                        track.IsEmpty)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}