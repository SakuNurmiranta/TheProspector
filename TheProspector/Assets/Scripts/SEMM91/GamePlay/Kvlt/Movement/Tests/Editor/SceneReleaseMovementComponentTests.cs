using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class SceneReleaseMovementComponentTests
    {
        [Test]
        public void
            NaturalDriftComponent_PreservesSignedProvenance()
        {
            SceneReleaseMovementComponent component =
                new SceneReleaseMovementComponent(
                    "RELEASE",
                    "KVLT",
                    6,
                    SceneReleaseMovementComponentKind
                        .NaturalDrift,
                    -0.25f,
                    "NATURAL_DRIFT_SOURCE"
                );

            Assert.That(
                component.SceneReleaseId,
                Is.EqualTo("RELEASE")
            );

            Assert.That(
                component.SceneId,
                Is.EqualTo("KVLT")
            );

            Assert.That(
                component.SettledTurn,
                Is.EqualTo(6)
            );

            Assert.That(
                component.Kind,
                Is.EqualTo(
                    SceneReleaseMovementComponentKind
                        .NaturalDrift
                )
            );

            Assert.That(
                component.Delta,
                Is.EqualTo(-0.25f)
            );

            Assert.That(
                component.SourceId,
                Is.EqualTo(
                    "NATURAL_DRIFT_SOURCE"
                )
            );
        }

        [Test]
        public void
            ZeroMovementComponent_IsValidSettledFact()
        {
            Assert.DoesNotThrow(
                () =>
                    new SceneReleaseMovementComponent(
                        "RELEASE",
                        "KVLT",
                        6,
                        SceneReleaseMovementComponentKind
                            .NaturalDrift,
                        0f,
                        "STABLE"
                    )
            );
        }

        [Test]
        public void
            InvalidMovementComponent_IsRejected()
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    new SceneReleaseMovementComponent(
                        "RELEASE",
                        "KVLT",
                        6,
                        SceneReleaseMovementComponentKind
                            .NaturalDrift,
                        float.NaN,
                        "SOURCE"
                    )
            );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    new SceneReleaseMovementComponent(
                        "RELEASE",
                        "KVLT",
                        -1,
                        SceneReleaseMovementComponentKind
                            .NaturalDrift,
                        0f,
                        "SOURCE"
                    )
            );

            Assert.Throws<
                ArgumentException>(
                () =>
                    new SceneReleaseMovementComponent(
                        "",
                        "KVLT",
                        6,
                        SceneReleaseMovementComponentKind
                            .NaturalDrift,
                        0f,
                        "SOURCE"
                    )
            );
        }
    }
}