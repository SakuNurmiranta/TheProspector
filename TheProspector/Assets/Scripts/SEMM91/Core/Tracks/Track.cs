using System.Collections.Generic;
using SEMM91.Core.Ideas;

namespace SEMM91.Core.Tracks
{
    public class Track
    {
        public string TrackId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<Idea> Ideas => _ideas;
        public float Conveyance { get; private set; }
        
        private readonly List<Idea> _ideas = new();

        public Track(string trackId, string displayName, float initialConveyance)
        {
            TrackId = trackId;
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