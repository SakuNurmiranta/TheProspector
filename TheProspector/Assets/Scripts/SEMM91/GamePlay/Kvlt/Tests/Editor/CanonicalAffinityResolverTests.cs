using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Normative;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class CanonicalAffinityResolverTests
    {
        [Test]
        public void MissingDirectPrecedent_IsNotResolved()
        {
            CanonState canon =
                new CanonState();

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            bool resolved =
                resolver.TryResolveDirectAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    out float affinity
                );

            Assert.That(
                resolved,
                Is.False
            );

            Assert.That(
                affinity,
                Is.EqualTo(0f)
            );
        }

        [TestCase(
            TagDegree.Transgressive,
            TagDegree.Transgressive,
            1f)]
        [TestCase(
            TagDegree.Dominant,
            TagDegree.Transgressive,
            1f)]
        [TestCase(
            TagDegree.Transgressive,
            TagDegree.Dominant,
            0.5f)]
        [TestCase(
            TagDegree.Transgressive,
            TagDegree.Weak,
            0f)]
        [TestCase(
            TagDegree.Transgressive,
            TagDegree.Neutral,
            -0.5f)]
        public void DirectCanon_UsesCanonicalRatchet(
            TagDegree canonicalDegree,
            TagDegree recordedDegree,
            float expectedAffinity)
        {
            CanonState canon =
                CreateCanon(
                    TagPole.Negative,
                    canonicalDegree
                );

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            bool resolved =
                resolver.TryResolveDirectAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    recordedDegree,
                    out float affinity
                );

            Assert.That(
                resolved,
                Is.True
            );

            Assert.That(
                affinity,
                Is.EqualTo(expectedAffinity)
            );
        }

        [Test]
        public void DegreeZeroCanon_IsStillDirectCanonicalPrecedent()
        {
            CanonState canon =
                CreateCanon(
                    TagPole.Negative,
                    TagDegree.Neutral
                );

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            bool resolved =
                resolver.TryResolveDirectAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Neutral,
                    out float affinity
                );

            Assert.That(
                resolved,
                Is.True
            );

            Assert.That(
                affinity,
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void OppositePoleCanon_DoesNotCountAsDirectPrecedent()
        {
            CanonState canon =
                CreateCanon(
                    TagPole.Positive,
                    TagDegree.Transgressive
                );

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            bool resolved =
                resolver.TryResolveDirectAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive,
                    out _
                );

            Assert.That(
                resolved,
                Is.False
            );
        }

        private static CanonState CreateCanon(
            TagPole pole,
            TagDegree degree)
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Symbolic,
                pole,
                degree,
                CanonProvenanceKind.ScenarioSeed,
                $"TEST_{pole}_{degree}"
            );

            return canon;
        }
        
        [Test]
        public void NoDirectPrecedent_CanonicalAntiTagReturnsNegativeOne()
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

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            float affinity =
                resolver.ResolveAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                );

            Assert.That(
                affinity,
                Is.EqualTo(-1f)
            );
        }
        
        [Test]
        public void DirectPrecedent_WinsEvenWhenAntiTagIsAlsoCanonical()
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

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            float affinity =
                resolver.ResolveAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                );

            Assert.That(
                affinity,
                Is.EqualTo(1f)
            );
        }
        
        [Test]
        public void NoDirectOrOpposedPrecedent_AdjacentCanonReturnsHalf()
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Interpretive,
                TagPole.Positive,
                TagDegree.Dominant,
                CanonProvenanceKind.ScenarioSeed,
                "MEANING"
            );

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            float affinity =
                resolver.ResolveAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                );

            Assert.That(
                affinity,
                Is.EqualTo(0.5f)
            );
        }
        
        [Test]
        public void Opposition_WinsWhenCanonAlsoContainsAdjacentTag()
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

            canon.TryRecordPrecedent(
                TagAxis.Interpretive,
                TagPole.Positive,
                TagDegree.Transgressive,
                CanonProvenanceKind.ScenarioSeed,
                "MEANING"
            );

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            float affinity =
                resolver.ResolveAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                );

            Assert.That(
                affinity,
                Is.EqualTo(-1f)
            );
        }
        
        [Test]
        public void MultipleCanonicalAdjacencies_DoNotStack()
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

            canon.TryRecordPrecedent(
                TagAxis.Temporal,
                TagPole.Positive,
                TagDegree.Dominant,
                CanonProvenanceKind.ScenarioSeed,
                "SLOW"
            );

            canon.TryRecordPrecedent(
                TagAxis.Expressive,
                TagPole.Positive,
                TagDegree.Transgressive,
                CanonProvenanceKind.ScenarioSeed,
                "HONED"
            );

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            float affinity =
                resolver.ResolveAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                );

            Assert.That(
                affinity,
                Is.EqualTo(0.5f)
            );
        }
        
        [TestCase(TagDegree.Neutral)]
        [TestCase(TagDegree.Transgressive)]
        public void RelationalOpposition_IgnoresCanonicalDegree(
            TagDegree canonicalDegree)
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Symbolic,
                TagPole.Negative,
                canonicalDegree,
                CanonProvenanceKind.ScenarioSeed,
                $"PROFANE_{canonicalDegree}"
            );

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            Assert.That(
                resolver.ResolveAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                ),
                Is.EqualTo(-1f)
            );
        }
        
        [TestCase(TagDegree.Neutral)]
        [TestCase(TagDegree.Transgressive)]
        public void RelationalAdjacency_IgnoresCanonicalDegree(
            TagDegree canonicalDegree)
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Interpretive,
                TagPole.Positive,
                canonicalDegree,
                CanonProvenanceKind.ScenarioSeed,
                $"MEANING_{canonicalDegree}"
            );

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            Assert.That(
                resolver.ResolveAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                ),
                Is.EqualTo(0.5f)
            );
        }
        
        [Test]
        public void UnrelatedCanon_ReturnsZero()
        {
            CanonState canon =
                new CanonState();

            canon.TryRecordPrecedent(
                TagAxis.Emotional,
                TagPole.Positive,
                TagDegree.Transgressive,
                CanonProvenanceKind.ScenarioSeed,
                "WARM"
            );

            CanonicalAffinityResolver resolver =
                new CanonicalAffinityResolver();

            float affinity =
                resolver.ResolveAffinity(
                    canon,
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                );

            Assert.That(
                affinity,
                Is.EqualTo(0f)
            );
        }
        
        
        
        
        
        
    }
}