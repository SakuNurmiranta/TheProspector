using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Gestation.Questing
{
    public enum QuestingPolarity
    {
        Negative = -1,
        Neutral = 0,
        Positive = 1
    }

    public enum QuestingInterruptionCause
    {
        None = 0,
        ProjectionBoundaryExceeded = 1
    }

    public sealed class QuestingRequest
    {
        public TagAxis ActiveAxis { get; }
        public float MoodValue { get; }
        public float CompositeValue { get; }
        public float SceneSynchronisation { get; }

        public QuestingRequest(
            TagAxis activeAxis,
            float moodValue,
            float compositeValue,
            float sceneSynchronisation)
        {
            ActiveAxis = activeAxis;
            MoodValue = moodValue;
            CompositeValue = compositeValue;
            SceneSynchronisation = sceneSynchronisation;
        }
    }

    public sealed class QuestingResult
    {
        public TagAxis ActiveAxis { get; }
        public float InputMoodValue { get; }
        public float CompositeValue { get; }
        public float SynchronisationValue { get; }

        public float Slope { get; }
        public float ProjectedValue { get; }

        public QuestingPolarity OutputPolarity { get; }

        public int? OutputDegree { get; }
        public bool IsInterrupted { get; }
        public bool IsStable => !IsInterrupted;

        public QuestingInterruptionCause InterruptionCause { get; }

        public bool ProducesTransientTag =>
            IsStable &&
            OutputPolarity != QuestingPolarity.Neutral &&
            OutputDegree.HasValue;

        internal QuestingResult(
            TagAxis activeAxis,
            float inputMoodValue,
            float compositeValue,
            float synchronisationValue,
            float slope,
            float projectedValue,
            QuestingPolarity outputPolarity,
            int? outputDegree,
            bool isInterrupted,
            QuestingInterruptionCause interruptionCause)
        {
            ActiveAxis = activeAxis;
            InputMoodValue = inputMoodValue;
            CompositeValue = compositeValue;
            SynchronisationValue = synchronisationValue;
            Slope = slope;
            ProjectedValue = projectedValue;
            OutputPolarity = outputPolarity;
            OutputDegree = outputDegree;
            IsInterrupted = isInterrupted;
            InterruptionCause = interruptionCause;
        }
    }
}



