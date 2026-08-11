using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Movement;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Canon.Tests.Editor
{
    public class
        SceneReleaseCanonGravityDecompositionTests
    {
        private readonly
            SceneReleaseCanonGravityDecompositionService
            service =
                new();

        [Test]
        public void
            ActivePairContributionsSumToFrozenReleaseGravity()
        {
            Fixture fixture =
                BuildFixture();

            SceneReleaseCanonGravityDecompositionState
                result =
                    service.Apply(
                        fixture.Release,
                        fixture.Freeze,
                        fixture.Merge
                    );

            Assert.That(
                result.ActivePairContributions.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                result.ActivePairContributionTotal,
                Is.EqualTo(
                    fixture.Release
                        .FrozenPostAssimilationGravity
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            ClaimFormulaUsesIdeaCountAndReleaseTrackCount()
        {
            Fixture fixture =
                BuildFixture();

            SceneReleaseCanonGravityDecompositionState
                result =
                    service.Apply(
                        fixture.Release,
                        fixture.Freeze,
                        fixture.Merge
                    );

            Assert.That(
                result.TryGetActivePairContribution(
                    "TRACK_FRONTIER",
                    "IDEA_FRONTIER",
                    0,
                    out
                        SceneReleaseActivePairGravityContribution
                        contribution
                ),
                Is.True
            );

            TrackLegitimacyEvaluation track =
                FindTrack(
                    fixture.Freeze.FinalLegitimacy,
                    "TRACK_FRONTIER"
                );

            IdeaTrveEvaluation idea =
                FindIdea(
                    track,
                    "IDEA_FRONTIER"
                );

            float expectedTrackContribution =
                Math.Abs(track.SignedResonance) *
                (
                    idea.ActiveTrveContribution /
                    track.Trve.IdeaEvaluations.Count
                ) /
                6f;

            float expectedReleaseContribution =
                expectedTrackContribution /
                fixture.Freeze
                    .FinalLegitimacy
                    .TrackEvaluations.Count;

            Assert.That(
                contribution.IdeaCountTrack,
                Is.EqualTo(2)
            );

            Assert.That(
                contribution.TrackCountRelease,
                Is.EqualTo(2)
            );

            Assert.That(
                contribution
                    .ClaimTrackGravityContribution,
                Is.EqualTo(
                    expectedTrackContribution
                ).Within(0.0001f)
            );

            Assert.That(
                contribution
                    .ClaimReleaseGravityContribution,
                Is.EqualTo(
                    expectedReleaseContribution
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            NonBreakthroughActivePairContributesGravityButCreatesNoFrontierPayout()
        {
            Fixture fixture =
                BuildFixture();

            SceneReleaseCanonGravityDecompositionState
                result =
                    service.Apply(
                        fixture.Release,
                        fixture.Freeze,
                        fixture.Merge
                    );

            Assert.That(
                result.TryGetActivePairContribution(
                    "TRACK_CANONICAL",
                    "IDEA_CANONICAL",
                    0,
                    out
                        SceneReleaseActivePairGravityContribution
                        active
                ),
                Is.True
            );

            Assert.That(
                active
                    .ClaimReleaseGravityContribution,
                Is.GreaterThanOrEqualTo(0f)
            );

            Assert.That(
                result.TryGetFrontierContribution(
                    "TRACK_CANONICAL",
                    "IDEA_CANONICAL",
                    0,
                    out _
                ),
                Is.False
            );
        }

        [Test]
        public void
            ActualFrontierClaimReceivesItsExactFrozenPairContribution()
        {
            Fixture fixture =
                BuildFixture();

            SceneReleaseCanonGravityDecompositionState
                result =
                    service.Apply(
                        fixture.Release,
                        fixture.Freeze,
                        fixture.Merge
                    );

            Assert.That(
                result.FrontierContributions.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.TryGetActivePairContribution(
                    "TRACK_FRONTIER",
                    "IDEA_FRONTIER",
                    0,
                    out
                        SceneReleaseActivePairGravityContribution
                        active
                ),
                Is.True
            );

            Assert.That(
                result.TryGetFrontierContribution(
                    "TRACK_FRONTIER",
                    "IDEA_FRONTIER",
                    0,
                    out
                        SceneReleaseCanonFrontierGravityContribution
                        frontier
                ),
                Is.True
            );

            Assert.That(
                frontier
                    .ClaimReleaseGravityContribution,
                Is.EqualTo(
                    active
                        .ClaimReleaseGravityContribution
                ).Within(0.0001f)
            );

            Assert.That(
                frontier.CanonicalActivatorPlayerId,
                Is.EqualTo(
                    "PLAYER_FRONTIER"
                )
            );

            Assert.That(
                frontier.FrontierClaim
                    .SourceOwnerEntityId,
                Is.EqualTo("OWNER")
            );

            Assert.That(
                frontier.CanonicalActivatorPlayerId,
                Is.Not.EqualTo(
                    frontier.FrontierClaim
                        .SourceOwnerEntityId
                )
            );
        }

        [Test]
        public void
            SolitaryIdeaDoesNotBecomeGravityClaim()
        {
            Fixture fixture =
                BuildFixture();

            SceneReleaseCanonGravityDecompositionState
                result =
                    service.Apply(
                        fixture.Release,
                        fixture.Freeze,
                        fixture.Merge
                    );

            Assert.That(
                result.TryGetActivePairContribution(
                    "TRACK_FRONTIER",
                    "IDEA_SOLITARY",
                    1,
                    out _
                ),
                Is.False
            );

            /*
             * The solitary Idea still belongs to the
             * Track and therefore remains part of the
             * IdeaCount denominator.
             */
            Assert.That(
                result.TryGetActivePairContribution(
                    "TRACK_FRONTIER",
                    "IDEA_FRONTIER",
                    0,
                    out
                        SceneReleaseActivePairGravityContribution
                        frontier
                ),
                Is.True
            );

            Assert.That(
                frontier.IdeaCountTrack,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void
            GravityDecompositionCanOnlyBeAttachedOnce()
        {
            Fixture fixture =
                BuildFixture();

            SceneReleaseCanonGravityDecompositionState
                first =
                    service.Apply(
                        fixture.Release,
                        fixture.Freeze,
                        fixture.Merge
                    );

            Assert.That(
                fixture.Release
                    .CanonGravityDecompositionState,
                Is.SameAs(first)
            );

            Assert.That(
                fixture.Release
                    .HasCanonGravityDecomposition,
                Is.True
            );

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    service.Apply(
                        fixture.Release,
                        fixture.Freeze,
                        fixture.Merge
                    )
            );
        }

        private static Fixture BuildFixture()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                new(
                    "Release",
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

            /*
             * Actual frontier:
             *
             * Symbolic2 active against old Canon
             * Symbolic1.
             */
            ApplyCovered(
                release,
                "TRACK_FRONTIER",
                "IDEA_FRONTIER",
                0,
                "PLAYER_FRONTIER",
                TagAxis.Symbolic,
                TagDegree.Dominant,
                TagDegree.Dominant
            );

            /*
             * Already-canonical active material:
             *
             * Physical1 active against old Canon
             * Physical1.
             *
             * It contributes to final G_release but
             * establishes no new frontier.
             */
            ApplyCovered(
                release,
                "TRACK_CANONICAL",
                "IDEA_CANONICAL",
                0,
                "PLAYER_OTHER",
                TagAxis.Physical,
                TagDegree.Weak,
                TagDegree.Weak
            );

            TrackEvaluationEnvironment environment =
                Environment();

            SceneReleaseLegitimacyEvaluation
                preMovementLegitimacy =
                    new
                        SceneReleaseLegitimacyEvaluator()
                        .Evaluate(
                            release,
                            demo,
                            environment
                        );

            CanonState previousCanon =
                PreviousCanon();

            SceneReleaseCanonBreakthroughEvaluation
                breakthrough =
                    new
                        SceneReleaseCanonBreakthroughEvaluator()
                        .Evaluate(
                            release,
                            demo,
                            preMovementLegitimacy,
                            previousCanon
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
                        previousCanon,
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

            Assert.That(
                merge.FrontierClaims.Count,
                Is.EqualTo(1)
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

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            return new Fixture(
                release,
                merge,
                freeze
            );
        }

        private static DemoTape Demo()
        {
            DemoTapeIdeaSnapshot frontier =
                Pair(
                    "IDEA_FRONTIER",
                    0,
                    TagAxis.Symbolic,
                    TagDegree.Dominant
                );

            DemoTapeIdeaSnapshot solitary =
                new(
                    "IDEA_SOLITARY",
                    1,
                    "ASPECT_SOLITARY",
                    IdeaPayloadType.SingleTag,
                    "AUTHOR",
                    TagContainerType.Transient,
                    1f,
                    new[]
                    {
                        new DemoTapeTagOccurrenceSnapshot(
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            TagDegree.Weak,
                            DemoTapeTagOccurrenceRole
                                .Solitary
                        )
                    }
                );

            DemoTapeTrackSnapshot frontierTrack =
                new(
                    "TRACK_FRONTIER",
                    "Frontier Track",
                    1f,
                    1f,
                    new[]
                    {
                        frontier,
                        solitary
                    }
                );

            DemoTapeTrackSnapshot canonicalTrack =
                new(
                    "TRACK_CANONICAL",
                    "Canonical Track",
                    1f,
                    1f,
                    new[]
                    {
                        Pair(
                            "IDEA_CANONICAL",
                            0,
                            TagAxis.Physical,
                            TagDegree.Weak
                        )
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
                    frontierTrack,
                    canonicalTrack
                }
            );
        }

        private static DemoTapeIdeaSnapshot Pair(
            string ideaId,
            int ideaIndex,
            TagAxis axis,
            TagDegree dominantDegree)
        {
            return new DemoTapeIdeaSnapshot(
                ideaId,
                ideaIndex,
                "ASPECT_" + ideaId,
                IdeaPayloadType.TagPair,
                "AUTHOR",
                TagContainerType.Transient,
                1f,
                new[]
                {
                    new DemoTapeTagOccurrenceSnapshot(
                        axis,
                        TagPole.Negative,
                        dominantDegree,
                        DemoTapeTagOccurrenceRole
                            .PairDominant
                    ),

                    new DemoTapeTagOccurrenceSnapshot(
                        axis,
                        TagPole.Positive,
                        TagDegree.Weak,
                        DemoTapeTagOccurrenceRole
                            .PairSubmissive
                    )
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
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Physical,
                            TagPole.Negative,
                            TagDegree.Weak,
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

            society.SetNormativeDegree(
                TagAxis.Physical,
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
                    "SYMBOLIC_SEED"
                ),
                Is.True
            );

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Physical,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind.ScenarioSeed,
                    "PHYSICAL_SEED"
                ),
                Is.True
            );

            return canon;
        }

        private static void ApplyCovered(
            SceneRelease release,
            string trackId,
            string ideaId,
            int ideaIndex,
            string actorId,
            TagAxis axis,
            TagDegree appliedDegree,
            TagDegree recordedDegree)
        {
            string behavior =
                "PRAXIS_" + ideaId;

            ActivationLegitimacyCandidate candidate =
                new(
                    ActivationAttemptRoute.Performance,
                    "HAPPENING_" + ideaId,
                    "INTENT_" + ideaId,
                    actorId,
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    trackId,
                    ideaId,
                    ideaIndex,
                    behavior,
                    axis,
                    TagPole.Negative,
                    appliedDegree,
                    appliedDegree,
                    recordedDegree
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT_" + ideaId,
                    "KVLT",
                    behavior,
                    axis,
                    TagPole.Negative,
                    appliedDegree,
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

        private static TrackLegitimacyEvaluation
            FindTrack(
                SceneReleaseLegitimacyEvaluation
                    release,
                string trackId)
        {
            foreach (
                TrackLegitimacyEvaluation track
                in release.TrackEvaluations)
            {
                if (track.SourceTrackId ==
                    trackId)
                {
                    return track;
                }
            }

            Assert.Fail(
                "Track not found | " +
                trackId
            );

            return null;
        }

        private static IdeaTrveEvaluation
            FindIdea(
                TrackLegitimacyEvaluation track,
                string ideaId)
        {
            foreach (
                IdeaTrveEvaluation idea
                in track.Trve.IdeaEvaluations)
            {
                if (idea.SourceIdeaId ==
                    ideaId)
                {
                    return idea;
                }
            }

            Assert.Fail(
                "Idea not found | " +
                ideaId
            );

            return null;
        }

        private sealed class Fixture
        {
            public SceneRelease Release { get; }

            public CanonSimultaneousMergeEvaluation
                Merge { get; }

            public SceneReleaseCanonFreezeApplication
                Freeze { get; }

            public Fixture(
                SceneRelease release,
                CanonSimultaneousMergeEvaluation merge,
                SceneReleaseCanonFreezeApplication freeze)
            {
                Release =
                    release;

                Merge =
                    merge;

                Freeze =
                    freeze;
            }
        }
    }
}