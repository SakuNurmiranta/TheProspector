using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Movement;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Kvlt.Canon.Tests.Editor
{
    public class
        CanonSimultaneousMergeEvaluatorTests
    {
        private readonly
            CanonSimultaneousMergeEvaluator evaluator =
                new();

        [Test]
        public void
            HigherSimultaneousClaimEstablishesResultingFrontier()
        {
            CanonState oldCanon =
                Canon(
                    TagAxis.Symbolic,
                    TagDegree.Weak
                );

            SceneReleaseCanonMergeCandidate degreeTwo =
                Candidate(
                    "A",
                    ownerId: "OWNER_A",
                    actorId: "PLAYER_A",
                    TagAxis.Symbolic,
                    TagDegree.Dominant,
                    oldCanonDegree: TagDegree.Weak
                );

            SceneReleaseCanonMergeCandidate degreeThree =
                Candidate(
                    "B",
                    ownerId: "OWNER_B",
                    actorId: "PLAYER_B",
                    TagAxis.Symbolic,
                    TagDegree.Transgressive,
                    oldCanonDegree: TagDegree.Weak
                );

            CanonSimultaneousMergeEvaluation result =
                evaluator.Evaluate(
                    oldCanon,
                    new[]
                    {
                        degreeTwo,
                        degreeThree
                    },
                    "TENURE_A"
                );

            Assert.That(
                result.NewCanon.TryGetCanonicalDegree(
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

            Assert.That(
                result.QualifyingBreakthroughClaimCount,
                Is.EqualTo(2)
            );

            Assert.That(
                result.FrontierClaimCount,
                Is.EqualTo(1)
            );

            Assert.That(
                result.FrontierClaims[0]
                    .SceneReleaseId,
                Is.EqualTo(
                    degreeThree.Release.ReleaseId
                )
            );
        }

        [Test]
        public void
            SimultaneousMergeDoesNotMutatePreviousCanon()
        {
            CanonState oldCanon =
                Canon(
                    TagAxis.Symbolic,
                    TagDegree.Weak
                );

            CanonSimultaneousMergeEvaluation result =
                evaluator.Evaluate(
                    oldCanon,
                    new[]
                    {
                        Candidate(
                            "A",
                            "OWNER_A",
                            "PLAYER_A",
                            TagAxis.Symbolic,
                            TagDegree.Transgressive,
                            TagDegree.Weak
                        )
                    },
                    "TENURE_A"
                );

            Assert.That(
                oldCanon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out TagDegree oldDegree
                ),
                Is.True
            );

            Assert.That(
                oldDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            Assert.That(
                result.NewCanon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out TagDegree newDegree
                ),
                Is.True
            );

            Assert.That(
                newDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                result.NewCanon,
                Is.Not.SameAs(oldCanon)
            );
        }

        [Test]
        public void
            EqualHighestClaimsPreserveCoincidentFrontierProvenance()
        {
            CanonState oldCanon =
                Canon(
                    TagAxis.Symbolic,
                    TagDegree.Weak
                );

            var first =
                Candidate(
                    "A",
                    "OWNER_A",
                    "PLAYER_A",
                    TagAxis.Symbolic,
                    TagDegree.Transgressive,
                    TagDegree.Weak
                );

            var second =
                Candidate(
                    "B",
                    "OWNER_B",
                    "PLAYER_B",
                    TagAxis.Symbolic,
                    TagDegree.Transgressive,
                    TagDegree.Weak
                );

            CanonSimultaneousMergeEvaluation result =
                evaluator.Evaluate(
                    oldCanon,
                    new[]
                    {
                        first,
                        second
                    },
                    "TENURE_A"
                );

            Assert.That(
                result.FrontierClaimCount,
                Is.EqualTo(2)
            );

            int matchingCanonRecords =
                0;

            foreach (
                CanonPrecedentRecord record
                in result.NewCanon.Records)
            {
                if (record.Axis ==
                        TagAxis.Symbolic &&
                    record.Pole ==
                        TagPole.Negative &&
                    record.Degree ==
                        TagDegree.Transgressive)
                {
                    matchingCanonRecords++;
                }
            }

            Assert.That(
                matchingCanonRecords,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void
            DifferentTagPolaritiesMergeIndependently()
        {
            CanonState oldCanon =
                new();

            var symbolic =
                Candidate(
                    "A",
                    "OWNER_A",
                    "PLAYER_A",
                    TagAxis.Symbolic,
                    TagDegree.Dominant,
                    TagDegree.Neutral
                );

            var physical =
                Candidate(
                    "B",
                    "OWNER_B",
                    "PLAYER_B",
                    TagAxis.Physical,
                    TagDegree.Transgressive,
                    TagDegree.Neutral
                );

            CanonSimultaneousMergeEvaluation result =
                evaluator.Evaluate(
                    oldCanon,
                    new[]
                    {
                        symbolic,
                        physical
                    },
                    "TENURE_A"
                );

            Assert.That(
                result.NewCanon.TryGetCanonicalDegree(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out TagDegree symbolicDegree
                ),
                Is.True
            );

            Assert.That(
                symbolicDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                result.NewCanon.TryGetCanonicalDegree(
                    TagAxis.Physical,
                    TagPole.Negative,
                    out TagDegree physicalDegree
                ),
                Is.True
            );

            Assert.That(
                physicalDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                result.FrontierClaimCount,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void
            FrontierActivatorComesFromAcceptedActivationNotReleaseOwner()
        {
            CanonState oldCanon =
                Canon(
                    TagAxis.Symbolic,
                    TagDegree.Weak
                );

            var candidate =
                Candidate(
                    "A",
                    ownerId:
                        "RELEASE_OWNER",
                    actorId:
                        "OTHER_PLAYER_AGENT",
                    axis:
                        TagAxis.Symbolic,
                    activeDegree:
                        TagDegree.Dominant,
                    oldCanonDegree:
                        TagDegree.Weak
                );

            CanonSimultaneousMergeEvaluation result =
                evaluator.Evaluate(
                    oldCanon,
                    new[]
                    {
                        candidate
                    },
                    "TENURE_42"
                );

            SceneReleaseCanonFrontierClaim frontier =
                result.FrontierClaims[0];

            Assert.That(
                frontier.SourceOwnerEntityId,
                Is.EqualTo(
                    "RELEASE_OWNER"
                )
            );

            Assert.That(
                frontier.CanonicalActivatorPlayerId,
                Is.EqualTo(
                    "OTHER_PLAYER_AGENT"
                )
            );

            Assert.That(
                frontier.CanonicalActivatorPlayerId,
                Is.Not.EqualTo(
                    frontier.SourceOwnerEntityId
                )
            );

            Assert.That(
                frontier.KeeperTenureId,
                Is.EqualTo(
                    "TENURE_42"
                )
            );
        }

        [Test]
        public void
            BreakthroughEvaluatedAgainstDifferentCanonIsRejected()
        {
            CanonState suppliedOldCanon =
                Canon(
                    TagAxis.Symbolic,
                    TagDegree.Dominant
                );

            /*
             * This candidate says its D3 breakthrough
             * was evaluated against Canon D1:
             *
             * B = 2.
             *
             * The supplied frozen Canon is actually D2,
             * so the correct B would be 1.
             */
            var staleCandidate =
                Candidate(
                    "A",
                    "OWNER_A",
                    "PLAYER_A",
                    TagAxis.Symbolic,
                    TagDegree.Transgressive,
                    TagDegree.Weak
                );

            Assert.Throws<
                System.InvalidOperationException>(
                () =>
                    evaluator.Evaluate(
                        suppliedOldCanon,
                        new[]
                        {
                            staleCandidate
                        },
                        "TENURE_A"
                    )
            );
        }

        private static CanonState Canon(
            TagAxis axis,
            TagDegree degree)
        {
            CanonState canon =
                new();

            Assert.That(
                canon.TryRecordPrecedent(
                    axis,
                    TagPole.Negative,
                    degree,
                    CanonProvenanceKind.ScenarioSeed,
                    "SEED_" + axis
                ),
                Is.True
            );

            return canon;
        }

        private static
            SceneReleaseCanonMergeCandidate
            Candidate(
                string suffix,
                string ownerId,
                string actorId,
                TagAxis axis,
                TagDegree activeDegree,
                TagDegree oldCanonDegree)
        {
            SceneRelease release =
                new(
                    "Release " + suffix,
                    "DEMO_" + suffix,
                    ownerId,
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

            ApplyActivation(
                release,
                actorId,
                axis,
                activeDegree
            );

            Assert.That(
                release.TryApplyFieldMovement(
                    0.60f,
                    6,
                    out _
                ),
                Is.True
            );

            bool hasOldCanon =
                oldCanonDegree !=
                TagDegree.Neutral;

            SceneReleaseCanonBreakthroughClaim claim =
                new(
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    release.SourceOwnerEntityId,
                    release.HostedSceneNodeId,
                    "TRACK_" + suffix,
                    "IDEA_" + suffix,
                    0,
                    6,
                    axis,
                    TagPole.Negative,
                    activeDegree,
                    activeDegree,
                    hasOldCanon,
                    oldCanonDegree,
                    axis,
                    TagPole.Positive,
                    TagDegree.Weak,
                    true
                );

            Assert.That(
                claim.IsQualifyingBreakthrough,
                Is.True
            );

            SceneReleaseCanonBreakthroughEvaluation
                breakthrough =
                    new(
                        release.ReleaseId,
                        release.SourceDemoTapeId,
                        release.SourceOwnerEntityId,
                        release.HostedSceneNodeId,
                        6,
                        0.50f,
                        new[]
                        {
                            claim
                        }
                    );

            SceneReleaseNexusBoundaryEvaluation nexus =
                new(
                    breakthrough,
                    nexusBoundary: 1f,
                    fieldPosition:
                        release.FieldPositionState
                            .CurrentPosition,
                    SceneReleaseNexusBoundaryDisposition
                        .CanonCandidate
                );

            return new
                SceneReleaseCanonMergeCandidate(
                    release,
                    nexus
                );
        }

        private static void ApplyActivation(
            SceneRelease release,
            string actorId,
            TagAxis axis,
            TagDegree degree)
        {
            string suffix =
                release.DisplayName
                    .Replace("Release ", "");

            string behavior =
                "PRAXIS_" +
                suffix;

            ActivationLegitimacyCandidate candidate =
                new(
                    ActivationAttemptRoute.Performance,
                    "HAPPENING_" + suffix,
                    "INTENT_" + suffix,
                    actorId,
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    "TRACK_" + suffix,
                    "IDEA_" + suffix,
                    0,
                    behavior,
                    axis,
                    TagPole.Negative,
                    degree,
                    degree,
                    degree
                );

            AcceptedTransgressionRecord precedent =
                new(
                    "AT_" + suffix,
                    "KVLT",
                    behavior,
                    axis,
                    TagPole.Negative,
                    degree,
                    6,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "TEST_" + suffix
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
    }
}