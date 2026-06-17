using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Entities;
using SEMM91.GamePlay.Gestation.Questing;
using UnityEngine;

namespace SEMM91.GamePlay.Gestation.Tests
{
    public class TheWhiteSlotMachineTests
    {
        private GameObject _characterObject;

        [TearDown]
        public void TearDown()
        {
            if (_characterObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_characterObject);
            }
        }

        [Test]
        public void TryResolve_JoinsMoodCompositeAndProjection()
        {
            QuestingCompositeContribution contribution =
                CreateContribution(
                    sourceId: "world-event:test",
                    axis: TagAxis.Symbolic,
                    value: -1.25f
                );

            CapturingProvider provider =
                new CapturingProvider(contribution);

            TheWhiteSlotMachine machine =
                CreateMachine(provider);

            GameEntity character = CreateCharacterWithMood(
                entityId: "CHARACTER_A",
                axis: TagAxis.Symbolic,
                pole: TagPole.Negative,
                degree: TagDegree.Weak
            );

            bool success = machine.TryResolve(
                character,
                currentGlobalTurn: 4,
                sceneSynchronisation: 1.0f,
                out PajazzoResolution resolution,
                out string failureReason
            );

            Assert.That(success, Is.True);
            Assert.That(failureReason, Is.Empty);
            Assert.That(resolution, Is.Not.Null);

            Assert.That(
                resolution.MoodInput.SignedValue,
                Is.EqualTo(-1.0f)
            );

            Assert.That(
                resolution.Composite.CompositeValue,
                Is.EqualTo(-1.25f)
            );

            Assert.That(
                resolution.Projection.Slope,
                Is.EqualTo(-0.25f)
            );

            Assert.That(
                resolution.Projection.ProjectedValue,
                Is.EqualTo(-1.5f)
            );

            Assert.That(
                resolution.Projection.OutputPolarity,
                Is.EqualTo(QuestingPolarity.Negative)
            );

            Assert.That(
                resolution.Projection.OutputDegree,
                Is.EqualTo(1)
            );

            Assert.That(
                resolution.Projection.IsStable,
                Is.True
            );
        }

        [Test]
        public void TryResolve_ProjectionBoundaryProducesInterruption()
        {
            CapturingProvider provider =
                new CapturingProvider(
                    CreateContribution(
                        "world-event:contradiction",
                        TagAxis.Symbolic,
                        -1.0f
                    )
                );

            TheWhiteSlotMachine machine =
                CreateMachine(provider);

            GameEntity character = CreateCharacterWithMood(
                entityId: "CHARACTER_A",
                axis: TagAxis.Symbolic,
                pole: TagPole.Positive,
                degree: TagDegree.Dominant
            );

            bool success = machine.TryResolve(
                character,
                currentGlobalTurn: 2,
                sceneSynchronisation: 1.0f,
                out PajazzoResolution resolution,
                out _
            );

            Assert.That(success, Is.True);

            Assert.That(
                resolution.Projection.ProjectedValue,
                Is.EqualTo(-4.0f)
            );

            Assert.That(
                resolution.Projection.IsInterrupted,
                Is.True
            );

            Assert.That(
                resolution.Projection.ProducesTransientTag,
                Is.False
            );
        }

        [Test]
        public void TryResolve_ForwardsCharacterAxisAndTurnToProviders()
        {
            CapturingProvider provider =
                new CapturingProvider();

            TheWhiteSlotMachine machine =
                CreateMachine(provider);

            GameEntity character = CreateCharacterWithMood(
                entityId: "CHARACTER_CONTEXT",
                axis: TagAxis.Existential,
                pole: TagPole.Positive,
                degree: TagDegree.Weak
            );

            bool success = machine.TryResolve(
                character,
                currentGlobalTurn: 7,
                sceneSynchronisation: 1.0f,
                out _,
                out _
            );

            Assert.That(success, Is.True);
            Assert.That(provider.LastContext, Is.Not.Null);

            Assert.That(
                provider.LastContext.CharacterId,
                Is.EqualTo("CHARACTER_CONTEXT")
            );

            Assert.That(
                provider.LastContext.ActiveAxis,
                Is.EqualTo(TagAxis.Existential)
            );

            Assert.That(
                provider.LastContext.CurrentGlobalTurn,
                Is.EqualTo(7)
            );
        }

        [Test]
        public void TryResolve_MissingMood_ReturnsFalseBeforeProvidersRun()
        {
            CapturingProvider provider =
                new CapturingProvider();

            TheWhiteSlotMachine machine =
                CreateMachine(provider);

            GameEntity character =
                CreateCharacter("CHARACTER_NO_MOOD");

            bool success = machine.TryResolve(
                character,
                currentGlobalTurn: 1,
                sceneSynchronisation: 1.0f,
                out PajazzoResolution resolution,
                out string failureReason
            );

            Assert.That(success, Is.False);
            Assert.That(resolution, Is.Null);
            Assert.That(provider.CallCount, Is.EqualTo(0));

            Assert.That(
                failureReason,
                Does.Contain("no Mood tag container")
            );
        }

        [Test]
        public void TryResolve_RetainsAppliedContributionBreakdown()
        {
            QuestingCompositeContribution first =
                CreateContribution(
                    "world-event:first",
                    TagAxis.Expressive,
                    0.50f
                );

            QuestingCompositeContribution second =
                CreateContribution(
                    "world-event:second",
                    TagAxis.Expressive,
                    0.25f
                );

            TheWhiteSlotMachine machine =
                CreateMachine(
                    new CapturingProvider(first, second)
                );

            GameEntity character = CreateCharacterWithMood(
                entityId: "CHARACTER_A",
                axis: TagAxis.Expressive,
                pole: TagPole.Positive,
                degree: TagDegree.Weak
            );

            bool success = machine.TryResolve(
                character,
                currentGlobalTurn: 3,
                sceneSynchronisation: 1.0f,
                out PajazzoResolution resolution,
                out _
            );

            Assert.That(success, Is.True);

            Assert.That(
                resolution.Composite.AppliedContributions.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                resolution.Composite.AppliedContributions[0],
                Is.SameAs(first)
            );

            Assert.That(
                resolution.Composite.AppliedContributions[1],
                Is.SameAs(second)
            );
        }

        [Test]
        public void TryResolve_InvalidSynchronisation_UsesProjectionValidation()
        {
            TheWhiteSlotMachine machine =
                CreateMachine(new CapturingProvider());

            GameEntity character = CreateCharacterWithMood(
                entityId: "CHARACTER_A",
                axis: TagAxis.Symbolic,
                pole: TagPole.Negative,
                degree: TagDegree.Weak
            );

            Assert.Throws<ArgumentOutOfRangeException>(
                () => machine.TryResolve(
                    character,
                    currentGlobalTurn: 1,
                    sceneSynchronisation: 0.0f,
                    out _,
                    out _
                )
            );
        }

        private static TheWhiteSlotMachine CreateMachine(
            params IQuestingCompositeContributionProvider[]
                providers)
        {
            QuestingCompositeAssembler assembler =
                new QuestingCompositeAssembler(
                    new QuestingCompositeBuilder(),
                    providers
                );

            return new TheWhiteSlotMachine(
                new QuestingMoodInputReader(),
                assembler,
                new QuestingProjectionResolver()
            );
        }

        private GameEntity CreateCharacterWithMood(
            string entityId,
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            GameEntity character = CreateCharacter(entityId);

            character.AddTagContainer(
                TagContainerType.Mood
            );

            bool tagSet = character.TrySetTag(
                TagContainerType.Mood,
                new TagInstance(axis, pole, degree)
            );

            Assert.That(tagSet, Is.True);

            return character;
        }

        private GameEntity CreateCharacter(string entityId)
        {
            _characterObject =
                new GameObject("PajazzoTestCharacter");

            GameEntity character =
                _characterObject.AddComponent<GameEntity>();

            character.InitializeIdentity(
                explicitEntityId: entityId,
                displayName: "Pajazzo Test Character",
                entityType: GameEntityType.Character
            );

            return character;
        }

        private static QuestingCompositeContribution
            CreateContribution(
                string sourceId,
                TagAxis axis,
                float value)
        {
            return new QuestingCompositeContribution(
                sourceId,
                axis,
                value,
                "Test contribution"
            );
        }

        private sealed class CapturingProvider :
            IQuestingCompositeContributionProvider
        {
            private readonly IReadOnlyList
                <QuestingCompositeContribution> _contributions;

            public QuestingCompositeContext LastContext
            {
                get;
                private set;
            }

            public int CallCount { get; private set; }

            public CapturingProvider(
                params QuestingCompositeContribution[]
                    contributions)
            {
                _contributions =
                    contributions ??
                    Array.Empty<QuestingCompositeContribution>();
            }

            public IReadOnlyList<QuestingCompositeContribution>
                BuildContributions(
                    QuestingCompositeContext context)
            {
                CallCount++;
                LastContext = context;

                return _contributions;
            }
        }
    }
}