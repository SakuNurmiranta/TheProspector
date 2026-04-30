using System;

namespace SEMM91.Core.Tags
{
    [Serializable]
    public struct TagInstance
    {
        public TagAxis Axis;
        public TagPole Pole;
        public TagDegree Degree;

        public TagInstance(TagAxis axis, TagPole pole, TagDegree degree)
        {
            Axis = axis;
            Pole = pole;
            Degree = degree;
        }

        public bool IsOpposedTo(TagInstance other)
        {
            return Axis == other.Axis && Pole != other.Pole;
        }

        public bool HasTrveMinimumDegree()
        {
            return Degree >= TagDegree.Weak;
        }

        public override string ToString()
        {
            return $"{Axis} {Pole} {Degree}";
        }
    }
}