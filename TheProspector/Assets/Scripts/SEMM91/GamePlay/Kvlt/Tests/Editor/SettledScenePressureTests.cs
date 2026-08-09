using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Tests.Editor
{
    public class SettledScenePressureTests
    {
        [Test]
        public void EmptyPressure_ReturnsZeroForRawAndEffective()
        {
            SettledScenePressure pressure =
                SettledScenePressure.Empty;

            Assert.That(
                pressure.EntryCount,
                Is.EqualTo(0)
            );

            Assert.That(
                pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(0f)
            );

            Assert.That(
                pressure.GetEffectivePressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void Pressure_PreservesSignedRawAndEffectiveValues()
        {
            SettledScenePressure pressure =
                new SettledScenePressure(
                    new[]
                    {
                        new ScenePressureEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            2f,
                            1.25f
                        ),

                        new ScenePressureEntry(
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            -1f,
                            -0.5f
                        )
                    }
                );

            Assert.That(
                pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(2f)
            );

            Assert.That(
                pressure.GetEffectivePressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(1.25f)
            );

            Assert.That(
                pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.EqualTo(-1f)
            );

            Assert.That(
                pressure.GetEffectivePressure(
                    TagAxis.Symbolic,
                    TagPole.Positive
                ),
                Is.EqualTo(-0.5f)
            );
        }

        [Test]
        public void RawPressure_IsRetainedWhenEffectivePressureIsZero()
        {
            SettledScenePressure pressure =
                new SettledScenePressure(
                    new[]
                    {
                        new ScenePressureEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            3f,
                            0f
                        )
                    }
                );

            Assert.That(
                pressure.EntryCount,
                Is.EqualTo(1)
            );

            Assert.That(
                pressure.GetRawPressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(3f)
            );

            Assert.That(
                pressure.GetEffectivePressure(
                    TagAxis.Symbolic,
                    TagPole.Negative
                ),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void CompletelyZeroEntry_IsNotStored()
        {
            SettledScenePressure pressure =
                new SettledScenePressure(
                    new[]
                    {
                        new ScenePressureEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            0f,
                            0f
                        )
                    }
                );

            Assert.That(
                pressure.EntryCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void DuplicateTagKey_IsRejected()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new SettledScenePressure(
                        new[]
                        {
                            new ScenePressureEntry(
                                TagAxis.Symbolic,
                                TagPole.Negative,
                                1f,
                                1f
                            ),

                            new ScenePressureEntry(
                                TagAxis.Symbolic,
                                TagPole.Negative,
                                2f,
                                2f
                            )
                        }
                    )
            );
        }

        [Test]
        public void NonFinitePressure_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    new ScenePressureEntry(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        float.NaN,
                        0f
                    )
            );

            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    new ScenePressureEntry(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        0f,
                        float.PositiveInfinity
                    )
            );
        }

        [Test]
        public void SeededWorldState_CanReplaceSettledPressureSnapshot()
        {
            SeededWorldState world =
                new SeededWorldState(
                    new CollectiveRegistry()
                );

            Assert.That(
                world.KvltScenePressure,
                Is.SameAs(
                    SettledScenePressure.Empty
                )
            );

            SettledScenePressure settled =
                new SettledScenePressure(
                    new[]
                    {
                        new ScenePressureEntry(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            2f,
                            0.75f
                        )
                    }
                );

            world.ApplySettledScenePressure(
                settled
            );

            Assert.That(
                world.KvltScenePressure,
                Is.SameAs(settled)
            );

            Assert.That(
                world.KvltScenePressure
                    .GetEffectivePressure(
                        TagAxis.Symbolic,
                        TagPole.Negative
                    ),
                Is.EqualTo(0.75f)
            );
        }
    }
}