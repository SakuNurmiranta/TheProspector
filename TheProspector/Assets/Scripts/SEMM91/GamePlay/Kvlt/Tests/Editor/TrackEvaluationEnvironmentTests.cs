using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Society;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class TrackEvaluationEnvironmentTests
    {
        [Test]
        public void Environment_PreservesSettledTurn()
        {
            TrackEvaluationEnvironment environment =
                CreateEnvironment(
                    settledTurn: 7
                );

            Assert.That(
                environment.SettledTurn,
                Is.EqualTo(7)
            );
        }

        [Test]
        public void Environment_FreezesSocietyNormsAtConstruction()
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Transgressive
            );

            TrackEvaluationEnvironment environment =
                new TrackEvaluationEnvironment(
                    3,
                    NormativeCentre.Neutral,
                    NormativeCentre.Neutral,
                    society
                );

            Assert.That(
                environment
                    .GetSocietyNormativeForce(
                        TagAxis.Symbolic,
                        TagPole.Positive
                    ),
                Is.EqualTo(3f)
            );

            society.SetNormativeDegree(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Neutral
            );

            Assert.That(
                environment
                    .GetSocietyNormativeForce(
                        TagAxis.Symbolic,
                        TagPole.Positive
                    ),
                Is.EqualTo(3f),
                "Frozen environment must not change " +
                "when authoritative Society state changes."
            );
        }

        [Test]
        public void MissingSocietyNorm_RemainsZeroForce()
        {
            TrackEvaluationEnvironment environment =
                CreateEnvironment();

            Assert.That(
                environment
                    .TryGetSocietyNormativeDegree(
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        out _
                    ),
                Is.False
            );

            Assert.That(
                environment
                    .GetSocietyNormativeForce(
                        TagAxis.Symbolic,
                        TagPole.Positive
                    ),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void CurrentAndCanonNormativeCentres_RemainDistinct()
        {
            NormativeCentre current =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            TagDegree.Weak,
                            -0.25f
                        )
                    }
                );

            NormativeCentre canonOnly =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            TagDegree.Weak,
                            -1f
                        )
                    }
                );

            TrackEvaluationEnvironment environment =
                new TrackEvaluationEnvironment(
                    4,
                    current,
                    canonOnly,
                    new SocietyNormativeProfile()
                );

            Assert.That(
                environment.CurrentNormativeCentre
                    .GetAffinity(
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        TagDegree.Weak
                    ),
                Is.EqualTo(-0.25f)
            );

            Assert.That(
                environment.CanonNormativeCentre
                    .GetAffinity(
                        TagAxis.Symbolic,
                        TagPole.Positive,
                        TagDegree.Weak
                    ),
                Is.EqualTo(-1f)
            );
        }

        [Test]
        public void InvalidEnvironmentInputs_AreRejected()
        {
            SocietyNormativeProfile society =
                new SocietyNormativeProfile();

            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    new TrackEvaluationEnvironment(
                        -1,
                        NormativeCentre.Neutral,
                        NormativeCentre.Neutral,
                        society
                    )
            );

            Assert.Throws<ArgumentNullException>(
                () =>
                    new TrackEvaluationEnvironment(
                        0,
                        null,
                        NormativeCentre.Neutral,
                        society
                    )
            );

            Assert.Throws<ArgumentNullException>(
                () =>
                    new TrackEvaluationEnvironment(
                        0,
                        NormativeCentre.Neutral,
                        null,
                        society
                    )
            );

            Assert.Throws<ArgumentNullException>(
                () =>
                    new TrackEvaluationEnvironment(
                        0,
                        NormativeCentre.Neutral,
                        NormativeCentre.Neutral,
                        null
                    )
            );
        }

        private static TrackEvaluationEnvironment
            CreateEnvironment(
                int settledTurn = 0)
        {
            return new TrackEvaluationEnvironment(
                settledTurn,
                NormativeCentre.Neutral,
                NormativeCentre.Neutral,
                new SocietyNormativeProfile()
            );
        }
    }
}