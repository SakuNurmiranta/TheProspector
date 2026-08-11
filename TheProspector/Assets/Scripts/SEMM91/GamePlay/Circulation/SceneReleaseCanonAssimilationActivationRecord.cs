using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Canon;

namespace SEMM91.GamePlay.Circulation
{
    /// <summary>
    /// Immutable authoritative history of Canon_t
    /// raising one formal pair's legitimate activation
    /// during Canon Assimilation.
    ///
    /// This is intentionally separate from ordinary
    /// praxis activation history.
    /// </summary>
    public sealed class
        SceneReleaseCanonAssimilationActivationRecord
    {
        private readonly
            CanonPrecedentRecord[]
            canonicalPrecedents;

        public SceneReleasePairActivationKey
            Key { get; }

        public TagAxis DominantAxis { get; }

        public TagPole DominantPole { get; }

        public TagDegree
            RecordedDominantDegree { get; }

        public TagDegree
            PreviousActivationDegree { get; }

        public TagDegree
            ExistingCanonicalDegree { get; }

        public TagDegree
            CanonicalActivationDegree { get; }

        public TagDegree
            NewActivationDegree { get; }

        public int OccurredTurn { get; }

        public IReadOnlyList<
                CanonPrecedentRecord>
            CanonicalPrecedents =>
            canonicalPrecedents;

        public bool RaisedActivation =>
            (int)NewActivationDegree >
            (int)PreviousActivationDegree;

        public
            SceneReleaseCanonAssimilationActivationRecord(
                SceneReleasePairActivationKey key,
                TagAxis dominantAxis,
                TagPole dominantPole,
                TagDegree recordedDominantDegree,
                TagDegree previousActivationDegree,
                TagDegree existingCanonicalDegree,
                TagDegree newActivationDegree,
                int occurredTurn,
                IReadOnlyList<
                    CanonPrecedentRecord>
                    sourceCanonicalPrecedents)
        {
            if (sourceCanonicalPrecedents == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceCanonicalPrecedents)
                );
            }

            if (sourceCanonicalPrecedents.Count == 0)
            {
                throw new ArgumentException(
                    "Canon Assimilation history requires " +
                    "canonical provenance.",
                    nameof(sourceCanonicalPrecedents)
                );
            }

            ValidateDegree(
                recordedDominantDegree
            );

            ValidateDegree(
                previousActivationDegree
            );

            ValidateDegree(
                existingCanonicalDegree
            );

            ValidateDegree(
                newActivationDegree
            );

            if (occurredTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(occurredTurn)
                );
            }

            TagDegree canonicalActivation =
                (TagDegree)Math.Min(
                    (int)recordedDominantDegree,
                    (int)existingCanonicalDegree
                );

            TagDegree expectedNew =
                (TagDegree)Math.Max(
                    (int)previousActivationDegree,
                    (int)canonicalActivation
                );

            if (newActivationDegree !=
                expectedNew)
            {
                throw new ArgumentException(
                    "Assimilation history does not match " +
                    "the canonical activation formula.",
                    nameof(newActivationDegree)
                );
            }

            if ((int)newActivationDegree <=
                (int)previousActivationDegree)
            {
                throw new ArgumentException(
                    "Assimilation activation record must " +
                    "represent an actual activation raise.",
                    nameof(newActivationDegree)
                );
            }

            canonicalPrecedents =
                new CanonPrecedentRecord[
                    sourceCanonicalPrecedents.Count
                ];

            for (int index = 0;
                 index < sourceCanonicalPrecedents.Count;
                 index++)
            {
                CanonPrecedentRecord precedent =
                    sourceCanonicalPrecedents[index] ??
                    throw new ArgumentException(
                        "Canon Assimilation provenance " +
                        "cannot contain null."
                    );

                if (precedent.Axis !=
                        dominantAxis ||
                    precedent.Pole !=
                        dominantPole ||
                    precedent.Degree !=
                        existingCanonicalDegree)
                {
                    throw new ArgumentException(
                        "Canon Assimilation provenance " +
                        "does not match the canonical " +
                        "activation source."
                    );
                }

                canonicalPrecedents[index] =
                    precedent;
            }

            Key =
                key;

            DominantAxis =
                dominantAxis;

            DominantPole =
                dominantPole;

            RecordedDominantDegree =
                recordedDominantDegree;

            PreviousActivationDegree =
                previousActivationDegree;

            ExistingCanonicalDegree =
                existingCanonicalDegree;

            CanonicalActivationDegree =
                canonicalActivation;

            NewActivationDegree =
                newActivationDegree;

            OccurredTurn =
                occurredTurn;
        }

        private static void ValidateDegree(
            TagDegree degree)
        {
            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    degree))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(degree)
                );
            }
        }
    }
}