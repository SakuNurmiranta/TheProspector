namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class IdeaTrveCapabilityEvaluation
    {
        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public bool IsFormalPair { get; }

        public PairIntegrity PairIntegrity { get; }

        public int DominantDegree { get; }

        public float SocietyBacking { get; }

        public bool IsTrveCapable { get; }

        public float ContextualTrvePotential { get; }

        public IdeaTrveCapabilityEvaluation(
            string sourceIdeaId,
            int ideaIndex,
            bool isFormalPair,
            PairIntegrity pairIntegrity,
            int dominantDegree,
            float societyBacking,
            bool isTrveCapable,
            float contextualTrvePotential)
        {
            SourceIdeaId =
                sourceIdeaId;

            IdeaIndex =
                ideaIndex;

            IsFormalPair =
                isFormalPair;

            PairIntegrity =
                pairIntegrity;

            DominantDegree =
                dominantDegree;

            SocietyBacking =
                societyBacking;

            IsTrveCapable =
                isTrveCapable;

            ContextualTrvePotential =
                contextualTrvePotential;
        }
    }
}