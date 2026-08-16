using System;
using NUnit.Framework;

namespace SEMM91.GamePlay.Kvlt.Movement.Tests.Editor
{
    public class
        SceneReleaseNaturalDriftEvaluatorTests
    {
        private readonly
            SceneReleaseNaturalDriftEvaluator
                evaluator =
                    new();

        [Test]
        public void
            RelativeSurfaceProducesOutwardStableAndInwardDrift()
        {
            SceneFieldSurfaceSnapshot snapshot =
                Snapshot(
                    1f,
                    2f,
                    3f
                );

            var results =
                evaluator.Evaluate(
                    snapshot,
                    fieldDriftScale: 3f
                );

            Assert.That(
                results.Count,
                Is.EqualTo(3)
            );

            /*
             * Mean = 2.
             *
             * LOW:
             * ((1 - 2) / 6) * 3 = -0.5
             *
             * MID:
             * ((2 - 2) / 6) * 3 = 0
             *
             * HIGH:
             * ((3 - 2) / 6) * 3 = +0.5
             */

            Assert.That(
                results[0].SceneReleaseId,
                Is.EqualTo("HIGH")
                    .Or.EqualTo("LOW")
                    .Or.EqualTo("MID")
            );

            SceneReleaseNaturalDriftEvaluation
                low =
                    Find(
                        results,
                        "LOW"
                    );

            SceneReleaseNaturalDriftEvaluation
                middle =
                    Find(
                        results,
                        "MID"
                    );

            SceneReleaseNaturalDriftEvaluation
                high =
                    Find(
                        results,
                        "HIGH"
                    );

            Assert.That(
                low.NaturalDrift,
                Is.EqualTo(-0.5f)
                    .Within(0.0001f)
            );

            Assert.That(
                low.RelativeSurface,
                Is.EqualTo(-1f)
            );

            Assert.That(
                middle.NaturalDrift,
                Is.EqualTo(0f)
                    .Within(0.0001f)
            );

            Assert.That(
                middle.RelativeSurface,
                Is.EqualTo(0f)
            );

            Assert.That(
                high.NaturalDrift,
                Is.EqualTo(0.5f)
                    .Within(0.0001f)
            );

            Assert.That(
                high.RelativeSurface,
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void
            LoneQualifyingRelease_HasExactlyZeroNaturalDrift()
        {
            SceneFieldSurfaceSnapshot snapshot =
                new SceneFieldSurfaceSnapshot(
                    "KVLT",
                    6,
                    new[]
                    {
                        Entry(
                            "ONLY",
                            3f
                        )
                    }
                );

            var results =
                evaluator.Evaluate(
                    snapshot,
                    fieldDriftScale: 100f
                );

            Assert.That(
                results.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                results[0].IsLoneQualifyingRelease,
                Is.True
            );

            Assert.That(
                results[0].FieldMeanSurface,
                Is.EqualTo(3f)
            );

            Assert.That(
                results[0].NaturalDrift,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void
            ZeroDriftScale_ProducesZeroAppliedNaturalDrift()
        {
            SceneFieldSurfaceSnapshot snapshot =
                Snapshot(
                    1f,
                    3f
                );

            var results =
                evaluator.Evaluate(
                    snapshot,
                    fieldDriftScale: 0f
                );

            Assert.That(
                results.Count,
                Is.EqualTo(2)
            );

            foreach (
                SceneReleaseNaturalDriftEvaluation
                    result
                in results)
            {
                Assert.That(
                    result.NaturalDrift,
                    Is.EqualTo(0f)
                );
            }
        }

        [Test]
        public void
            InvalidFieldDriftScale_IsRejected()
        {
            SceneFieldSurfaceSnapshot snapshot =
                Snapshot(
                    1f,
                    3f
                );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    evaluator.Evaluate(
                        snapshot,
                        -1f
                    )
            );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    evaluator.Evaluate(
                        snapshot,
                        float.NaN
                    )
            );

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    evaluator.Evaluate(
                        snapshot,
                        float.PositiveInfinity
                    )
            );
        }

        private static SceneFieldSurfaceSnapshot
            Snapshot(
                params float[] surfaces)
        {
            SceneFieldSurfaceEntry[] entries =
                new SceneFieldSurfaceEntry[
                    surfaces.Length
                ];

            string[] names =
            {
                "LOW",
                "MID",
                "HIGH"
            };

            for (int index = 0;
                 index < surfaces.Length;
                 index++)
            {
                entries[index] =
                    Entry(
                        names[index],
                        surfaces[index]
                    );
            }

            return new SceneFieldSurfaceSnapshot(
                "KVLT",
                6,
                entries
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

        private static
            SceneReleaseNaturalDriftEvaluation
            Find(
                System.Collections.Generic
                    .IReadOnlyList<
                        SceneReleaseNaturalDriftEvaluation>
                    results,
                string releaseId)
        {
            foreach (
                SceneReleaseNaturalDriftEvaluation
                    result
                in results)
            {
                if (result.SceneReleaseId ==
                    releaseId)
                {
                    return result;
                }
            }

            Assert.Fail(
                "Natural Drift result not found | " +
                $"release={releaseId}"
            );

            return null;
        }
    }
}