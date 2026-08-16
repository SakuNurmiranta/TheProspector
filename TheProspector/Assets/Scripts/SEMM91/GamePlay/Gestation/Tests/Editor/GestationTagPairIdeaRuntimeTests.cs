using NUnit.Framework;
using SEMM91.Core.Entities;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;
using SEMM91.GamePlay.Agency;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Scenario;
using SEMM91.GamePlay.Society;
using UnityEngine;

namespace SEMM91.GamePlay.Gestation.Tests.Editor
{
    public sealed class GestationTagPairIdeaRuntimeTests
    {
        private GameEntity _entity;
        private GestationActionResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _entity =
                new PlayerEntityBootstrapper(
                    log: _ => { }
                ).CreatePlayerEntity(
                    12,
                    "Pair Test Band"
                );

            _resolver =
                new GestationActionResolver(
                    log: _ => { },
                    errorLog: _ => { }
                );

            _resolver.Initialize();
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
            _resolver = null;
        }

        [Test]
        public void OpposedHeldTagsCreateTrveEligibleFormalPairIdea()
        {
            bool created =
                _resolver.ResolveCreateTagPairIdea(
                    12,
                    _entity,
                    TagContainerType.Conviction
                );

            Assert.That(created, Is.True);
            Assert.That(_entity.Ideas.Count, Is.EqualTo(1));

            Idea idea = _entity.Ideas[0];

            Assert.That(
                idea.PayloadType,
                Is.EqualTo(IdeaPayloadType.TagPair)
            );

            Assert.That(idea.TagPair, Is.Not.Null);
            Assert.That(idea.TagPair.IsValidOpposition(), Is.True);
            Assert.That(idea.IsTRVEEligible(), Is.True);

            Assert.That(
                idea.TagPair.DominantTag.pole,
                Is.EqualTo(TagPole.Negative)
            );

            Assert.That(
                idea.TagPair.SubmissiveTag.pole,
                Is.EqualTo(TagPole.Positive)
            );

            Track track =
                new Track(
                    "BOT_PAIR_TRACK",
                    "Bot Pair Track",
                    0.35f,
                    0
                );

            track.AddIdea(idea);

            DemoTapeTrackSnapshot snapshot =
                DemoTapeTrackSnapshotFactory.Create(
                    track,
                    0.35f
                );

            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            foreach (KvltSocietyNormSeed seed
                     in Peak2KvltScenarioProfileFactory
                         .CreateDefault()
                         .StartingSocietyNorms)
            {
                society.SetNormativeDegree(
                    seed.Axis,
                    seed.Pole,
                    seed.Degree
                );
            }

            TrackEvaluationEnvironment environment =
                new TrackEvaluationEnvironment(
                    0,
                    NormativeCentre.Neutral,
                    NormativeCentre.Neutral,
                    society
                );

            TrackSocietyBackingEvaluation backing =
                new TrackSocietyBackingEvaluator()
                    .Evaluate(
                        snapshot,
                        environment
                    );

            TrackTrveCapabilityEvaluation capability =
                new TrackTrveCapabilityEvaluator()
                    .Evaluate(
                        snapshot,
                        backing
                    );

            Assert.That(
                capability.HasTrveCapableIdea,
                Is.True
            );

            Assert.That(
                capability.IdeaEvaluations[0]
                    .SocietyBacking,
                Is.GreaterThan(0f)
            );
        }

        [Test]
        public void MissingOpposedHeldTagRejectsPairWithoutCreatingIdea()
        {
            Assert.That(
                _entity.TrySetTag(
                    TagContainerType.Mood,
                    new TagInstance(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak
                    )
                ),
                Is.True
            );

            bool created =
                _resolver.ResolveCreateTagPairIdea(
                    12,
                    _entity,
                    TagContainerType.Conviction
                );

            Assert.That(created, Is.False);
            Assert.That(_entity.Ideas, Is.Empty);
        }
    }
}
