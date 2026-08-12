using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Movement;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Standing.Tests.Editor
{
    public class
        SceneStandingCorpusProjectorTests
    {
        private readonly
            SceneStandingCorpusProjector projector =
                new();

        [Test]
        public void
            MixedRememberedCorpus_ProjectsCorrectLifecycleKinds()
        {
            SceneRelease active =
                ActiveField(
                    "ACTIVE"
                );

            SceneRelease canon =
                RetainedCanon();

            SceneRelease rejected =
                Rejected(
                    "REJECTED"
                );

            SceneRelease failed =
                new(
                    "Failed",
                    "DEMO_FAILED",
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            Assert.That(
                failed.TryFailToFetter(6),
                Is.True
            );

            SceneRelease fringe =
                new(
                    "Fringe",
                    "DEMO_FRINGE",
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            SceneStandingEvaluation result =
                projector.Project(
                    "OWNER",
                    "KVLT",
                    8,
                    new[]
                    {
                        fringe,
                        rejected,
                        failed,
                        canon,
                        active
                    },
                    Policy()
                );

            Assert.That(
                result.Contributions.Count,
                Is.EqualTo(3)
            );

            SceneStandingContribution
                activeContribution =
                    Find(
                        result,
                        active.ReleaseId
                    );

            SceneStandingContribution
                canonContribution =
                    Find(
                        result,
                        canon.ReleaseId
                    );

            SceneStandingContribution
                rejectionContribution =
                    Find(
                        result,
                        rejected.ReleaseId
                    );

            Assert.That(
                activeContribution.Kind,
                Is.EqualTo(
                    SceneStandingContributionKind
                        .ActiveField
                )
            );

            Assert.That(
                canonContribution.Kind,
                Is.EqualTo(
                    SceneStandingContributionKind
                        .CanonLegacy
                )
            );

            Assert.That(
                rejectionContribution.Kind,
                Is.EqualTo(
                    SceneStandingContributionKind
                        .RejectionScar
                )
            );

            Assert.That(
                result.HasStanding,
                Is.True
            );
        }

        [Test]
        public void
            ActiveFieldUsesCurrentPositionAndLatestPositionBasisTurn()
        {
            SceneRelease release =
                ActiveField(
                    "ACTIVE"
                );

            SceneStandingEvaluation result =
                projector.Project(
                    "OWNER",
                    "KVLT",
                    8,
                    new[]
                    {
                        release
                    },
                    Policy()
                );

            SceneStandingContribution contribution =
                result.Contributions[0];

            Assert.That(
                contribution.Position,
                Is.EqualTo(0.40f)
                    .Within(0.0001f)
            );

            Assert.That(
                contribution.Weight,
                Is.EqualTo(1f)
            );

            Assert.That(
                contribution.BasisTurn,
                Is.EqualTo(6)
            );
        }

        [Test]
        public void
            CanonLegacyWeightUsesNewPrecedentDifferential()
        {
            SceneRelease release =
                RetainedCanon();

            SceneStandingEvaluation result =
                projector.Project(
                    "OWNER",
                    "KVLT",
                    6,
                    new[]
                    {
                        release
                    },
                    Policy()
                );

            SceneStandingContribution contribution =
                result.Contributions[0];

            /*
             * Old Canon = Symbolic1.
             * Release establishes Symbolic2.
             *
             * Realized breakthrough differential = 1.
             *
             * Policy:
             * base 1 + differential 1 × 2 = 3.
             *
             * We specifically do NOT use absolute
             * canonical degree 2 as the multiplier.
             */
            Assert.That(
                contribution.Weight,
                Is.EqualTo(3f)
                    .Within(0.0001f)
            );

            Assert.That(
                contribution.Position,
                Is.EqualTo(1.10f)
                    .Within(0.0001f)
            );

            Assert.That(
                contribution.BasisTurn,
                Is.EqualTo(6)
            );
        }

        [Test]
        public void
            CanonRetainedAndHistoricalCanonUseSameFrozenLegacyAnchor()
        {
            SceneRelease release =
                RetainedCanon();

            SceneStandingContribution retained =
                projector.Project(
                    "OWNER",
                    "KVLT",
                    6,
                    new[]
                    {
                        release
                    },
                    Policy()
                ).Contributions[0];

            Assert.That(
                new
                    SceneReleaseCanonTenureTransitionService()
                    .Apply(
                        new[]
                        {
                            release
                        },
                        "TENURE_A",
                        "TENURE_B",
                        7
                    ).Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                )
            );

            SceneStandingContribution historical =
                projector.Project(
                    "OWNER",
                    "KVLT",
                    8,
                    new[]
                    {
                        release
                    },
                    Policy()
                ).Contributions[0];

            Assert.That(
                historical.Kind,
                Is.EqualTo(
                    SceneStandingContributionKind
                        .CanonLegacy
                )
            );

            Assert.That(
                historical.Position,
                Is.EqualTo(
                    retained.Position
                ).Within(0.0001f)
            );

            Assert.That(
                historical.Weight,
                Is.EqualTo(
                    retained.Weight
                ).Within(0.0001f)
            );

            Assert.That(
                historical.BasisTurn,
                Is.EqualTo(
                    retained.BasisTurn
                )
            );
        }

        [Test]
        public void
            RejectionScarWeightUsesPenetrationAndOvershoot()
        {
            SceneRelease release =
                Rejected(
                    "REJECTED"
                );

            SceneStandingContribution contribution =
                projector.Project(
                    "OWNER",
                    "KVLT",
                    8,
                    new[]
                    {
                        release
                    },
                    Policy()
                ).Contributions[0];

            /*
             * outer boundary       = 0.0
             * peak inward position = 1.0
             * rejected position    = -0.3
             *
             * prior penetration = 1.0
             * overshoot         = 0.3
             *
             * policy:
             *
             * 1
             * + 1.0 × 2
             * + 0.3 × 3
             * = 3.9
             */
            Assert.That(
                contribution.Position,
                Is.EqualTo(-0.30f)
                    .Within(0.0001f)
            );

            Assert.That(
                contribution.Weight,
                Is.EqualTo(3.90f)
                    .Within(0.0001f)
            );

            Assert.That(
                contribution.BasisTurn,
                Is.EqualTo(7)
            );
        }

        [Test]
        public void
            FringeAndFailedToFetterDoNotCreateStanding()
        {
            SceneRelease fringe =
                new(
                    "Fringe",
                    "DEMO_FRINGE",
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            SceneRelease failed =
                new(
                    "Failed",
                    "DEMO_FAILED",
                    "OWNER",
                    "KVLT",
                    4,
                    1f
                );

            Assert.That(
                failed.TryFailToFetter(6),
                Is.True
            );

            SceneStandingEvaluation result =
                projector.Project(
                    "OWNER",
                    "KVLT",
                    8,
                    new[]
                    {
                        fringe,
                        failed
                    },
                    Policy()
                );

            Assert.That(
                result.HasStanding,
                Is.False
            );

            Assert.That(
                result.Standing,
                Is.Null
            );

            Assert.That(
                result.Contributions,
                Is.Empty
            );
        }

        [Test]
        public void
            ForeignOwnerAndForeignSceneAreIgnored()
        {
            SceneRelease own =
                ActiveField(
                    "OWN"
                );

            SceneRelease foreignOwner =
                ActiveField(
                    "FOREIGN_OWNER",
                    owner: "OTHER_OWNER"
                );

            SceneRelease foreignScene =
                ActiveField(
                    "FOREIGN_SCENE",
                    scene: "OTHER_SCENE"
                );

            SceneStandingEvaluation result =
                projector.Project(
                    "OWNER",
                    "KVLT",
                    8,
                    new[]
                    {
                        foreignScene,
                        own,
                        foreignOwner
                    },
                    Policy()
                );

            Assert.That(
                result.Contributions.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.Contributions[0]
                    .SourceSceneReleaseId,
                Is.EqualTo(
                    own.ReleaseId
                )
            );
        }

        [Test]
        public void
            DuplicateSceneReleaseIdentityIsRejected()
        {
            SceneRelease release =
                ActiveField(
                    "ACTIVE"
                );

            Assert.Throws<
                ArgumentException>(
                () =>
                    projector.Project(
                        "OWNER",
                        "KVLT",
                        8,
                        new[]
                        {
                            release,
                            release
                        },
                        Policy()
                    )
            );
        }

        private static
            SceneStandingProjectionPolicy
            Policy()
        {
            return new SceneStandingProjectionPolicy(
                canonLegacyBaseWeight: 1f,
                canonLegacyBreakthroughDegreeWeight: 2f,
                rejectionScarBaseWeight: 1f,
                rejectionPeakPenetrationWeight: 2f,
                rejectionOutwardOvershootWeight: 3f
            );
        }

        private static SceneRelease ActiveField(
            string suffix,
            string owner = "OWNER",
            string scene = "KVLT")
        {
            SceneRelease release =
                new(
                    "Active " + suffix,
                    "DEMO_" + suffix,
                    owner,
                    scene,
                    4,
                    1f
                );

            Assert.That(
                release.TryFetter(5),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    0.20f,
                    5
                ),
                Is.True
            );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.20f,
                    6,
                    out _
                ),
                Is.True
            );

            return release;
        }

        private static SceneRelease Rejected(
            string suffix)
        {
            SceneRelease release =
                new(
                    "Rejected " + suffix,
                    "DEMO_" + suffix,
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
                    0.50f,
                    5
                ),
                Is.True
            );

            /*
             * Peak inward = 1.0.
             */
            Assert.That(
                release.TryApplyFieldMovement(
                    0.50f,
                    6,
                    out _
                ),
                Is.True
            );

            /*
             * Final position = -0.3.
             */
            Assert.That(
                release.TryApplyFieldMovement(
                    -1.30f,
                    7,
                    out _
                ),
                Is.True
            );

            Assert.That(
                release.TryReject(
                    7,
                    outerBoundary: 0f
                ),
                Is.True
            );

            return release;
        }

        private static SceneRelease RetainedCanon()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                new(
                    "Canon",
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
                    0.50f,
                    5
                ),
                Is.True
            );

            ApplyCovered(
                release
            );

            TrackEvaluationEnvironment environment =
                Environment();

            SceneReleaseLegitimacyEvaluation
                preMovement =
                    new
                        SceneReleaseLegitimacyEvaluator()
                        .Evaluate(
                            release,
                            demo,
                            environment
                        );

            CanonState oldCanon =
                PreviousCanon();

            SceneReleaseCanonBreakthroughEvaluation
                breakthrough =
                    new
                        SceneReleaseCanonBreakthroughEvaluator()
                        .Evaluate(
                            release,
                            demo,
                            preMovement,
                            oldCanon
                        );

            Assert.That(
                breakthrough
                    .HasQualifyingBreakthrough,
                Is.True
            );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.60f,
                    6,
                    out _
                ),
                Is.True
            );

            SceneReleaseNexusBoundaryEvaluation nexus =
                new
                    SceneReleaseNexusBoundaryEvaluator()
                    .Evaluate(
                        release,
                        breakthrough,
                        1f
                    );

            Assert.That(
                nexus.IsCanonCandidate,
                Is.True
            );

            CanonSimultaneousMergeEvaluation merge =
                new
                    CanonSimultaneousMergeEvaluator()
                    .Evaluate(
                        oldCanon,
                        new[]
                        {
                            new
                                SceneReleaseCanonMergeCandidate(
                                    release,
                                    nexus
                                )
                        },
                        "TENURE_A"
                    );

            SceneReleaseCanonAssimilationEvaluation
                assimilation =
                    new
                        SceneReleaseCanonAssimilationEvaluator()
                        .Evaluate(
                            release,
                            demo,
                            nexus,
                            merge
                        );

            SceneReleaseCanonAssimilationApplication
                appliedAssimilation =
                    new
                        SceneReleaseCanonAssimilationService()
                        .Apply(
                            release,
                            assimilation
                        );

            SceneReleaseCanonFreezeApplication freeze =
                new SceneReleaseCanonFreezeService()
                    .Apply(
                        release,
                        demo,
                        environment,
                        appliedAssimilation
                    );

            new
                SceneReleaseCanonGravityDecompositionService()
                .Apply(
                    release,
                    freeze,
                    merge
                );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            return release;
        }

        private static DemoTape Demo()
        {
            DemoTapeIdeaSnapshot idea =
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
                        new
                            DemoTapeTagOccurrenceSnapshot(
                                TagAxis.Symbolic,
                                TagPole.Negative,
                                TagDegree.Dominant,
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
                    "TRACK",
                    "Track",
                    1f,
                    1f,
                    new[]
                    {
                        idea
                    }
                );

            return new DemoTape(
                "DEMO_CANON",
                "Canon Demo",
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

        private static
            TrackEvaluationEnvironment
            Environment()
        {
            NormativeCentre centre =
                new(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant,
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
                6,
                centre,
                centre,
                society
            );
        }

        private static CanonState PreviousCanon()
        {
            CanonState canon =
                new();

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind.ScenarioSeed,
                    "OLD_CANON"
                ),
                Is.True
            );

            return canon;
        }

        private static void ApplyCovered(
            SceneRelease release)
        {
            ActivationLegitimacyCandidate candidate =
                new(
                    ActivationAttemptRoute.Performance,
                    "HAPPENING",
                    "INTENT",
                    "PLAYER_ACTIVATOR",
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    "TRACK",
                    "IDEA",
                    0,
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    TagDegree.Dominant,
                    TagDegree.Dominant
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT",
                    "KVLT",
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
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

        private static SceneStandingContribution Find(
            SceneStandingEvaluation evaluation,
            string releaseId)
        {
            foreach (
                SceneStandingContribution contribution
                in evaluation.Contributions)
            {
                if (contribution
                        .SourceSceneReleaseId ==
                    releaseId)
                {
                    return contribution;
                }
            }

            Assert.Fail(
                "Standing contribution not found | " +
                releaseId
            );

            return null;
        }
    }
}