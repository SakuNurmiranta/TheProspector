using System;
using System.Collections;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.Kvlt.Standing;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Society;
using SEMM91.GamePlay.World;
using UnityEngine;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Runtime
{
    /// <summary>
    /// Camp-3 runtime acceptance probe.
    ///
    /// Runs two complete scene settlements across two
    /// actual Unity frames.
    ///
    /// Turn 6:
    ///     Canon1
    ///     + active degree-2 release
    ///     -> Canon2
    ///     -> CanonRetained
    ///     -> publish world state
    ///
    /// Turn 7:
    ///     world-published Canon2
    ///     + another active degree-2 release
    ///     -> no breakthrough
    ///     -> remains Field
    ///
    /// PASS therefore proves that settlement output
    /// becomes authoritative input to the following
    /// runtime settlement rather than feeding back
    /// inside the same pass.
    /// </summary>
    public sealed class
        KvltSceneSettlementRuntimeAcceptance :
        MonoBehaviour
    {
        [SerializeField]
        private bool runOnStart = true;

        private readonly
            KvltSceneSettlementService
            settlementService =
                new();

        private readonly
            CanonicalNormativeCentreDeriver
            normativeDeriver =
                new();

        private SeededWorldState world;

        private KvltSceneSettlementPolicy policy;

        private DemoTape firstTape;

        private SceneRelease firstRelease;

        private KvltSceneSettlementResult
            firstSettlement;

        private float
            firstFrozenGravity;

        private IEnumerator Start()
        {
            if (!runOnStart)
            {
                yield break;
            }

            if (!RunPhase(
                    InitializeRuntimeWorld,
                    "world bootstrap"))
            {
                yield break;
            }

            if (!RunPhase(
                    RunTurnSixCanonization,
                    "turn 6 settlement"))
            {
                yield break;
            }

            /*
             * Real Unity runtime boundary.
             *
             * Turn 7 must read only state that survived
             * publication across this frame boundary.
             */
            yield return null;

            if (!RunPhase(
                    RunTurnSevenPublishedStateProof,
                    "turn 7 settlement"))
            {
                yield break;
            }

            Debug.Log(
                "[CAMP 3 RUNTIME ACCEPTANCE PASS] " +
                "turn6=Canon1->Canon2, " +
                "turn7=published Canon2 observed, " +
                "equal-degree release remained Field, " +
                $"scoreEvents={world.KvltScoreLedger.Count}, " +
                $"lastPublishedTurn=" +
                $"{world.LastAppliedKvltSettlementTurn}"
            );

            enabled =
                false;
        }

        private bool RunPhase(
            Action phase,
            string phaseName)
        {
            try
            {
                phase();

                Debug.Log(
                    "[CAMP 3 RUNTIME] " +
                    $"PASS phase={phaseName}"
                );

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[CAMP 3 RUNTIME ACCEPTANCE FAIL] " +
                    $"phase={phaseName} | " +
                    exception.Message
                );

                Debug.LogException(
                    exception
                );

                enabled =
                    false;

                return false;
            }
        }

        private void InitializeRuntimeWorld()
        {
            world =
                new SeededWorldState(
                    new CollectiveRegistry()
                );

            Require(
                world.KvltCanon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind
                        .ScenarioSeed,
                    "RUNTIME_START_CANON"
                ),
                "Could not establish runtime " +
                "Scene_t Canon."
            );

            world.SocietyNorms.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            world.ApplySettledScenePressure(
                SettledScenePressure.Empty
            );

            world.ApplySettledNormativeCentre(
                normativeDeriver.Derive(
                    world.KvltCanon
                )
            );

            policy =
                new KvltSceneSettlementPolicy(
                    fieldDriftScale: 0f,
                    breakthroughDriftMultiplier:
                        0.60f,
                    nexusBoundary: 1f,
                    outerBoundary: 0f,

                    standingProjectionPolicy:
                        new
                            SceneStandingProjectionPolicy(
                                canonLegacyBaseWeight:
                                    1f,
                                canonLegacyBreakthroughDegreeWeight:
                                    1f,
                                rejectionScarBaseWeight:
                                    1f,
                                rejectionPeakPenetrationWeight:
                                    1f,
                                rejectionOutwardOvershootWeight:
                                    1f
                            ),

                    pressureRebuildPolicy:
                        new
                            ScenePressureRebuildPolicy(
                                neutralDirectionScale:
                                    0.25f,
                                counterCanonicalScale:
                                    0.50f,
                                maxAbsoluteEffectivePressure:
                                    0.75f
                            ),

                    normativePressureBlendPolicy:
                        new
                            NormativePressureBlendPolicy(
                                provisionalAffinityCeiling:
                                    0.50f
                            )
                );

            Require(
                world.LastAppliedKvltSettlementTurn ==
                -1,
                "Fresh runtime world already reports " +
                "a published KVLT settlement."
            );
        }

        private void RunTurnSixCanonization()
        {
            firstTape =
                PairDemo(
                    "RUNTIME_DEMO_A",
                    TagDegree.Dominant
                );

            firstRelease =
                new SceneRelease(
                    "Runtime Canon Candidate A",
                    firstTape.DemoTapeId,
                    "RUNTIME_OWNER_A",
                    "KVLT",
                    releasedTurn: 4,
                    sourceConveyance: 1f
                );

            Require(
                firstRelease.TryFetter(5),
                "Turn-6 candidate could not fetter."
            );

            Require(
                firstRelease
                    .TryEstablishFieldPosition(
                        0.50f,
                        5
                    ),
                "Turn-6 candidate could not establish " +
                "Field Position."
            );

            Activate(
                firstRelease,
                firstTape,
                TagDegree.Dominant,
                occurredTurn: 6
            );

            world.AddSceneRelease(
                firstRelease
            );

            firstSettlement =
                settlementService.Settle(
                    "KVLT",
                    6,
                    "TENURE_A",
                    world.KvltCanon,
                    EnvironmentFromWorld(6),
                    world.SceneReleases,
                    new[]
                    {
                        firstTape
                    },
                    world.KvltScoreLedger,
                    policy
                );

            Require(
                firstSettlement.HasCanonization,
                "Degree-2 release did not canonize " +
                "against Scene_t degree 1."
            );

            Require(
                firstRelease.LifecycleState ==
                SceneReleaseLifecycleState
                    .CanonRetained,
                "Successful runtime candidate did not " +
                "enter CanonRetained."
            );

            Require(
                firstRelease.IsActivationFrozen,
                "Canonized runtime release did not " +
                "freeze activation."
            );

            Require(
                firstRelease
                    .HasCanonGravityDecomposition,
                "Canonized runtime release has no " +
                "claim Gravity decomposition."
            );

            RequireCanonDegree(
                firstSettlement.NextCanon,
                TagDegree.Dominant,
                "Turn-6 NextCanon"
            );

            /*
             * The canonized artifact is no longer
             * ordinary Field, so it cannot survive into
             * next Dynamic Pressure.
             */
            Require(
                firstSettlement
                    .NextPressure
                    .SourceContributionCount == 0,
                "CanonRetained release leaked into " +
                "next Dynamic Scene Pressure."
            );

            firstFrozenGravity =
                firstRelease
                    .FrozenPostAssimilationGravity;

            Require(
                firstFrozenGravity > 0f,
                "Runtime canonization froze no Gravity."
            );

            RequireApproximately(
                world.KvltScoreLedger
                    .GetLifetimeTotal(
                        "RUNTIME_OWNER_A"
                    ),
                firstFrozenGravity,
                "Turn-6 retained Canon payout"
            );

            /*
             * Before publication the authoritative
             * world still contains Scene_t Canon1.
             */
            RequireCanonDegree(
                world.KvltCanon,
                TagDegree.Weak,
                "World Canon before turn-6 publication"
            );

            Require(
                world.TryApplyKvltSceneSettlement(
                    firstSettlement
                ),
                "World rejected valid turn-6 " +
                "settlement publication."
            );

            Require(
                world.LastAppliedKvltSettlementTurn ==
                6,
                "World did not record turn-6 " +
                "settlement publication."
            );

            RequireCanonDegree(
                world.KvltCanon,
                TagDegree.Dominant,
                "World Canon after turn-6 publication"
            );

            Require(
                world.KvltScenePressure.EntryCount == 0,
                "Published turn-6 Pressure should be " +
                "empty after sole Field release " +
                "canonized."
            );

            RequireApproximately(
                world.KvltNormativeCentre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                ),
                firstSettlement
                    .NextNormativeCentre
                    .Centre
                    .GetAffinity(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    ),
                "Published turn-6 Normative Centre"
            );
        }

        private void
            RunTurnSevenPublishedStateProof()
        {
            DemoTape secondTape =
                PairDemo(
                    "RUNTIME_DEMO_B",
                    TagDegree.Dominant
                );

            SceneRelease secondRelease =
                new(
                    "Runtime Equal-Degree Release B",
                    secondTape.DemoTapeId,
                    "RUNTIME_OWNER_B",
                    "KVLT",
                    releasedTurn: 6,
                    sourceConveyance: 1f
                );

            Require(
                secondRelease.TryFetter(7),
                "Turn-7 release could not fetter."
            );

            Require(
                secondRelease
                    .TryEstablishFieldPosition(
                        0.50f,
                        7
                    ),
                "Turn-7 release could not establish " +
                "Field Position."
            );

            Activate(
                secondRelease,
                secondTape,
                TagDegree.Dominant,
                occurredTurn: 7
            );

            world.AddSceneRelease(
                secondRelease
            );

            /*
             * Environment is constructed from the
             * WORLD-PUBLISHED result of turn 6.
             *
             * This is the runtime handoff being tested.
             */
            KvltSceneSettlementResult
                secondSettlement =
                    settlementService.Settle(
                        "KVLT",
                        7,
                        "TENURE_A",
                        world.KvltCanon,
                        EnvironmentFromWorld(7),
                        world.SceneReleases,
                        new[]
                        {
                            firstTape,
                            secondTape
                        },
                        world.KvltScoreLedger,
                        policy
                    );

            /*
             * The decisive assertion:
             *
             * World Canon is now D2.
             * New release is D2.
             *
             * There is therefore no realized
             * breakthrough and no Canon merge.
             *
             * If turn 7 had seen stale Canon1, this
             * release would incorrectly canonize.
             */
            Require(
                !secondSettlement.HasCanonization,
                "Turn 7 produced a false breakthrough; " +
                "published Canon2 was not respected."
            );

            Require(
                secondSettlement.CanonMerge == null,
                "Equal-degree turn-7 material created " +
                "an unexpected Canon merge."
            );

            Require(
                secondRelease.LifecycleState ==
                SceneReleaseLifecycleState.Field,
                "Equal-degree turn-7 release did not " +
                "remain ordinary Field."
            );

            Require(
                !secondRelease.IsCanonized,
                "Equal-degree turn-7 release was " +
                "incorrectly canonized."
            );

            RequireCanonDegree(
                secondSettlement.NextCanon,
                TagDegree.Dominant,
                "Turn-7 NextCanon"
            );

            /*
             * Existing retained release receives its
             * second tenure payout.
             */
            RequireApproximately(
                world.KvltScoreLedger
                    .GetLifetimeTotal(
                        "RUNTIME_OWNER_A"
                    ),
                firstFrozenGravity * 2f,
                "Turn-7 retained Canon payout"
            );

            Require(
                world.TryApplyKvltSceneSettlement(
                    secondSettlement
                ),
                "World rejected valid turn-7 " +
                "settlement publication."
            );

            Require(
                world.LastAppliedKvltSettlementTurn ==
                7,
                "World did not advance publication " +
                "through turn 7."
            );

            RequireCanonDegree(
                world.KvltCanon,
                TagDegree.Dominant,
                "World Canon after turn-7 publication"
            );

            /*
             * Release B remains Field, so its recorded
             * pair is now represented in rebuilt raw
             * Pressure.
             */
            RequireApproximately(
                world.KvltScenePressure
                    .GetRawPressure(
                        TagAxis.Symbolic,
                        TagPole.Negative
                    ),
                2f,
                "Turn-7 dominant raw Pressure"
            );

            RequireApproximately(
                world.KvltScenePressure
                    .GetRawPressure(
                        TagAxis.Symbolic,
                        TagPole.Positive
                    ),
                -1f,
                "Turn-7 submissive raw Pressure"
            );

            /*
             * Chronological publication barrier:
             * an old valid result may not roll the
             * world backwards.
             */
            Require(
                !world.TryApplyKvltSceneSettlement(
                    firstSettlement
                ),
                "World accepted stale turn-6 " +
                "settlement after turn 7."
            );

            Require(
                world.LastAppliedKvltSettlementTurn ==
                7,
                "Stale publication changed the " +
                "authoritative settled turn."
            );
        }

        private TrackEvaluationEnvironment
            EnvironmentFromWorld(
                int settledTurn)
        {
            return new TrackEvaluationEnvironment(
                settledTurn,
                world.KvltNormativeCentre,
                normativeDeriver.Derive(
                    world.KvltCanon
                ),
                world.SocietyNorms
            );
        }

        private static void Activate(
            SceneRelease release,
            DemoTape tape,
            TagDegree degree,
            int occurredTurn)
        {
            string trackId =
                "TRACK_" +
                tape.DemoTapeId;

            string ideaId =
                "IDEA_" +
                tape.DemoTapeId;

            string behaviorId =
                "RUNTIME_PERFORMANCE_" +
                tape.DemoTapeId;

            ActivationLegitimacyCandidate candidate =
                new(
                    ActivationAttemptRoute.Performance,
                    "HAPPENING_" +
                    tape.DemoTapeId,
                    "INTENT_" +
                    tape.DemoTapeId,
                    "ACTIVATOR_" +
                    tape.DemoTapeId,
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    trackId,
                    ideaId,
                    0,
                    behaviorId,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    degree,
                    degree,
                    degree
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT_" +
                    tape.DemoTapeId,
                    "KVLT",
                    behaviorId,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    degree,
                    occurredTurn,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "RUNTIME_ACCEPTANCE"
                );

            ActivationLegitimacyAssessment assessment =
                new(
                    candidate,
                    ActivationLegitimacyDisposition
                        .Covered,
                    precedent
                );

            Require(
                new
                    SceneReleaseActivationStateService()
                    .ApplyCovered(
                        release,
                        assessment,
                        occurredTurn
                    ),
                "Could not apply runtime activation | " +
                $"demo={tape.DemoTapeId}"
            );
        }

        private static DemoTape PairDemo(
            string id,
            TagDegree dominantDegree)
        {
            DemoTapeIdeaSnapshot idea =
                new(
                    "IDEA_" + id,
                    0,
                    "ASPECT_" + id,
                    IdeaPayloadType.TagPair,
                    "RUNTIME_AUTHOR",
                    TagContainerType.Transient,
                    1f,
                    new[]
                    {
                        new
                            DemoTapeTagOccurrenceSnapshot(
                                TagAxis.Symbolic,
                                TagPole.Negative,
                                dominantDegree,
                                DemoTapeTagOccurrenceRole
                                    .PairDominant
                            ),

                        new
                            DemoTapeTagOccurrenceSnapshot(
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
                    "TRACK_" + id,
                    "Runtime Track " + id,
                    1f,
                    1f,
                    new[]
                    {
                        idea
                    }
                );

            return new DemoTape(
                id,
                "Runtime Demo " + id,
                "RUNTIME_SET",
                "Runtime Set",
                1,
                1,
                1f,
                new[]
                {
                    track
                }
            );
        }

        private static void RequireCanonDegree(
            CanonState canon,
            TagDegree expected,
            string label)
        {
            Require(
                canon != null &&
                canon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out TagDegree actual
                ),
                label +
                " has no Symbolic Negative Canon."
            );

            canon.TryGetCanonicalDegree(
                TagAxis.Symbolic,
                TagPole.Negative,
                out TagDegree degree
            );

            Require(
                degree == expected,
                label +
                " degree mismatch | " +
                $"expected={expected} | " +
                $"actual={degree}"
            );
        }

        private static void RequireApproximately(
            float actual,
            float expected,
            string label)
        {
            if (Math.Abs(
                    actual -
                    expected) >
                0.0001f)
            {
                throw new InvalidOperationException(
                    label +
                    " mismatch | " +
                    $"expected={expected:F4} | " +
                    $"actual={actual:F4}"
                );
            }
        }

        private static void Require(
            bool condition,
            string failure)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    failure
                );
            }
        }
    }
}