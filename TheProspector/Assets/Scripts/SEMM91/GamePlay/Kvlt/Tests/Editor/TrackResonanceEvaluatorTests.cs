using NUnit.Framework;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class TrackResonanceEvaluatorTests
    {
        [Test]
        public void MaximumExtremity_PreservesFullPositiveSurface()
        {
            TrackSurfaceExtremityEvaluation source =
                Create(
                    Idea(
                        "IDEA",
                        0,
                        surface: 2f,
                        extremity: 3f
                    )
                );

            TrackResonanceEvaluation result =
                new TrackResonanceEvaluator()
                    .Evaluate(source);

            // 2 * (3 / 3) = 2

            Assert.That(
                result.SignedResonance,
                Is.EqualTo(2f)
            );

            Assert.That(
                result.Resonance,
                Is.EqualTo(2f)
            );
        }

        [Test]
        public void NegativeSurface_PreservesSignedDirectionButPositiveMagnitude()
        {
            TrackSurfaceExtremityEvaluation source =
                Create(
                    Idea(
                        "IDEA",
                        0,
                        surface: -2f,
                        extremity: 3f
                    )
                );

            TrackResonanceEvaluation result =
                new TrackResonanceEvaluator()
                    .Evaluate(source);

            Assert.That(
                result.SignedResonance,
                Is.EqualTo(-2f)
            );

            Assert.That(
                result.Resonance,
                Is.EqualTo(2f)
            );
        }

        [Test]
        public void Extremity_ScalesSurfaceContribution()
        {
            TrackSurfaceExtremityEvaluation source =
                Create(
                    Idea(
                        "IDEA",
                        0,
                        surface: 2f,
                        extremity: 1.5f
                    )
                );

            TrackResonanceEvaluation result =
                new TrackResonanceEvaluator()
                    .Evaluate(source);

            // factor = 1.5 / 3 = 0.5
            // r = 2 * 0.5 = 1

            Assert.That(
                result.IdeaEvaluations[0]
                    .ExtremityFactor,
                Is.EqualTo(0.5f)
            );

            Assert.That(
                result.IdeaEvaluations[0]
                    .SignedResonanceContribution,
                Is.EqualTo(1f)
            );

            Assert.That(
                result.Resonance,
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void OpposedIdeaContributions_CancelBeforeAbsoluteMagnitude()
        {
            TrackSurfaceExtremityEvaluation source =
                Create(
                    Idea(
                        "POSITIVE",
                        0,
                        surface: 2f,
                        extremity: 3f
                    ),

                    Idea(
                        "NEGATIVE",
                        1,
                        surface: -2f,
                        extremity: 3f
                    )
                );

            TrackResonanceEvaluation result =
                new TrackResonanceEvaluator()
                    .Evaluate(source);

            // +2 and -2
            //
            // mean = 0
            // abs(mean) = 0
            //
            // NOT mean(abs) = 2

            Assert.That(
                result.IdeaEvaluations[0]
                    .SignedResonanceContribution,
                Is.EqualTo(2f)
            );

            Assert.That(
                result.IdeaEvaluations[1]
                    .SignedResonanceContribution,
                Is.EqualTo(-2f)
            );

            Assert.That(
                result.SignedResonance,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.Resonance,
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void TrackResonance_IsMeanAcrossAllIdeas()
        {
            TrackSurfaceExtremityEvaluation source =
                Create(
                    Idea(
                        "FIRST",
                        0,
                        surface: 3f,
                        extremity: 3f
                    ),

                    Idea(
                        "SECOND",
                        1,
                        surface: 1f,
                        extremity: 3f
                    )
                );

            TrackResonanceEvaluation result =
                new TrackResonanceEvaluator()
                    .Evaluate(source);

            // r1 = 3
            // r2 = 1
            //
            // mean = 2

            Assert.That(
                result.SignedResonance,
                Is.EqualTo(2f)
            );

            Assert.That(
                result.Resonance,
                Is.EqualTo(2f)
            );
        }

        [Test]
        public void Evaluation_PreservesTrackAndIdeaProvenance()
        {
            TrackSurfaceExtremityEvaluation source =
                Create(
                    Idea(
                        "FIRST",
                        0,
                        1f,
                        1f
                    ),

                    Idea(
                        "SECOND",
                        1,
                        -1f,
                        2f
                    )
                );

            TrackResonanceEvaluation result =
                new TrackResonanceEvaluator()
                    .Evaluate(source);

            Assert.That(
                result.SourceTrackId,
                Is.EqualTo("TRACK")
            );

            Assert.That(
                result.IdeaEvaluations.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                result.IdeaEvaluations[0]
                    .SourceIdeaId,
                Is.EqualTo("FIRST")
            );

            Assert.That(
                result.IdeaEvaluations[0]
                    .IdeaIndex,
                Is.EqualTo(0)
            );

            Assert.That(
                result.IdeaEvaluations[1]
                    .SourceIdeaId,
                Is.EqualTo("SECOND")
            );

            Assert.That(
                result.IdeaEvaluations[1]
                    .IdeaIndex,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void EmptyTrackEvaluation_HasZeroResonance()
        {
            TrackSurfaceExtremityEvaluation source =
                Create();

            TrackResonanceEvaluation result =
                new TrackResonanceEvaluator()
                    .Evaluate(source);

            Assert.That(
                result.SignedResonance,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.Resonance,
                Is.EqualTo(0f)
            );

            Assert.That(
                result.IdeaEvaluations,
                Is.Empty
            );
        }

        private static
            TrackSurfaceExtremityEvaluation
            Create(
                params
                    IdeaSurfaceExtremityEvaluation[]
                    ideas)
        {
            float surfaceTotal = 0f;
            float extremityTotal = 0f;

            foreach (
                IdeaSurfaceExtremityEvaluation idea
                in ideas)
            {
                surfaceTotal +=
                    idea.SurfaceContribution;

                extremityTotal +=
                    idea.Extremity;
            }

            float count =
                ideas.Length;

            return new TrackSurfaceExtremityEvaluation(
                "TRACK",
                count == 0
                    ? 0f
                    : surfaceTotal / count,
                count == 0
                    ? 0f
                    : extremityTotal / count,
                ideas
            );
        }

        private static
            IdeaSurfaceExtremityEvaluation
            Idea(
                string id,
                int index,
                float surface,
                float extremity)
        {
            return new IdeaSurfaceExtremityEvaluation(
                id,
                index,
                surface,
                extremity
            );
        }
    }
}