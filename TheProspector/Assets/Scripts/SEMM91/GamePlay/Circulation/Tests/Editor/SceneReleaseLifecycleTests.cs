using NUnit.Framework;

namespace SEMM91.GamePlay.Circulation
    .Tests.Editor
{
    public class SceneReleaseLifecycleTests
    {
        [Test]
        public void NewRelease_StartsInFringeWithoutTransitionHistory()
        {
            SceneRelease release =
                CreateRelease(
                    releasedTurn: 3
                );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .Fringe
                )
            );

            Assert.That(
                release.LifecycleTransitions,
                Is.Empty
            );

            // Existing compatibility state
            // remains untouched.

            Assert.That(
                release.LegacyState,
                Is.EqualTo(
                    SceneLegacyState.Active
                )
            );
        }

        [Test]
        public void FringeRelease_CanFetterIntoField()
        {
            SceneRelease release =
                CreateRelease(
                    releasedTurn: 3
                );

            bool changed =
                release.TryFetter(
                    globalTurn: 3
                );

            Assert.That(
                changed,
                Is.True
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .Field
                )
            );

            Assert.That(
                release.LifecycleTransitions.Count,
                Is.EqualTo(1)
            );

            SceneReleaseLifecycleTransition
                transition =
                    release.LifecycleTransitions[0];

            Assert.That(
                transition.FromState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .Fringe
                )
            );

            Assert.That(
                transition.ToState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .Field
                )
            );

            Assert.That(
                transition.GlobalTurn,
                Is.EqualTo(3)
            );
        }

        [Test]
        public void FringeRelease_CanBecomeFailedToFetter()
        {
            SceneRelease release =
                CreateRelease(
                    releasedTurn: 3
                );

            bool changed =
                release.TryFailToFetter(
                    globalTurn: 5
                );

            Assert.That(
                changed,
                Is.True
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

            Assert.That(
                release.LifecycleTransitions[0]
                    .GlobalTurn,
                Is.EqualTo(5)
            );
        }

        [Test]
        public void FieldAndFailedStates_AreTerminalWithinCamp2Lifecycle()
        {
            SceneRelease field =
                CreateRelease(
                    releasedTurn: 2
                );

            Assert.That(
                field.TryFetter(2),
                Is.True
            );

            Assert.That(
                field.TryFailToFetter(4),
                Is.False
            );

            Assert.That(
                field.LifecycleTransitions.Count,
                Is.EqualTo(1)
            );

            SceneRelease failed =
                CreateRelease(
                    releasedTurn: 2
                );

            Assert.That(
                failed.TryFailToFetter(4),
                Is.True
            );

            Assert.That(
                failed.TryFetter(5),
                Is.False
            );

            Assert.That(
                failed.LifecycleTransitions.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void LifecycleTransition_BeforeReleaseTurn_IsRejected()
        {
            SceneRelease release =
                CreateRelease(
                    releasedTurn: 5
                );

            Assert.That(
                release.TryFetter(4),
                Is.False
            );

            Assert.That(
                release.TryFailToFetter(4),
                Is.False
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .Fringe
                )
            );

            Assert.That(
                release.LifecycleTransitions,
                Is.Empty
            );
        }

        private static SceneRelease
            CreateRelease(
                int releasedTurn)
        {
            return new SceneRelease(
                "Release",
                "DEMO",
                "OWNER",
                "KVLT",
                releasedTurn,
                1f
            );
        }
    }
}