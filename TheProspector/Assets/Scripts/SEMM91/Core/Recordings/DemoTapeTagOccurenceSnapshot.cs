using System;
using SEMM91.Core.Tags;

namespace SEMM91.Core.Recordings
{
    /// <summary>
    /// Immutable recording-time copy of one concrete Tag
    /// occurrence inside an Idea.
    /// </summary>
    [Serializable]
    public sealed class DemoTapeTagOccurrenceSnapshot
    {
        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree Degree { get; }

        public DemoTapeTagOccurrenceRole Role { get; }

        public DemoTapeTagOccurrenceSnapshot(
            TagAxis axis,
            TagPole pole,
            TagDegree degree,
            DemoTapeTagOccurrenceRole role)
        {
            Axis = axis;
            Pole = pole;
            Degree = degree;
            Role = role;
        }
    }
}