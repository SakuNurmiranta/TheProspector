using System;

namespace SEMM91.GamePlay.Events
{
    public sealed class HappeningContext
    {
        public string ContextId { get; }

        public string Purpose { get; }

        public HappeningContextAnchorKind
            AnchorKind { get; }

        public string AnchorId { get; }

        public string AuthorEntityId { get; }

        public int AuthoredTurn { get; }

        public bool HasAnchor =>
            AnchorKind !=
            HappeningContextAnchorKind.None;

        public HappeningContext(
            string contextId,
            string purpose,
            HappeningContextAnchorKind anchorKind,
            string anchorId,
            string authorEntityId,
            int authoredTurn)
        {
            if (string.IsNullOrWhiteSpace(
                    contextId))
            {
                throw new ArgumentException(
                    "Happening Context requires an ID.",
                    nameof(contextId)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    purpose))
            {
                throw new ArgumentException(
                    "Happening Context requires an " +
                    "organizer-authored purpose.",
                    nameof(purpose)
                );
            }

            if (!Enum.IsDefined(
                    typeof(HappeningContextAnchorKind),
                    anchorKind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(anchorKind),
                    anchorKind,
                    "Invalid Happening Context anchor kind."
                );
            }

            bool hasAnchorId =
                !string.IsNullOrWhiteSpace(
                    anchorId
                );

            if (anchorKind ==
                    HappeningContextAnchorKind.None &&
                hasAnchorId)
            {
                throw new ArgumentException(
                    "An unanchored Happening Context " +
                    "cannot specify an anchor ID.",
                    nameof(anchorId)
                );
            }

            if (anchorKind !=
                    HappeningContextAnchorKind.None &&
                !hasAnchorId)
            {
                throw new ArgumentException(
                    "An anchored Happening Context " +
                    "requires an anchor ID.",
                    nameof(anchorId)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    authorEntityId))
            {
                throw new ArgumentException(
                    "Happening Context requires an author.",
                    nameof(authorEntityId)
                );
            }

            if (authoredTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(authoredTurn),
                    authoredTurn,
                    "Context authored turn cannot be negative."
                );
            }

            ContextId =
                contextId.Trim();

            Purpose =
                purpose.Trim();

            AnchorKind =
                anchorKind;

            AnchorId =
                hasAnchorId
                    ? anchorId.Trim()
                    : string.Empty;

            AuthorEntityId =
                authorEntityId.Trim();

            AuthoredTurn =
                authoredTurn;
        }
    }
}