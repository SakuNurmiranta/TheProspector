using System;
using SEMM91.Core.Entities;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Gestation.Questing
{
    /// <summary>
    /// Normalized mood input per quest
    /// </summary>
    public sealed class QuestingMoodInput
    {
        public TagInstance MoodTag { get; }
        public TagAxis ActiveAxis => MoodTag.axis;
        public TagPole Polarity => MoodTag.pole;
        public TagDegree Degree => MoodTag.degree;
        public float SignedValue { get; }

        internal QuestingMoodInput(
            TagInstance moodTag,
            float signedValue)
        {
            MoodTag = moodTag;
            SignedValue = signedValue;
        }
    }

    /// <summary>
    /// Converts tag for Questing.
    /// </summary>
        public sealed class QuestingMoodInputReader
        {
            public bool TryRead(
                GameEntity character,
                out QuestingMoodInput input,
                out string failureReason)
            {
                input = null;
                failureReason = string.Empty;

                if (character == null)
                {
                    failureReason =
                        "Cannot read Questing Mood from a null character.";

                    return false;
                }

                if (!character.TryGetTagContainer(
                        TagContainerType.Mood,
                        out TagContainer moodContainer))
                {
                    failureReason =
                        $"Character {character.EntityId} " +
                        "has no Mood tag container.";

                    return false;
                }

                if (!moodContainer.HasHeldTag ||
                    moodContainer.HeldTag == null)
                {
                    failureReason =
                        $"Character {character.EntityId} " +
                        "has no held Mood tag.";

                    return false;
                }

                TagInstance moodTag =
                    moodContainer.HeldTag.TagInstance;

                if (!Enum.IsDefined(
                        typeof(TagAxis),
                        moodTag.axis))
                {
                    failureReason =
                        $"Character {character.EntityId} " +
                        $"has invalid Mood axis {moodTag.axis}.";

                    return false;
                }

                if (!Enum.IsDefined(
                        typeof(TagPole),
                        moodTag.pole))
                {
                    failureReason =
                        $"Character {character.EntityId} " +
                        $"has invalid Mood pole {moodTag.pole}.";

                    return false;
                }

                if (!Enum.IsDefined(
                        typeof(TagDegree),
                        moodTag.degree))
                {
                    failureReason =
                        $"Character {character.EntityId} " +
                        $"has invalid Mood degree {moodTag.degree}.";

                    return false;
                }

                float magnitude = (int)moodTag.degree;

                float signedValue;

                if (magnitude == 0.0f)
                {
                    signedValue = 0.0f;
                }
                else
                {
                    signedValue =
                        moodTag.pole == TagPole.Negative
                            ? -magnitude
                            : magnitude;
                }

                input = new QuestingMoodInput(
                    moodTag,
                    signedValue
                );

                return true;
            }
        }
    }
