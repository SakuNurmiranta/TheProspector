using System;
using UnityEngine.Serialization;

namespace SEMM91.Core.Tags
{
    [Serializable]
    public struct TagInstance
    {
        [FormerlySerializedAs("Axis")] public TagAxis axis;
        [FormerlySerializedAs("Pole")] public TagPole pole;
        [FormerlySerializedAs("Degree")] public TagDegree degree;

        public TagInstance(TagAxis axis, TagPole pole, TagDegree degree)
        {
            this.axis = axis;
            this.pole = pole;
            this.degree = degree;
        }

        public bool IsOpposedTo(TagInstance other)
        {
            return axis == other.axis && pole != other.pole;
        }

        
        // ReSharper disable once InconsistentNaming
        public bool HasTRVEMinimumDegree()
        {
            return degree >= TagDegree.Weak;
        }

        public override string ToString()
        {
            return $"{axis} {pole} {degree}";
        }
    }
}