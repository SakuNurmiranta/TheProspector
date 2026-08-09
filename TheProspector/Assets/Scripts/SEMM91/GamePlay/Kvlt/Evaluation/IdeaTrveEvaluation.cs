namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class IdeaTrveEvaluation
    {
        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public bool IsFormalPair { get; }

        public PairIntegrity PairIntegrity { get; }

        public bool IsTrveCapable { get; }

        public int DominantDegree { get; }

        public float SocietyBacking { get; }

        public float ContextualTrvePotential { get; }

        public int LegitimateActiveDegree { get; }

        public float ActivationFraction { get; }

        public float ActiveTrveContribution { get; }

        public IdeaTrveEvaluation(
            string sourceIdeaId,
            int ideaIndex,
            bool isFormalPair,
            PairIntegrity pairIntegrity,
            bool isTrveCapable,
            int dominantDegree,
            float societyBacking,
            float contextualTrvePotential,
            int legitimateActiveDegree,
            float activationFraction,
            float activeTrveContribution)
        {
            SourceIdeaId =
                sourceIdeaId;

            IdeaIndex =
                ideaIndex;

            IsFormalPair =
                isFormalPair;

            PairIntegrity =
                pairIntegrity;

            IsTrveCapable =
                isTrveCapable;

            DominantDegree =
                dominantDegree;

            SocietyBacking =
                societyBacking;

            ContextualTrvePotential =
                contextualTrvePotential;

            LegitimateActiveDegree =
                legitimateActiveDegree;

            ActivationFraction =
                activationFraction;

            ActiveTrveContribution =
                activeTrveContribution;
        }
    }
}