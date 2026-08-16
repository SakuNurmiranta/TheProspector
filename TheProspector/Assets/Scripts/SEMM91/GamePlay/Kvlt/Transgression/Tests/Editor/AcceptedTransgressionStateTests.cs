using NUnit.Framework;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Kvlt.Transgression.Tests
{
    public class AcceptedTransgressionStateTests
    {
        [Test]
        public void NoPrecedent_CoversNothingIncludingDegreeZero()
        {
            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            Assert.That(
                state.IsCovered(
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Neutral
                ),
                Is.False
            );

            Assert.That(
                state.IsCovered(
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                ),
                Is.False
            );
        }

        [Test]
        public void AcceptedDegreeTwo_CoversSamePraxisThroughDegreeTwoOnly()
        {
            AcceptedTransgressionState state =
                CreateStateWith(
                    Record(
                        "AT",
                        "CHURCH_ARSON",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            Assert.That(
                state.IsCovered(
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Neutral
                ),
                Is.True
            );

            Assert.That(
                state.IsCovered(
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                ),
                Is.True
            );

            Assert.That(
                state.IsCovered(
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                ),
                Is.True
            );

            Assert.That(
                state.IsCovered(
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive
                ),
                Is.False
            );
        }

        [Test]
        public void CoverageRequiresExactBehaviorType()
        {
            AcceptedTransgressionState state =
                CreateStateWith(
                    Record(
                        "AT",
                        "CHURCH_ARSON",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive
                    )
                );

            Assert.That(
                state.IsCovered(
                    "SELF_CUTTING",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                ),
                Is.False
            );
        }

        [Test]
        public void CoverageRequiresExactTagDirection()
        {
            AcceptedTransgressionState state =
                CreateStateWith(
                    Record(
                        "AT",
                        "CHURCH_ARSON",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Transgressive
                    )
                );

            Assert.That(
                state.IsCovered(
                    "CHURCH_ARSON",
                    TagAxis.Physical,
                    TagPole.Negative,
                    TagDegree.Weak
                ),
                Is.False
            );

            Assert.That(
                state.IsCovered(
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                ),
                Is.False
            );
        }

        [Test]
        public void DegreeZeroPrecedent_IsRealPrecedentRatherThanAutomaticRule()
        {
            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            AcceptedTransgressionRecord record =
                Record(
                    "AT_ZERO",
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Neutral
                );

            Assert.That(
                state.TryAccept(record),
                Is.True
            );

            Assert.That(
                state.IsCovered(
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Neutral
                ),
                Is.True
            );

            Assert.That(
                state.IsCovered(
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                ),
                Is.False
            );
        }

        [Test]
        public void HigherAcceptance_RaisesCurrentCeilingAndPreservesHistory()
        {
            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            AcceptedTransgressionRecord first =
                Record(
                    "AT_ONE",
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    turn: 1
                );

            AcceptedTransgressionRecord raised =
                Record(
                    "AT_TWO",
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    turn: 5
                );

            Assert.That(
                state.TryAccept(first),
                Is.True
            );

            Assert.That(
                state.TryAccept(raised),
                Is.True
            );

            Assert.That(
                state.History.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                state.TryGetCurrent(
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    out AcceptedTransgressionRecord
                        current
                ),
                Is.True
            );

            Assert.That(
                current,
                Is.SameAs(raised)
            );

            Assert.That(
                current.AcceptedDegree,
                Is.EqualTo(
                    TagDegree.Dominant
                )
            );
        }

        [Test]
        public void EqualOrLowerAcceptance_DoesNotDuplicateOrRegressPrecedent()
        {
            AcceptedTransgressionState state =
                CreateStateWith(
                    Record(
                        "AT_TWO",
                        "CHURCH_ARSON",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                );

            Assert.That(
                state.TryAccept(
                    Record(
                        "AT_EQUAL",
                        "CHURCH_ARSON",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant,
                        turn: 2
                    )
                ),
                Is.False
            );

            Assert.That(
                state.TryAccept(
                    Record(
                        "AT_LOWER",
                        "CHURCH_ARSON",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak,
                        turn: 3
                    )
                ),
                Is.False
            );

            Assert.That(
                state.History.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void IndependentPraxisKeys_MaintainIndependentCeilings()
        {
            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            Assert.That(
                state.TryAccept(
                    Record(
                        "AT_ARSON",
                        "CHURCH_ARSON",
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    )
                ),
                Is.True
            );

            Assert.That(
                state.TryAccept(
                    Record(
                        "AT_CUTTING",
                        "SELF_CUTTING",
                        TagAxis.Physical,
                        TagPole.Negative,
                        TagDegree.Weak
                    )
                ),
                Is.True
            );

            Assert.That(
                state.IsCovered(
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                ),
                Is.True
            );

            Assert.That(
                state.IsCovered(
                    "SELF_CUTTING",
                    TagAxis.Physical,
                    TagPole.Negative,
                    TagDegree.Dominant
                ),
                Is.False
            );
        }

        [Test]
        public void CoveringLookup_ReturnsExactProvenanceRecord()
        {
            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            AcceptedTransgressionRecord precedent =
                new AcceptedTransgressionRecord(
                    "AT_CRISIS_17",
                    "KVLT",
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    7,
                    AcceptedTransgressionSourceKind
                        .AllegianceCrisis,
                    "CRISIS_17"
                );

            Assert.That(
                state.TryAccept(precedent),
                Is.True
            );

            Assert.That(
                state.TryGetCoveringPrecedent(
                    "CHURCH_ARSON",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    out AcceptedTransgressionRecord
                        covering
                ),
                Is.True
            );

            Assert.That(
                covering,
                Is.SameAs(precedent)
            );

            Assert.That(
                covering.SourceKind,
                Is.EqualTo(
                    AcceptedTransgressionSourceKind
                        .AllegianceCrisis
                )
            );

            Assert.That(
                covering.SourceId,
                Is.EqualTo("CRISIS_17")
            );
        }

        [Test]
        public void ScenarioSeedAndCrisisUseSameCoverageMechanism()
        {
            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            AcceptedTransgressionRecord seed =
                new AcceptedTransgressionRecord(
                    "AT_SEED",
                    "KVLT",
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    0,
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed,
                    "DEFAULT_SCENARIO"
                );

            Assert.That(
                state.TryAccept(seed),
                Is.True
            );

            Assert.That(
                state.IsCovered(
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                ),
                Is.True
            );

            AcceptedTransgressionRecord crisis =
                new AcceptedTransgressionRecord(
                    "AT_CRISIS",
                    "KVLT",
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant,
                    6,
                    AcceptedTransgressionSourceKind
                        .AllegianceCrisis,
                    "CRISIS_6"
                );

            Assert.That(
                state.TryAccept(crisis),
                Is.True
            );

            Assert.That(
                state.IsCovered(
                    "PUBLIC_PERFORMANCE",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                ),
                Is.True
            );

            Assert.That(
                state.History[0].SourceKind,
                Is.EqualTo(
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed
                )
            );

            Assert.That(
                state.History[1].SourceKind,
                Is.EqualTo(
                    AcceptedTransgressionSourceKind
                        .AllegianceCrisis
                )
            );
        }

        private static
            AcceptedTransgressionState
            CreateStateWith(
                AcceptedTransgressionRecord record)
        {
            AcceptedTransgressionState state =
                new AcceptedTransgressionState(
                    "KVLT"
                );

            Assert.That(
                state.TryAccept(record),
                Is.True
            );

            return state;
        }

        private static
            AcceptedTransgressionRecord
            Record(
                string id,
                string behaviorTypeId,
                TagAxis axis,
                TagPole pole,
                TagDegree degree,
                int turn = 1)
        {
            return new AcceptedTransgressionRecord(
                id,
                "KVLT",
                behaviorTypeId,
                axis,
                pole,
                degree,
                turn,
                AcceptedTransgressionSourceKind
                    .ScenarioSeed,
                "TEST_SCENARIO"
            );
        }
    }
}