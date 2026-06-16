using NUnit.Framework;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Gestation.Tests
{
    public class QuestingProjectionResolverTests
    {
        private QuestingProjectionResolver resolver;

        [SetUp]
        public void SetUp()
        {
            resolver = new QuestingProjectionResolver();
        }

        [Test]
        public void Resolve_RedReferenceCase_ReturnsStableNegativeDegreeThree()
        {
            QuestingRequest request = new QuestingRequest(
                TagAxis.Existential,
                moodValue: -3.0f,
                compositeValue: -3.25f,
                sceneSynchronisation: 1.0f
            );

            QuestingResult result = resolver.Resolve(request);

            Assert.That(result.Slope, Is.EqualTo(-0.25f).Within(0.0001f));
            Assert.That(result.ProjectedValue, Is.EqualTo(-3.5f).Within(0.0001f));

            Assert.That(result.OutputPolarity, Is.EqualTo(QuestingPolarity.Negative));
            Assert.That(result.OutputDegree, Is.EqualTo(3));

            Assert.That(result.IsStable, Is.True);
            Assert.That(result.IsInterrupted, Is.False);
            Assert.That(result.ProducesTransientTag, Is.True);
            Assert.That(
                result.InterruptionCause,
                Is.EqualTo(QuestingInterruptionCause.None)
            );
        }

        [Test]
        public void Resolve_WhenMoodEqualsComposite_ReturnsFlatProjection()
        {
            QuestingRequest request = new QuestingRequest(
                TagAxis.Symbolic,
                moodValue: 2.0f,
                compositeValue: 2.0f,
                sceneSynchronisation: 0.25f
            );

            QuestingResult result = resolver.Resolve(request);

            Assert.That(result.Slope, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(result.ProjectedValue, Is.EqualTo(2.0f).Within(0.0001f));

            Assert.That(result.OutputPolarity, Is.EqualTo(QuestingPolarity.Positive));
            Assert.That(result.OutputDegree, Is.EqualTo(2));

            Assert.That(result.IsStable, Is.True);
            Assert.That(result.ProducesTransientTag, Is.True);
        }

        [Test]
        public void Resolve_WhenProjectionIsZero_ReturnsStableNeutralWithoutTag()
        {
            QuestingRequest request = new QuestingRequest(
                TagAxis.Expressive,
                moodValue: 0.0f,
                compositeValue: 0.0f,
                sceneSynchronisation: 1.0f
            );

            QuestingResult result = resolver.Resolve(request);

            Assert.That(result.Slope, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(result.ProjectedValue, Is.EqualTo(0.0f).Within(0.0001f));

            Assert.That(result.OutputPolarity, Is.EqualTo(QuestingPolarity.Neutral));
            Assert.That(result.OutputDegree, Is.Null);

            Assert.That(result.IsStable, Is.True);
            Assert.That(result.IsInterrupted, Is.False);
            Assert.That(result.ProducesTransientTag, Is.False);
        }

        [Test]
        public void Resolve_WhenProjectionReachesOuterBoundary_ReturnsInterruption()
        {
            QuestingRequest request = new QuestingRequest(
                TagAxis.Symbolic,
                moodValue: 2.0f,
                compositeValue: -1.0f,
                sceneSynchronisation: 0.5f
            );

            QuestingResult result = resolver.Resolve(request);

            Assert.That(result.Slope, Is.EqualTo(-6.0f).Within(0.0001f));
            Assert.That(result.ProjectedValue, Is.EqualTo(-10.0f).Within(0.0001f));

            Assert.That(result.OutputPolarity, Is.EqualTo(QuestingPolarity.Negative));
            Assert.That(result.OutputDegree, Is.Null);

            Assert.That(result.IsStable, Is.False);
            Assert.That(result.IsInterrupted, Is.True);
            Assert.That(result.ProducesTransientTag, Is.False);
            Assert.That(
                result.InterruptionCause,
                Is.EqualTo(
                    QuestingInterruptionCause.ProjectionBoundaryExceeded
                )
            );
        }

        [TestCase(0.01f, 0)]
        [TestCase(0.99f, 0)]
        [TestCase(1.00f, 1)]
        [TestCase(1.99f, 1)]
        [TestCase(2.00f, 2)]
        [TestCase(2.99f, 2)]
        [TestCase(3.00f, 3)]
        [TestCase(3.99f, 3)]
        public void Resolve_StablePositiveProjection_UsesFlooredMagnitude(
            float projectedValue,
            int expectedDegree)
        {
            // With m = 0 and d = 1:
            // q = 0 + 2c, so c = q / 2.
            QuestingRequest request = new QuestingRequest(
                TagAxis.Expressive,
                moodValue: 0.0f,
                compositeValue: projectedValue / 2.0f,
                sceneSynchronisation: 1.0f
            );

            QuestingResult result = resolver.Resolve(request);

            Assert.That(
                result.ProjectedValue,
                Is.EqualTo(projectedValue).Within(0.0001f)
            );

            Assert.That(result.OutputDegree, Is.EqualTo(expectedDegree));
            Assert.That(result.IsInterrupted, Is.False);
            Assert.That(result.ProducesTransientTag, Is.True);
        }

        [TestCase(4.0f)]
        [TestCase(-4.0f)]
        public void Resolve_ProjectionExactlyAtBoundary_IsInterrupted(
            float projectedValue)
        {
            QuestingRequest request = new QuestingRequest(
                TagAxis.Existential,
                moodValue: 0.0f,
                compositeValue: projectedValue / 2.0f,
                sceneSynchronisation: 1.0f
            );

            QuestingResult result = resolver.Resolve(request);

            Assert.That(
                result.ProjectedValue,
                Is.EqualTo(projectedValue).Within(0.0001f)
            );

            Assert.That(result.IsInterrupted, Is.True);
            Assert.That(result.OutputDegree, Is.Null);
            Assert.That(result.ProducesTransientTag, Is.False);
        }

        [Test]
        public void Resolve_LowerSynchronisation_IncreasesMismatchMagnitude()
        {
            QuestingRequest highSynchronisationRequest = new QuestingRequest(
                TagAxis.Symbolic,
                moodValue: 1.0f,
                compositeValue: 2.0f,
                sceneSynchronisation: 1.0f
            );

            QuestingRequest lowSynchronisationRequest = new QuestingRequest(
                TagAxis.Symbolic,
                moodValue: 1.0f,
                compositeValue: 2.0f,
                sceneSynchronisation: 0.5f
            );

            QuestingResult highSynchronisationResult =
                resolver.Resolve(highSynchronisationRequest);

            QuestingResult lowSynchronisationResult =
                resolver.Resolve(lowSynchronisationRequest);

            Assert.That(
                System.Math.Abs(lowSynchronisationResult.ProjectedValue),
                Is.GreaterThan(
                    System.Math.Abs(
                        highSynchronisationResult.ProjectedValue
                    )
                )
            );
        }

        [Test]
        public void Resolve_WhenSynchronisationIsZero_Throws()
        {
            QuestingRequest request = new QuestingRequest(
                TagAxis.Symbolic,
                moodValue: 1.0f,
                compositeValue: 1.0f,
                sceneSynchronisation: 0.0f
            );

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => resolver.Resolve(request)
            );
        }

        [Test]
        public void Resolve_WhenAxisIsOutsideVerticalSlice_Throws()
        {
            QuestingRequest request = new QuestingRequest(
                TagAxis.Emotional,
                moodValue: 1.0f,
                compositeValue: 1.0f,
                sceneSynchronisation: 1.0f
            );

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => resolver.Resolve(request)
            );
        }
    }
}