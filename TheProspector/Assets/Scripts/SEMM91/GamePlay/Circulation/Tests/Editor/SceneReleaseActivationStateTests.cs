using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Circulation.Tests.Editor
{
    public class SceneReleaseActivationStateTests
    {
        private readonly
            SceneReleaseActivationStateService service =
                new();

        [Test]
        public void CoveredActivation_CreatesAuthoritativePairState()
        {
            SceneRelease release =
                Release();

            ActivationLegitimacyAssessment assessment =
                Covered(
                    Candidate(
                        TagDegree.Dominant,
                        TagDegree.Transgressive,
                        release.ReleaseId
                    ),
                    Accepted(
                        "AT",
                        TagDegree.Transgressive,
                        0
                    )
                );

            Assert.That(
                service.ApplyCovered(
                    release,
                    assessment,
                    8
                ),
                Is.True
            );

            Assert.That(
                release.TryGetPairActivationState(
                    "TRACK",
                    "IDEA",
                    0,
                    out SceneReleasePairActivationState
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
                release.ActivationHistory.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.ActivationHistory[0].Kind,
                Is.EqualTo(
                    SceneReleaseActivationHistoryKind
                        .CoveredApplied
                )
            );
        }

        [Test]
        public void Activation_IsNonAdditiveAndNeverRegresses()
        {
            SceneRelease release =
                Release();

            AcceptedTransgressionRecord precedent =
                Accepted(
                    "AT",
                    TagDegree.Transgressive,
                    0
                );

            Assert.That(
                service.ApplyCovered(
                    release,
                    Covered(
                        Candidate(
                            TagDegree.Weak,
                            TagDegree.Transgressive,
                            release.ReleaseId
                        ),
                        precedent
                    ),
                    8
                ),
                Is.True
            );

            Assert.That(
                service.ApplyCovered(
                    release,
                    Covered(
                        Candidate(
                            TagDegree.Dominant,
                            TagDegree.Transgressive,
                            release.ReleaseId
                        ),
                        precedent
                    ),
                    9
                ),
                Is.True
            );

            Assert.That(
                service.ApplyCovered(
                    release,
                    Covered(
                        Candidate(
                            TagDegree.Weak,
                            TagDegree.Transgressive,
                            release.ReleaseId
                        ),
                        precedent
                    ),
                    10
                ),
                Is.True
            );

            release.TryGetPairActivationState(
                "TRACK",
                "IDEA",
                0,
                out SceneReleasePairActivationState state
            );

            Assert.That(
                state.CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                release.ActivationHistory.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                release.ActivationHistory[2]
                    .RaisedActivation,
                Is.False
            );

            Assert.That(
                release.ActivationHistory[2]
                    .PreviousActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            Assert.That(
                release.ActivationHistory[2]
                    .NewActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );
        }

        [Test]
        public void ActivationCannotExceedRecordedDominantDegree()
        {
            SceneRelease release =
                Release();

            ActivationLegitimacyCandidate invalid =
                Candidate(
                    TagDegree.Transgressive,
                    TagDegree.Dominant,
                    release.ReleaseId
                );

            Assert.That(
                service.ApplyCovered(
                    release,
                    Covered(
                        invalid,
                        Accepted(
                            "AT",
                            TagDegree.Transgressive,
                            0
                        )
                    ),
                    8
                ),
                Is.False
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );

            Assert.That(
                release.ActivationHistory,
                Is.Empty
            );
        }

        [Test]
        public void CrisisLegitimizedActivation_PreservesCrisisAndPrecedentProvenance()
        {
            SceneRelease release =
                Release();

            ActivationLegitimacyCandidate candidate =
                Candidate(
                    TagDegree.Dominant,
                    TagDegree.Dominant,
                    release.ReleaseId
                );

            AcceptedTransgressionRecord precedent =
                Accepted(
                    "AT_CRISIS",
                    TagDegree.Transgressive,
                    8,
                    AcceptedTransgressionSourceKind
                        .AllegianceCrisis,
                    "CRISIS"
                );

            LegitimizedActivationCandidate legitimate =
                new LegitimizedActivationCandidate(
                    candidate,
                    "CRISIS",
                    precedent,
                    8
                );

            Assert.That(
                service.ApplyCrisisLegitimized(
                    release,
                    legitimate
                ),
                Is.True
            );

            SceneReleaseActivationHistoryRecord history =
                release.ActivationHistory[0];

            Assert.That(
                history.Kind,
                Is.EqualTo(
                    SceneReleaseActivationHistoryKind
                        .CrisisAcceptedApplied
                )
            );

            Assert.That(
                history.AllegianceCrisisQuestionId,
                Is.EqualTo("CRISIS")
            );

            Assert.That(
                history.CoveringPrecedent,
                Is.SameAs(precedent)
            );
        }

        [Test]
        public void PendingStorage_DoesNotActivatePair()
        {
            SceneRelease release =
                Release();

            PendingActivationCandidate candidate =
                Pending(
                    Candidate(
                        TagDegree.Dominant,
                        TagDegree.Dominant,
                        release.ReleaseId
                    ),
                    "CRISIS",
                    8
                );

            Assert.That(
                service.StorePending(
                    release,
                    candidate,
                    out SceneReleasePendingActivation
                        pending
                ),
                Is.True
            );

            Assert.That(
                pending.IsRedeemed,
                Is.False
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
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
                release.ActivationHistory[0]
                    .PreviousActivationDegree,
                Is.EqualTo(TagDegree.Neutral)
            );

            Assert.That(
                release.ActivationHistory[0]
                    .NewActivationDegree,
                Is.EqualTo(TagDegree.Neutral)
            );
        }

        [Test]
        public void SamePendingCandidate_CannotBeStoredTwice()
        {
            SceneRelease release =
                Release();

            PendingActivationCandidate candidate =
                Pending(
                    Candidate(
                        TagDegree.Dominant,
                        TagDegree.Dominant,
                        release.ReleaseId
                    ),
                    "CRISIS",
                    8
                );

            Assert.That(
                service.StorePending(
                    release,
                    candidate,
                    out _
                ),
                Is.True
            );

            Assert.That(
                service.StorePending(
                    release,
                    candidate,
                    out _
                ),
                Is.False
            );

            Assert.That(
                release.PendingActivations.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void LaterAcceptedPraxis_RedeemsPendingAndAppliesActivationNow()
        {
            SceneRelease release =
                Release();

            Assert.That(
                service.StorePending(
                    release,
                    Pending(
                        Candidate(
                            TagDegree.Dominant,
                            TagDegree.Transgressive,
                            release.ReleaseId
                        ),
                        "CRISIS",
                        8
                    ),
                    out SceneReleasePendingActivation
                        pending
                ),
                Is.True
            );

            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            AcceptedTransgressionRecord precedent =
                Accepted(
                    "AT_LATER",
                    TagDegree.Transgressive,
                    12
                );

            Assert.That(
                state.TryAccept(precedent),
                Is.True
            );

            Assert.That(
                service.RedeemEligiblePending(
                    release,
                    state,
                    12
                ),
                Is.EqualTo(1)
            );

            Assert.That(
                pending.IsRedeemed,
                Is.True
            );

            Assert.That(
                pending.RedeemedTurn,
                Is.EqualTo(12)
            );

            Assert.That(
                pending
                    .RedeemingAcceptedTransgressionId,
                Is.EqualTo("AT_LATER")
            );

            release.TryGetPairActivationState(
                "TRACK",
                "IDEA",
                0,
                out SceneReleasePairActivationState
                    activation
            );

            Assert.That(
                activation.CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );
        }

        [Test]
        public void NonCoveringPrecedent_LeavesPendingDormant()
        {
            SceneRelease release =
                Release();

            Assert.That(
                service.StorePending(
                    release,
                    Pending(
                        Candidate(
                            TagDegree.Dominant,
                            TagDegree.Transgressive,
                            release.ReleaseId
                        ),
                        "CRISIS",
                        8
                    ),
                    out SceneReleasePendingActivation
                        pending
                ),
                Is.True
            );

            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            Assert.That(
                state.TryAccept(
                    Accepted(
                        "AT_TOO_LOW",
                        TagDegree.Dominant,
                        12
                    )
                ),
                Is.True
            );

            Assert.That(
                service.RedeemEligiblePending(
                    release,
                    state,
                    12
                ),
                Is.EqualTo(0)
            );

            Assert.That(
                pending.IsRedeemed,
                Is.False
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );
        }

        [Test]
        public void RedemptionHistoryUsesRedemptionTurnNotOriginalPendingTurn()
        {
            SceneRelease release =
                Release();

            Assert.That(
                service.StorePending(
                    release,
                    Pending(
                        Candidate(
                            TagDegree.Weak,
                            TagDegree.Dominant,
                            release.ReleaseId
                        ),
                        "CRISIS",
                        8
                    ),
                    out _
                ),
                Is.True
            );

            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            Assert.That(
                state.TryAccept(
                    Accepted(
                        "AT",
                        TagDegree.Transgressive,
                        15
                    )
                ),
                Is.True
            );

            Assert.That(
                service.RedeemEligiblePending(
                    release,
                    state,
                    15
                ),
                Is.EqualTo(1)
            );

            Assert.That(
                release.ActivationHistory.Count,
                Is.EqualTo(2)
            );

            SceneReleaseActivationHistoryRecord
                redemption =
                    release.ActivationHistory[1];

            Assert.That(
                redemption.Kind,
                Is.EqualTo(
                    SceneReleaseActivationHistoryKind
                        .PendingRedeemed
                )
            );

            Assert.That(
                redemption.OccurredTurn,
                Is.EqualTo(15)
            );

            Assert.That(
                redemption.OccurredTurn,
                Is.Not.EqualTo(8)
            );
        }

        [Test]
        public void SeveralPendingPairs_CanRedeemFromOneLaterPraxisPrecedent()
        {
            SceneRelease release =
                Release();

            Assert.That(
                service.StorePending(
                    release,
                    Pending(
                        Candidate(
                            "TRACK_A",
                            "IDEA_A",
                            0,
                            TagDegree.Weak,
                            TagDegree.Dominant,
                            release.ReleaseId
                        ),
                        "CRISIS",
                        8
                    ),
                    out _
                ),
                Is.True
            );

            Assert.That(
                service.StorePending(
                    release,
                    Pending(
                        Candidate(
                            "TRACK_B",
                            "IDEA_B",
                            1,
                            TagDegree.Dominant,
                            TagDegree.Transgressive,
                            release.ReleaseId
                        ),
                        "CRISIS",
                        8
                    ),
                    out _
                ),
                Is.True
            );

            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            Assert.That(
                state.TryAccept(
                    Accepted(
                        "AT",
                        TagDegree.Transgressive,
                        12
                    )
                ),
                Is.True
            );

            Assert.That(
                service.RedeemEligiblePending(
                    release,
                    state,
                    12
                ),
                Is.EqualTo(2)
            );

            Assert.That(
                release.PairActivationStates.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                release.PendingActivations[0]
                    .IsRedeemed,
                Is.True
            );

            Assert.That(
                release.PendingActivations[1]
                    .IsRedeemed,
                Is.True
            );
        }

        [Test]
        public void PendingRedemption_IsNonAdditiveAgainstExistingHigherActivation()
        {
            SceneRelease release =
                Release();

            AcceptedTransgressionRecord early =
                Accepted(
                    "AT_EARLY",
                    TagDegree.Transgressive,
                    0
                );

            Assert.That(
                service.ApplyCovered(
                    release,
                    Covered(
                        Candidate(
                            TagDegree.Dominant,
                            TagDegree.Transgressive,
                            release.ReleaseId
                        ),
                        early
                    ),
                    5
                ),
                Is.True
            );

            Assert.That(
                service.StorePending(
                    release,
                    Pending(
                        Candidate(
                            TagDegree.Weak,
                            TagDegree.Transgressive,
                            release.ReleaseId
                        ),
                        "CRISIS",
                        8
                    ),
                    out _
                ),
                Is.True
            );

            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            AcceptedTransgressionRecord later =
                Accepted(
                    "AT_LATER",
                    TagDegree.Transgressive,
                    12
                );

            Assert.That(
                state.TryAccept(later),
                Is.True
            );

            Assert.That(
                service.RedeemEligiblePending(
                    release,
                    state,
                    12
                ),
                Is.EqualTo(1)
            );

            release.TryGetPairActivationState(
                "TRACK",
                "IDEA",
                0,
                out SceneReleasePairActivationState
                    activation
            );

            Assert.That(
                activation.CurrentActivationDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );

            SceneReleaseActivationHistoryRecord
                redemption =
                    release.ActivationHistory[
                        release.ActivationHistory.Count - 1
                    ];

            Assert.That(
                redemption.RaisedActivation,
                Is.False
            );
        }

        [Test]
        public void CandidateForDifferentReleaseCannotMutateState()
        {
            SceneRelease release =
                Release();

            ActivationLegitimacyCandidate candidate =
                Candidate(
                    TagDegree.Dominant,
                    TagDegree.Dominant,
                    sceneReleaseId: "OTHER_RELEASE"
                );

            Assert.That(
                service.ApplyCovered(
                    release,
                    Covered(
                        candidate,
                        Accepted(
                            "AT",
                            TagDegree.Transgressive,
                            0
                        )
                    ),
                    8
                ),
                Is.False
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );
        }

        private static SceneRelease Release()
        {
            return new SceneRelease(
                "RELEASE",
                "DEMO",
                "OWNER",
                "KVLT_SCENE",
                4,
                1f
            );
        }

        private static
            ActivationLegitimacyAssessment
            Covered(
                ActivationLegitimacyCandidate candidate,
                AcceptedTransgressionRecord precedent)
        {
            return new ActivationLegitimacyAssessment(
                candidate,
                ActivationLegitimacyDisposition.Covered,
                precedent
            );
        }

        private static PendingActivationCandidate Pending(
            ActivationLegitimacyCandidate candidate,
            string crisisId,
            int turn)
        {
            return new PendingActivationCandidate(
                candidate,
                crisisId,
                turn
            );
        }

        private static
            AcceptedTransgressionRecord
            Accepted(
                string id,
                TagDegree degree,
                int turn,
                AcceptedTransgressionSourceKind sourceKind =
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                string sourceId = "SCENARIO")
        {
            return new AcceptedTransgressionRecord(
                id,
                "KVLT",
                "CHURCH_ARSON",
                TagAxis.Symbolic,
                TagPole.Negative,
                degree,
                turn,
                sourceKind,
                sourceId
            );
        }

        private static
            ActivationLegitimacyCandidate
            Candidate(
                TagDegree appliedDegree,
                TagDegree recordedDominantDegree,
                string sceneReleaseId = "RELEASE")
        {
            return Candidate(
                "TRACK",
                "IDEA",
                0,
                appliedDegree,
                recordedDominantDegree,
                sceneReleaseId
            );
        }

        private static
            ActivationLegitimacyCandidate
            Candidate(
                string trackId,
                string ideaId,
                int ideaIndex,
                TagDegree appliedDegree,
                TagDegree recordedDominantDegree,
                string sceneReleaseId = "RELEASE")
        {
            return new ActivationLegitimacyCandidate(
                ActivationAttemptRoute.Hail,
                "HAPPENING",
                "INTENT",
                "PLAYER_A",
                sceneReleaseId,
                "DEMO",
                trackId,
                ideaId,
                ideaIndex,
                "CHURCH_ARSON",
                TagAxis.Symbolic,
                TagPole.Negative,

                // Praxis remains degree 3 regardless
                // of the pair's applied activation cap.
                TagDegree.Transgressive,
                appliedDegree,
                recordedDominantDegree,
                "BEHAVIOR",
                "HAIL",
                "ASPECT_SATAN"
            );
        }
    }
}