using System;
using SEMM91.Core.Entities;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Gestation.Questing
{
    /// <summary>
    /// Converts a resolved Questing projection into the existing
    /// character transient-tag state.
    ///
    /// Neutral and interrupted projections are successful no-op
    /// outcomes and do not clear an existing transient tag.
    /// </summary>
    public sealed class QuestingOutcomeApplicator
    {
        public bool TryApply(
            GameEntity character,
            QuestingResult result,
            out TagInstance? appliedTag,
            out string failureReason)
        {
            appliedTag = null;
            failureReason = string.Empty;

            if (character == null)
            {
                failureReason =
                    "Cannot apply Questing outcome to a null character.";

                return false;
            }

            if (result == null)
            {
                failureReason =
                    "Cannot apply a null Questing result.";

                return false;
            }

            if (!result.ProducesTransientTag)
            {
                return true;
            }

            if (!result.OutputDegree.HasValue)
            {
                failureReason =
                    "Questing result claims to produce a transient " +
                    "tag but has no output degree.";

                return false;
            }

            int degreeValue = result.OutputDegree.Value;

            if (degreeValue < (int)TagDegree.Neutral ||
                degreeValue > (int)TagDegree.Transgressive)
            {
                failureReason =
                    $"Questing output degree {degreeValue} cannot " +
                    "be converted into TagDegree.";

                return false;
            }

            TagPole pole;

            switch (result.OutputPolarity)
            {
                case QuestingPolarity.Negative:
                    pole = TagPole.Negative;
                    break;

                case QuestingPolarity.Positive:
                    pole = TagPole.Positive;
                    break;

                default:
                    failureReason =
                        $"Questing polarity " +
                        $"{result.OutputPolarity} cannot produce " +
                        "a transient tag.";

                    return false;
            }

            TagInstance transientTag =
                new TagInstance(
                    result.ActiveAxis,
                    pole,
                    (TagDegree)degreeValue
                );

            if (!character.TrySetTag(
                    TagContainerType.Transient,
                    transientTag))
            {
                failureReason =
                    $"Character {character.EntityId} has no usable " +
                    "Transient tag container.";

                return false;
            }

            appliedTag = transientTag;
            return true;
        }
    }
}