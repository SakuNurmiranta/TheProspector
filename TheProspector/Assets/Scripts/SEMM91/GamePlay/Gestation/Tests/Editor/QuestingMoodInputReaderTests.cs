using NUnit.Framework;
using SEMM91.Core.Entities;
using SEMM91.Core.Tags;
using UnityEngine;

namespace SEMM91.GamePlay.Gestation.Questing.Tests
{
    public class QuestingMoodInputReaderTests
    {
        private QuestingMoodInputReader _reader;
        private GameObject _characterObject;

        [SetUp]
        public void SetUp()
        {
            _reader = new QuestingMoodInputReader();
        }

        [TearDown]
        public void TearDown()
        {
            if (_characterObject != null)
            {
                Object.DestroyImmediate(_characterObject);
            }
        }

        [Test]
        public void TryRead_NegativeWeakMood_ReturnsMinusOne()
        {
            GameEntity character = CreateCharacterWithMood(
                new TagInstance(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                )
            );

            bool success = _reader.TryRead(
                character,
                out QuestingMoodInput input,
                out string failureReason
            );

            Assert.That(success, Is.True);
            Assert.That(failureReason, Is.Empty);
            Assert.That(input, Is.Not.Null);

            Assert.That(
                input.ActiveAxis,
                Is.EqualTo(TagAxis.Symbolic)
            );

            Assert.That(
                input.Polarity,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                input.Degree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(input.SignedValue, Is.EqualTo(-1.0f));
        }

        [Test]
        public void TryRead_PositiveDominantMood_ReturnsPlusTwo()
        {
            GameEntity character = CreateCharacterWithMood(
                new TagInstance(
                    TagAxis.Existential,
                    TagPole.Positive,
                    TagDegree.Dominant
                )
            );

            bool success = _reader.TryRead(
                character,
                out QuestingMoodInput input,
                out _
            );

            Assert.That(success, Is.True);

            Assert.That(
                input.ActiveAxis,
                Is.EqualTo(TagAxis.Existential)
            );

            Assert.That(input.SignedValue, Is.EqualTo(2.0f));
        }

        [Test]
        public void TryRead_NeutralMood_ReturnsExactZero()
        {
            GameEntity character = CreateCharacterWithMood(
                new TagInstance(
                    TagAxis.Expressive,
                    TagPole.Negative,
                    TagDegree.Neutral
                )
            );

            bool success = _reader.TryRead(
                character,
                out QuestingMoodInput input,
                out _
            );

            Assert.That(success, Is.True);
            Assert.That(input.SignedValue, Is.EqualTo(0.0f));
        }

        [Test]
        public void TryRead_MissingMoodContainer_ReturnsFalse()
        {
            GameEntity character = CreateCharacter();

            bool success = _reader.TryRead(
                character,
                out QuestingMoodInput input,
                out string failureReason
            );

            Assert.That(success, Is.False);
            Assert.That(input, Is.Null);

            Assert.That(
                failureReason,
                Does.Contain("no Mood tag container")
            );
        }

        [Test]
        public void TryRead_EmptyMoodContainer_ReturnsFalse()
        {
            GameEntity character = CreateCharacter();

            character.AddTagContainer(
                TagContainerType.Mood
            );

            bool success = _reader.TryRead(
                character,
                out QuestingMoodInput input,
                out string failureReason
            );

            Assert.That(success, Is.False);
            Assert.That(input, Is.Null);

            Assert.That(
                failureReason,
                Does.Contain("no held Mood tag")
            );
        }

        [Test]
        public void TryRead_NullCharacter_ReturnsFalse()
        {
            bool success = _reader.TryRead(
                null,
                out QuestingMoodInput input,
                out string failureReason
            );

            Assert.That(success, Is.False);
            Assert.That(input, Is.Null);

            Assert.That(
                failureReason,
                Does.Contain("null character")
            );
        }

        [Test]
        public void TryRead_InvalidMoodAxis_ReturnsFalse()
        {
            GameEntity character = CreateCharacterWithMood(
                new TagInstance(
                    (TagAxis)999,
                    TagPole.Positive,
                    TagDegree.Weak
                )
            );

            bool success = _reader.TryRead(
                character,
                out QuestingMoodInput input,
                out string failureReason
            );

            Assert.That(success, Is.False);
            Assert.That(input, Is.Null);

            Assert.That(
                failureReason,
                Does.Contain("invalid Mood axis")
            );
        }

        private GameEntity CreateCharacterWithMood(
            TagInstance moodTag)
        {
            GameEntity character = CreateCharacter();

            character.AddTagContainer(
                TagContainerType.Mood
            );

            bool tagSet = character.TrySetTag(
                TagContainerType.Mood,
                moodTag
            );

            Assert.That(tagSet, Is.True);

            return character;
        }

        private GameEntity CreateCharacter()
        {
            _characterObject =
                new GameObject("QuestingMoodReaderTestCharacter");

            GameEntity character =
                _characterObject.AddComponent<GameEntity>();

            character.InitializeIdentity(
                explicitEntityId: "TEST_CHARACTER",
                displayName: "Test Character",
                entityType: GameEntityType.Character
            );

            return character;
        }
    }
}