using System;
using NUnit.Framework;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Pressure;

namespace SEMM91.GamePlay.Kvlt.Normative.Tests.Editor
{
    public class
        PressureAwareNormativeCentreDeriverTests
    {
        private readonly
            CanonicalNormativeCentreDeriver deriver =
                new();

        [Test]
        public void
            EmptyPressurePreservesCanonOnlyCentre()
        {
            CanonState canon =
                Canon(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive
                );

            NormativeCentre canonOnly =
                deriver.Derive(
                    canon
                );

            NormativeCentreDerivationEvaluation
                pressured =
                    deriver.DeriveWithPressure(
                        canon,
                        Pressure(
                            SettledScenePressure.Empty
                        ),
                        Policy()
                    );

            foreach (
                TagDegree degree
                in Enum.GetValues(
                    typeof(TagDegree)))
            {
                Assert.That(
                    pressured.Centre.GetAffinity(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        degree
                    ),
                    Is.EqualTo(
                        canonOnly.GetAffinity(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            degree
                        )
                    ).Within(0.0001f)
                );
            }
        }

        [Test]
        public void
            NeutralDirectionGetsWeakProvisionalAffinity()
        {
            NormativeCentreDerivationEvaluation
                result =
                    deriver.DeriveWithPressure(
                        new CanonState(),
                        Pressure(
                            TagAxis.Existential,
                            TagPole.Negative,
                            0.75f
                        ),
                        Policy()
                    );

            /*
             * Canon baseline = 0.
             *
             * Pressure target = +0.5.
             * Strength = 0.75.
             *
             * 0 → +0.5 at 75% = +0.375.
             */
            Assert.That(
                result.Centre.GetAffinity(
                    TagAxis.Existential,
                    TagPole.Negative,
                    TagDegree.Dominant
                ),
                Is.EqualTo(0.375f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            PressureCanSoftenOppositionIntoWeakProvisionalAttraction()
        {
            /*
             * Profane is direct Canon.
             *
             * Sacred therefore has relational
             * Canon affinity -1.
             */
            CanonState canon =
                Canon(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                );

            NormativeCentreDerivationEvaluation
                result =
                    deriver.DeriveWithPressure(
                        canon,
                        Pressure(
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            0.75f
                        ),
                        Policy()
                    );

            /*
             * -1 → +0.5 at 75%
             *
             * = +0.125.
             */
            Assert.That(
                result.Centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                ),
                Is.EqualTo(0.125f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            PositivePressureCannotAmplifyDirectCanon()
        {
            CanonState canon =
                Canon(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            NormativeCentreDerivationEvaluation
                result =
                    deriver.DeriveWithPressure(
                        canon,
                        Pressure(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            0.75f
                        ),
                        Policy()
                    );

            /*
             * Defensive proof even if malformed or
             * externally manufactured Pressure reaches
             * this layer.
             *
             * Direct Canon remains +1.
             */
            Assert.That(
                result.Centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                ),
                Is.EqualTo(1f)
            );
        }

        [Test]
        public void
            CounterPressureWeakensPositiveDirectCanonWithoutInvertingIt()
        {
            CanonState canon =
                Canon(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                );

            NormativeCentreDerivationEvaluation
                result =
                    deriver.DeriveWithPressure(
                        canon,
                        Pressure(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            -0.75f
                        ),
                        Policy()
                    );

            /*
             * Direct affinity +1.
             *
             * Counter-pressure on direct Canon moves
             * toward 0, not toward negative provisional
             * affinity.
             *
             * +1 → 0 at 75% = +0.25.
             */
            Assert.That(
                result.Centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                ),
                Is.EqualTo(0.25f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            CanonicalRatchetRemainsDegreeSensitiveUnderCounterPressure()
        {
            CanonState canon =
                Canon(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive
                );

            NormativeCentreDerivationEvaluation
                result =
                    deriver.DeriveWithPressure(
                        canon,
                        Pressure(
                            TagAxis.Symbolic,
                            TagPole.Negative,
                            -0.50f
                        ),
                        Policy()
                    );

            /*
             * Canon3 baselines:
             *
             * degree3 = +1
             * degree2 = +0.5
             * degree1 =  0
             * degree0 = -0.5
             *
             * With -0.5 counter-pressure:
             *
             * positive baselines move halfway to 0.
             *
             * neutral/negative baselines move halfway
             * toward -1.
             */
            Assert.That(
                result.Centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Transgressive
                ),
                Is.EqualTo(0.50f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.Centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                ),
                Is.EqualTo(0.25f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.Centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak
                ),
                Is.EqualTo(-0.50f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.Centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Neutral
                ),
                Is.EqualTo(-0.75f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            SameDirectionPressureDoesNotAmplifyAdjacentAffinityBeyondItsWeakCeiling()
        {
            /*
             * Existing Camp-1 adjacency calibration:
             *
             * Interpretive Positive Canon
             * makes Symbolic Positive adjacent +0.5.
             */
            CanonState canon =
                Canon(
                    TagAxis.Interpretive,
                    TagPole.Positive,
                    TagDegree.Weak
                );

            NormativeCentreDerivationEvaluation
                result =
                    deriver.DeriveWithPressure(
                        canon,
                        Pressure(
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            0.75f
                        ),
                        Policy()
                    );

            /*
             * Canon adjacency already equals the
             * provisional Pressure target +0.5.
             *
             * Pressure therefore cannot elevate it.
             */
            Assert.That(
                result.Centre.GetAffinity(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Dominant
                ),
                Is.EqualTo(0.50f)
                    .Within(0.0001f)
            );
        }

        [Test]
        public void
            DerivationPreservesCanonicalPressureAndFinalAffinityProvenance()
        {
            CanonState canon =
                Canon(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Dominant
                );

            NormativeCentreDerivationEvaluation
                result =
                    deriver.DeriveWithPressure(
                        canon,
                        Pressure(
                            TagAxis.Symbolic,
                            TagPole.Positive,
                            0.75f,
                            settledTurn: 8
                        ),
                        Policy()
                    );

            Assert.That(
                result.SceneId,
                Is.EqualTo("KVLT")
            );

            Assert.That(
                result.SettledTurn,
                Is.EqualTo(8)
            );

            Assert.That(
                result.TryGetEntry(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak,
                    out
                        NormativeCentreDerivationEntry
                        entry
                ),
                Is.True
            );

            Assert.That(
                entry.HasDirectCanon,
                Is.False
            );

            Assert.That(
                entry.CanonicalAffinity,
                Is.EqualTo(-1f)
            );

            Assert.That(
                entry.EffectivePressure,
                Is.EqualTo(0.75f)
            );

            Assert.That(
                entry.FinalAffinity,
                Is.EqualTo(0.125f)
                    .Within(0.0001f)
            );

            Assert.That(
                entry.PressureChangedAffinity,
                Is.True
            );

            Assert.That(
                result.Centre.GetAffinity(
                    entry.Axis,
                    entry.Pole,
                    entry.RecordedDegree
                ),
                Is.EqualTo(
                    entry.FinalAffinity
                ).Within(0.0001f)
            );
        }

        private static
            NormativePressureBlendPolicy
            Policy()
        {
            /*
             * Test calibration only.
             *
             * Scenario Profile owns the production
             * default later.
             */
            return new NormativePressureBlendPolicy(
                provisionalAffinityCeiling: 0.50f
            );
        }

        private static
            ScenePressureRebuildEvaluation
            Pressure(
                SettledScenePressure pressure,
                int settledTurn = 6)
        {
            return new
                ScenePressureRebuildEvaluation(
                    "KVLT",
                    settledTurn,
                    pressure,
                    Array.Empty<
                        ScenePressureSourceContribution>()
                );
        }

        private static
            ScenePressureRebuildEvaluation
            Pressure(
                TagAxis axis,
                TagPole pole,
                float effectivePressure,
                int settledTurn = 6)
        {
            return Pressure(
                new SettledScenePressure(
                    new[]
                    {
                        new ScenePressureEntry(
                            axis,
                            pole,
                            rawPressure:
                                effectivePressure,
                            effectivePressure:
                                effectivePressure
                        )
                    }
                ),
                settledTurn
            );
        }

        private static CanonState Canon(
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            CanonState canon =
                new();

            Assert.That(
                canon.TryRecordPrecedent(
                    axis,
                    pole,
                    degree,
                    CanonProvenanceKind
                        .ScenarioSeed,
                    "CANON"
                ),
                Is.True
            );

            return canon;
        }
    }
}