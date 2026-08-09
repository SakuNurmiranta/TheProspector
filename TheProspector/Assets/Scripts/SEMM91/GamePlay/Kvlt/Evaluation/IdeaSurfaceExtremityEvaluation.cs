namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class IdeaSurfaceExtremityEvaluation
    {
        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public float SurfaceContribution { get; }

        public float Extremity { get; }

        public IdeaSurfaceExtremityEvaluation(
            string sourceIdeaId,
            int ideaIndex,
            float surfaceContribution,
            float extremity)
        {
            SourceIdeaId = sourceIdeaId;
            IdeaIndex = ideaIndex;
            SurfaceContribution = surfaceContribution;
            Extremity = extremity;
        }
    }
}