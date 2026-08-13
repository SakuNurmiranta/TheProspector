using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Movement;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.Kvlt.Standing;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltPostHappeningCanonizationScreeningServiceTests
    {
        private const int WinterTurn =
            6;

        private readonly
            KvltPostHappeningCanonizationScreeningService
            screeningService =
                new();

        private readonly
            SceneReleaseActivationStateService
            activationService =
                new();

        private readonly
            SceneReleaseLegitimacyEvaluator
            legitimacyEvaluator =
                new();

        [Test]
        public void
            PostHappeningScreeningCanRunAfterSameTurnMovement()
        {
            Fixture fixture =
                CreateFixture();

            Assert.That(
                fixture.Release.TryApplyFieldMovement(
                    0.15f,
                    WinterTurn,
                    out _
                ),
                Is.True
            );

            Assert.That(
                fixture.Release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(0.55f)
                    .Within(0.0001f)
            );

            KvltPostHappeningCanonizationScreeningResult
                result =
                    Screen(
                        fixture
                    );

            Assert.That(
                result.BreakthroughsByRelease.Count,
                Is.EqualTo(1)
            );

            SceneReleaseCanonBreakthroughEvaluation
                breakthrough =
                    result.BreakthroughsByRelease[
                        fixture.Release.ReleaseId
                    ];

            Assert.That(
                breakthrough.HasQualifyingBreakthrough,
                Is.False
            );

            Assert.That(
                breakthrough.StartFieldPosition,
                Is.EqualTo(0.55f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            SameTurnActivationAfterMovementChangesCanonScreeningNotPosition()
        {
            Fixture fixture =
                CreateFixture();

            Assert.That(
                fixture.Release.TryApplyFieldMovement(
                    0.15f,
                    WinterTurn,
                    out _
                ),
                Is.True
            );

            float settledWinterPosition =
                fixture.Release
                    .FieldPositionState
                    .CurrentPosition;

            SceneReleaseLegitimacyEvaluation
                beforeActivation =
                    legitimacyEvaluator.Evaluate(
                        fixture.Release,
                        fixture.Demo,
                        fixture.Environment
                    );

            Assert.That(
                beforeActivation.HasTrve,
                Is.False
            );

            ApplyWinterActivation(
                fixture
            );

            /*
             * Happening activation is semantic state.
             * It must not retroactively mutate the
             * already-settled Winter movement.
             */
            Assert.That(
                fixture.Release
                    .FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(
                    settledWinterPosition
                ).Within(0.0001f)
            );

            Assert.That(
                fixture.Release
                    .FieldPositionState
                    .LastMovementTurn,
                Is.EqualTo(
                    WinterTurn
                )
            );

            KvltPostHappeningCanonizationScreeningResult
                result =
                    Screen(
                        fixture
                    );

            SceneReleaseLegitimacyEvaluation
                afterActivation =
                    result.LegitimacyByRelease[
                        fixture.Release.ReleaseId
                    ];

            SceneReleaseCanonBreakthroughEvaluation
                breakthrough =
                    result.BreakthroughsByRelease[
                        fixture.Release.ReleaseId
                    ];

            Assert.That(
                afterActivation.HasTrve,
                Is.True
            );

            Assert.That(
                breakthrough
                    .HasQualifyingBreakthrough,
                Is.True
            );

            Assert.That(
                breakthrough.QualifyingClaims.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                breakthrough.QualifyingClaims[0]
                    .ActiveDominantDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                breakthrough.QualifyingClaims[0]
                    .CanonicalDominantDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            Assert.That(
                breakthrough.QualifyingClaims[0]
                    .RealizedBreakthroughDifferential,
                Is.EqualTo(1)
            );

            Assert.That(
                breakthrough.StartFieldPosition,
                Is.EqualTo(
                    settledWinterPosition
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            DefaultBreakthroughEvaluationStillRejectsAfterSameTurnMovement()
        {
            Fixture fixture =
                CreateFixture();

            Assert.That(
                fixture.Release.TryApplyFieldMovement(
                    0.15f,
                    WinterTurn,
                    out _
                ),
                Is.True
            );

            SceneReleaseLegitimacyEvaluation
                legitimacy =
                    legitimacyEvaluator.Evaluate(
                        fixture.Release,
                        fixture.Demo,
                        fixture.Environment
                    );

            /*
             * Existing callers use the default
             * PreMovement contract and remain protected
             * from accidentally screening after
             * movement.
             */
            Assert.Throws<
                System.InvalidOperationException>(
                () =>
                    new
                        SceneReleaseCanonBreakthroughEvaluator()
                        .Evaluate(
                            fixture.Release,
                            fixture.Demo,
                            legitimacy,
                            fixture.Canon
                        )
            );
        }

        [Test]
        public void
            PostHappeningBreakthroughCanCanonizeFromPriorTurnNexusPosition()
        {
            Fixture fixture =
                CreateFixture();

            /*
             * The release reaches the Nexus during the
             * preceding turn and simply remains there.
             *
             * There is deliberately NO Winter movement.
             */
            Assert.That(
                fixture.Release.TryApplyFieldMovement(
                    0.60f,
                    WinterTurn - 1,
                    out _
                ),
                Is.True
            );

            Assert.That(
                fixture.Release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(1.0f)
                    .Within(0.0001f)
            );

            Assert.That(
                fixture.Release.FieldPositionState
                    .LastMovementTurn,
                Is.EqualTo(
                    WinterTurn - 1
                )
            );

            /*
             * A Winter Happening now establishes the
             * legitimate activation which creates the
             * realized breakthrough.
             *
             * This must not invent another movement.
             */
            ApplyWinterActivation(
                fixture
            );

            KvltPostHappeningCanonizationScreeningResult
                screening =
                    Screen(
                        fixture
                    );

            SceneReleaseCanonBreakthroughEvaluation
                breakthrough =
                    screening.BreakthroughsByRelease[
                        fixture.Release.ReleaseId
                    ];

            Assert.That(
                breakthrough.HasQualifyingBreakthrough,
                Is.True
            );

            Assert.That(
                breakthrough.StartFieldPosition,
                Is.EqualTo(1.0f)
                    .Within(0.0001f)
            );

            KvltCanonizationSettlementResult
                canonization =
                    new
                        KvltCanonizationSettlementService()
                        .Settle(
                            "KVLT",
                            WinterTurn,
                            "TENURE_TEST",
                            fixture.Canon,
                            fixture.Environment,
                            new[]
                            {
                                fixture.Release
                            },
                            new[]
                            {
                                fixture.Demo
                            },
                            screening
                                .BreakthroughsByRelease,
                            CreateSettlementPolicy(),
                            SceneReleaseCanonBreakthroughEvaluationPhase
                                .PostHappening
                        );

            Assert.That(
                canonization.HasCanonization,
                Is.True
            );

            Assert.That(
                canonization.FreezeApplications.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                fixture.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                fixture.Release.IsCanonized,
                Is.True
            );

            Assert.That(
                fixture.Release.CanonizedTurn,
                Is.EqualTo(
                    WinterTurn
                )
            );

            Assert.That(
                fixture.Release
                    .CanonizedUnderKeeperTenureId,
                Is.EqualTo(
                    "TENURE_TEST"
                )
            );

            Assert.That(
                fixture.Release
                    .HasCanonGravityDecomposition,
                Is.True
            );

            /*
             * Critical chronology proof:
             *
             * Canonization occurred on WinterTurn,
             * but the last movement remains the prior
             * turn. No artificial Winter movement was
             * created merely to satisfy Canonization.
             */
            Assert.That(
                fixture.Release.FieldPositionState
                    .LastMovementTurn,
                Is.EqualTo(
                    WinterTurn - 1
                )
            );

            Assert.That(
                fixture.Release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(1.0f)
                    .Within(0.0001f)
            );

            Assert.That(
                canonization.NextCanon
                    .TryGetCanonicalDegree(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        out TagDegree
                            newCanonicalDegree
                    ),
                Is.True
            );

            Assert.That(
                newCanonicalDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );
        }

        [Test]
        public void
            LegacyCanonizationOverloadStillRequiresSameTurnMovement()
        {
            Fixture fixture =
                CreateFixture();

            /*
             * Again, the only movement belongs to the
             * preceding turn.
             */
            Assert.That(
                fixture.Release.TryApplyFieldMovement(
                    0.60f,
                    WinterTurn - 1,
                    out _
                ),
                Is.True
            );

            Assert.That(
                fixture.Release.FieldPositionState
                    .CurrentPosition,
                Is.EqualTo(1.0f)
                    .Within(0.0001f)
            );

            ApplyWinterActivation(
                fixture
            );

            KvltPostHappeningCanonizationScreeningResult
                screening =
                    Screen(
                        fixture
                    );

            Assert.That(
                screening
                    .BreakthroughsByRelease[
                        fixture.Release.ReleaseId
                    ]
                    .HasQualifyingBreakthrough,
                Is.True
            );

            /*
             * Omitting the explicit phase uses the
             * historical PreMovement contract.
             *
             * It must therefore continue demanding a
             * same-turn movement transition and reject
             * this post-Happening state.
             */
            Assert.Throws<
                System.InvalidOperationException>(
                () =>
                    new
                        KvltCanonizationSettlementService()
                        .Settle(
                            "KVLT",
                            WinterTurn,
                            "TENURE_TEST",
                            fixture.Canon,
                            fixture.Environment,
                            new[]
                            {
                                fixture.Release
                            },
                            new[]
                            {
                                fixture.Demo
                            },
                            screening
                                .BreakthroughsByRelease,
                            CreateSettlementPolicy()
                        )
            );

            /*
             * Failed legacy interpretation must not
             * partially canonize or freeze the release.
             */
            Assert.That(
                fixture.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                fixture.Release.IsCanonized,
                Is.False
            );

            Assert.That(
                fixture.Release
                    .HasCanonGravityDecomposition,
                Is.False
            );

            Assert.That(
                fixture.Release.FieldPositionState
                    .LastMovementTurn,
                Is.EqualTo(
                    WinterTurn - 1
                )
            );
        }

        private
            KvltPostHappeningCanonizationScreeningResult
            Screen(
                Fixture fixture)
        {
            return screeningService.Screen(
                "KVLT",
                WinterTurn,
                fixture.Canon,
                fixture.Environment,
                new[]
                {
                    fixture.Release
                },
                new[]
                {
                    fixture.Demo
                }
            );
        }

        private void ApplyWinterActivation(
            Fixture fixture)
        {
            ActivationLegitimacyCandidate candidate =
                new(
                    ActivationAttemptRoute.Hail,
                    "HAPPENING_WINTER",
                    "INTENT_WINTER",
                    "ACTOR",
                    fixture.Release.ReleaseId,
                    fixture.Demo.DemoTapeId,
                    "TRACK_A",
                    "IDEA_A",
                    0,
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    TagDegree.Dominant,
                    TagDegree.Transgressive,
                    "BEHAVIOR_WINTER",
                    "HAIL_WINTER",
                    "ASPECT_SATAN"
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT_WINTER",
                    "KVLT",
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    WinterTurn,
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
                activationService.ApplyCovered(
                    fixture.Release,
                    assessment,
                    WinterTurn
                ),
                Is.True
            );
        }

        private static
            KvltSceneSettlementPolicy
            CreateSettlementPolicy()
        {
            /*
             * Only NexusBoundary participates in the
             * Canonization service under test.
             *
             * The remaining policy objects are valid
             * minimal calibration objects required by
             * the established aggregate policy type.
             */
            return new KvltSceneSettlementPolicy(
                fieldDriftScale:
                    1.0f,
                breakthroughDriftMultiplier:
                    1.0f,
                nexusBoundary:
                    1.0f,
                outerBoundary:
                    0.0f,
                standingProjectionPolicy:
                    new SceneStandingProjectionPolicy(
                        canonLegacyBaseWeight:
                            1.0f,
                        canonLegacyBreakthroughDegreeWeight:
                            0.0f,
                        rejectionScarBaseWeight:
                            1.0f,
                        rejectionPeakPenetrationWeight:
                            0.0f,
                        rejectionOutwardOvershootWeight:
                            0.0f
                    ),
                pressureRebuildPolicy:
                    new ScenePressureRebuildPolicy(
                        neutralDirectionScale:
                            1.0f,
                        counterCanonicalScale:
                            1.0f,
                        maxAbsoluteEffectivePressure:
                            0.5f
                    ),
                normativePressureBlendPolicy:
                    new NormativePressureBlendPolicy(
                        provisionalAffinityCeiling:
                            0.5f
                    )
            );
        }

        private static Fixture CreateFixture()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                new(
                    "Release A",
                    demo.DemoTapeId,
                    "OWNER",
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
                    0.40f,
                    5
                ),
                Is.True
            );

            CanonState canon =
                new();

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind
                        .ScenarioSeed,
                    "CANON_SEED"
                ),
                Is.True
            );

            SocietyNormativeProfile society =
                new();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            TrackEvaluationEnvironment
                environment =
                    new(
                        WinterTurn,
                        NormativeCentre.Neutral,
                        NormativeCentre.Neutral,
                        society
                    );

            return new Fixture(
                release,
                demo,
                canon,
                environment
            );
        }

        private static DemoTape Demo()
        {
            DemoTapeIdeaSnapshot pair =
                new(
                    "IDEA_A",
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
                    "TRACK_A",
                    "Track A",
                    1f,
                    1f,
                    new[]
                    {
                        pair
                    }
                );

            return new DemoTape(
                "DEMO_A",
                "Demo A",
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

        private sealed class Fixture
        {
            public SceneRelease Release { get; }

            public DemoTape Demo { get; }

            public CanonState Canon { get; }

            public TrackEvaluationEnvironment
                Environment { get; }

            public Fixture(
                SceneRelease release,
                DemoTape demo,
                CanonState canon,
                TrackEvaluationEnvironment environment)
            {
                Release =
                    release;

                Demo =
                    demo;

                Canon =
                    canon;

                Environment =
                    environment;
            }
        }
    }
}