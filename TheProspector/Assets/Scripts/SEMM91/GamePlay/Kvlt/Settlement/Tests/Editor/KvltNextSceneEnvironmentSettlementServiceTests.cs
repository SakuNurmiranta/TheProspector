using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;

namespace SEMM91.GamePlay.Kvlt.Settlement.Tests.Editor
{
    public sealed class
        KvltNextSceneEnvironmentSettlementServiceTests
    {
        private const string SceneId =
            "KVLT";

        private const int Turn =
            8;

        private const int NextTurn =
            9;

        private readonly
            KvltNextSceneEnvironmentSettlementService
            service =
                new();

        [Test]
        public void
            EnvironmentCannotBeDerivedWhileFetteredReleaseStillAwaitsIngress()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                new(
                    "Awaiting Entry",
                    demo.DemoTapeId,
                    "OWNER",
                    SceneId,
                    Turn,
                    1f
                );

            Assert.That(
                release.TryFetter(
                    Turn
                ),
                Is.True
            );

            Assert.That(
                release.HasFieldPosition,
                Is.False
            );

            KvltNextTurnIngressSettlementResult
                nominalIngressPhase =
                    EmptyIngressResult();

            Assert.Throws<
                InvalidOperationException>(
                () =>
                    service.Settle(
                        SceneId,
                        new CanonState(),
                        new[]
                        {
                            release
                        },
                        new[]
                        {
                            demo
                        },
                        nominalIngressPhase,
                        PressurePolicy(),
                        NormativePolicy()
                    )
            );
        }

        [Test]
        public void
            IngressedReleaseContributesToPublishedNextTurnPressureAndNormativeCentre()
        {
            DemoTape demo =
                Demo();

            SceneRelease release =
                new(
                    "Resident",
                    demo.DemoTapeId,
                    "OWNER",
                    SceneId,
                    Turn,
                    1f
                );

            Assert.That(
                release.TryFetter(
                    Turn
                ),
                Is.True
            );

            Assert.That(
                release.TryEstablishFieldPosition(
                    0.20f,
                    NextTurn
                ),
                Is.True
            );

            KvltNextSceneEnvironmentSettlementResult
                result =
                    service.Settle(
                        SceneId,
                        new CanonState(),
                        new[]
                        {
                            release
                        },
                        new[]
                        {
                            demo
                        },
                        EmptyIngressResult(),
                        PressurePolicy(),
                        NormativePolicy()
                    );

            Assert.That(
                result.CompletedTurn,
                Is.EqualTo(
                    Turn
                )
            );

            Assert.That(
                result.PublishedTurn,
                Is.EqualTo(
                    NextTurn
                )
            );

            Assert.That(
                result.Pressure.SettledTurn,
                Is.EqualTo(
                    NextTurn
                )
            );

            /*
             * One Symbolic/Negative Weak solitary
             * occurrence contributes raw +1.
             */
            Assert.That(
                result.Pressure.Pressure
                    .GetRawPressure(
                        TagAxis.Symbolic,
                        TagPole.Negative
                    ),
                Is.EqualTo(1f)
            );

            /*
             * Empty Canon:
             *
             * raw +1
             * × neutral scale .25
             * = effective +.25.
             */
            Assert.That(
                result.Pressure.Pressure
                    .GetEffectivePressure(
                        TagAxis.Symbolic,
                        TagPole.Negative
                    ),
                Is.EqualTo(0.25f)
                    .Within(0.0001f)
            );

            /*
             * Empty Canon baseline = 0.
             *
             * Pressure target = +.5.
             * effective strength = .25.
             *
             * 0 → .5 at 25% = .125.
             */
            Assert.That(
                result.NormativeCentre.Centre
                    .GetAffinity(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak
                    ),
                Is.EqualTo(0.125f)
                    .Within(0.0001f)
            );

            Assert.That(
                result.CanonNormativeCentre
                    .GetAffinity(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak
                    ),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void
            FinalCanonIsImmediatelyPresentInPublishedNextTurnCentre()
        {
            CanonState canon =
                new();

            Assert.That(
                canon.TryRecordPrecedent(
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    CanonProvenanceKind.SceneRelease,
                    "CANON_RELEASE",
                    establishedTurn:
                        Turn
                ),
                Is.True
            );

            KvltNextSceneEnvironmentSettlementResult
                result =
                    service.Settle(
                        SceneId,
                        canon,
                        Array.Empty<SceneRelease>(),
                        Array.Empty<DemoTape>(),
                        EmptyIngressResult(),
                        PressurePolicy(),
                        NormativePolicy()
                    );

            Assert.That(
                result.Pressure
                    .SourceContributionCount,
                Is.EqualTo(0)
            );

            Assert.That(
                result.CanonNormativeCentre
                    .GetAffinity(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak
                    ),
                Is.EqualTo(1f)
            );

            Assert.That(
                result.NormativeCentre.Centre
                    .GetAffinity(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak
                    ),
                Is.EqualTo(1f)
            );

            /*
             * A winter Canon breakthrough therefore
             * exists in the state published for the
             * immediately following Spring.
             */
            Assert.That(
                result.NormativeCentre.SettledTurn,
                Is.EqualTo(
                    NextTurn
                )
            );
        }

        private static
            KvltNextTurnIngressSettlementResult
            EmptyIngressResult()
        {
            return new
                KvltNextTurnIngressSettlementResult(
                    SceneId,
                    Turn,
                    NextTurn,
                    Array.Empty<
                        SceneReleaseIngressEvaluation>()
                );
        }

        private static
            ScenePressureRebuildPolicy
            PressurePolicy()
        {
            return new ScenePressureRebuildPolicy(
                neutralDirectionScale:
                    0.25f,
                counterCanonicalScale:
                    0.50f,
                maxAbsoluteEffectivePressure:
                    0.75f
            );
        }

        private static
            NormativePressureBlendPolicy
            NormativePolicy()
        {
            return new NormativePressureBlendPolicy(
                provisionalAffinityCeiling:
                    0.50f
            );
        }

        private static DemoTape Demo()
        {
            DemoTapeIdeaSnapshot idea =
                new(
                    "IDEA",
                    0,
                    "ASPECT",
                    IdeaPayloadType.SingleTag,
                    "AUTHOR",
                    TagContainerType.Transient,
                    1f,
                    new[]
                    {
                        new
                            DemoTapeTagOccurrenceSnapshot(
                                TagAxis.Symbolic,
                                TagPole.Negative,
                                TagDegree.Weak,
                                DemoTapeTagOccurrenceRole
                                    .Solitary
                            )
                    }
                );

            DemoTapeTrackSnapshot track =
                new(
                    "TRACK",
                    "Track",
                    1f,
                    1f,
                    new[]
                    {
                        idea
                    }
                );

            return new DemoTape(
                "DEMO",
                "Demo",
                "SET",
                "Set",
                1,
                1,
                1f,
                new[]
                {
                    track
                }
            );
        }
    }
}