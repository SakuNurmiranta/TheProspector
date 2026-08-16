using System;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Pressure
{
    /// <summary>
    /// One provenance-bearing recorded semantic atom
    /// contributing to rebuilt Raw Field Pressure.
    ///
    /// Solitary and pair-dominant occurrences are
    /// positive. Pair-submissive occurrences are
    /// negative.
    /// </summary>
    public sealed class ScenePressureSourceContribution
    {
        public string SceneId { get; }

        public int SettledTurn { get; }

        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SourceTrackId { get; }

        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree RecordedDegree { get; }

        public DemoTapeTagOccurrenceRole Role { get; }

        public float SignedRawContribution { get; }

        public ScenePressureSourceContribution(
            string sceneId,
            int settledTurn,
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex,
            TagAxis axis,
            TagPole pole,
            TagDegree recordedDegree,
            DemoTapeTagOccurrenceRole role,
            float signedRawContribution)
        {
            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            SceneReleaseId =
                RequireText(
                    sceneReleaseId,
                    nameof(sceneReleaseId)
                );

            SourceDemoTapeId =
                RequireText(
                    sourceDemoTapeId,
                    nameof(sourceDemoTapeId)
                );

            SourceOwnerEntityId =
                RequireText(
                    sourceOwnerEntityId,
                    nameof(sourceOwnerEntityId)
                );

            SourceTrackId =
                RequireText(
                    sourceTrackId,
                    nameof(sourceTrackId)
                );

            SourceIdeaId =
                RequireText(
                    sourceIdeaId,
                    nameof(sourceIdeaId)
                );

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            if (ideaIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ideaIndex)
                );
            }

            if (recordedDegree ==
                TagDegree.Neutral)
            {
                throw new ArgumentException(
                    "Zero-degree semantic material does " +
                    "not create a Pressure contribution.",
                    nameof(recordedDegree)
                );
            }

            if (float.IsNaN(
                    signedRawContribution) ||
                float.IsInfinity(
                    signedRawContribution) ||
                signedRawContribution == 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(signedRawContribution)
                );
            }

            float expectedMagnitude =
                (int)recordedDegree;

            float expectedSign =
                role ==
                DemoTapeTagOccurrenceRole.PairSubmissive
                    ? -1f
                    : 1f;

            float expected =
                expectedMagnitude *
                expectedSign;

            if (Math.Abs(
                    signedRawContribution -
                    expected) >
                0.0001f)
            {
                throw new ArgumentException(
                    "Pressure contribution does not " +
                    "match recorded Tag degree/role.",
                    nameof(signedRawContribution)
                );
            }

            SettledTurn =
                settledTurn;

            IdeaIndex =
                ideaIndex;

            Axis =
                axis;

            Pole =
                pole;

            RecordedDegree =
                recordedDegree;

            Role =
                role;

            SignedRawContribution =
                signedRawContribution;
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