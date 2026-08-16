using System;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Transgression
{
    /// <summary>
    /// Immutable provenance-bearing acceptance event.
    ///
    /// A later higher-degree record can supersede this
    /// as the current coverage ceiling without deleting
    /// this historical record.
    /// </summary>
    public sealed class AcceptedTransgressionRecord
    {
        public string AcceptedTransgressionId { get; }

        public string KvltEntityId { get; }

        public AcceptedTransgressionKey Key { get; }

        public TagDegree AcceptedDegree { get; }

        public int AcceptedTurn { get; }

        public AcceptedTransgressionSourceKind
            SourceKind { get; }

        public string SourceId { get; }

        public AcceptedTransgressionRecord(
            string acceptedTransgressionId,
            string kvltEntityId,
            string behaviorTypeId,
            TagAxis axis,
            TagPole pole,
            TagDegree acceptedDegree,
            int acceptedTurn,
            AcceptedTransgressionSourceKind sourceKind,
            string sourceId)
        {
            AcceptedTransgressionId =
                RequireText(
                    acceptedTransgressionId,
                    nameof(acceptedTransgressionId)
                );

            KvltEntityId =
                RequireText(
                    kvltEntityId,
                    nameof(kvltEntityId)
                );

            Key =
                new AcceptedTransgressionKey(
                    behaviorTypeId,
                    axis,
                    pole
                );

            if (!Enum.IsDefined(
                    typeof(TagDegree),
                    acceptedDegree))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(acceptedDegree)
                );
            }

            if (acceptedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(acceptedTurn),
                    acceptedTurn,
                    "Accepted Transgression turn " +
                    "cannot be negative."
                );
            }

            if (!Enum.IsDefined(
                    typeof(
                        AcceptedTransgressionSourceKind
                    ),
                    sourceKind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceKind)
                );
            }

            AcceptedDegree =
                acceptedDegree;

            AcceptedTurn =
                acceptedTurn;

            SourceKind =
                sourceKind;

            SourceId =
                RequireText(
                    sourceId,
                    nameof(sourceId)
                );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Accepted Transgression provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}