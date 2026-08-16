using System;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Converts currently implemented movement
    /// policies into the complete frozen movement
    /// evaluation for one SceneRelease.
    ///
    /// Entry 5 has exactly one implemented movement
    /// component: Natural Drift.
    /// </summary>
    public sealed class SceneReleaseMovementEvaluator
    {
        public SceneReleaseMovementEvaluation Evaluate(
            SceneReleaseNaturalDriftEvaluation
                naturalDrift)
        {
            if (naturalDrift == null)
            {
                throw new ArgumentNullException(
                    nameof(naturalDrift)
                );
            }

            SceneReleaseMovementComponent
                naturalDriftComponent =
                    new SceneReleaseMovementComponent(
                        naturalDrift.SceneReleaseId,
                        naturalDrift.SceneId,
                        naturalDrift.SettledTurn,
                        SceneReleaseMovementComponentKind
                            .NaturalDrift,
                        naturalDrift.NaturalDrift,
                        BuildNaturalDriftSourceId(
                            naturalDrift
                        )
                    );

            return new SceneReleaseMovementEvaluation(
                naturalDrift.SceneReleaseId,
                naturalDrift.SourceDemoTapeId,
                naturalDrift.SourceOwnerEntityId,
                naturalDrift.SceneId,
                naturalDrift.SettledTurn,
                naturalDrift.StartFieldPosition,
                new[]
                {
                    naturalDriftComponent
                }
            );
        }

        public SceneReleaseMovementEvaluation Evaluate(
            SceneReleaseNaturalDriftEvaluation
                naturalDrift,
            SceneReleaseCanonBreakthroughMovementEvaluation
                breakthrough)
        {
            if (naturalDrift == null)
            {
                throw new ArgumentNullException(
                    nameof(naturalDrift)
                );
            }

            if (breakthrough == null)
            {
                throw new ArgumentNullException(
                    nameof(breakthrough)
                );
            }

            if (naturalDrift.SceneReleaseId !=
                breakthrough.SceneReleaseId)
            {
                throw new ArgumentException(
                    "Natural Drift and Canon Breakthrough " +
                    "belong to different SceneReleases."
                );
            }

            if (naturalDrift.SourceDemoTapeId !=
                breakthrough.SourceDemoTapeId)
            {
                throw new ArgumentException(
                    "Natural Drift and Canon Breakthrough " +
                    "have different DemoTape provenance."
                );
            }

            if (naturalDrift.SourceOwnerEntityId !=
                breakthrough.SourceOwnerEntityId)
            {
                throw new ArgumentException(
                    "Natural Drift and Canon Breakthrough " +
                    "have different owner provenance."
                );
            }

            if (naturalDrift.SceneId !=
                breakthrough.SceneId)
            {
                throw new ArgumentException(
                    "Natural Drift and Canon Breakthrough " +
                    "belong to different scenes."
                );
            }

            if (naturalDrift.SettledTurn !=
                breakthrough.SettledTurn)
            {
                throw new ArgumentException(
                    "Natural Drift and Canon Breakthrough " +
                    "belong to different settlement turns."
                );
            }

            if (Math.Abs(
                    naturalDrift.StartFieldPosition -
                    breakthrough.StartFieldPosition) >
                0.0001f)
            {
                throw new ArgumentException(
                    "Natural Drift and Canon Breakthrough " +
                    "were not evaluated from the same frozen " +
                    "Field Position."
                );
            }

            SceneReleaseMovementComponent
                naturalDriftComponent =
                    new SceneReleaseMovementComponent(
                        naturalDrift.SceneReleaseId,
                        naturalDrift.SceneId,
                        naturalDrift.SettledTurn,
                        SceneReleaseMovementComponentKind
                            .NaturalDrift,
                        naturalDrift.NaturalDrift,
                        BuildNaturalDriftSourceId(
                            naturalDrift
                        )
                    );

            SceneReleaseMovementComponent
                breakthroughComponent =
                    new SceneReleaseMovementComponent(
                        breakthrough.SceneReleaseId,
                        breakthrough.SceneId,
                        breakthrough.SettledTurn,
                        SceneReleaseMovementComponentKind
                            .CanonBreakthrough,
                        breakthrough.BreakthroughDrift,
                        BuildCanonBreakthroughSourceId(
                            breakthrough
                        )
                    );

            return new SceneReleaseMovementEvaluation(
                naturalDrift.SceneReleaseId,
                naturalDrift.SourceDemoTapeId,
                naturalDrift.SourceOwnerEntityId,
                naturalDrift.SceneId,
                naturalDrift.SettledTurn,
                naturalDrift.StartFieldPosition,
                new[]
                {
                    naturalDriftComponent,
                    breakthroughComponent
                }
            );
        }

        private static string
            BuildNaturalDriftSourceId(
                SceneReleaseNaturalDriftEvaluation
                    naturalDrift)
        {
            return
                "MOVEMENT|NATURAL_DRIFT|" +
                naturalDrift.SettledTurn +
                "|" +
                naturalDrift.SceneReleaseId;
        }
        
        private static string
            BuildCanonBreakthroughSourceId(
                SceneReleaseCanonBreakthroughMovementEvaluation
                    breakthrough)
        {
            return
                "MOVEMENT|CANON_BREAKTHROUGH|" +
                breakthrough.SettledTurn +
                "|" +
                breakthrough.SceneReleaseId;
        }
    }
}