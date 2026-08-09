namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class IdeaSocietyBackingEvaluation
    {
        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public bool IsFormalPair { get; }

        public float RecordedSubmissiveSemanticForce
        {
            get;
        }

        public float SocietyNormativeForce
        {
            get;
        }

        public float SocietyBacking
        {
            get;
        }

        public IdeaSocietyBackingEvaluation(
            string sourceIdeaId,
            int ideaIndex,
            bool isFormalPair,
            float recordedSubmissiveSemanticForce,
            float societyNormativeForce,
            float societyBacking)
        {
            SourceIdeaId =
                sourceIdeaId;

            IdeaIndex =
                ideaIndex;

            IsFormalPair =
                isFormalPair;

            RecordedSubmissiveSemanticForce =
                recordedSubmissiveSemanticForce;

            SocietyNormativeForce =
                societyNormativeForce;

            SocietyBacking =
                societyBacking;
        }
    }
}