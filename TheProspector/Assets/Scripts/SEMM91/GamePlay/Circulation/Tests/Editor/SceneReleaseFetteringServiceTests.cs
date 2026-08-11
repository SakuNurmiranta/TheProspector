using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Circulation.Tests.editor
{
    public class
        SceneReleaseFetteringServiceTests
    {
        private readonly
            SceneReleaseFetteringService service =
                new();

        [Test]
        public void
            ReleaseTurnWithoutActiveTrve_RemainsFringe()
        {
            SceneRelease release =
                Release(
                    releasedTurn: 5
                );

            SceneReleaseFetteringResult result =
                service.Resolve(
                    release,
                    hasActiveTrve: false,
                    globalTurn: 5
                );

            Assert.That(
                result,
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .GraceOpen
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Fringe
                )
            );

            Assert.That(
                release.LifecycleTransitions,
                Is.Empty
            );
        }

        [Test]
        public void
            ActiveTrveOnReleaseTurn_FettersImmediately()
        {
            SceneRelease release =
                Release(
                    releasedTurn: 5
                );

            SceneReleaseFetteringResult result =
                service.Resolve(
                    release,
                    hasActiveTrve: true,
                    globalTurn: 5
                );

            Assert.That(
                result,
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .Fettered
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            Assert.That(
                release.LifecycleTransitions.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            ActiveTrveDuringFollowingTurn_Fetters()
        {
            SceneRelease release =
                Release(
                    releasedTurn: 5
                );

            Assert.That(
                service.Resolve(
                    release,
                    false,
                    5
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .GraceOpen
                )
            );

            SceneReleaseFetteringResult result =
                service.Resolve(
                    release,
                    hasActiveTrve: true,
                    globalTurn: 6
                );

            Assert.That(
                result,
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .Fettered
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );
        }

        [Test]
        public void
            EntireFollowingTurnWithoutAttempt_FailsToFetter()
        {
            SceneRelease release =
                Release(
                    releasedTurn: 5
                );

            Assert.That(
                service.Resolve(
                    release,
                    false,
                    5
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .GraceOpen
                )
            );

            SceneReleaseFetteringResult result =
                service.Resolve(
                    release,
                    hasActiveTrve: false,
                    globalTurn: 6
                );

            Assert.That(
                result,
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .FailedToFetter
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .FailedToFetter
                )
            );

            Assert.That(
                release.LifecycleTransitions.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            GraceTurnAttempt_ProtectsEvenWithoutSuccessfulActivation()
        {
            SceneRelease release =
                Release(
                    releasedTurn: 5
                );

            RecordPerformanceAttempt(
                release,
                declaredTurn: 6,
                intentId: "ATTEMPT"
            );

            SceneReleaseFetteringResult result =
                service.Resolve(
                    release,
                    hasActiveTrve: false,
                    globalTurn: 6
                );

            Assert.That(
                result,
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .GraceProtectedByAttempt
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Fringe
                )
            );

            Assert.That(
                release.LifecycleTransitions,
                Is.Empty
            );

            /*
             * Nothing in this test claims that the
             * attempt succeeded.
             *
             * Attempt history alone is sufficient
             * for Fringe grace protection.
             */
            Assert.That(
                release.ActivationAttempts.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                release.PairActivationStates,
                Is.Empty
            );
        }

        [Test]
        public void
            AttemptOnReleaseTurnAlone_DoesNotProtectFollowingTurn()
        {
            SceneRelease release =
                Release(
                    releasedTurn: 5
                );

            RecordPerformanceAttempt(
                release,
                declaredTurn: 5,
                intentId: "RELEASE_TURN_ATTEMPT"
            );

            Assert.That(
                service.Resolve(
                    release,
                    hasActiveTrve: false,
                    globalTurn: 5
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .GraceOpen
                )
            );

            /*
             * The explicit grace window is turn 6,
             * not the original release turn.
             */
            Assert.That(
                service.Resolve(
                    release,
                    hasActiveTrve: false,
                    globalTurn: 6
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .FailedToFetter
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .FailedToFetter
                )
            );
        }

        [Test]
        public void
            ProtectedFringe_CanLaterFetterWhenTrveBecomesActive()
        {
            SceneRelease release =
                Release(
                    releasedTurn: 5
                );

            RecordPerformanceAttempt(
                release,
                declaredTurn: 6,
                intentId: "FAILED_ATTEMPT"
            );

            Assert.That(
                service.Resolve(
                    release,
                    false,
                    6
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .GraceProtectedByAttempt
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Fringe
                )
            );

            /*
             * The design currently gives a qualifying
             * grace-turn attempt continued
             * consideration. It does not define a
             * second automatic expiry deadline.
             */
            Assert.That(
                service.Resolve(
                    release,
                    hasActiveTrve: true,
                    globalTurn: 7
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .Fettered
                )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );
        }

        [Test]
        public void
            FieldAndFailedReleases_AreNotReprocessed()
        {
            SceneRelease field =
                Release(
                    releasedTurn: 5
                );

            Assert.That(
                field.TryFetter(5),
                Is.True
            );

            Assert.That(
                service.Resolve(
                    field,
                    false,
                    6
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .NoChange
                )
            );

            Assert.That(
                field.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Field
                )
            );

            SceneRelease failed =
                Release(
                    releasedTurn: 5
                );

            Assert.That(
                failed.TryFailToFetter(6),
                Is.True
            );

            Assert.That(
                service.Resolve(
                    failed,
                    true,
                    7
                ),
                Is.EqualTo(
                    SceneReleaseFetteringResult
                        .NoChange
                )
            );

            Assert.That(
                failed.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .FailedToFetter
                )
            );
        }

        [Test]
        public void
            SettlementCannotPredateRelease()
        {
            SceneRelease release =
                Release(
                    releasedTurn: 5
                );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    service.Resolve(
                        release,
                        false,
                        4
                    )
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState.Fringe
                )
            );
        }

        private static SceneRelease Release(
            int releasedTurn)
        {
            return new SceneRelease(
                "Test Release",
                "DEMO",
                "OWNER",
                "KVLT",
                releasedTurn,
                1f
            );
        }

        private static void
            RecordPerformanceAttempt(
                SceneRelease release,
                int declaredTurn,
                string intentId)
        {
            SceneReleaseActivationAttempt attempt =
                SceneReleaseActivationAttempt
                    .ForPerformance(
                        "HAPPENING",
                        intentId,
                        "ACTOR",
                        declaredTurn,
                        release.ReleaseId,
                        release.SourceDemoTapeId
                    );

            Assert.That(
                release.TryRecordActivationAttempt(
                    attempt
                ),
                Is.True
            );
        }
    }
}