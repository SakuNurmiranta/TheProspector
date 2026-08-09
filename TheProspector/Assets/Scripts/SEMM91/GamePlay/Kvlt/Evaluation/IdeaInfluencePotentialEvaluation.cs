namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class IdeaInfluencePotentialEvaluation
    {
        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public float CanonSurfaceContribution { get; }

        public float Extremity { get; }

        public float CanonSignedResonanceContribution
        {
            get;
        }

        public float MaximumValidTrveContribution
        {
            get;
        }

        public IdeaInfluencePotentialEvaluation(
            string sourceIdeaId,
            int ideaIndex,
            float canonSurfaceContribution,
            float extremity,
            float canonSignedResonanceContribution,
            float maximumValidTrveContribution)
        {
            SourceIdeaId =
                sourceIdeaId;

            IdeaIndex =
                ideaIndex;

            CanonSurfaceContribution =
                canonSurfaceContribution;

            Extremity =
                extremity;

            CanonSignedResonanceContribution =
                canonSignedResonanceContribution;

            MaximumValidTrveContribution =
                maximumValidTrveContribution;
        }
    }
}