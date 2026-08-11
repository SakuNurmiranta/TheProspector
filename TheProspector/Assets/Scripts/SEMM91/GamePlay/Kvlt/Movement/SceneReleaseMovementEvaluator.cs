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
    }
}