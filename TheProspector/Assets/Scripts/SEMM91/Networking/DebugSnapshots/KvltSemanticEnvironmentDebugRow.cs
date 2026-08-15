using System;
using Unity.Netcode;

namespace SEMM91.Networking.DebugSnapshots
{
    public struct KvltSemanticEnvironmentDebugRow :
        INetworkSerializable,
        IEquatable<KvltSemanticEnvironmentDebugRow>
    {
        public byte AxisValue;
        public byte PoleValue;
        public byte DegreeValue;
        public bool HasDerivation;
        public bool HasDirectCanon;
        public float CanonicalAffinity;
        public float RawPressure;
        public float EffectivePressure;
        public float FinalAffinity;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref AxisValue);
            serializer.SerializeValue(ref PoleValue);
            serializer.SerializeValue(ref DegreeValue);
            serializer.SerializeValue(ref HasDerivation);
            serializer.SerializeValue(ref HasDirectCanon);
            serializer.SerializeValue(ref CanonicalAffinity);
            serializer.SerializeValue(ref RawPressure);
            serializer.SerializeValue(ref EffectivePressure);
            serializer.SerializeValue(ref FinalAffinity);
        }

        public bool Equals(
            KvltSemanticEnvironmentDebugRow other)
        {
            return AxisValue == other.AxisValue &&
                   PoleValue == other.PoleValue &&
                   DegreeValue == other.DegreeValue &&
                   HasDerivation == other.HasDerivation &&
                   HasDirectCanon == other.HasDirectCanon &&
                   CanonicalAffinity.Equals(
                       other.CanonicalAffinity
                   ) &&
                   RawPressure.Equals(other.RawPressure) &&
                   EffectivePressure.Equals(
                       other.EffectivePressure
                   ) &&
                   FinalAffinity.Equals(
                       other.FinalAffinity
                   );
        }

        public override bool Equals(object obj)
        {
            return obj is
                       KvltSemanticEnvironmentDebugRow other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(AxisValue);
            hash.Add(PoleValue);
            hash.Add(DegreeValue);
            hash.Add(HasDerivation);
            hash.Add(HasDirectCanon);
            hash.Add(CanonicalAffinity);
            hash.Add(RawPressure);
            hash.Add(EffectivePressure);
            hash.Add(FinalAffinity);
            return hash.ToHashCode();
        }
    }
}