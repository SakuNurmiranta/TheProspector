using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Canon
{
    /// <summary>
    /// Immutable result of applying one complete
    /// validated Canon Assimilation plan.
    /// </summary>
    public sealed class
        SceneReleaseCanonAssimilationApplication
    {
        private readonly
            SceneReleaseCanonAssimilationActivationRecord[]
            activationRecords;

        public SceneReleaseCanonAssimilationEvaluation
            Evaluation { get; }

        public IReadOnlyList<
                SceneReleaseCanonAssimilationActivationRecord>
            ActivationRecords =>
            activationRecords;

        public int RaisedPairCount =>
            activationRecords.Length;

        public SceneReleaseCanonAssimilationApplication(
            SceneReleaseCanonAssimilationEvaluation
                evaluation,
            IReadOnlyList<
                SceneReleaseCanonAssimilationActivationRecord>
                sourceActivationRecords)
        {
            Evaluation =
                evaluation ??
                throw new ArgumentNullException(
                    nameof(evaluation)
                );

            if (sourceActivationRecords == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceActivationRecords)
                );
            }

            if (sourceActivationRecords.Count !=
                evaluation.RaisedPairCount)
            {
                throw new ArgumentException(
                    "Applied assimilation history count " +
                    "does not match evaluated changes.",
                    nameof(sourceActivationRecords)
                );
            }

            activationRecords =
                new
                    SceneReleaseCanonAssimilationActivationRecord[
                        sourceActivationRecords.Count
                    ];

            for (int index = 0;
                 index < sourceActivationRecords.Count;
                 index++)
            {
                activationRecords[index] =
                    sourceActivationRecords[index] ??
                    throw new ArgumentException(
                        "Assimilation application cannot " +
                        "contain null history record.",
                        nameof(sourceActivationRecords)
                    );
            }
        }
    }
}