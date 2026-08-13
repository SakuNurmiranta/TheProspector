using NUnit.Framework;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.World;
using UnityEditor;
using UnityEngine;

namespace SEMM91.GamePlay.Kvlt.Scenario.Tests.Editor
{
    public sealed class
        KvltStartingCanonReleaseBootstrapperTests
    {
        private const string FreezingMoonPath =
            "Assets/Scripts/SEMM91/GamePlay/" +
            "Kvlt/Scenario/Data/FreezingMoon.json";

        private const string KeeperTenureId =
            "TENURE_MAYHEM_INITIAL";

        private readonly
            KvltStartingCanonReleaseBootstrapper
            bootstrapper =
                new();

        [Test]
        public void
            ApplyCreatesTurnZeroCanonRetainedRelease()
        {
            Context context =
                CreateContext();

            KvltStartingCanonReleaseBootstrapResult
                result =
                    Apply(context);

            SceneRelease release =
                result.Release;

            Assert.That(
                context.World.SceneReleases.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                release.ReleasedTurn,
                Is.EqualTo(0)
            );

            Assert.That(
                release.CanonizedTurn,
                Is.EqualTo(0)
            );

            Assert.That(
                release.CanonizedUnderKeeperTenureId,
                Is.EqualTo(
                    KeeperTenureId
                )
            );

            Assert.That(
                release.HasFieldPosition,
                Is.False
            );

            Assert.That(
                context.DemoTape.SceneState,
                Is.EqualTo(
                    DemoTapeSceneState.Hosted
                )
            );
        }

        [Test]
        public void
            ApplySeedsExactlyThreeDegreeOnePairActivations()
        {
            Context context =
                CreateContext();

            SceneRelease release =
                Apply(context).Release;

            Assert.That(
                release.PairActivationStates.Count,
                Is.EqualTo(3)
            );

            foreach (
                SceneReleasePairActivationState state
                in release.PairActivationStates)
            {
                Assert.That(
                    state.RecordedDominantDegree,
                    Is.EqualTo(
                        TagDegree.Weak
                    )
                );

                Assert.That(
                    state.CurrentActivationDegree,
                    Is.EqualTo(
                        TagDegree.Weak
                    )
                );
            }

            Assert.That(
                release.TryGetPairActivationState(
                    "FREEZING_MOON_TRACK_1",
                    "FM_T1_PROFANE_SACRED",
                    0,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.TryGetPairActivationState(
                    "FREEZING_MOON_TRACK_2",
                    "FM_T2_MORBID_VITAL",
                    0,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.TryGetPairActivationState(
                    "FREEZING_MOON_TRACK_3",
                    "FM_T3_MALEVOLENT_BENEVOLENT",
                    0,
                    out _
                ),
                Is.True
            );
        }

        [Test]
        public void
            ApplyFreezesProductionLegitimacyGravity()
        {
            Context context =
                CreateContext();

            KvltStartingCanonReleaseBootstrapResult
                result =
                    Apply(context);

            Assert.That(
                result.FinalLegitimacy.HasTrve,
                Is.True
            );

            Assert.That(
                result.Release
                    .FrozenPostAssimilationGravity,
                Is.EqualTo(
                    result.FinalLegitimacy.Gravity
                ).Within(0.0001f)
            );

            Assert.That(
                result.FreezeState
                    .FrozenPairActivations.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                result.Release.IsActivationFrozen,
                Is.True
            );
        }

        [Test]
        public void
            SupportingSolitaryIdeasDiluteTrackTrve()
        {
            Context context =
                CreateContext();

            KvltStartingCanonReleaseBootstrapResult
                result =
                    Apply(context);

            foreach (
                TrackLegitimacyEvaluation track
                in result.FinalLegitimacy
                    .TrackEvaluations)
            {
                Assert.That(
                    track.Trve.IdeaEvaluations.Count,
                    Is.EqualTo(3)
                );

                IdeaTrveEvaluation activePair =
                    null;

                int zeroContributionIdeas =
                    0;

                foreach (
                    IdeaTrveEvaluation idea
                    in track.Trve.IdeaEvaluations)
                {
                    if (idea.ActiveTrveContribution >
                        0f)
                    {
                        Assert.That(
                            activePair,
                            Is.Null
                        );

                        activePair =
                            idea;
                    }
                    else
                    {
                        zeroContributionIdeas++;
                    }
                }

                Assert.That(
                    activePair,
                    Is.Not.Null
                );

                Assert.That(
                    zeroContributionIdeas,
                    Is.EqualTo(2)
                );

                /*
                 * One active TRVE pair distributed across
                 * three real Ideas.
                 */
                Assert.That(
                    track.T,
                    Is.EqualTo(
                        activePair
                            .ActiveTrveContribution /
                        3f
                    ).Within(0.0001f)
                );
            }
        }

        [Test]
        public void
            InitialStateDoesNotInventActivationOrAssimilationHistory()
        {
            Context context =
                CreateContext();

            SceneRelease release =
                Apply(context).Release;

            Assert.That(
                release.ActivationHistory,
                Is.Empty
            );

            Assert.That(
                release.ActivationAttempts,
                Is.Empty
            );

            Assert.That(
                release.CanonAssimilationHistory,
                Is.Empty
            );

            Assert.That(
                release.LifecycleTransitions.Count,
                Is.EqualTo(1)
            );

            SceneReleaseLifecycleTransition
                transition =
                    release.LifecycleTransitions[0];

            Assert.That(
                transition.FromState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Fringe
                )
            );

            Assert.That(
                transition.ToState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                transition.GlobalTurn,
                Is.EqualTo(0)
            );
        }

        private KvltStartingCanonReleaseBootstrapResult
            Apply(
                Context context)
        {
            return bootstrapper.Apply(
                context.World,
                context.DemoTape,
                "ENTITY_MAYHEM_TEST",
                StartingCollectiveBootstrapper
                    .NodeKvltScene,
                KeeperTenureId,
                startingTurn:
                    0
            );
        }

        private static Context CreateContext()
        {
            SeededWorldState world =
                new(
                    new CollectiveRegistry()
                );

            TextAsset asset =
                AssetDatabase
                    .LoadAssetAtPath<TextAsset>(
                        FreezingMoonPath
                    );

            Assert.That(
                asset,
                Is.Not.Null
            );

            DemoTape tape =
                AuthoredDemoTapeJsonLoader.Load(
                    asset,
                    "ENTITY_MAYHEM_TEST",
                    recordedTurn:
                        0
                );

            new KvltStartingWorldStateBootstrapper()
                .Apply(
                    Peak2KvltScenarioProfileFactory
                        .CreateDefault(),
                    world,
                    tape,
                    startingTurn:
                        0
                );

            return new Context(
                world,
                tape
            );
        }

        private sealed class Context
        {
            public SeededWorldState World { get; }

            public DemoTape DemoTape { get; }

            public Context(
                SeededWorldState world,
                DemoTape demoTape)
            {
                World =
                    world;

                DemoTape =
                    demoTape;
            }
        }
    }
}