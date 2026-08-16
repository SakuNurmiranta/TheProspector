using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Scenario;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Score;
using SEMM91.GamePlay.Society;
using SEMM91.GamePlay.World;
using UnityEditor;
using UnityEngine;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltTurnScoreSettlementServiceTests
    {
        private const int Turn =
            8;

        private const string SceneId =
            "KVLT_SCENE";

        private const string InitialTenureId =
            "TENURE_MAYHEM_INITIAL";

        private const string NextTenureId =
            "TENURE_NEXT_KEEPER";

        private const string MayhemEntityId =
            "ENTITY_MAYHEM_TEST";

        private const string FreezingMoonPath =
            "Assets/Scripts/SEMM91/GamePlay/" +
            "Kvlt/Scenario/Data/FreezingMoon.json";

        private readonly
            KvltTurnScoreSettlementService
            service =
                new();

        [Test]
        public void
            NewlyFetteredReleaseGetsResonanceButNoFieldGravity()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                new(
                    "Newly Fettered",
                    demo.DemoTapeId,
                    "OWNER",
                    SceneId,
                    Turn - 1,
                    1f
                );

            ApplyWeakActivation(
                release,
                demo,
                Turn
            );

            Assert.That(
                release.TryFetter(
                    Turn
                ),
                Is.True
            );

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    Evaluate(
                        release,
                        demo,
                        Turn
                    );

            Assert.That(
                evaluation.HasTrve,
                Is.True
            );

            Assert.That(
                evaluation.Resonance,
                Is.GreaterThan(0f)
            );

            Assert.That(
                evaluation.Gravity,
                Is.GreaterThan(0f)
            );

            ScoreLedger ledger =
                new();

            KvltTurnScoreSettlementResult result =
                service.Settle(
                    SceneId,
                    Turn,
                    "TENURE",
                    new CanonState(),
                    new[]
                    {
                        release
                    },
                    new Dictionary<
                        string,
                        SceneReleaseLegitimacyEvaluation>
                    {
                        {
                            release.ReleaseId,
                            evaluation
                        }
                    },
                    ledger
                );

            Assert.That(
                result.CountOf(
                    ScoreEventKind
                        .FirstFetterResonance
                ),
                Is.EqualTo(1)
            );

            Assert.That(
                result.CountOf(
                    ScoreEventKind.FieldGravity
                ),
                Is.EqualTo(0)
            );

            Assert.That(
                result.EventCount,
                Is.EqualTo(1)
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                ledger.History[0].Amount,
                Is.EqualTo(
                    evaluation.Resonance
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            ExistingFieldReleaseGetsRecurringFieldGravity()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                new(
                    "Existing Field",
                    demo.DemoTapeId,
                    "OWNER",
                    SceneId,
                    Turn - 2,
                    1f
                );

            ApplyWeakActivation(
                release,
                demo,
                Turn - 1
            );

            Assert.That(
                release.TryFetter(
                    Turn - 1
                ),
                Is.True
            );

            SceneReleaseLegitimacyEvaluation
                evaluation =
                    Evaluate(
                        release,
                        demo,
                        Turn
                    );

            ScoreLedger ledger =
                new();

            KvltTurnScoreSettlementResult result =
                service.Settle(
                    SceneId,
                    Turn,
                    "TENURE",
                    new CanonState(),
                    new[]
                    {
                        release
                    },
                    new Dictionary<
                        string,
                        SceneReleaseLegitimacyEvaluation>
                    {
                        {
                            release.ReleaseId,
                            evaluation
                        }
                    },
                    ledger
                );

            /*
             * No delayed First-Fetter award is
             * manufactured merely because the ledger
             * did not happen to contain turn-7 Score.
             */
            Assert.That(
                result.CountOf(
                    ScoreEventKind
                        .FirstFetterResonance
                ),
                Is.EqualTo(0)
            );

            Assert.That(
                result.CountOf(
                    ScoreEventKind.FieldGravity
                ),
                Is.EqualTo(1)
            );

            Assert.That(
                result.EventCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.AmountOf(
                    ScoreEventKind.FieldGravity
                ),
                Is.EqualTo(
                    evaluation.Gravity
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            CanonRetainedReleaseGetsFrozenGravityWithoutOrdinaryEvaluation()
        {
            StartingCanonContext context =
                CreateStartingCanonContext();

            ScoreLedger ledger =
                new();

            KvltTurnScoreSettlementResult result =
                service.Settle(
                    StartingCollectiveBootstrapper
                        .NodeKvltScene,
                    globalTurn:
                        2,
                    currentKeeperTenureId:
                        InitialTenureId,
                    historicalPayoutCanon:
                        context.World.KvltCanon,
                    releases:
                        new[]
                        {
                            context.Release
                        },
                    legitimacyByRelease:
                        new Dictionary<
                            string,
                            SceneReleaseLegitimacyEvaluation>(),
                    ledger:
                        ledger
                );

            Assert.That(
                result.CountOf(
                    ScoreEventKind
                        .CanonRetainedGravity
                ),
                Is.EqualTo(1)
            );

            Assert.That(
                result.CountOf(
                    ScoreEventKind.FieldGravity
                ),
                Is.EqualTo(0)
            );

            Assert.That(
                result.EventCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.AmountOf(
                    ScoreEventKind
                        .CanonRetainedGravity
                ),
                Is.EqualTo(
                    context.Release
                        .FrozenPostAssimilationGravity
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            HistoricalCanonPaysStillCurrentFrontierInstitutionalGravity()
        {
            StartingCanonContext context =
                CreateStartingCanonContext();

            IReadOnlyList<
                    SceneReleaseCanonTenureTransitionApplication>
                transition =
                    new
                        SceneReleaseCanonTenureTransitionService()
                        .Apply(
                            new[]
                            {
                                context.Release
                            },
                            InitialTenureId,
                            NextTenureId,
                            globalTurn:
                                3
                        );

            Assert.That(
                transition.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                context.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                )
            );

            ScoreLedger ledger =
                new();

            KvltTurnScoreSettlementResult result =
                service.Settle(
                    StartingCollectiveBootstrapper
                        .NodeKvltScene,
                    globalTurn:
                        4,
                    currentKeeperTenureId:
                        NextTenureId,
                    historicalPayoutCanon:
                        context.World.KvltCanon,
                    releases:
                        new[]
                        {
                            context.Release
                        },
                    legitimacyByRelease:
                        new Dictionary<
                            string,
                            SceneReleaseLegitimacyEvaluation>(),
                    ledger:
                        ledger
                );

            /*
             * Freezing Moon's starting institution
             * carries the three ScenarioSeed frontier
             * claims established in Entries 8-9.
             */
            Assert.That(
                context.Release
                    .CanonGravityDecompositionState
                    .FrontierContributions
                    .Count,
                Is.EqualTo(3)
            );

            Assert.That(
                result.CountOf(
                    ScoreEventKind
                        .InstitutionalGravity
                ),
                Is.EqualTo(3)
            );

            Assert.That(
                result.CountOf(
                    ScoreEventKind
                        .CanonRetainedGravity
                ),
                Is.EqualTo(0)
            );

            Assert.That(
                result.EventCount,
                Is.EqualTo(3)
            );

            Assert.That(
                result.AmountOf(
                    ScoreEventKind
                        .InstitutionalGravity
                ),
                Is.EqualTo(
                    context.Release
                        .CanonGravityDecompositionState
                        .FrontierContributionTotal
                ).Within(0.0001f)
            );

            foreach (
                ScoreEvent scoreEvent
                in result.ScoreEvents)
            {
                Assert.That(
                    scoreEvent.BeneficiaryEntityId,
                    Is.EqualTo(
                        MayhemEntityId
                    )
                );
            }
        }

        private static void ApplyWeakActivation(
            SceneRelease release,
            DemoTape demo,
            int appliedTurn)
        {
            ActivationLegitimacyCandidate candidate =
                new(
                    ActivationAttemptRoute.Performance,
                    "HAPPENING",
                    "PERFORM",
                    "ACTOR",
                    release.ReleaseId,
                    demo.DemoTapeId,
                    "TRACK",
                    "IDEA",
                    0,
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    TagDegree.Weak,
                    TagDegree.Transgressive
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT_PUBLIC_PERFORMANCE",
                    "KVLT",
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    0,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "TEST"
                );

            ActivationLegitimacyAssessment assessment =
                new(
                    candidate,
                    ActivationLegitimacyDisposition
                        .Covered,
                    precedent
                );

            Assert.That(
                new SceneReleaseActivationStateService()
                    .ApplyCovered(
                        release,
                        assessment,
                        appliedTurn
                    ),
                Is.True
            );
        }

        private static
            SceneReleaseLegitimacyEvaluation
            Evaluate(
                SceneRelease release,
                DemoTape demo,
                int settledTurn)
        {
            return new
                SceneReleaseLegitimacyEvaluator()
                .Evaluate(
                    release,
                    demo,
                    EvaluationEnvironment(
                        settledTurn
                    )
                );
        }

        private static
            TrackEvaluationEnvironment
            EvaluationEnvironment(
                int settledTurn)
        {
            /*
             * The Score tests need an actually resonant
             * release, not merely a structurally valid TRVE
             * release.
             *
             * The test Demo's dominant recorded Tag is:
             *
             * Symbolic / Negative / Transgressive
             *
             * so the current Normative Centre must positively
             * recognize that direction for Surface, Resonance
             * and consequently Gravity to be non-zero.
             *
             * This mirrors the established Score test fixture
             * rather than changing production evaluation.
             */
            NormativeCentre centre =
                new(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant,
                            1f
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            1f
                        )
                    }
                );

            SocietyNormativeProfile society =
                new();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            return new TrackEvaluationEnvironment(
                settledTurn,
                centre,
                centre,
                society
            );
        }

        private static DemoTape Demo()
        {
            DemoTapeIdeaSnapshot pair =
                new(
                    "IDEA",
                    0,
                    "ASPECT",
                    IdeaPayloadType.TagPair,
                    "AUTHOR",
                    TagContainerType.Transient,
                    1f,
                    new[]
                    {
                        new DemoTapeTagOccurrenceSnapshot(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            DemoTapeTagOccurrenceRole
                                .PairDominant
                        ),

                        new DemoTapeTagOccurrenceSnapshot(
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            TagDegree.Weak,
                            DemoTapeTagOccurrenceRole
                                .PairSubmissive
                        )
                    }
                );

            DemoTapeTrackSnapshot track =
                new(
                    "TRACK",
                    "Track",
                    1f,
                    1f,
                    new[]
                    {
                        pair
                    }
                );

            return new DemoTape(
                "DEMO",
                "Demo",
                "SET",
                "Set",
                1,
                1,
                1f,
                new[]
                {
                    track
                }
            );
        }

        private static StartingCanonContext
            CreateStartingCanonContext()
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
                    MayhemEntityId,
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

            KvltStartingCanonReleaseBootstrapResult
                releaseBootstrap =
                    new
                        KvltStartingCanonReleaseBootstrapper()
                        .Apply(
                            world,
                            tape,
                            MayhemEntityId,
                            StartingCollectiveBootstrapper
                                .NodeKvltScene,
                            InitialTenureId,
                            startingCanonScenePosition:
                                1f,
                            startingTurn:
                                0
                        );

            IReadOnlyList<
                    SceneReleaseCanonFrontierClaim>
                frontierClaims =
                    new
                        KvltStartingCanonFrontierResolver()
                        .Resolve(
                            world,
                            tape,
                            releaseBootstrap.Release,
                            MayhemEntityId
                        );

            new
                SceneReleaseCanonGravityDecompositionService()
                .Apply(
                    releaseBootstrap.Release,
                    releaseBootstrap
                        .FinalLegitimacy,
                    frontierClaims
                );

            return new StartingCanonContext(
                world,
                releaseBootstrap.Release
            );
        }

        private sealed class
            StartingCanonContext
        {
            public SeededWorldState World { get; }

            public SceneRelease Release { get; }

            public StartingCanonContext(
                SeededWorldState world,
                SceneRelease release)
            {
                World =
                    world;

                Release =
                    release;
            }
        }
    }
}
