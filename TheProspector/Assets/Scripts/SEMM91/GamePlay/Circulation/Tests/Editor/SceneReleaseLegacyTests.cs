using NUnit.Framework;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Circulation
    .Tests.Editor
{
    public class SceneReleaseLegacyTests
    {
        [Test]
        public void NewReleaseStartsActive()
        {
            SceneRelease release =
                CreateRelease(
                    "Release A",
                    "Owner_A",
                    releasedTurn: 2
                );

            Assert.AreEqual(
                SceneLegacyState.Active,
                release.LegacyState
            );

            Assert.IsTrue(
                release
                    .IsEligiblePrimaryWorkProxy
            );

            Assert.AreEqual(
                -1,
                release
                    .CanonizationSubjectSinceRound
            );

            Assert.AreEqual(
                -1,
                release.CanonizedRound
            );
        }

        [Test]
        public void LegacyLifecycleIsOneWay()
        {
            SceneRelease release =
                CreateRelease(
                    "Release A",
                    "Owner_A",
                    releasedTurn: 2
                );

            Assert.IsTrue(
                release
                    .TryBeginCanonizationSubject(
                        1
                    )
            );

            Assert.AreEqual(
                SceneLegacyState
                    .CanonizationSubject,
                release.LegacyState
            );

            Assert.IsFalse(
                release
                    .IsEligiblePrimaryWorkProxy
            );

            Assert.IsTrue(
                release
                    .TryAdvanceCanonizationSubjectYear()
            );

            Assert.AreEqual(
                1,
                release.CanonizationSubjectYears
            );

            Assert.IsTrue(
                release.TryCanonize(
                    canonizedRound: 2,
                    incomingKeeperClientId: 7
                )
            );

            Assert.AreEqual(
                SceneLegacyState.Canonized,
                release.LegacyState
            );

            Assert.AreEqual(
                2,
                release.CanonizedRound
            );

            Assert.AreEqual(
                7ul,
                release
                    .CanonizedAtTransitionToKeeperClientId
            );

            Assert.IsFalse(
                release
                    .TryBeginCanonizationSubject(
                        3
                    )
            );
        }

        [Test]
        public void PrimaryProxyUsesMostRecentRelease()
        {
            SeededWorldState world =
                new SeededWorldState(null);

            SceneRelease older =
                CreateRelease(
                    "Older",
                    "Owner_A",
                    releasedTurn: 1
                );

            SceneRelease newer =
                CreateRelease(
                    "Newer",
                    "Owner_A",
                    releasedTurn: 3
                );

            world.AddSceneRelease(older);
            world.AddSceneRelease(newer);

            bool found =
                world
                    .TrySelectPrimaryWorkProxyForOwner(
                        "Owner_A",
                        out SceneRelease selected
                    );

            Assert.IsTrue(found);
            Assert.AreSame(newer, selected);
        }

        [Test]
        public void SameTurnUsesLaterRegistration()
        {
            SeededWorldState world =
                new SeededWorldState(null);

            SceneRelease first =
                CreateRelease(
                    "First",
                    "Owner_A",
                    releasedTurn: 3
                );

            SceneRelease second =
                CreateRelease(
                    "Second",
                    "Owner_A",
                    releasedTurn: 3
                );

            world.AddSceneRelease(first);
            world.AddSceneRelease(second);

            world.TrySelectPrimaryWorkProxyForOwner(
                "Owner_A",
                out SceneRelease selected
            );

            Assert.AreSame(second, selected);
        }

        [Test]
        public void SubjectIsExcludedFromNewPrimarySelection()
        {
            SeededWorldState world =
                new SeededWorldState(null);

            SceneRelease olderActive =
                CreateRelease(
                    "Older Active",
                    "Owner_A",
                    releasedTurn: 1
                );

            SceneRelease newerSubject =
                CreateRelease(
                    "Newer Subject",
                    "Owner_A",
                    releasedTurn: 3
                );

            newerSubject
                .TryBeginCanonizationSubject(1);

            world.AddSceneRelease(olderActive);
            world.AddSceneRelease(newerSubject);

            bool found =
                world
                    .TrySelectPrimaryWorkProxyForOwner(
                        "Owner_A",
                        out SceneRelease selected
                    );

            Assert.IsTrue(found);
            Assert.AreSame(
                olderActive,
                selected
            );
        }

        private static SceneRelease
            CreateRelease(
                string displayName,
                string ownerEntityId,
                int releasedTurn)
        {
            return new SceneRelease(
                displayName,
                sourceDemoTapeId:
                    $"{displayName}_Demo",
                sourceOwnerEntityId:
                    ownerEntityId,
                hostedSceneNodeId:
                    "KVLT",
                releasedTurn,
                sourceConveyance: 1.0f
            );
        }
    }
}