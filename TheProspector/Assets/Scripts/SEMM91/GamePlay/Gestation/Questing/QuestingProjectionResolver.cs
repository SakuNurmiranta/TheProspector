using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Gestation.Questing
{
    public sealed class QuestingProjectionResolver
    {
        public const float MinimumMoodValue = -3.0f;
        public const float MaximumMoodValue = 3.0f;

        public const float MinimumCompositeValue = -4.0f;
        public const float MaximumCompositeValue = 4.0f;

        public const float MinimumSynchronisation = 0.05f;
        public const float MaximumSynchronisation = 1.0f;

        public const float OutputX = 2.0f;
        public const float InterruptionBoundary = 4.0f;

        public QuestingResult Resolve(QuestingRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            ValidateAxis(request.ActiveAxis);
            ValidateFinite(nameof(request.MoodValue), request.MoodValue);
            ValidateFinite(nameof(request.CompositeValue), request.CompositeValue);
            ValidateFinite(
                nameof(request.SceneSynchronisation),
                request.SceneSynchronisation
            );

            ValidateRange(
                nameof(request.MoodValue),
                request.MoodValue,
                MinimumMoodValue,
                MaximumMoodValue
            );

            ValidateRange(
                nameof(request.CompositeValue),
                request.CompositeValue,
                MinimumCompositeValue,
                MaximumCompositeValue
            );

            ValidateRange(
                nameof(request.SceneSynchronisation),
                request.SceneSynchronisation,
                MinimumSynchronisation,
                MaximumSynchronisation
            );

            float slope =
                (request.CompositeValue - request.MoodValue) /
                request.SceneSynchronisation;

            float projectedValue =
                request.MoodValue + OutputX * slope;
            
            float magnitude = Math.Abs(projectedValue);
            
            bool isInterrupted = 
                magnitude >= InterruptionBoundary;
            
            QuestingPolarity polarity =
                DeterminePolarity(projectedValue);

            int? degree = DetermineDegree(
                magnitude,
                polarity,
                isInterrupted
            );
            
            QuestingInterruptionCause interruptionCause =
                isInterrupted
                ? QuestingInterruptionCause.ProjectionBoundaryExceeded
                : QuestingInterruptionCause.None;

            return new QuestingResult(
                request.ActiveAxis,
                request.MoodValue,
                request.CompositeValue,
                request.SceneSynchronisation,
                slope,
                projectedValue,
                polarity,
                degree,
                isInterrupted,
                interruptionCause
            );
        }

        private static QuestingPolarity DeterminePolarity(
            float projectedValue)
        {
            if (projectedValue < 0.0f)
                return QuestingPolarity.Negative;
            
            if (projectedValue > 0.0f)
                return QuestingPolarity.Positive;
            
            return QuestingPolarity.Neutral;
        }

        private static int? DetermineDegree(
            float magnitude,
            QuestingPolarity polarity,
            bool isInterrupted)
        {
            if (isInterrupted ||
                polarity == QuestingPolarity.Neutral)
            {
                return null;
            }

            int degree = (int)Math.Floor(magnitude);

            return Math.Min(3, degree);
        }

        private static void ValidateAxis(TagAxis axis)
        {
            bool isVerticalSliceAxis =
                axis == TagAxis.Symbolic ||
                axis == TagAxis.Existential ||
                axis == TagAxis.Expressive;

            if (!isVerticalSliceAxis)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(axis),
                    axis,
                    "Pajazzo currently only supports vertical slice axes: Symbolic, Existential, Expressive."
                );
            }
        }

        private static void ValidateFinite(
            string parameterName,
            float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    $"{parameterName} must be finite."
                );
            }
        }

        private static void ValidateRange(
            string parameterName,
            float value,
            float minimum,
            float maximum)
        {
            if (value < minimum || value > maximum)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    $"{parameterName} must be between {minimum} and {maximum}."
                );
            }
        }
    }
}