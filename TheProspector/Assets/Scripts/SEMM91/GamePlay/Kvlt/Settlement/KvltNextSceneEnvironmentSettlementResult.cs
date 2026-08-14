using System;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Immutable semantic environment prepared for
    /// the next published turn.
    ///
    /// This result is produced only after next-turn
    /// Field ingress has completed.
    /// </summary>
    public sealed class
        KvltNextSceneEnvironmentSettlementResult
    {
        public string SceneId { get; }

        public int CompletedTurn { get; }

        public int PublishedTurn { get; }

        public ScenePressureRebuildEvaluation
            Pressure { get; }

        public NormativeCentre
            CanonNormativeCentre { get; }

        public NormativeCentreDerivationEvaluation
            NormativeCentre { get; }

        public
            KvltNextSceneEnvironmentSettlementResult(
                string sceneId,
                int completedTurn,
                int publishedTurn,
                ScenePressureRebuildEvaluation
                    pressure,
                NormativeCentre
                    canonNormativeCentre,
                NormativeCentreDerivationEvaluation
                    normativeCentre)
        {
            if (string.IsNullOrWhiteSpace(
                    sceneId))
            {
                throw new ArgumentException(
                    "Next scene environment identity " +
                    "cannot be empty.",
                    nameof(sceneId)
                );
            }

            if (completedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedTurn)
                );
            }

            if (publishedTurn !=
                completedTurn + 1)
            {
                throw new ArgumentException(
                    "Next scene environment must " +
                    "describe exactly the following " +
                    "turn.",
                    nameof(publishedTurn)
                );
            }

            Pressure =
                pressure ??
                throw new ArgumentNullException(
                    nameof(pressure)
                );

            CanonNormativeCentre =
                canonNormativeCentre ??
                throw new ArgumentNullException(
                    nameof(canonNormativeCentre)
                );

            NormativeCentre =
                normativeCentre ??
                throw new ArgumentNullException(
                    nameof(normativeCentre)
                );

            SceneId =
                sceneId.Trim();

            CompletedTurn =
                completedTurn;

            PublishedTurn =
                publishedTurn;

            if (Pressure.SceneId !=
                    SceneId ||
                Pressure.SettledTurn !=
                    PublishedTurn)
            {
                throw new ArgumentException(
                    "Next Pressure does not describe " +
                    "the requested published scene " +
                    "state.",
                    nameof(pressure)
                );
            }

            if (NormativeCentre.SceneId !=
                    SceneId ||
                NormativeCentre.SettledTurn !=
                    PublishedTurn)
            {
                throw new ArgumentException(
                    "Next Normative Centre does not " +
                    "describe the requested published " +
                    "scene state.",
                    nameof(normativeCentre)
                );
            }
        }
    }
}