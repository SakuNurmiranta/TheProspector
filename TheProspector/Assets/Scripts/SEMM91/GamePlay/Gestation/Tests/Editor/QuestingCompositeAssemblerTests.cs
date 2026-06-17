using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Gestation.Questing.Tests
{
    public class QuestingCompositeAssemblerTests
    {
        [Test]
        public void Build_ProvidersAreAppliedInRegistrationOrder()
        {
            QuestingCompositeContribution first =
                CreateContribution(
                    "provider:first",
                    -0.5f
                );

            QuestingCompositeContribution second =
                CreateContribution(
                    "provider:second",
                    -0.75f
                );

            StubProvider firstProvider =
                new StubProvider(first);

            StubProvider secondProvider =
                new StubProvider(second);

            QuestingCompositeAssembler assembler =
                CreateAssembler(
                    firstProvider,
                    secondProvider
                );

            QuestingCompositeBuildResult result =
                assembler.Build(CreateContext());

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

            Assert.That(result.RawValue, Is.EqualTo(-1.25f));
        }

        [Test]
        public void Build_ForwardsSameContextToEveryProvider()
        {
            StubProvider firstProvider = new StubProvider();
            StubProvider secondProvider = new StubProvider();

            QuestingCompositeAssembler assembler =
                CreateAssembler(
                    firstProvider,
                    secondProvider
                );

            QuestingCompositeContext context =
                CreateContext();

            assembler.Build(context);

            Assert.That(
                firstProvider.LastContext,
                Is.SameAs(context)
            );

            Assert.That(
                secondProvider.LastContext,
                Is.SameAs(context)
            );
        }

        [Test]
        public void Build_ProviderOutputUsesBuilderClamping()
        {
            StubProvider provider =
                new StubProvider(
                    CreateContribution("one", 3.0f),
                    CreateContribution("two", 2.5f)
                );

            QuestingCompositeAssembler assembler =
                CreateAssembler(provider);

            QuestingCompositeBuildResult result =
                assembler.Build(CreateContext());

            Assert.That(result.RawValue, Is.EqualTo(5.5f));

            Assert.That(
                result.CompositeValue,
                Is.EqualTo(4.0f)
            );

            Assert.That(result.WasClamped, Is.True);
        }

        [Test]
        public void Build_NoProviders_ReturnsNeutralComposite()
        {
            QuestingCompositeAssembler assembler =
                CreateAssembler();

            QuestingCompositeBuildResult result =
                assembler.Build(CreateContext());

            Assert.That(result.RawValue, Is.EqualTo(0.0f));

            Assert.That(
                result.CompositeValue,
                Is.EqualTo(0.0f)
            );

            Assert.That(
                result.AppliedContributions,
                Is.Empty
            );
        }

        [Test]
        public void Build_ProviderReturnsNull_Throws()
        {
            NullReturningProvider provider =
                new NullReturningProvider();

            QuestingCompositeAssembler assembler =
                CreateAssembler(provider);

            Assert.Throws<InvalidOperationException>(
                () => assembler.Build(CreateContext())
            );
        }

        private static QuestingCompositeAssembler
            CreateAssembler(
                params IQuestingCompositeContributionProvider[]
                    providers)
        {
            return new QuestingCompositeAssembler(
                new QuestingCompositeBuilder(),
                providers
            );
        }

        private static QuestingCompositeContext CreateContext()
        {
            return new QuestingCompositeContext(
                characterId: "Leader_0",
                activeAxis: TagAxis.Symbolic,
                currentGlobalTurn: 4
            );
        }

        private static QuestingCompositeContribution
            CreateContribution(
                string sourceId,
                float value)
        {
            return new QuestingCompositeContribution(
                sourceId: sourceId,
                axis: TagAxis.Symbolic,
                value: value,
                description: "Test contribution"
            );
        }

        private sealed class StubProvider :
            IQuestingCompositeContributionProvider
        {
            private readonly IReadOnlyList
                <QuestingCompositeContribution> _contributions;

            public QuestingCompositeContext LastContext { get; private set; }

            public StubProvider(
                params QuestingCompositeContribution[]
                    contributions)
            {
                _contributions =
                    contributions ??
                    Array.Empty<QuestingCompositeContribution>();
            }

            public IReadOnlyList<QuestingCompositeContribution>
                BuildContributions(
                    QuestingCompositeContext context)
            {
                LastContext = context;
                return _contributions;
            }
        }

        private sealed class NullReturningProvider :
            IQuestingCompositeContributionProvider
        {
            public IReadOnlyList<QuestingCompositeContribution>
                BuildContributions(
                    QuestingCompositeContext context)
            {
                return null;
            }
        }
    }
}