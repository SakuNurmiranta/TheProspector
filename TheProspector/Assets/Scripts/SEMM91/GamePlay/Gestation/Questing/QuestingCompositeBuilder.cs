using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Gestation.Questing
{
    /// <summary>
    /// One already-interpreted signed influence on a Questing
    /// composite.
    ///
    /// This type records a contribution. It does not decide what
    /// actions or events mean.
    /// </summary>
    public sealed class QuestingCompositeContribution
    {
        public string SourceId { get; }
        public TagAxis Axis { get; }
        public float Value { get; }
        public string Description { get; }

        public QuestingCompositeContribution(
            string sourceId,
            TagAxis axis,
            float value,
            string description)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "Composite contribution source ID is required.",
                    nameof(sourceId)
                );
            }

            if (float.IsNaN(value) ||
                float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Composite contribution must be finite."
                );
            }

            SourceId = sourceId;
            Axis = axis;
            Value = value;
            Description = description ?? string.Empty;
        }
    }

    /// <summary>
    /// Inspectable result of building one Questing composite.
    /// </summary>
    public sealed class QuestingCompositeBuildResult
    {
        public TagAxis ActiveAxis { get; }
        public float RawValue { get; }
        public float CompositeValue { get; }

        public IReadOnlyList<QuestingCompositeContribution>
            AppliedContributions { get; }

        public bool WasClamped =>
            RawValue != CompositeValue;

        internal QuestingCompositeBuildResult(
            TagAxis activeAxis,
            float rawValue,
            float compositeValue,
            IReadOnlyList<QuestingCompositeContribution>
                appliedContributions)
        {
            ActiveAxis = activeAxis;
            RawValue = rawValue;
            CompositeValue = compositeValue;
            AppliedContributions = appliedContributions;
        }
    }

    /// <summary>
    /// Pure sum-and-clamp stage for Questing composite values.
    ///
    /// Contribution interpretation must happen before this class
    /// is called.
    /// </summary>
    public sealed class QuestingCompositeBuilder
    {
        public const float MinimumCompositeValue = -4.0f;
        public const float MaximumCompositeValue = 4.0f;

        public QuestingCompositeBuildResult Build(
            TagAxis activeAxis,
            IEnumerable<QuestingCompositeContribution>
                contributions)
        {
            if (contributions == null)
            {
                throw new ArgumentNullException(
                    nameof(contributions)
                );
            }

            List<QuestingCompositeContribution> applied =
                new List<QuestingCompositeContribution>();

            float rawValue = 0.0f;

            foreach (QuestingCompositeContribution contribution
                     in contributions)
            {
                if (contribution == null)
                {
                    throw new ArgumentException(
                        "Composite contribution collection " +
                        "contains a null entry.",
                        nameof(contributions)
                    );
                }

                if (contribution.Axis != activeAxis)
                    continue;

                rawValue += contribution.Value;

                if (float.IsNaN(rawValue) ||
                    float.IsInfinity(rawValue))
                {
                    throw new InvalidOperationException(
                        "Composite contribution sum became " +
                        "non-finite."
                    );
                }

                applied.Add(contribution);
            }

            float compositeValue =
                Math.Clamp(
                    rawValue,
                    MinimumCompositeValue,
                    MaximumCompositeValue
                );

            return new QuestingCompositeBuildResult(
                activeAxis: activeAxis,
                rawValue: rawValue,
                compositeValue: compositeValue,
                appliedContributions: applied.ToArray()
            );
        }
    }
}