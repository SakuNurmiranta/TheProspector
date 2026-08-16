using System;
using NUnit.Framework;
using SEMM91.GamePlay.Actions.History;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltHappeningRuntimePreparationServiceTests
    {
        private const string SceneId =
            "SCENE_NODE_KVLT";

        private const int Turn =
            3;

        private readonly
            KvltHappeningRuntimePreparationService
            service =
                new();

        [Test]
        public void
            EmptyAuthoritativeWorldProducesEmptyPreparationPackage()
        {
            SeededWorldState world =
                World();

            KvltHappeningRuntimePreparationResult
                result =
                    service.Prepare(
                        world,
                        SceneId,
                        Turn,
                        Environment(
                            world
                        ),
                        new[]
                        {
                            "PLAYER_A",
                            "PLAYER_B"
                        },
                        keeperEntityId:
                            "PLAYER_A"
                    );

            Assert.That(
                result.GlobalTurn,
                Is.EqualTo(
                    Turn
                )
            );

            Assert.That(
                result.PublicSources,
                Is.Empty
            );

            Assert.That(
                result.Preparation
                    .PreparedHappenings,
                Is.Empty
            );

            Assert.That(
                result.RequiresCrisisVoting,
                Is.False
            );
        }

        [Test]
        public void
            CurrentResolvingHappeningIsPreparedFromWorldRegistry()
        {
            SeededWorldState world =
                World();

            Happening happening =
                new(
                    "HAPPENING",
                    "KVLT",
                    "PLAYER_A",
                    "promotion",
                    "night",
                    "NODE",
                    Turn,
                    new CharacterActionKey(
                        "PLAYER_A",
                        Turn,
                        0
                    )
                );

            Assert.That(
                happening.TryBeginResolving(
                    Turn
                ),
                Is.True
            );

            world.KvltHappeningRegistry.Record(
                happening
            );

            KvltHappeningRuntimePreparationResult
                result =
                    service.Prepare(
                        world,
                        SceneId,
                        Turn,
                        Environment(
                            world
                        ),
                        new[]
                        {
                            "PLAYER_A",
                            "PLAYER_B"
                        },
                        keeperEntityId:
                            "PLAYER_A"
                    );

            Assert.That(
                result.Preparation
                    .PreparedHappenings.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.Preparation
                    .PreparedHappenings[0],
                Is.SameAs(
                    happening
                )
            );

            Assert.That(
                result.RequiresCrisisVoting,
                Is.False
            );
        }

        [Test]
        public void
            TerminalReleaseIsExcludedButActiveOrphanFails()
        {
            SeededWorldState world =
                World();

            SceneRelease terminal =
                new(
                    "Terminal",
                    "MISSING_TERMINAL_DEMO",
                    "MISSING_TERMINAL_OWNER",
                    SceneId,
                    0,
                    1f
                );

            Assert.That(
                terminal.TryFailToFetter(
                    1
                ),
                Is.True
            );

            world.AddSceneRelease(
                terminal
            );

            /*
             * Terminal public history is not part of
             * the mutable Happening activation source
             * population, so its absent source owner is
             * irrelevant to this phase.
             */
            Assert.DoesNotThrow(
                () =>
                    service.Prepare(
                        world,
                        SceneId,
                        Turn,
                        Environment(
                            world
                        ),
                        new[]
                        {
                            "PLAYER_A",
                            "PLAYER_B"
                        },
                        "PLAYER_A"
                    )
            );

            SceneRelease activeOrphan =
                new(
                    "Active Orphan",
                    "MISSING_ACTIVE_DEMO",
                    "MISSING_ACTIVE_OWNER",
                    SceneId,
                    0,
                    1f
                );

            world.AddSceneRelease(
                activeOrphan
            );

            Assert.That(
                activeOrphan.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .Fringe
                )
            );

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    service.Prepare(
                        world,
                        SceneId,
                        Turn,
                        Environment(
                            world
                        ),
                        new[]
                        {
                            "PLAYER_A",
                            "PLAYER_B"
                        },
                        "PLAYER_A"
                    )
            );
        }

        private static SeededWorldState World()
        {
            return new SeededWorldState(
                new CollectiveRegistry()
            );
        }

        private static
            TrackEvaluationEnvironment
            Environment(
                SeededWorldState world)
        {
            return new TrackEvaluationEnvironment(
                Turn,
                NormativeCentre.Neutral,
                NormativeCentre.Neutral,
                world.SocietyNorms
            );
        }
        
        [Test]
public void
    CommittedHappeningClosesWindowAndSettlesIntentBeforePreparation()
{
    SeededWorldState world =
        World();

    Happening happening =
        new(
            "HAPPENING",
            "KVLT",
            "PLAYER_A",
            "promotion",
            "night",
            "NODE",
            Turn,
            new CharacterActionKey(
                "PLAYER_A",
                Turn,
                0
            )
        );

    Assert.That(
        happening.TryAddContext(
            new HappeningContext(
                "CONTEXT",
                "performance",
                HappeningContextAnchorKind.None,
                string.Empty,
                "PLAYER_A",
                Turn
            )
        ),
        Is.True
    );

    Assert.That(
        happening.TryAddParticipant(
            "PLAYER_A"
        ),
        Is.True
    );

    Assert.That(
        happening.TryRecordParticipantIntent(
            new HappeningPerformIntent(
                "INTENT",
                "HAPPENING",
                "CONTEXT",
                "PLAYER_A",
                Turn,
                "UNMATCHED_RELEASE"
            )
        ),
        Is.True
    );

    world.KvltHappeningRegistry.Record(
        happening
    );

    KvltHappeningRuntimePreparationResult
        result =
            service.Prepare(
                world,
                SceneId,
                Turn,
                Environment(
                    world
                ),
                new[]
                {
                    "PLAYER_A",
                    "PLAYER_B"
                },
                "PLAYER_A"
            );

    Assert.That(
        happening.LifecycleState,
        Is.EqualTo(
            HappeningLifecycleState.Resolving
        )
    );

    Assert.That(
        happening.IntentResolutions.Count,
        Is.EqualTo(1)
    );

    Assert.That(
        happening.IntentResolutions[0].Outcome,
        Is.EqualTo(
            HappeningIntentOutcome.Succeeded
        )
    );

    Assert.That(
        result.Preparation.PreparedHappenings.Count,
        Is.EqualTo(1)
    );
}
        
    }
    
}