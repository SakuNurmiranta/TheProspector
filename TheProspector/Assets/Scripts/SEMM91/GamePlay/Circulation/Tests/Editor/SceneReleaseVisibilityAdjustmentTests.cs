using NUnit.Framework;

namespace SEMM91.GamePlay.Circulation
    .Tests.Editor
{
    public class
        SceneReleaseVisibilityAdjustmentTests
    {
        [Test]
        public void NewReleaseUsesOrganicReach()
        {
            SceneRelease release =
                CreateRelease();

            Assert.IsFalse(
                release
                    .HasPendingVisibilityAdjustment
            );

            Assert.AreEqual(
                release.CirculationState.Reach,
                release.EffectiveVisibility,
                0.0001f
            );
        }

        [Test]
        public void BoostChangesEffectiveVisibilityOnly()
        {
            SceneRelease release =
                CreateRelease();

            float organicReach =
                release.CirculationState.Reach;

            bool success =
                release.TryStageVisibilityAdjustment(
                    requestedDelta: 0.25f,
                    out float appliedDelta
                );

            Assert.IsTrue(success);

            Assert.AreEqual(
                organicReach,
                release.CirculationState.Reach,
                0.0001f
            );

            Assert.AreEqual(
                0.25f,
                appliedDelta,
                0.0001f
            );

            Assert.AreEqual(
                organicReach + 0.25f,
                release.EffectiveVisibility,
                0.0001f
            );
        }

        [Test]
        public void SuppressionClampsAtZero()
        {
            SceneRelease release =
                CreateRelease();

            bool success =
                release.TryStageVisibilityAdjustment(
                    requestedDelta: -1.0f,
                    out float appliedDelta
                );

            Assert.IsTrue(success);

            Assert.AreEqual(
                -0.10f,
                appliedDelta,
                0.0001f
            );

            Assert.AreEqual(
                0.0f,
                release.EffectiveVisibility,
                0.0001f
            );
        }

        [Test]
        public void BoostClampsAtOne()
        {
            SceneRelease release =
                CreateRelease();

            bool success =
                release.TryStageVisibilityAdjustment(
                    requestedDelta: 2.0f,
                    out float appliedDelta
                );

            Assert.IsTrue(success);

            Assert.AreEqual(
                0.90f,
                appliedDelta,
                0.0001f
            );

            Assert.AreEqual(
                1.0f,
                release.EffectiveVisibility,
                0.0001f
            );
        }

        [Test]
        public void ConsumingAdjustmentRestoresOrganicVisibility()
        {
            SceneRelease release =
                CreateRelease();

            float organicReach =
                release.CirculationState.Reach;

            release.TryStageVisibilityAdjustment(
                requestedDelta: 0.30f,
                out _
            );

            bool consumed =
                release
                    .ConsumePendingVisibilityAdjustment();

            Assert.IsTrue(consumed);

            Assert.IsFalse(
                release
                    .HasPendingVisibilityAdjustment
            );

            Assert.AreEqual(
                0.0f,
                release
                    .PendingVisibilityAdjustment,
                0.0001f
            );

            Assert.AreEqual(
                organicReach,
                release.EffectiveVisibility,
                0.0001f
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
        
        [Test]
        public void PreviewDoesNotMutateRelease()
        {
            SceneRelease release =
                CreateRelease();

            float organicReach =
                release.CirculationState.Reach;

            bool success =
                release.TryPreviewVisibilityAdjustment(
                    requestedDelta: 0.25f,
                    out float appliedDelta
                );

            Assert.IsTrue(success);

            Assert.AreEqual(
                0.25f,
                appliedDelta,
                0.0001f
            );

            Assert.IsFalse(
                release.HasPendingVisibilityAdjustment
            );

            Assert.AreEqual(
                0.0f,
                release.PendingVisibilityAdjustment,
                0.0001f
            );

            Assert.AreEqual(
                organicReach,
                release.EffectiveVisibility,
                0.0001f
            );
        }
    }
}