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
    }
}