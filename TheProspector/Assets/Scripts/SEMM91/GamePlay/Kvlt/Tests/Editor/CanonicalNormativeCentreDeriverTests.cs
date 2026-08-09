using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Normative;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class CanonicalNormativeCentreDeriverTests
    {
        [Test]
        public void EmptyCanon_DerivesNeutralCentre()
        {
            CanonState canon =
                new CanonState();

            CanonicalNormativeCentreDeriver deriver =
                new CanonicalNormativeCentreDeriver();

            NormativeCentre centre =
                deriver.Derive(canon);

            Assert.That(
                centre.NonZeroAffinityCount,
                Is.EqualTo(0)
            );

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive
                ),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void DirectCanon_DerivesDegreeSensitiveRatchet()
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Transgressive,
                CanonProvenanceKind.ScenarioSeed,
                "PROFANE_3"
            );

            CanonicalNormativeCentreDeriver deriver =
                new CanonicalNormativeCentreDeriver();

            NormativeCentre centre =
                deriver.Derive(canon);

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Neutral
                ),
                Is.EqualTo(-0.5f)
            );

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                ),
                Is.EqualTo(0f)
            );

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                ),
                Is.EqualTo(0.5f)
            );

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive
                ),
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void RelationalOpposition_IsDegreeInsensitive()
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Weak,
                CanonProvenanceKind.ScenarioSeed,
                "PROFANE"
            );

            CanonicalNormativeCentreDeriver deriver =
                new CanonicalNormativeCentreDeriver();

            NormativeCentre centre =
                deriver.Derive(canon);

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Neutral
                ),
                Is.EqualTo(-1f)
            );

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                ),
                Is.EqualTo(-1f)
            );

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                ),
                Is.EqualTo(-1f)
            );

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Transgressive
                ),
                Is.EqualTo(-1f)
            );
        }

        [Test]
        public void RelationalAdjacency_IsDegreeInsensitive()
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Interpretive,
                TagPole.Positive,
                TagDegree.Neutral,
                CanonProvenanceKind.ScenarioSeed,
                "MEANING_0"
            );

            CanonicalNormativeCentreDeriver deriver =
                new CanonicalNormativeCentreDeriver();

            NormativeCentre centre =
                deriver.Derive(canon);

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Neutral
                ),
                Is.EqualTo(0.5f)
            );

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Transgressive
                ),
                Is.EqualTo(0.5f)
            );
        }

        [Test]
        public void DirectPrecedent_RemainsAuthoritativeInDerivedCentre()
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Symbolic,
                TagPole.Positive,
                TagDegree.Weak,
                CanonProvenanceKind.ScenarioSeed,
                "SACRED_1"
            );

            canon.TryRecordPrecedent(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Transgressive,
                CanonProvenanceKind.ScenarioSeed,
                "PROFANE_3"
            );

            canon.TryRecordPrecedent(
                TagAxis.Interpretive,
                TagPole.Positive,
                TagDegree.Transgressive,
                CanonProvenanceKind.ScenarioSeed,
                "MEANING_3"
            );

            CanonicalNormativeCentreDeriver deriver =
                new CanonicalNormativeCentreDeriver();

            NormativeCentre centre =
                deriver.Derive(canon);

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                ),
                Is.EqualTo(1f)
            );

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Neutral
                ),
                Is.EqualTo(0.5f)
            );
        }

        [Test]
        public void DerivedCentre_DoesNotChangeWhenCanonLaterChanges()
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Interpretive,
                TagPole.Positive,
                TagDegree.Weak,
                CanonProvenanceKind.ScenarioSeed,
                "MEANING"
            );

            CanonicalNormativeCentreDeriver deriver =
                new CanonicalNormativeCentreDeriver();

            NormativeCentre firstCentre =
                deriver.Derive(canon);

            Assert.That(
                firstCentre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                ),
                Is.EqualTo(0.5f)
            );

            canon.TryRecordPrecedent(
                TagAxis.Symbolic,
                TagPole.Negative,
                TagDegree.Weak,
                CanonProvenanceKind.ScenarioSeed,
                "PROFANE"
            );

            Assert.That(
                firstCentre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                ),
                Is.EqualTo(0.5f),
                "Previously derived Centre must remain frozen."
            );

            NormativeCentre secondCentre =
                deriver.Derive(canon);

            Assert.That(
                secondCentre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                ),
                Is.EqualTo(-1f)
            );
        }
    }
}