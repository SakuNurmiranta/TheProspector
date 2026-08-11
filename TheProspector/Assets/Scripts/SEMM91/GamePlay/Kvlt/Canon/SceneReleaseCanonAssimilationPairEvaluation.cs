using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Frozen Canon Assimilation interpretation of
    /// one formal recorded Tag-pair.
    ///
    /// Assimilation can only fill activation to a
    /// degree already recognized by Canon_t.
    /// </summary>
    public sealed class
        SceneReleaseCanonAssimilationPairEvaluation
    {
        private readonly
            CanonPrecedentRecord[]
            canonicalPrecedents;

        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public string SourceTrackId { get; }

        public string SourceIdeaId { get; }

        public int IdeaIndex { get; }

        public int SettledTurn { get; }

        public TagAxis DominantAxis { get; }

        public TagPole DominantPole { get; }

        public TagDegree
            RecordedDominantDegree { get; }

        public TagDegree
            ExistingActivationDegree { get; }

        public bool HasCanonicalPrecedent { get; }

        public TagDegree
            ExistingCanonicalDegree { get; }

        public IReadOnlyList<
                CanonPrecedentRecord>
            CanonicalPrecedents =>
            canonicalPrecedents;

        public TagDegree
            CanonicalActivationDegree { get; }

        public TagDegree
            FinalActivationDegree { get; }

        public bool RaisesActivation =>
            (int)FinalActivationDegree >
            (int)ExistingActivationDegree;

        public SceneReleaseCanonAssimilationPairEvaluation(
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            string sourceTrackId,
            string sourceIdeaId,
            int ideaIndex,
            int settledTurn,
            TagAxis dominantAxis,
            TagPole dominantPole,
            TagDegree recordedDominantDegree,
            TagDegree existingActivationDegree,
            bool hasCanonicalPrecedent,
            TagDegree existingCanonicalDegree,
            IReadOnlyList<
                CanonPrecedentRecord>
                sourceCanonicalPrecedents)
        {
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

            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
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

            if (ideaIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ideaIndex)
                );
            }

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            ValidateDegree(
                recordedDominantDegree,
                nameof(recordedDominantDegree)
            );

            ValidateDegree(
                existingActivationDegree,
                nameof(existingActivationDegree)
            );

            ValidateDegree(
                existingCanonicalDegree,
                nameof(existingCanonicalDegree)
            );

            if (recordedDominantDegree ==
                TagDegree.Neutral)
            {
                throw new ArgumentException(
                    "Formal pair dominant degree " +
                    "must be at least degree 1.",
                    nameof(recordedDominantDegree)
                );
            }

            if ((int)existingActivationDegree >
                (int)recordedDominantDegree)
            {
                throw new ArgumentException(
                    "Existing activation cannot exceed " +
                    "recorded dominant degree.",
                    nameof(existingActivationDegree)
                );
            }

            if (sourceCanonicalPrecedents == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceCanonicalPrecedents)
                );
            }

            IdeaIndex =
                ideaIndex;

            SettledTurn =
                settledTurn;

            DominantAxis =
                dominantAxis;

            DominantPole =
                dominantPole;

            RecordedDominantDegree =
                recordedDominantDegree;

            ExistingActivationDegree =
                existingActivationDegree;

            HasCanonicalPrecedent =
                hasCanonicalPrecedent;

            ExistingCanonicalDegree =
                existingCanonicalDegree;

            canonicalPrecedents =
                new CanonPrecedentRecord[
                    sourceCanonicalPrecedents.Count
                ];

            for (int index = 0;
                 index < sourceCanonicalPrecedents.Count;
                 index++)
            {
                CanonPrecedentRecord record =
                    sourceCanonicalPrecedents[index] ??
                    throw new ArgumentException(
                        "Canon assimilation provenance " +
                        "cannot contain null precedent.",
                        nameof(sourceCanonicalPrecedents)
                    );

                if (record.Axis !=
                        dominantAxis ||
                    record.Pole !=
                        dominantPole ||
                    record.Degree !=
                        existingCanonicalDegree)
                {
                    throw new ArgumentException(
                        "Canon assimilation precedent " +
                        "does not match the current " +
                        "canonical ceiling.",
                        nameof(sourceCanonicalPrecedents)
                    );
                }

                canonicalPrecedents[index] =
                    record;
            }

            if (hasCanonicalPrecedent)
            {
                if (canonicalPrecedents.Length == 0)
                {
                    throw new ArgumentException(
                        "Canonical assimilation requires " +
                        "provenance for the recognized " +
                        "canonical ceiling.",
                        nameof(sourceCanonicalPrecedents)
                    );
                }
            }
            else
            {
                if (existingCanonicalDegree !=
                        TagDegree.Neutral ||
                    canonicalPrecedents.Length != 0)
                {
                    throw new ArgumentException(
                        "Absent Canon precedent must use " +
                        "degree 0 and no provenance."
                    );
                }
            }

            CanonicalActivationDegree =
                hasCanonicalPrecedent
                    ? (TagDegree)Math.Min(
                        (int)recordedDominantDegree,
                        (int)existingCanonicalDegree
                    )
                    : TagDegree.Neutral;

            FinalActivationDegree =
                (TagDegree)Math.Max(
                    (int)existingActivationDegree,
                    (int)CanonicalActivationDegree
                );
        }

        private static void ValidateDegree(
            TagDegree degree,
            string parameterName)
        {
            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    degree))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
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
                    "Canon assimilation provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}