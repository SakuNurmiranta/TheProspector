using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    /// <summary>
    /// Immutable Scenario declaration for historically
    /// accepted praxis.
    ///
    /// EstablishedTurn may be -1 to represent
    /// pre-session cultural history.
    ///
    /// Do not convert this to the current
    /// AcceptedTransgressionRecord yet; that runtime
    /// type presently rejects negative turns.
    /// </summary>
    public sealed class KvltAcceptedTransgressionSeed
    {
        public string SeedId { get; }

        public string BehaviorTypeId { get; }

        public TagAxis Axis { get; }

        public TagPole Pole { get; }

        public TagDegree AcceptedDegree { get; }

        public int EstablishedTurn { get; }

        public string SourceId { get; }

        public KvltAcceptedTransgressionSeed(
            string seedId,
            string behaviorTypeId,
            TagAxis axis,
            TagPole pole,
            TagDegree acceptedDegree,
            int establishedTurn,
            string sourceId)
        {
            SeedId =
                RequireText(
                    seedId,
                    nameof(seedId)
                );

            BehaviorTypeId =
                RequireText(
                    behaviorTypeId,
                    nameof(behaviorTypeId)
                );

            SourceId =
                RequireText(
                    sourceId,
                    nameof(sourceId)
                );

            if (!Enum.IsDefined(
                    typeof(TagAxis),
                    axis))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(axis)
                );
            }

            if (!Enum.IsDefined(
                    typeof(TagPole),
                    pole))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pole)
                );
            }

            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    acceptedDegree))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(acceptedDegree)
                );
            }

            if (establishedTurn < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(establishedTurn)
                );
            }

            Axis = axis;
            Pole = pole;
            AcceptedDegree = acceptedDegree;
            EstablishedTurn = establishedTurn;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Scenario semantic seed identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}