using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Scenario;
using SEMM91.GamePlay.Kvlt.Settlement;
using SEMM91.GamePlay.Score;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.TurnFlow.Tests.Editor
{
    public sealed class KvltTurnResolutionRecordTests
    {
        private const string SceneId =
            "SCENE_NODE_KVLT";

        [Test]
        public void
            NonWinterRecordPreservesCompleteCausalTail()
        {
            KvltTurnResolutionRecord record =
                EmptyRecord();

            Assert.That(record.SettledTurn, Is.EqualTo(0));
            Assert.That(record.PublishedTurn, Is.EqualTo(1));
            Assert.That(record.IsYearEnd, Is.False);
            Assert.That(record.Happening, Is.Not.Null);
            Assert.That(
                record.CanonizationScreening,
                Is.Not.Null
            );
            Assert.That(record.Score, Is.Not.Null);
            Assert.That(record.Standing, Is.Not.Null);
            Assert.That(record.Ingress, Is.Not.Null);
            Assert.That(record.NextEnvironment, Is.Not.Null);
            Assert.That(record.Canonization, Is.Null);
            Assert.That(record.KeeperTransition, Is.Null);
        }

        [Test]
        public void
            WorldAcceptsRecordOnceAndRejectsReplay()
        {
            SeededWorldState world =
                World();

            KvltTurnResolutionRecord record =
                EmptyRecord();

            Assert.That(
                world.TryRecordKvltTurnResolution(record),
                Is.True
            );

            Assert.That(
                world.TryRecordKvltTurnResolution(record),
                Is.False
            );

            Assert.That(
                world.LatestKvltTurnResolutionRecord,
                Is.SameAs(record)
            );
        }

        [Test]
        public void
            RecordRejectsScoreFromAnotherTurn()
        {
            Assert.Throws<ArgumentException>(
                () => EmptyRecord(scoreTurn: 1)
            );
        }

        private static KvltTurnResolutionRecord
            EmptyRecord(int scoreTurn = 0)
        {
            const int turn = 0;

            SeededWorldState world =
                World();

            KvltScenarioProfile scenario =
                Peak2KvltScenarioProfileFactory
                    .CreateDefault();

            KvltExistingFieldRuntimeSettlementResult
                existing =
                    new
                        KvltExistingFieldRuntimeSettlementService()
                        .Settle(
                            world,
                            scenario,
                            SceneId,
                            turn
                        );

            KvltHappeningConsequenceSettlementResult
                happening =
                    new(
                        turn,
                        Array.Empty<Happening>(),
                        Array.Empty<
                            ActivationCrisisSettlement>(),
                        Array.Empty<
                            SceneReleaseLegitimacyEvaluation>(),
                        new Dictionary<
                            string,
                            SceneReleaseLegitimacyEvaluation>(),
                        new Dictionary<
                            string,
                            SceneReleaseFetteringResult>(),
                        0,
                        0,
                        0,
                        0,
                        0
                    );

            KvltTurnScoreSettlementResult score =
                new(
                    SceneId,
                    scoreTurn,
                    Array.Empty<ScoreEvent>()
                );

            KvltPostHappeningCanonizationScreeningResult
                screening =
                    new
                        KvltPostHappeningRuntimeSettlementService()
                        .Screen(
                            world,
                            SceneId,
                            turn,
                            existing.EvaluationEnvironment
                        );

            KvltSettledSceneStandingResult standing =
                new KvltSettledSceneStandingService()
                    .Settle(
                        SceneId,
                        turn,
                        world.SceneReleases,
                        scenario.StandingProjectionPolicy
                    );

            KvltNextTurnIngressSettlementResult ingress =
                new KvltNextTurnIngressSettlementService()
                    .Settle(
                        SceneId,
                        turn,
                        scenario.FreshReleasePosition,
                        scenario.InnerFieldEntryCeiling,
                        world.SceneReleases
                    );

            KvltNextSceneEnvironmentSettlementResult
                environment =
                    new
                        KvltNextSceneEnvironmentSettlementService()
                        .Settle(
                            SceneId,
                            world.KvltCanon,
                            world.SceneReleases,
                            Array.Empty<DemoTape>(),
                            ingress,
                            scenario.PressureRebuildPolicy,
                            scenario
                                .NormativePressureBlendPolicy
                        );

            return new KvltTurnResolutionRecord(
                new KvltTurnChronologyPlanner()
                    .Plan(turn, 4),
                existing,
                happening,
                screening,
                null,
                score,
                Array.Empty<YearInfluenceEvaluation>(),
                null,
                Array.Empty<
                    SceneReleaseCanonTenureTransitionApplication>(),
                standing,
                ingress,
                environment
            );
        }

        private static SeededWorldState World()
        {
            return new SeededWorldState(
                new CollectiveRegistry()
            );
        }
    }
}