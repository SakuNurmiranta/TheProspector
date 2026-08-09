namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class IdeaGravityEvaluation
    {
        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public float SignedResonanceContribution { get; }

        public float TrveFraction { get; }

        public float SignedGravityContribution { get; }

        public IdeaGravityEvaluation(
            string sourceIdeaId,
            int ideaIndex,
            float signedResonanceContribution,
            float trveFraction,
            float signedGravityContribution)
        {
            SourceIdeaId =
                sourceIdeaId;

            IdeaIndex =
                ideaIndex;

            SignedResonanceContribution =
                signedResonanceContribution;

            TrveFraction =
                trveFraction;

            SignedGravityContribution =
                signedGravityContribution;
        }
    }
}