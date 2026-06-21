using NUnit.Framework;
using SEMM91.Core.Entities;
using SEMM91.Core.Tags;
using UnityEngine;

namespace SEMM91.GamePlay.Gestation.Questing.Tests
{
    public class QuestingOutcomeApplicatorTests
    {
        private QuestingOutcomeApplicator _applicator;
        private QuestingProjectionResolver _resolver;
        private GameObject _characterObject;

        [SetUp]
        public void SetUp()
        {
            _applicator = new QuestingOutcomeApplicator();
            _resolver = new QuestingProjectionResolver();
        }

        [TearDown]
        public void TearDown()
        {
            if (_characterObject != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    _characterObject
                );
            }
        }

        [Test]
        public void TryApply_PositiveStableResult_WritesTransientTag()
        {
            GameEntity character =
                CreateCharacterWithTransientContainer();

            QuestingResult result = Resolve(
                mood: 0.0f,
                composite: 1.0f
            );

            bool success = _applicator.TryApply(
                character,
                result,
                out TagInstance? appliedTag,
                out string failureReason
            );

            Assert.That(success, Is.True);
            Assert.That(failureReason, Is.Empty);
            Assert.That(appliedTag.HasValue, Is.True);

            AssertStoredTag(
                character,
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Dominant
            );
        }

        [Test]
        public void TryApply_NegativeStableResult_WritesTransientTag()
        {
            GameEntity character =
                CreateCharacterWithTransientContainer();

            QuestingResult result = Resolve(
                mood: 0.0f,
                composite: -0.75f
            );

            bool success = _applicator.TryApply(
                character,
                result,
                out TagInstance? appliedTag,
                out _
            );

            Assert.That(success, Is.True);
            Assert.That(appliedTag.HasValue, Is.True);

            AssertStoredTag(
                character,
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Weak
            );
        }

        [Test]
        public void TryApply_DegreeZeroResult_WritesDirectedNeutralDegree()
        {
            GameEntity character =
                CreateCharacterWithTransientContainer();

            QuestingResult result = Resolve(
                mood: 0.0f,
                composite: 0.20f
            );

            bool success = _applicator.TryApply(
                character,
                result,
                out TagInstance? appliedTag,
                out _
            );

            Assert.That(success, Is.True);
            Assert.That(appliedTag.HasValue, Is.True);

            AssertStoredTag(
                character,
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Neutral
            );
        }

        [Test]
        public void TryApply_InterruptedResult_PreservesExistingTransient()
        {
            GameEntity character =
                CreateCharacterWithTransientContainer();

            SetTransient(
                character,
                TagAxis.Expressive,
                TagPole.Negative,
                TagDegree.Weak
            );

            QuestingResult interrupted =
                _resolver.Resolve(
                    new QuestingRequest(
                        activeAxis: TagAxis.Symbolic,
                        moodValue: 2.0f,
                        compositeValue: -1.0f,
                        sceneSynchronisation: 1.0f
                    )
                );

            bool success = _applicator.TryApply(
                character,
                interrupted,
                out TagInstance? appliedTag,
                out string failureReason
            );

            Assert.That(success, Is.True);
            Assert.That(failureReason, Is.Empty);
            Assert.That(appliedTag.HasValue, Is.False);

            AssertStoredTag(
                character,
                TagAxis.Expressive,
                TagPole.Negative,
                TagDegree.Weak
            );
        }

        [Test]
        public void TryApply_NeutralResult_PreservesExistingTransient()
        {
            GameEntity character =
                CreateCharacterWithTransientContainer();

            SetTransient(
                character,
                TagAxis.Expressive,
                TagPole.Positive,
                TagDegree.Dominant
            );

            QuestingResult neutral =
                _resolver.Resolve(
                    new QuestingRequest(
                        activeAxis: TagAxis.Symbolic,
                        moodValue: 1.0f,
                        compositeValue: 0.5f,
                        sceneSynchronisation: 1.0f
                    )
                );

            bool success = _applicator.TryApply(
                character,
                neutral,
                out TagInstance? appliedTag,
                out _
            );

            Assert.That(success, Is.True);
            Assert.That(appliedTag.HasValue, Is.False);

            AssertStoredTag(
                character,
                TagAxis.Expressive,
                TagPole.Positive,
                TagDegree.Dominant
            );
        }

        [Test]
        public void TryApply_ProducedTag_ReplacesExistingTransient()
        {
            GameEntity character =
                CreateCharacterWithTransientContainer();

            SetTransient(
                character,
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Weak
            );

            QuestingResult result = Resolve(
                mood: 0.0f,
                composite: 1.0f
            );

            bool success = _applicator.TryApply(
                character,
                result,
                out _,
                out _
            );

            Assert.That(success, Is.True);

            AssertStoredTag(
                character,
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Dominant
            );
        }

        [Test]
        public void TryApply_MissingTransientContainer_ReturnsFalse()
        {
            GameEntity character = CreateCharacter();

            QuestingResult result = Resolve(
                mood: 0.0f,
                composite: 1.0f
            );

            bool success = _applicator.TryApply(
                character,
                result,
                out TagInstance? appliedTag,
                out string failureReason
            );

            Assert.That(success, Is.False);
            Assert.That(appliedTag.HasValue, Is.False);

            Assert.That(
                failureReason,
                Does.Contain("Transient tag container")
            );
        }

        [Test]
        public void TryApply_NullCharacter_ReturnsFalse()
        {
            QuestingResult result = Resolve(
                mood: 0.0f,
                composite: 1.0f
            );

            bool success = _applicator.TryApply(
                null,
                result,
                out TagInstance? appliedTag,
                out string failureReason
            );

            Assert.That(success, Is.False);
            Assert.That(appliedTag.HasValue, Is.False);

            Assert.That(
                failureReason,
                Does.Contain("null character")
            );
        }

        private QuestingResult Resolve(
            float mood,
            float composite)
        {
            return _resolver.Resolve(
                new QuestingRequest(
                    activeAxis: TagAxis.Symbolic,
                    moodValue: mood,
                    compositeValue: composite,
                    sceneSynchronisation: 1.0f
                )
            );
        }

        private GameEntity
            CreateCharacterWithTransientContainer()
        {
            GameEntity character = CreateCharacter();

            character.AddTagContainer(
                TagContainerType.Transient
            );

            return character;
        }

        private GameEntity CreateCharacter()
        {
            _characterObject =
                new GameObject(
                    "QuestingOutcomeApplicatorTestCharacter"
                );

            GameEntity character =
                _characterObject.AddComponent<GameEntity>();

            character.InitializeIdentity(
                explicitEntityId: "TEST_CHARACTER",
                displayName: "Test Character",
                entityType: GameEntityType.Character
            );

            return character;
        }

        private static void SetTransient(
            GameEntity character,
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            bool set = character.TrySetTag(
                TagContainerType.Transient,
                new TagInstance(axis, pole, degree)
            );

            Assert.That(set, Is.True);
        }

        private static void AssertStoredTag(
            GameEntity character,
            TagAxis expectedAxis,
            TagPole expectedPole,
            TagDegree expectedDegree)
        {
            bool found = character.TryGetTagContainer(
                TagContainerType.Transient,
                out TagContainer container
            );

            Assert.That(found, Is.True);
            Assert.That(container.HasHeldTag, Is.True);
            Assert.That(container.HeldTag, Is.Not.Null);

            TagInstance stored =
                container.HeldTag.TagInstance;

            Assert.That(
                stored.axis,
                Is.EqualTo(expectedAxis)
            );

            Assert.That(
                stored.pole,
                Is.EqualTo(expectedPole)
            );

            Assert.That(
                stored.degree,
                Is.EqualTo(expectedDegree)
            );
        }
    }
}