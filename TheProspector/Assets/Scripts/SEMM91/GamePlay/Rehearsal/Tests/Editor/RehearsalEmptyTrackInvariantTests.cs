using NUnit.Framework;
using SEMM91.Core.Entities;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;
using UnityEngine;

namespace SEMM91.GamePlay.Rehearsal.Tests.Editor
{
    public sealed class
        RehearsalEmptyTrackInvariantTests
    {
        private GameObject _entityObject;
        private GameEntity _entity;
        private RehearsalActionResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _entityObject =
                new GameObject("Rehearsal Test Entity");

            _entity =
                _entityObject.AddComponent<GameEntity>();

            _entity.InitializeIdentity(
                "ENTITY_TEST",
                "Test Entity",
                GameEntityType.Character
            );

            _resolver =
                new RehearsalActionResolver(
                    getCurrentTurn: () => 1,
                    log: _ => { }
                );
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(
                _entityObject
            );
        }

        [Test]
        public void Track_WithNoIdeas_IsEmpty()
        {
            Track track =
                CreateEmptyTrack("TRACK_EMPTY");

            Assert.That(
                track.IsEmpty,
                Is.True
            );

            track.AddIdea(
                CreateIdea("IDEA_FIRST")
            );

            Assert.That(
                track.IsEmpty,
                Is.False
            );
        }

        [Test]
        public void RehearsalSet_WithNoTracks_HasNoEmptyTrack()
        {
            RehearsalSet set =
                CreateSet("SET_EMPTY");

            Assert.That(
                set.HasEmptyTrack,
                Is.False
            );
        }

        [Test]
        public void RehearsalSet_WithEmptyTrack_HasEmptyTrack()
        {
            RehearsalSet set =
                CreateSet("SET_A");

            set.AddTrack(
                CreateEmptyTrack("TRACK_A")
            );

            Assert.That(
                set.HasEmptyTrack,
                Is.True
            );
        }

        [Test]
        public void CreateTrack_ActiveSetAlreadyHasEmptyTrack_Rejects()
        {
            RehearsalSet set =
                CreateSet("SET_A");

            set.AddTrack(
                CreateEmptyTrack("TRACK_EXISTING")
            );

            AddAndActivate(set);

            bool succeeded =
                _resolver
                    .TryCreateEmptyTrackInActiveSet(
                        0,
                        _entity,
                        1,
                        out string message
                    );

            Assert.That(
                succeeded,
                Is.False
            );

            Assert.That(
                set.VhsTracks.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                message,
                Does.Contain("already contains")
            );
        }

        [Test]
        public void CreateTrack_EmptyTrackInDifferentSet_Allows()
        {
            RehearsalSet setA =
                CreateSet("SET_A");

            setA.AddTrack(
                CreateEmptyTrack("TRACK_A")
            );

            RehearsalSet setB =
                CreateSet("SET_B");

            _entity.AddVhsSet(setA);
            _entity.AddVhsSet(setB);
            _entity.SetActiveVhsSet(setB);

            bool succeeded =
                _resolver
                    .TryCreateEmptyTrackInActiveSet(
                        0,
                        _entity,
                        1,
                        out _
                    );

            Assert.That(
                succeeded,
                Is.True
            );

            Assert.That(
                setA.VhsTracks.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                setB.VhsTracks.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void CreateTrack_AfterFirstIdeaAdded_AllowsNextEmptyTrack()
        {
            RehearsalSet set =
                CreateSet("SET_A");

            Track firstTrack =
                CreateEmptyTrack("TRACK_FIRST");

            set.AddTrack(firstTrack);
            AddAndActivate(set);

            firstTrack.AddIdea(
                CreateIdea("IDEA_FIRST")
            );

            bool succeeded =
                _resolver
                    .TryCreateEmptyTrackInActiveSet(
                        0,
                        _entity,
                        1,
                        out _
                    );

            Assert.That(
                succeeded,
                Is.True
            );

            Assert.That(
                set.VhsTracks.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                set.VhsTracks[0].IsEmpty,
                Is.False
            );

            Assert.That(
                set.VhsTracks[1].IsEmpty,
                Is.True
            );
        }

        private void AddAndActivate(
            RehearsalSet set)
        {
            _entity.AddVhsSet(set);
            _entity.SetActiveVhsSet(set);
        }

        private static RehearsalSet CreateSet(
            string id)
        {
            return new RehearsalSet(
                id,
                id,
                0
            );
        }

        private static Track CreateEmptyTrack(
            string id)
        {
            return new Track(
                id,
                "Untitled Track",
                0.0f,
                0
            );
        }

        private static Idea CreateIdea(
            string id)
        {
            return new Idea(
                ideaId: id,
                aspectId: "ASPECT_TEST",
                tagInstance: new TagInstance(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                ),
                conveyance: 1.0f,
                sourceEntityId: "ENTITY_TEST",
                sourceContainerType:
                    TagContainerType.Transient
            );
        }
        
        [Test]
        public void TrySetGeneratedDisplayName_ValidTitle_UpdatesSetTitle()
        {
            RehearsalSet set =
                new RehearsalSet(
                    "SET_A",
                    "Set_1",
                    0
                );

            bool changed =
                set.TrySetGeneratedDisplayName(
                    "Frozen Damnation"
                );

            Assert.That(changed, Is.True);

            Assert.That(
                set.DisplayName,
                Is.EqualTo("Frozen Damnation")
            );
        }

        [Test]
        public void TrySetGeneratedDisplayName_WhitespaceOnly_PreservesExistingTitle()
        {
            RehearsalSet set =
                new RehearsalSet(
                    "SET_A",
                    "Set_1",
                    0
                );

            bool changed =
                set.TrySetGeneratedDisplayName("   ");

            Assert.That(changed, Is.False);

            Assert.That(
                set.DisplayName,
                Is.EqualTo("Set_1")
            );
        }
        
        [Test]
        public void RecordActiveSet_CopiesSetDisplayNameVerbatimToDemo()
        {
            RehearsalSet set =
                CreateSet("SET_A");

            Assert.That(
                set.TrySetGeneratedDisplayName(
                    "Frozen Damnation"
                ),
                Is.True
            );

            Track track =
                CreateEmptyTrack("TRACK_A");

            track.AddIdea(
                CreateIdea("IDEA_A")
            );

            set.AddTrack(track);

            AddAndActivate(set);

            bool succeeded =
                _resolver.TryRecordActiveSetToDemo(
                    0,
                    _entity,
                    1,
                    out string message
                );

            Assert.That(
                succeeded,
                Is.True,
                message
            );

            Assert.That(
                _entity.DemoTapes.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                _entity.DemoTapes[0].DisplayName,
                Is.EqualTo("Frozen Damnation")
            );

            Assert.That(
                _entity.DemoTapes[0].SourceSetName,
                Is.EqualTo("Frozen Damnation")
            );
        }
        
        [Test]
        public void AppendIdea_RegeneratesActiveRehearsalSetTitle()
        {
            RehearsalSet set =
                CreateSet("SET_NAMING");

            Track track =
                CreateEmptyTrack("TRACK_A");

            set.AddTrack(track);

            AddAndActivate(set);

            _entity.AddIdea(
                CreateIdea("IDEA_A")
            );

            bool succeeded =
                _resolver.TryAppendNextIdeaToLatestTrack(
                    0,
                    _entity,
                    out string message
                );

            Assert.That(
                succeeded,
                Is.True,
                message
            );

            bool expectedAvailable =
                ReleaseNamingGenerator.TryGenerate(
                    set,
                    out string expectedTitle
                );

            Assert.That(
                expectedAvailable,
                Is.True
            );

            Assert.That(
                set.DisplayName,
                Is.EqualTo(expectedTitle)
            );

            Assert.That(
                set.DisplayName,
                Is.Not.EqualTo("SET_NAMING")
            );
        }
    }
}