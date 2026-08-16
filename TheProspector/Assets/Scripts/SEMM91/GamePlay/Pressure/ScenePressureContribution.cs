using System;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Pressure
{
    [Serializable]
    public sealed class ScenePressureContribution
    {
        public string SourceReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SourceTrackId { get; }

        public string SourceIdeaId { get; }

        public DemoTapeTagOccurrenceRole SourceRole
        {
            get;
        }

        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree RecordedDegree { get; }

        /// <summary>
        /// Signed semantic contribution before any
        /// field-strength weighting.
        ///
        /// Solitary       = +degree
        /// PairDominant   = +degree
        /// PairSubmissive = -degree
        /// </summary>
        public float DegreeBasisContribution { get; }

        /// <summary>
        /// Non-negative scaling supplied by the later
        /// Pressure builder / Scenario Profile logic.
        ///
        /// A weight may change magnitude but never the
        /// semantic direction of the contribution.
        /// </summary>
        public float FieldStrengthWeight { get; }

        public float RawContribution =>
            DegreeBasisContribution *
            FieldStrengthWeight;

        public ScenePressureContribution(
            string sourceReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sourceTrackId,
            string sourceIdeaId,
            DemoTapeTagOccurrenceRole sourceRole,
            TagAxis axis,
            TagPole pole,
            TagDegree recordedDegree,
            float fieldStrengthWeight)
        {
            SourceReleaseId = RequireText(
                sourceReleaseId,
                nameof(sourceReleaseId)
            );

            SourceDemoTapeId = RequireText(
                sourceDemoTapeId,
                nameof(sourceDemoTapeId)
            );

            SourceOwnerEntityId = RequireText(
                sourceOwnerEntityId,
                nameof(sourceOwnerEntityId)
            );

            SourceTrackId = RequireText(
                sourceTrackId,
                nameof(sourceTrackId)
            );

            SourceIdeaId = RequireText(
                sourceIdeaId,
                nameof(sourceIdeaId)
            );

            ValidateRole(
                sourceRole
            );

            if (float.IsNaN(fieldStrengthWeight) ||
                float.IsInfinity(fieldStrengthWeight) ||
                fieldStrengthWeight < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fieldStrengthWeight),
                    fieldStrengthWeight,
                    "Field-strength weight must be finite and non-negative."
                );
            }

            SourceRole = sourceRole;
            Axis = axis;
            Pole = pole;
            RecordedDegree = recordedDegree;

            DegreeBasisContribution =
                ResolveDegreeBasisContribution(
                    sourceRole,
                    recordedDegree
                );

            FieldStrengthWeight =
                fieldStrengthWeight;
        }

        private static float
            ResolveDegreeBasisContribution(
                DemoTapeTagOccurrenceRole role,
                TagDegree degree)
        {
            float magnitude =
                (float)(int)degree;

            return role switch
            {
                DemoTapeTagOccurrenceRole.Solitary =>
                    magnitude,

                DemoTapeTagOccurrenceRole.PairDominant =>
                    magnitude,

                DemoTapeTagOccurrenceRole.PairSubmissive =>
                    -magnitude,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(role),
                        role,
                        "Unsupported Tag occurrence role."
                    )
            };
        }

        private static void ValidateRole(
            DemoTapeTagOccurrenceRole role)
        {
            switch (role)
            {
                case DemoTapeTagOccurrenceRole.Solitary:
                case DemoTapeTagOccurrenceRole.PairDominant:
                case DemoTapeTagOccurrenceRole.PairSubmissive:
                    return;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(role),
                        role,
                        "Unsupported Tag occurrence role."
                    );
            }
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Pressure provenance cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}