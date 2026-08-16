using System;
using NUnit.Framework;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class TrackGravityEvaluatorTests
    {
        [Test]
        public void ZeroTrve_ProducesZeroGravityDespiteResonance()
        {
            TrackGravityEvaluation result =
                Evaluate(
                    signedResonance: 3f,
                    trve: 0f
                );

            Assert.That(
                result.TrveFraction,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.SignedGravity,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.Gravity,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void HalfMaximumTrve_ScalesResonanceByHalf()
        {
            TrackGravityEvaluation result =
                Evaluate(
                    signedResonance: 2f,
                    trve: 3f
                );

            Assert.That(
                result.TrveFraction,
                Is.EqualTo(0.5f)
            );

            Assert.That(
                result.SignedGravity,
                Is.EqualTo(1f)
            );

            Assert.That(
                result.Gravity,
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void MaximumTrve_PreservesFullResonance()
        {
            TrackGravityEvaluation result =
                Evaluate(
                    signedResonance: 2.25f,
                    trve: 6f
                );

            Assert.That(
                result.TrveFraction,
                Is.EqualTo(1f)
            );

            Assert.That(
                result.SignedGravity,
                Is.EqualTo(2.25f)
            );

            Assert.That(
                result.Gravity,
                Is.EqualTo(2.25f)
            );
        }

        [Test]
        public void NegativeResonance_ProducesNegativeSignedGravityAndPositiveMagnitude()
        {
            TrackGravityEvaluation result =
                Evaluate(
                    signedResonance: -2f,
                    trve: 3f
                );

            Assert.That(
                result.SignedGravity,
                Is.EqualTo(-1f)
            );

            Assert.That(
                result.Gravity,
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void IdeaDirectionalContributions_UseTrackTrveFraction()
        {
            TrackResonanceEvaluation resonance =
                new TrackResonanceEvaluation(
                    "TRACK",
                    1f,
                    new[]
                    {
                        new IdeaResonanceEvaluation(
                            "POSITIVE",
                            0,
                            2f,
                            3f,
                            1f,
                            2f
                        ),

                        new IdeaResonanceEvaluation(
                            "NEGATIVE",
                            1,
                            0f,
                            3f,
                            1f,
                            -1f
                        )
                    }
                );

            TrackTrveEvaluation trve =
                CreateTrve(
                    3f,
                    "POSITIVE",
                    "NEGATIVE"
                );

            TrackGravityEvaluation result =
                new TrackGravityEvaluator()
                    .Evaluate(
                        resonance,
                        trve
                    );

            Assert.That(
                result.TrveFraction,
                Is.EqualTo(0.5f)
            );

            Assert.That(
                result.IdeaEvaluations[0]
                    .SignedGravityContribution,
                Is.EqualTo(1f)
            );

            Assert.That(
                result.IdeaEvaluations[1]
                    .SignedGravityContribution,
                Is.EqualTo(-0.5f)
            );
        }

        [Test]
        public void MismatchedTrackIdentity_IsRejected()
        {
            TrackResonanceEvaluation resonance =
                new TrackResonanceEvaluation(
                    "TRACK_A",
                    1f,
                    Array.Empty<
                        IdeaResonanceEvaluation>()
                );

            TrackTrveEvaluation trve =
                new TrackTrveEvaluation(
                    "RELEASE",
                    "DEMO",
                    "TRACK_B",
                    1f,
                    Array.Empty<
                        IdeaTrveEvaluation>()
                );

            Assert.Throws<ArgumentException>(
                () =>
                    new TrackGravityEvaluator()
                        .Evaluate(
                            resonance,
                            trve
                        )
            );
        }

        private static TrackGravityEvaluation Evaluate(
            float signedResonance,
            float trve)
        {
            TrackResonanceEvaluation resonance =
                new TrackResonanceEvaluation(
                    "TRACK",
                    signedResonance,
                    Array.Empty<
                        IdeaResonanceEvaluation>()
                );

            TrackTrveEvaluation trveEvaluation =
                new TrackTrveEvaluation(
                    "RELEASE",
                    "DEMO",
                    "TRACK",
                    trve,
                    Array.Empty<
                        IdeaTrveEvaluation>()
                );

            return new TrackGravityEvaluator()
                .Evaluate(
                    resonance,
                    trveEvaluation
                );
        }

        private static TrackTrveEvaluation CreateTrve(
            float trve,
            params string[] ideaIds)
        {
            IdeaTrveEvaluation[] ideas =
                new IdeaTrveEvaluation[
                    ideaIds.Length
                ];

            for (int index = 0;
                 index < ideaIds.Length;
                 index++)
            {
                ideas[index] =
                    new IdeaTrveEvaluation(
                        ideaIds[index],
                        index,
                        false,
                        PairIntegrity.NotApplicable,
                        false,
                        0,
                        0f,
                        0f,
                        0,
                        0f,
                        0f
                    );
            }

            return new TrackTrveEvaluation(
                "RELEASE",
                "DEMO",
                "TRACK",
                trve,
                ideas
            );
        }
    }
}