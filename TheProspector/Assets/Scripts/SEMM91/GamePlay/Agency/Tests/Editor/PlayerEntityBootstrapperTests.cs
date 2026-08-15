using NUnit.Framework;
using SEMM91.Core.Entities;
using SEMM91.Core.Tags;
using UnityEngine;

namespace SEMM91.GamePlay.Agency.Tests.Editor
{
    public sealed class
        PlayerEntityBootstrapperTests
    {
        private const ulong TestClientId =
            7;

        private PlayerEntityBootstrapper
            _bootstrapper;

        private GameEntity _entity;

        [SetUp]
        public void SetUp()
        {
            _bootstrapper =
                new PlayerEntityBootstrapper(
                    log: _ => { }
                );

            _entity =
                _bootstrapper.CreatePlayerEntity(
                    TestClientId,
                    "Test Band"
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (_entity != null)
            {
                Object.DestroyImmediate(
                    _entity.gameObject
                );
            }

            _entity = null;
            _bootstrapper = null;
        }

        [Test]
        public void
            CreatePlayerEntityCreatesCharacterIdentity()
        {
            Assert.That(
                _entity,
                Is.Not.Null
            );

            Assert.That(
                _entity.DisplayName,
                Is.EqualTo("Test Band")
            );

            Assert.That(
                _entity.EntityType,
                Is.EqualTo(
                    GameEntityType.Character
                )
            );
        }

        [Test]
        public void
            CreatePlayerEntityRegistersPlayableAspects()
        {
            Assert.That(
                _entity.AspectIds,
                Does.Contain(
                    "ASPECT_KNOWS_GUITAR"
                )
            );

            Assert.That(
                _entity.AspectIds,
                Does.Contain(
                    "ASPECT_HAS_GUITAR"
                )
            );

            Assert.That(
                _entity.AspectIds,
                Does.Contain(
                    "ASPECT_LYRICS"
                )
            );

            Assert.That(
                _entity.AspectIds,
                Does.Contain(
                    "ASPECT_VOCALS"
                )
            );

            Assert.That(
                _entity.AspectIds,
                Does.Contain(
                    "ASPECT_GUITAR"
                )
            );

            Assert.That(
                _entity.AspectIds,
                Does.Contain(
                    "ASPECT_DRUMS"
                )
            );
        }

        [Test]
        public void
            CreatePlayerEntityCreatesRequiredTagContainers()
        {
            Assert.That(
                _entity.TryGetTagContainer(
                    TagContainerType.Resonance,
                    out _
                ),
                Is.True
            );

            Assert.That(
                _entity.TryGetTagContainer(
                    TagContainerType.Conviction,
                    out _
                ),
                Is.True
            );

            Assert.That(
                _entity.TryGetTagContainer(
                    TagContainerType.Mood,
                    out _
                ),
                Is.True
            );

            Assert.That(
                _entity.TryGetTagContainer(
                    TagContainerType.Transient,
                    out _
                ),
                Is.True
            );
        }

        [Test]
        public void
            CreatePlayerEntitySeedsInitialCharacterTags()
        {
            Assert.That(
                _entity.TryGetTagContainer(
                    TagContainerType.Resonance,
                    out TagContainer resonance
                ),
                Is.True
            );

            Assert.That(
                resonance.HasHeldTag,
                Is.True
            );

            Assert.That(
                resonance.HeldTag,
                Is.Not.Null
            );

            Assert.That(
                resonance.HeldTag.TagInstance,
                Is.Not.Null
            );

            Assert.That(
                resonance.HeldTag.TagInstance.axis,
                Is.EqualTo(
                    TagAxis.Physical
                )
            );

            Assert.That(
                resonance.HeldTag.TagInstance.pole,
                Is.EqualTo(
                    TagPole.Negative
                )
            );

            Assert.That(
                resonance.HeldTag.TagInstance.degree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            Assert.That(
                _entity.TryGetTagContainer(
                    TagContainerType.Conviction,
                    out TagContainer conviction
                ),
                Is.True
            );

            Assert.That(
                conviction.HasHeldTag,
                Is.True
            );

            Assert.That(
                conviction.HeldTag,
                Is.Not.Null
            );

            Assert.That(
                conviction.HeldTag.TagInstance,
                Is.Not.Null
            );

            Assert.That(
                conviction.HeldTag.TagInstance.axis,
                Is.EqualTo(
                    TagAxis.Symbolic
                )
            );

            Assert.That(
                conviction.HeldTag.TagInstance.pole,
                Is.EqualTo(
                    TagPole.Negative
                )
            );

            Assert.That(
                conviction.HeldTag.TagInstance.degree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );
        }

        [Test]
        public void
            CreatePlayerEntitySeedsOpposedSymbolicPairSources()
        {
            Assert.That(
                _entity.TryGetTagContainer(
                    TagContainerType.Conviction,
                    out TagContainer conviction
                ),
                Is.True
            );

            Assert.That(
                _entity.TryGetTagContainer(
                    TagContainerType.Mood,
                    out TagContainer mood
                ),
                Is.True
            );

            Assert.That(
                conviction.HeldTag.TagInstance.pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                mood.HeldTag.TagInstance.pole,
                Is.EqualTo(TagPole.Positive)
            );

            Assert.That(
                conviction.HeldTag.TagInstance.IsOpposedTo(
                    mood.HeldTag.TagInstance
                ),
                Is.True
            );
        }
        [Test]
        public void
            CreatePlayerEntityStartsWithoutRehearsalMedia()
        {
            Assert.That(
                _entity.VhsSets,
                Is.Empty
            );

            Assert.That(
                _entity.ActiveVhsSetId,
                Is.Null.Or.Empty
            );

            Assert.That(
                _entity.GetTotalVhsTrackCountFromSets(),
                Is.EqualTo(0)
            );
        }

        [Test]
        public void
            CreatePlayerEntityStartsWithoutDemoMedia()
        {
            Assert.That(
                _entity.DemoTapes,
                Is.Empty
            );

            Assert.That(
                _entity.GetLatestUnreleasedDemoTape(),
                Is.Null
            );
        }

        [Test]
        public void
            CreatePlayerEntityStartsWithoutLooseIdeas()
        {
            Assert.That(
                _entity.Ideas,
                Is.Empty
            );
        }

        [Test]
        public void
            CreatePlayerEntityUsesRequestedDisplayName()
        {
            Object.DestroyImmediate(
                _entity.gameObject
            );

            _entity =
                _bootstrapper.CreatePlayerEntity(
                    TestClientId,
                    "Mayhem"
                );

            Assert.That(
                _entity.DisplayName,
                Is.EqualTo("Mayhem")
            );
        }

        [Test]
        public void
            CreatePlayerEntityRejectsEmptyDisplayName()
        {
            Assert.Throws<
                System.ArgumentException>(
                () =>
                    _bootstrapper
                        .CreatePlayerEntity(
                            TestClientId,
                            "   "
                        )
            );
        }
    }
}
