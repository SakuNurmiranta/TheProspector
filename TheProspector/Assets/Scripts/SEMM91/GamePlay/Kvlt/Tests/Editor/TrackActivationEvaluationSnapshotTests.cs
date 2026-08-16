using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class TrackActivationEvaluationSnapshotTests
    {
        [Test]
        public void MissingIdeaActivation_ReturnsZero()
        {
            TrackActivationEvaluationSnapshot snapshot =
                CreateSnapshot();

            Assert.That(
                snapshot.GetLegitimateActiveDegree(
                    "UNACTIVATED_IDEA"
                ),
                Is.EqualTo(0)
            );
        }

        [Test]
        public void ExplicitLegitimateActivation_IsReturned()
        {
            TrackActivationEvaluationSnapshot snapshot =
                CreateSnapshot(
                    new IdeaActivationEvaluationSnapshot(
                        "PAIR",
                        2
                    )
                );

            Assert.That(
                snapshot.GetLegitimateActiveDegree(
                    "PAIR"
                ),
                Is.EqualTo(2)
            );

            Assert.That(
                snapshot.IdeaActivations[0]
                    .HasLegitimateActivation,
                Is.True
            );
        }

        [Test]
        public void DegreeZeroActivation_IsValidButNotActive()
        {
            IdeaActivationEvaluationSnapshot activation =
                new IdeaActivationEvaluationSnapshot(
                    "PAIR",
                    0
                );

            Assert.That(
                activation.LegitimateActiveDegree,
                Is.EqualTo(0)
            );

            Assert.That(
                activation.HasLegitimateActivation,
                Is.False
            );
        }

        [Test]
        public void Snapshot_PreservesReleaseDemoAndTrackIdentity()
        {
            TrackActivationEvaluationSnapshot snapshot =
                new TrackActivationEvaluationSnapshot(
                    "RELEASE_4",
                    "DEMO_2",
                    "TRACK_7",
                    Array.Empty<
                        IdeaActivationEvaluationSnapshot>()
                );

            Assert.That(
                snapshot.SourceReleaseId,
                Is.EqualTo("RELEASE_4")
            );

            Assert.That(
                snapshot.SourceDemoTapeId,
                Is.EqualTo("DEMO_2")
            );

            Assert.That(
                snapshot.SourceTrackId,
                Is.EqualTo("TRACK_7")
            );
        }

        [Test]
        public void Snapshot_CopiesSourceActivationCollection()
        {
            List<IdeaActivationEvaluationSnapshot>
                source =
                    new()
                    {
                        new IdeaActivationEvaluationSnapshot(
                            "FIRST",
                            1
                        )
                    };

            TrackActivationEvaluationSnapshot snapshot =
                new TrackActivationEvaluationSnapshot(
                    "RELEASE",
                    "DEMO",
                    "TRACK",
                    source
                );

            source.Add(
                new IdeaActivationEvaluationSnapshot(
                    "SECOND",
                    2
                )
            );

            Assert.That(
                snapshot.IdeaActivations.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                snapshot.GetLegitimateActiveDegree(
                    "SECOND"
                ),
                Is.EqualTo(0)
            );
        }

        [Test]
        public void DuplicateIdeaActivation_IsRejected()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    CreateSnapshot(
                        new IdeaActivationEvaluationSnapshot(
                            "PAIR",
                            1
                        ),
                        new IdeaActivationEvaluationSnapshot(
                            "PAIR",
                            2
                        )
                    )
            );
        }

        [Test]
        public void InvalidActivationDegree_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    new IdeaActivationEvaluationSnapshot(
                        "PAIR",
                        -1
                    )
            );

            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    new IdeaActivationEvaluationSnapshot(
                        "PAIR",
                        4
                    )
            );
        }

        [Test]
        public void MissingProvenanceIdentity_IsRejected()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new TrackActivationEvaluationSnapshot(
                        "",
                        "DEMO",
                        "TRACK",
                        Array.Empty<
                            IdeaActivationEvaluationSnapshot>()
                    )
            );

            Assert.Throws<ArgumentException>(
                () =>
                    new TrackActivationEvaluationSnapshot(
                        "RELEASE",
                        "",
                        "TRACK",
                        Array.Empty<
                            IdeaActivationEvaluationSnapshot>()
                    )
            );

            Assert.Throws<ArgumentException>(
                () =>
                    new TrackActivationEvaluationSnapshot(
                        "RELEASE",
                        "DEMO",
                        "",
                        Array.Empty<
                            IdeaActivationEvaluationSnapshot>()
                    )
            );
        }

        private static
            TrackActivationEvaluationSnapshot
            CreateSnapshot(
                params
                    IdeaActivationEvaluationSnapshot[]
                    activations)
        {
            return new TrackActivationEvaluationSnapshot(
                "RELEASE",
                "DEMO",
                "TRACK",
                activations
            );
        }
    }
}