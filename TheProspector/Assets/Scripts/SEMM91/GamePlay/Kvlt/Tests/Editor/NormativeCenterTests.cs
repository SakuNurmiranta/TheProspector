using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class NormativeCentreTests
    {
        [Test]
        public void NeutralCentre_ReturnsZeroAffinity()
        {
            NormativeCentre centre =
                NormativeCentre.Neutral;

            Assert.That(
                centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive
                ),
                Is.EqualTo(0f)
            );

            Assert.That(
                centre.NonZeroAffinityCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void SameTag_CanHaveDifferentAffinityByRecordedDegree()
        {
            NormativeCentre centre =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Weak,
                            0f
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant,
                            0.5f
                        ),

                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Transgressive,
                            1f
                        )
                    }
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
        public void AffinityOutsideAllowedRange_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    new NormativeAffinityEntry(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak,
                        1.01f
                    )
            );

            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    new NormativeAffinityEntry(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak,
                        -1.01f
                    )
            );
        }

        [Test]
        public void DuplicateAffinityKey_IsRejected()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new NormativeCentre(
                        new[]
                        {
                            new NormativeAffinityEntry(
                                TagAxis.Symbolic,
                                TagPole.Negative,
                                TagDegree.Weak,
                                0.5f
                            ),

                            new NormativeAffinityEntry(
                                TagAxis.Symbolic,
                                TagPole.Negative,
                                TagDegree.Weak,
                                1f
                            )
                        }
                    )
            );
        }

        [Test]
        public void SeededWorldState_CanReplaceSettledNormativeCentreSnapshot()
        {
            SeededWorldState world =
                new SeededWorldState(
                    new CollectiveRegistry()
                );

            Assert.That(
                world.KvltNormativeCentre,
                Is.SameAs(
                    NormativeCentre.Neutral
                )
            );

            NormativeCentre settledCentre =
                new NormativeCentre(
                    new[]
                    {
                        new NormativeAffinityEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            TagDegree.Dominant,
                            0.5f
                        )
                    }
                );

            world.ApplySettledNormativeCentre(
                settledCentre
            );

            Assert.That(
                world.KvltNormativeCentre,
                Is.SameAs(settledCentre)
            );

            Assert.That(
                world.KvltNormativeCentre
                    .GetAffinity(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Dominant
                    ),
                Is.EqualTo(0.5f)
            );
        }
    }
}