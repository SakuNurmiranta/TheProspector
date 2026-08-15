using NUnit.Framework;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.World
    .Tests.Editor
{
    public class
        SeededWorldStateVisibilityAdjustmentTests
    {
        [Test]
        public void PendingBoostAffectsExactlyOneEvaluation()
        {
            SeededWorldState world =
                new SeededWorldState(
                    collectiveRegistry: null
                );

            SceneRelease release =
                CreateRelease();

            world.AddSceneRelease(release);

            world.EvaluateSceneOutputStandings(
                currentTurn: 1
            );

            float baselineVisibility =
                release.EffectiveVisibility;

            bool staged =
                release.TryStageVisibilityAdjustment(
                    requestedDelta: 0.25f,
                    out float appliedDelta
                );

            Assert.IsTrue(staged);

            Assert.AreEqual(
                0.25f,
                appliedDelta,
                0.0001f
            );

            float boostedVisibility =
                release.EffectiveVisibility;

            Assert.AreEqual(
                baselineVisibility + appliedDelta,
                boostedVisibility,
                0.0001f
            );

            world.EvaluateSceneOutputStandings(
                currentTurn: 2
            );

            Assert.IsFalse(
                release
                    .HasPendingVisibilityAdjustment
            );

            Assert.AreEqual(
                release.CirculationState.Reach,
                release.EffectiveVisibility,
                0.0001f
            );

            world.EvaluateSceneOutputStandings(
                currentTurn: 3
            );

            Assert.AreEqual(
                baselineVisibility,
                release.EffectiveVisibility,
                0.0001f
            );
        }

        [Test]
        public void SuppressionDoesNotChangeOrganicReach()
        {
            SeededWorldState world =
                new SeededWorldState(
                    collectiveRegistry: null
                );

            SceneRelease release =
                CreateRelease();

            world.AddSceneRelease(release);

            float organicReach =
                release.CirculationState.Reach;

            bool staged =
                release.TryStageVisibilityAdjustment(
                    requestedDelta: -1.0f,
                    out float appliedDelta
                );

            Assert.IsTrue(staged);

            Assert.AreEqual(
                -0.10f,
                appliedDelta,
                0.0001f
            );

            world.EvaluateSceneOutputStandings(
                currentTurn: 1
            );

            Assert.AreEqual(
                organicReach,
                release.CirculationState.Reach,
                0.0001f
            );

            Assert.AreEqual(
                organicReach,
                release.EffectiveVisibility,
                0.0001f
            );

            Assert.IsFalse(
                release
                    .HasPendingVisibilityAdjustment
            );
        }

        private static SceneRelease
            CreateRelease()
        {
            return new SceneRelease(
                displayName: "Test Release",
                sourceDemoTapeId: "Demo_A",
                sourceOwnerEntityId: "Owner_A",
                hostedSceneNodeId: "KVLT",
                releasedTurn: 1,
                sourceConveyance: 0.75f
            );
        }
    }
}
