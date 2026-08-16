using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.Kvlt.Standing;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Score;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public class KvltSceneSettlementServiceTests
    {
        private readonly
            KvltSceneSettlementService service =
                new();

        [Test]
        public void
            SuccessfulBreakthroughRunsThroughCanonRetentionAndNextState()
        {
            DemoTape tape =
                PairDemo(
                    "DEMO",
                    TagDegree.Dominant
                );

            SceneRelease release =
                FieldRelease(
                    tape,
                    "OWNER",
                    startPosition: 0.50f
                );

            Activate(
                release,
                TagDegree.Dominant
            );

            CanonState startCanon =
                CanonAt(
                    TagDegree.Weak
                );

            ScoreLedger ledger =
                new();

            KvltSceneSettlementResult result =
                service.Settle(
                    "KVLT",
                    6,
                    "TENURE_A",
                    startCanon,
                    Environment(
                        6,
                        startCanon
                    ),
                    new[]
                    {
                        release
                    },
                    new[]
                    {
                        tape
                    },
                    ledger,
                    Policy()
                );

            Assert.That(
                result.HasCanonization,
                Is.True
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                release.IsActivationFrozen,
                Is.True
            );

            Assert.That(
                release
                    .HasCanonGravityDecomposition,
                Is.True
            );

            Assert.That(
                result.NextCanon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out TagDegree nextDegree
                ),
                Is.True
            );

            Assert.That(
                nextDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            /*
             * CanonRetained is absent from next
             * Dynamic Field Pressure.
             */
            Assert.That(
                result.NextPressure
                    .SourceContributionCount,
                Is.EqualTo(0)
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    "OWNER"
                ),
                Is.EqualTo(
                    release
                        .FrozenPostAssimilationGravity
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            SuppliedSceneStartCanonIsNotMutatedBySettlement()
        {
            DemoTape tape =
                PairDemo(
                    "DEMO",
                    TagDegree.Dominant
                );

            SceneRelease release =
                FieldRelease(
                    tape,
                    "OWNER",
                    0.50f
                );

            Activate(
                release,
                TagDegree.Dominant
            );

            CanonState suppliedCanon =
                CanonAt(
                    TagDegree.Weak
                );

            KvltSceneSettlementResult result =
                service.Settle(
                    "KVLT",
                    6,
                    "TENURE_A",
                    suppliedCanon,
                    Environment(
                        6,
                        suppliedCanon
                    ),
                    new[]
                    {
                        release
                    },
                    new[]
                    {
                        tape
                    },
                    new ScoreLedger(),
                    Policy()
                );

            Assert.That(
                suppliedCanon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out TagDegree suppliedDegree
                ),
                Is.True
            );

            Assert.That(
                suppliedDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            Assert.That(
                result.NextCanon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out TagDegree nextDegree
                ),
                Is.True
            );

            Assert.That(
                nextDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );
        }

        [Test]
        public void
            SimultaneousCandidatesBothMoveAgainstSameSceneStartCanon()
        {
            DemoTape strongerTape =
                PairDemo(
                    "STRONG",
                    TagDegree.Transgressive
                );

            DemoTape lowerTape =
                PairDemo(
                    "LOWER",
                    TagDegree.Dominant
                );

            SceneRelease stronger =
                FieldRelease(
                    strongerTape,
                    "OWNER_A",
                    0.20f
                );

            SceneRelease lower =
                FieldRelease(
                    lowerTape,
                    "OWNER_B",
                    0.50f
                );

            Activate(
                stronger,
                TagDegree.Transgressive
            );

            Activate(
                lower,
                TagDegree.Dominant
            );

            CanonState startCanon =
                CanonAt(
                    TagDegree.Weak
                );

            KvltSceneSettlementResult result =
                service.Settle(
                    "KVLT",
                    6,
                    "TENURE_A",
                    startCanon,
                    Environment(
                        6,
                        startCanon
                    ),
                    new[]
                    {
                        lower,
                        stronger
                    },
                    new[]
                    {
                        lowerTape,
                        strongerTape
                    },
                    new ScoreLedger(),
                    Policy()
                );

            Assert.That(
                result.CanonMerge,
                Is.Not.Null
            );

            Assert.That(
                result.CanonMerge
                    .SuccessfulCandidateCount,
                Is.EqualTo(2)
            );

            Assert.That(
                result.NextCanon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out TagDegree degree
                ),
                Is.True
            );

            Assert.That(
                degree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            /*
             * LOWER was still a real degree-2
             * breakthrough against Scene_t degree 1.
             *
             * The stronger simultaneous degree 3
             * result did not erase its same-pass
             * movement/candidacy.
             */
            Assert.That(
                lower.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                stronger.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );
        }

        [Test]
        public void
            RejectedReleaseBecomesStandingScarAndLeavesNextPressure()
        {
            DemoTape tape =
                SingleDemo(
                    "REJECTED_DEMO"
                );

            SceneRelease release =
                FieldRelease(
                    tape,
                    "OWNER",
                    startPosition: -0.25f
                );

            CanonState canon =
                new();

            KvltSceneSettlementResult result =
                service.Settle(
                    "KVLT",
                    6,
                    "TENURE_A",
                    canon,
                    Environment(
                        6,
                        canon
                    ),
                    new[]
                    {
                        release
                    },
                    new[]
                    {
                        tape
                    },
                    new ScoreLedger(),
                    Policy()
                );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .Rejected
                )
            );

            Assert.That(
                result.RejectionApplications.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.NextPressure
                    .SourceContributionCount,
                Is.EqualTo(0)
            );

            Assert.That(
                result.TryGetStanding(
                    "OWNER",
                    out
                        SceneStandingEvaluation
                        standing
                ),
                Is.True
            );

            Assert.That(
                standing.HasStanding,
                Is.True
            );

            Assert.That(
                standing.Contributions.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                standing.Contributions[0].Kind,
                Is.EqualTo(
                    SceneStandingContributionKind
                        .RejectionScar
                )
            );
        }

        [Test]
        public void
            SettlementPublishesNextPressureAndNormativeCentreOnlyAfterLifecycleResolution()
        {
            DemoTape fieldTape =
                SingleDemo(
                    "FIELD_DEMO"
                );

            DemoTape canonTape =
                PairDemo(
                    "CANON_DEMO",
                    TagDegree.Dominant
                );

            SceneRelease survivingField =
                FieldRelease(
                    fieldTape,
                    "FIELD_OWNER",
                    0.25f
                );

            SceneRelease canonCandidate =
                FieldRelease(
                    canonTape,
                    "CANON_OWNER",
                    0.50f
                );

            Activate(
                canonCandidate,
                TagDegree.Dominant
            );

            CanonState startCanon =
                CanonAt(
                    TagDegree.Weak
                );

            KvltSceneSettlementResult result =
                service.Settle(
                    "KVLT",
                    6,
                    "TENURE_A",
                    startCanon,
                    Environment(
                        6,
                        startCanon
                    ),
                    new[]
                    {
                        canonCandidate,
                        survivingField
                    },
                    new[]
                    {
                        canonTape,
                        fieldTape
                    },
                    new ScoreLedger(),
                    Policy()
                );

            Assert.That(
                canonCandidate.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                survivingField.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            /*
             * Only the surviving Field cassette is
             * represented in next Pressure.
             */
            Assert.That(
                result.NextPressure
                    .SourceContributionCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.NextPressure
                    .Pressure
                    .GetRawPressure(
                        TagAxis.Existential,
                        TagPole.Negative
                    ),
                Is.EqualTo(1f)
            );

            /*
             * Next Normative Centre was derived only
             * after NextCanon and next Pressure were
             * complete.
             */
            Assert.That(
                result.NextNormativeCentre
                    .Centre,
                Is.Not.Null
            );

            Assert.That(
                result.NextCanonNormativeCentre,
                Is.Not.Null
            );
        }

        private static KvltSceneSettlementPolicy
            Policy()
        {
            return new KvltSceneSettlementPolicy(
                fieldDriftScale: 0f,

                /*
                 * D2 over Canon1:
                 * B = 1 → +0.60.
                 *
                 * D3 over Canon1:
                 * B = 2 → +1.20.
                 */
                breakthroughDriftMultiplier: 0.60f,

                nexusBoundary: 1f,
                outerBoundary: 0f,

                standingProjectionPolicy:
                    new SceneStandingProjectionPolicy(
                        canonLegacyBaseWeight: 1f,
                        canonLegacyBreakthroughDegreeWeight:
                            1f,
                        rejectionScarBaseWeight: 1f,
                        rejectionPeakPenetrationWeight:
                            1f,
                        rejectionOutwardOvershootWeight:
                            1f
                    ),

                pressureRebuildPolicy:
                    new ScenePressureRebuildPolicy(
                        neutralDirectionScale: 0.25f,
                        counterCanonicalScale: 0.50f,
                        maxAbsoluteEffectivePressure:
                            0.75f
                    ),

                normativePressureBlendPolicy:
                    new NormativePressureBlendPolicy(
                        provisionalAffinityCeiling:
                            0.50f
                    )
            );
        }

        private static TrackEvaluationEnvironment
            Environment(
                int turn,
                CanonState canon)
        {
            CanonicalNormativeCentreDeriver deriver =
                new();

            NormativeCentre centre =
                deriver.Derive(
                    canon
                );

            SocietyNormativeProfile society =
                new();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            society.SetNormativeDegree(
                TagAxis.Existential,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            return new TrackEvaluationEnvironment(
                turn,
                centre,
                centre,
                society
            );
        }

        private static SceneRelease FieldRelease(
            DemoTape tape,
            string owner,
            float startPosition)
        {
            SceneRelease release =
                new(
                    "Release " + tape.DemoTapeId,
                    tape.DemoTapeId,
                    owner,
                    "KVLT",
                    4,
                    1f
                );

            Assert.That(
                release.TryFetter(5),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    startPosition,
                    5
                ),
                Is.True
            );

            return release;
        }

        private static void Activate(
            SceneRelease release,
            TagDegree degree)
        {
            ActivationLegitimacyCandidate candidate =
                new(
                    ActivationAttemptRoute.Performance,
                    "HAPPENING_" + release.ReleaseId,
                    "INTENT_" + release.ReleaseId,
                    "ACTIVATOR_" + release.ReleaseId,
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    "TRACK_" + release.SourceDemoTapeId,
                    "IDEA_" + release.SourceDemoTapeId,
                    0,
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    degree,
                    degree,
                    degree
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT_" + release.ReleaseId,
                    "KVLT",
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    degree,
                    6,
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
                        6
                    ),
                Is.True
            );
        }

        private static CanonState CanonAt(
            TagDegree degree)
        {
            CanonState canon =
                new();

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    degree,
                    CanonProvenanceKind.ScenarioSeed,
                    "START_CANON"
                ),
                Is.True
            );

            return canon;
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
                    "AUTHOR",
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

            return Demo(
                id,
                idea
            );
        }

        private static DemoTape SingleDemo(
            string id)
        {
            DemoTapeIdeaSnapshot idea =
                new(
                    "IDEA_" + id,
                    0,
                    "ASPECT_" + id,
                    IdeaPayloadType.SingleTag,
                    "AUTHOR",
                    TagContainerType.Transient,
                    1f,
                    new[]
                    {
                        new
                            DemoTapeTagOccurrenceSnapshot(
                                TagAxis.Existential,
                                TagPole.Negative,
                                TagDegree.Weak,
                                DemoTapeTagOccurrenceRole
                                    .Solitary
                            )
                    }
                );

            return Demo(
                id,
                idea
            );
        }

        private static DemoTape Demo(
            string id,
            DemoTapeIdeaSnapshot idea)
        {
            DemoTapeTrackSnapshot track =
                new(
                    "TRACK_" + id,
                    "Track " + id,
                    1f,
                    1f,
                    new[]
                    {
                        idea
                    }
                );

            return new DemoTape(
                id,
                "Demo " + id,
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
    }
}