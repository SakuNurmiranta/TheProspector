using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Core.Entities;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;
using UnityEngine;

namespace SEMM91.GamePlay.Agency.Tests.Editor
{
    public sealed class PlayerEntityBootstrapperTests
    {
        private const ulong TestClientId = 7;

        private PlayerEntityBootstrapper _bootstrapper;
        private GameEntity _entity;

        [SetUp]
        public void SetUp()
        {
            _bootstrapper =
                new PlayerEntityBootstrapper(
                    log: _ => { }
                );

            _entity =
                _bootstrapper
                    .CreateStartingPlayerEntity(
                        TestClientId
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
            CreateStartingPlayerEntity_CreatesExpectedRehearsalSets()
        {
            Assert.That(
                _entity,
                Is.Not.Null
            );

            Assert.That(
                _entity.VhsSets.Count,
                Is.EqualTo(2)
            );

            RehearsalSet firstSet =
                _entity.VhsSets[0];

            RehearsalSet secondSet =
                _entity.VhsSets[1];

            Assert.That(
                firstSet.VhsSetId,
                Is.EqualTo(
                    "PRESESSION_VHS_SET_7_A"
                )
            );

            Assert.That(
                firstSet.DisplayName,
                Is.EqualTo(
                    "Rehearsal VHS A"
                )
            );

            Assert.That(
                firstSet.VhsTracks.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                secondSet.VhsSetId,
                Is.EqualTo(
                    "PRESESSION_VHS_SET_7_B"
                )
            );

            Assert.That(
                secondSet.DisplayName,
                Is.EqualTo(
                    "Rehearsal VHS B"
                )
            );

            Assert.That(
                secondSet.VhsTracks.Count,
                Is.EqualTo(4)
            );

            Assert.That(
                _entity
                    .GetTotalVhsTrackCountFromSets(),
                Is.EqualTo(7)
            );
        }

        [Test]
        public void
            CreateStartingPlayerEntity_SelectsFirstRehearsalSet()
        {
            RehearsalSet expectedActiveSet =
                _entity.VhsSets[0];

            Assert.That(
                _entity.ActiveVhsSetId,
                Is.EqualTo(
                    expectedActiveSet.VhsSetId
                )
            );

            Assert.That(
                _entity.GetActiveVhsSet(),
                Is.SameAs(expectedActiveSet)
            );
        }

        [Test]
        public void
            CreateStartingPlayerEntity_RegistersStartingTrackAspects()
        {
            Assert.That(
                _entity.AspectIds,
                Does.Contain("ASPECT_LYRICS")
            );

            Assert.That(
                _entity.AspectIds,
                Does.Contain("ASPECT_VOCALS")
            );

            Assert.That(
                _entity.AspectIds,
                Does.Contain("ASPECT_GUITAR")
            );

            Assert.That(
                _entity.AspectIds,
                Does.Contain("ASPECT_DRUMS")
            );
        }

        [Test]
        public void
            CreateStartingPlayerEntity_LeavesNoEmptyStartingTracks()
        {
            foreach (
                RehearsalSet rehearsalSet
                in _entity.VhsSets)
            {
                Assert.That(
                    rehearsalSet.HasEmptyTrack,
                    Is.False,
                    $"Set {rehearsalSet.VhsSetId} " +
                    "should contain no empty Tracks."
                );

                foreach (
                    Track track
                    in rehearsalSet.VhsTracks)
                {
                    Assert.That(
                        track.IsEmpty,
                        Is.False,
                        $"Track {track.VhsTrackId} " +
                        "should contain a starting Idea."
                    );

                    Assert.That(
                        track.Ideas.Count,
                        Is.EqualTo(1),
                        $"Track {track.VhsTrackId} " +
                        "should contain exactly one " +
                        "starting Idea."
                    );
                }
            }
        }

        [Test]
        public void
            CreateStartingPlayerEntity_SeedsAuthoredTrackSemantics()
        {
            AssertTrackFixture(
                setIndex: 0,
                trackIndex: 0,
                expectedAspectId:
                    "ASPECT_LYRICS",
                expectedAxis:
                    TagAxis.Symbolic
            );

            AssertTrackFixture(
                setIndex: 0,
                trackIndex: 1,
                expectedAspectId:
                    "ASPECT_VOCALS",
                expectedAxis:
                    TagAxis.Emotional
            );

            AssertTrackFixture(
                setIndex: 0,
                trackIndex: 2,
                expectedAspectId:
                    "ASPECT_GUITAR",
                expectedAxis:
                    TagAxis.Expressive
            );

            AssertTrackFixture(
                setIndex: 1,
                trackIndex: 0,
                expectedAspectId:
                    "ASPECT_DRUMS",
                expectedAxis:
                    TagAxis.Temporal
            );

            AssertTrackFixture(
                setIndex: 1,
                trackIndex: 1,
                expectedAspectId:
                    "ASPECT_LYRICS",
                expectedAxis:
                    TagAxis.Physical
            );

            AssertTrackFixture(
                setIndex: 1,
                trackIndex: 2,
                expectedAspectId:
                    "ASPECT_VOCALS",
                expectedAxis:
                    TagAxis.Existential
            );

            AssertTrackFixture(
                setIndex: 1,
                trackIndex: 3,
                expectedAspectId:
                    "ASPECT_GUITAR",
                expectedAxis:
                    TagAxis.Interpretive
            );
        }

        [Test]
        public void
            CreateStartingPlayerEntity_RepresentsEveryTagAxisOnce()
        {
            HashSet<TagAxis> representedAxes =
                new();

            foreach (
                RehearsalSet rehearsalSet
                in _entity.VhsSets)
            {
                foreach (
                    Track track
                    in rehearsalSet.VhsTracks)
                {
                    Idea idea =
                        track.Ideas[0];

                    bool added =
                        representedAxes.Add(
                            idea.TagInstance.axis
                        );

                    Assert.That(
                        added,
                        Is.True,
                        $"Tag axis " +
                        $"{idea.TagInstance.axis} " +
                        "was used by more than one " +
                        "starting Track."
                    );
                }
            }

            Assert.That(
                representedAxes.Count,
                Is.EqualTo(7)
            );
        }

        [Test]
        public void
            CreateStartingPlayerEntity_GeneratesEveryTrackNameFromSemantics()
        {
            foreach (
                RehearsalSet rehearsalSet
                in _entity.VhsSets)
            {
                foreach (
                    Track track
                    in rehearsalSet.VhsTracks)
                {
                    bool generated =
                        TrackNamingGenerator
                            .TryGenerate(
                                track,
                                out string expectedTitle
                            );

                    Assert.That(
                        generated,
                        Is.True,
                        $"Naming failed for Track " +
                        $"{track.VhsTrackId}."
                    );

                    Assert.That(
                        string.IsNullOrWhiteSpace(
                            track.DisplayName
                        ),
                        Is.False
                    );

                    Assert.That(
                        track.DisplayName,
                        Is.Not.EqualTo(
                            "Untitled Track"
                        )
                    );

                    Assert.That(
                        track.DisplayName,
                        Is.EqualTo(expectedTitle)
                    );
                }
            }
        }

        [Test]
        public void
            CreateStartingPlayerEntity_UsesUniqueTrackAndIdeaIds()
        {
            HashSet<string> trackIds =
                new();

            HashSet<string> ideaIds =
                new();

            foreach (
                RehearsalSet rehearsalSet
                in _entity.VhsSets)
            {
                foreach (
                    Track track
                    in rehearsalSet.VhsTracks)
                {
                    Assert.That(
                        trackIds.Add(
                            track.VhsTrackId
                        ),
                        Is.True,
                        $"Duplicate Track ID: " +
                        $"{track.VhsTrackId}"
                    );

                    Idea idea =
                        track.Ideas[0];

                    Assert.That(
                        ideaIds.Add(
                            idea.IdeaId
                        ),
                        Is.True,
                        $"Duplicate Idea ID: " +
                        $"{idea.IdeaId}"
                    );
                }
            }

            Assert.That(
                trackIds.Count,
                Is.EqualTo(7)
            );

            Assert.That(
                ideaIds.Count,
                Is.EqualTo(7)
            );
        }

        [Test]
        public void
            CreateStartingPlayerEntity_DoesNotLeaveTrackIdeasInInventory()
        {
            Assert.That(
                _entity.Ideas.Count,
                Is.EqualTo(0)
            );
        }

        private void AssertTrackFixture(
            int setIndex,
            int trackIndex,
            string expectedAspectId,
            TagAxis expectedAxis)
        {
            RehearsalSet rehearsalSet =
                _entity.VhsSets[setIndex];

            Track track =
                rehearsalSet
                    .VhsTracks[trackIndex];

            Assert.That(
                track.Ideas.Count,
                Is.EqualTo(1)
            );

            Idea idea =
                track.Ideas[0];

            Assert.That(
                idea.PayloadType,
                Is.EqualTo(
                    IdeaPayloadType.SingleTag
                )
            );

            Assert.That(
                idea.AspectId,
                Is.EqualTo(expectedAspectId)
            );

            Assert.That(
                idea.TagInstance.axis,
                Is.EqualTo(expectedAxis)
            );

            Assert.That(
                idea.TagInstance.pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                idea.TagInstance.degree,
                Is.EqualTo(TagDegree.Weak)
            );

            Assert.That(
                idea.SourceEntityId,
                Is.EqualTo(_entity.EntityId)
            );

            Assert.That(
                idea.SourceContainerType,
                Is.EqualTo(
                    TagContainerType.Transient
                )
            );

            Assert.That(
                idea.Conveyance,
                Is.EqualTo(1.0f)
                    .Within(0.0001f)
            );
        }
    }
}