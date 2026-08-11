using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Canon.Tests.Editor
{
    public class
        SceneReleaseCanonAssimilationServiceTests
    {
        private readonly
            SceneReleaseCanonAssimilationService service =
                new();

        [Test]
        public void
            DormantPair_IsRaisedOnlyToExistingCanon()
        {
            SceneRelease release =
                CandidateRelease();

            SceneReleaseCanonAssimilationEvaluation
                evaluation =
                    Assimilation(
                        release,
                        existing:
                            TagDegree.Neutral,
                        recorded:
                            TagDegree.Transgressive,
                        canon:
                            TagDegree.Dominant
                    );

            var application =
                service.Apply(
                    release,
                    evaluation
                );

            Assert.That(
                release.TryGetPairActivationState(
                    "TRACK",
                    "IDEA",
                    0,
                    out
                        SceneReleasePairActivationState
                        state
                ),
                Is.True
            );

            Assert.That(
                state.CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                state.RecordedDominantDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );

            Assert.That(
                application.RaisedPairCount,
                Is.EqualTo(1)
            );

            Assert.That(
                release.CanonAssimilationHistory.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.CanonAssimilationHistory[0]
                    .PreviousActivationDegree,
                Is.EqualTo(
                    TagDegree.Neutral
                )
            );

            Assert.That(
                release.CanonAssimilationHistory[0]
                    .NewActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );
        }

        [Test]
        public void
            ExistingAboveCanonActivation_IsPreservedWithoutNewHistory()
        {
            SceneRelease release =
                CandidateRelease();

            ApplyCovered(
                release,
                TagDegree.Transgressive,
                TagDegree.Transgressive
            );

            SceneReleaseCanonAssimilationEvaluation
                evaluation =
                    Assimilation(
                        release,
                        existing:
                            TagDegree.Transgressive,
                        recorded:
                            TagDegree.Transgressive,
                        canon:
                            TagDegree.Dominant
                    );

            var application =
                service.Apply(
                    release,
                    evaluation
                );

            Assert.That(
                application.RaisedPairCount,
                Is.EqualTo(0)
            );

            Assert.That(
                release.CanonAssimilationHistory,
                Is.Empty
            );

            Assert.That(
                release.TryGetPairActivationState(
                    "TRACK",
                    "IDEA",
                    0,
                    out
                        SceneReleasePairActivationState
                        state
                ),
                Is.True
            );

            Assert.That(
                state.CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Transgressive
                )
            );
        }

        [Test]
        public void
            PendingUpperDegree_RemainsPendingWhileCanonFillsLowerDegree()
        {
            SceneRelease release =
                CandidateRelease();

            ActivationLegitimacyCandidate candidate =
                Candidate(
                    release,
                    TagDegree.Transgressive,
                    TagDegree.Transgressive
                );

            PendingActivationCandidate pendingCandidate =
                new(
                    candidate,
                    "CRISIS",
                    6
                );

            Assert.That(
                new SceneReleaseActivationStateService()
                    .StorePending(
                        release,
                        pendingCandidate,
                        out
                            SceneReleasePendingActivation
                            pending
                    ),
                Is.True
            );

            SceneReleaseCanonAssimilationEvaluation
                evaluation =
                    Assimilation(
                        release,
                        existing:
                            TagDegree.Neutral,
                        recorded:
                            TagDegree.Transgressive,
                        canon:
                            TagDegree.Dominant
                    );

            service.Apply(
                release,
                evaluation
            );

            Assert.That(
                release.TryGetPairActivationState(
                    "TRACK",
                    "IDEA",
                    0,
                    out
                        SceneReleasePairActivationState
                        state
                ),
                Is.True
            );

            Assert.That(
                state.CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                pending.IsRedeemed,
                Is.False
            );

            Assert.That(
                release.PendingActivations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.ActivationHistory.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.ActivationHistory[0].Kind,
                Is.EqualTo(
                    SceneReleaseActivationHistoryKind
                        .PendingStored
                )
            );

            Assert.That(
                release.CanonAssimilationHistory.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            StaleActivationPlan_IsRejectedBeforeAssimilationMutation()
        {
            SceneRelease release =
                CandidateRelease();

            SceneReleaseCanonAssimilationEvaluation
                evaluation =
                    Assimilation(
                        release,
                        existing:
                            TagDegree.Neutral,
                        recorded:
                            TagDegree.Transgressive,
                        canon:
                            TagDegree.Dominant
                    );

            /*
             * Mutate A after the frozen assimilation
             * evaluation.
             */
            ApplyCovered(
                release,
                TagDegree.Weak,
                TagDegree.Transgressive
            );

            Assert.Throws<
                System.InvalidOperationException>(
                () =>
                    service.Apply(
                        release,
                        evaluation
                    )
            );

            Assert.That(
                release.CanonAssimilationHistory,
                Is.Empty
            );

            Assert.That(
                release.TryGetPairActivationState(
                    "TRACK",
                    "IDEA",
                    0,
                    out
                        SceneReleasePairActivationState
                        state
                ),
                Is.True
            );

            Assert.That(
                state.CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );
        }

        private static
            SceneReleaseCanonAssimilationEvaluation
            Assimilation(
                SceneRelease release,
                TagDegree existing,
                TagDegree recorded,
                TagDegree canon)
        {
            CanonPrecedentRecord precedent =
                new(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    canon,
                    CanonProvenanceKind.ScenarioSeed,
                    "CANON"
                );

            SceneReleaseCanonAssimilationPairEvaluation
                pair =
                    new(
                        release.ReleaseId,
                        release.SourceDemoTapeId,
                        release.SourceOwnerEntityId,
                        release.HostedSceneNodeId,
                        "TRACK",
                        "IDEA",
                        0,
                        6,
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        recorded,
                        existing,
                        true,
                        canon,
                        new[]
                        {
                            precedent
                        }
                    );

            return new
                SceneReleaseCanonAssimilationEvaluation(
                    NexusCandidate(
                        release
                    ),
                    new[]
                    {
                        pair
                    }
                );
        }

        private static SceneRelease CandidateRelease()
        {
            SceneRelease release =
                new(
                    "Release",
                    "DEMO",
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

            Assert.That(
                release.TryApplyFieldMovement(
                    0.60f,
                    6,
                    out _
                ),
                Is.True
            );

            return release;
        }

        private static
            SceneReleaseNexusBoundaryEvaluation
            NexusCandidate(
                SceneRelease release)
        {
            SceneReleaseCanonBreakthroughClaim claim =
                new(
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    release.SourceOwnerEntityId,
                    release.HostedSceneNodeId,
                    "TRACK",
                    "IDEA",
                    0,
                    6,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    TagDegree.Dominant,
                    true,
                    TagDegree.Weak,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak,
                    true
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

            return new
                SceneReleaseNexusBoundaryEvaluation(
                    breakthrough,
                    1f,
                    release.FieldPositionState
                        .CurrentPosition,
                    SceneReleaseNexusBoundaryDisposition
                        .CanonCandidate
                );
        }

        private static void ApplyCovered(
            SceneRelease release,
            TagDegree appliedDegree,
            TagDegree recordedDegree)
        {
            ActivationLegitimacyCandidate candidate =
                Candidate(
                    release,
                    appliedDegree,
                    recordedDegree
                );

            var precedent =
                new SEMM91.GamePlay.Kvlt.Transgression
                    .AcceptedTransgressionRecord(
                        "AT_" + appliedDegree,
                        "KVLT",
                        "PUBLIC_PERFORMANCE",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        appliedDegree,
                        0,
                        SEMM91.GamePlay.Kvlt.Transgression
                            .AcceptedTransgressionSourceKind
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

        private static ActivationLegitimacyCandidate
            Candidate(
                SceneRelease release,
                TagDegree appliedDegree,
                TagDegree recordedDegree)
        {
            return new ActivationLegitimacyCandidate(
                ActivationAttemptRoute.Performance,
                "HAPPENING",
                "INTENT",
                "ACTOR",
                release.ReleaseId,
                release.SourceDemoTapeId,
                "TRACK",
                "IDEA",
                0,
                "PUBLIC_PERFORMANCE",
                TagAxis.Symbolic,
                TagPole.Negative,
                appliedDegree,
                appliedDegree,
                recordedDegree
            );
        }
    }
}