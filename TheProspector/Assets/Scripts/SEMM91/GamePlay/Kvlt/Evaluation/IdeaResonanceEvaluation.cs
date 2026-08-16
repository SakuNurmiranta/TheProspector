namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class IdeaResonanceEvaluation
    {
        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public float SurfaceContribution { get; }

        public float Extremity { get; }

        public float ExtremityFactor { get; }

        public float SignedResonanceContribution { get; }

        public IdeaResonanceEvaluation(
            string sourceIdeaId,
            int ideaIndex,
            float surfaceContribution,
            float extremity,
            float extremityFactor,
            float signedResonanceContribution)
        {
            SourceIdeaId =
                sourceIdeaId;

            IdeaIndex =
                ideaIndex;

            SurfaceContribution =
                surfaceContribution;

            Extremity =
                extremity;

            ExtremityFactor =
                extremityFactor;

            SignedResonanceContribution =
                signedResonanceContribution;
        }
    }
}