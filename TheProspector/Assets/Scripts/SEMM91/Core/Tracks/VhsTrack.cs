//VhsTrack is the track in mutable draft state, before being released on a record. When a band performs live,
//it uses the VhsTrack version of the track, as it represents the bands current handle on the track.

using System.Collections.Generic;
using SEMM91.Core.Ideas;

namespace SEMM91.Core.Tracks
{
    public class VhsTrack
    {
        public string VhsTrackId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<Idea> Ideas => _ideas;
        public float Conveyance { get; private set; }
        
        private readonly List<Idea> _ideas = new();

        public VhsTrack(string vhsTrackId, string displayName, float initialConveyance)
        {
            VhsTrackId = vhsTrackId;
            DisplayName = displayName;
            Conveyance = initialConveyance;

        }

        public void AddIdea(Idea idea)
        {
            if (idea == null) 
                return;
            
            _ideas.Add(idea);
        }
    }
}