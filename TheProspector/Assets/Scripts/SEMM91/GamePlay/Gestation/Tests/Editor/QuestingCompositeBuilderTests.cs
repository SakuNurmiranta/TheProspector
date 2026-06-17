using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Gestation.Questing.Tests
{
    public class QuestingCompositeBuilderTests
    {
        private QuestingCompositeBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new QuestingCompositeBuilder();
        }

        [Test]
        public void Build_MatchingContributions_SumsAndPreservesOrder()
        {
            QuestingCompositeContribution first =
                CreateContribution(
                    sourceId: "action:turn1:position1",
                    axis: TagAxis.Symbolic,
                    value: -0.75f
                );

            QuestingCompositeContribution second =
                CreateContribution(
                    sourceId: "event:reaction1",
                    axis: TagAxis.Symbolic,
                    value: -0.50f
                );

            QuestingCompositeBuildResult result =
                builder.Build(
                    TagAxis.Symbolic,
                    new[] { first, second }
                );

            Assert.That(result.RawValue, Is.EqualTo(-1.25f));
            Assert.That(
                result.CompositeValue,
                Is.EqualTo(-1.25f)
            );

            Assert.That(result.WasClamped, Is.False);
            Assert.That(
                result.AppliedContributions.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                result.AppliedContributions[0],
                Is.SameAs(first)
            );

            Assert.That(
                result.AppliedContributions[1],
                Is.SameAs(second)
            );
        }

        [Test]
        public void Build_DifferentAxisContribution_IsNotApplied()
        {
            QuestingCompositeContribution symbolic =
                CreateContribution(
                    sourceId: "event:symbolic",
                    axis: TagAxis.Symbolic,
                    value: -1.0f
                );

            QuestingCompositeContribution existential =
                CreateContribution(
                    sourceId: "event:existential",
                    axis: TagAxis.Existential,
                    value: 2.0f
                );

            QuestingCompositeBuildResult result =
                builder.Build(
                    TagAxis.Symbolic,
                    new[] { symbolic, existential }
                );

            Assert.That(result.RawValue, Is.EqualTo(-1.0f));

            Assert.That(
                result.AppliedContributions.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                result.AppliedContributions[0],
                Is.SameAs(symbolic)
            );
        }

        [Test]
        public void Build_PositiveOverflow_ClampsAndRetainsRawValue()
        {
            QuestingCompositeBuildResult result =
                builder.Build(
                    TagAxis.Expressive,
                    new[]
                    {
                        CreateContribution(
                            "event:one",
                            TagAxis.Expressive,
                            3.0f
                        ),
                        CreateContribution(
                            "event:two",
                            TagAxis.Expressive,
                            2.5f
                        )
                    }
                );

            Assert.That(result.RawValue, Is.EqualTo(5.5f));

            Assert.That(
                result.CompositeValue,
                Is.EqualTo(4.0f)
            );

            Assert.That(result.WasClamped, Is.True);
        }

        [Test]
        public void Build_NegativeOverflow_ClampsAndRetainsRawValue()
        {
            QuestingCompositeBuildResult result =
                builder.Build(
                    TagAxis.Existential,
                    new[]
                    {
                        CreateContribution(
                            "event:one",
                            TagAxis.Existential,
                            -2.5f
                        ),
                        CreateContribution(
                            "event:two",
                            TagAxis.Existential,
                            -3.0f
                        )
                    }
                );

            Assert.That(result.RawValue, Is.EqualTo(-5.5f));

            Assert.That(
                result.CompositeValue,
                Is.EqualTo(-4.0f)
            );

            Assert.That(result.WasClamped, Is.True);
        }

        [Test]
        public void Build_NoMatchingContributions_ReturnsNeutralComposite()
        {
            QuestingCompositeBuildResult result =
                builder.Build(
                    TagAxis.Symbolic,
                    new[]
                    {
                        CreateContribution(
                            "event:emotional",
                            TagAxis.Emotional,
                            2.0f
                        )
                    }
                );

            Assert.That(result.RawValue, Is.EqualTo(0.0f));

            Assert.That(
                result.CompositeValue,
                Is.EqualTo(0.0f)
            );

            Assert.That(
                result.AppliedContributions,
                Is.Empty
            );

            Assert.That(result.WasClamped, Is.False);
        }

        [Test]
        public void Contribution_NonFiniteValue_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new QuestingCompositeContribution(
                    sourceId: "invalid",
                    axis: TagAxis.Symbolic,
                    value: float.NaN,
                    description: "Invalid test value"
                )
            );
        }

        private static QuestingCompositeContribution
            CreateContribution(
                string sourceId,
                TagAxis axis,
                float value)
        {
            return new QuestingCompositeContribution(
                sourceId: sourceId,
                axis: axis,
                value: value,
                description: "Test contribution"
            );
        }
    }
}