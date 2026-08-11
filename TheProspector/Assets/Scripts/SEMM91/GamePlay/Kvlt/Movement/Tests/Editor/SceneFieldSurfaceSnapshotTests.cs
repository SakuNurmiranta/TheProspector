using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class SceneFieldSurfaceSnapshotTests
    {
        [Test]
        public void
            EmptyPopulation_HasNoMeanSurface()
        {
            SceneFieldSurfaceSnapshot snapshot =
                new SceneFieldSurfaceSnapshot(
                    "KVLT",
                    6,
                    Array.Empty<
                        SceneFieldSurfaceEntry>()
                );

            Assert.That(
                snapshot.PopulationCount,
                Is.EqualTo(0)
            );

            Assert.That(
                snapshot.HasQualifyingPopulation,
                Is.False
            );

            Assert.That(
                snapshot.MeanSurface,
                Is.Null
            );
        }

        [Test]
        public void
            PopulationMeanUsesAllEntriesAndOrderIsDeterministic()
        {
            SceneFieldSurfaceSnapshot snapshot =
                new SceneFieldSurfaceSnapshot(
                    "KVLT",
                    6,
                    new[]
                    {
                        Entry(
                            "RELEASE_C",
                            3f
                        ),

                        Entry(
                            "RELEASE_A",
                            1f
                        ),

                        Entry(
                            "RELEASE_B",
                            2f
                        )
                    }
                );

            Assert.That(
                snapshot.PopulationCount,
                Is.EqualTo(3)
            );

            Assert.That(
                snapshot.MeanSurface,
                Is.EqualTo(2f)
                    .Within(0.0001f)
            );

            Assert.That(
                snapshot.Entries[0]
                    .SceneReleaseId,
                Is.EqualTo("RELEASE_A")
            );

            Assert.That(
                snapshot.Entries[1]
                    .SceneReleaseId,
                Is.EqualTo("RELEASE_B")
            );

            Assert.That(
                snapshot.Entries[2]
                    .SceneReleaseId,
                Is.EqualTo("RELEASE_C")
            );
        }

        [Test]
        public void
            DuplicateSceneReleaseContribution_IsRejected()
        {
            Assert.Throws<
                ArgumentException>(
                () =>
                    new SceneFieldSurfaceSnapshot(
                        "KVLT",
                        6,
                        new[]
                        {
                            Entry(
                                "SAME",
                                1f
                            ),

                            Entry(
                                "SAME",
                                3f
                            )
                        }
                    )
            );
        }

        [Test]
        public void
            SnapshotRejectsDifferentSceneOrTurn()
        {
            Assert.Throws<
                ArgumentException>(
                () =>
                    new SceneFieldSurfaceSnapshot(
                        "KVLT",
                        6,
                        new[]
                        {
                            new SceneFieldSurfaceEntry(
                                "RELEASE",
                                "DEMO",
                                "OWNER",
                                "OTHER_SCENE",
                                6,
                                1f,
                                0.2f
                            )
                        }
                    )
            );

            Assert.Throws<
                ArgumentException>(
                () =>
                    new SceneFieldSurfaceSnapshot(
                        "KVLT",
                        6,
                        new[]
                        {
                            new SceneFieldSurfaceEntry(
                                "RELEASE",
                                "DEMO",
                                "OWNER",
                                "KVLT",
                                5,
                                1f,
                                0.2f
                            )
                        }
                    )
            );
        }

        private static SceneFieldSurfaceEntry
            Entry(
                string releaseId,
                float surface)
        {
            return new SceneFieldSurfaceEntry(
                releaseId,
                "DEMO_" + releaseId,
                "OWNER",
                "KVLT",
                6,
                surface,
                0.25f
            );
        }
    }
}