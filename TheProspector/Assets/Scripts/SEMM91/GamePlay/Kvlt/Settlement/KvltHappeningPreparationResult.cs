using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Transgression;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Immutable result of the shared-window
    /// Happening preparation pass.
    ///
    /// Preparation establishes factual occurrence
    /// provenance and institutional questions, but
    /// deliberately does not apply activation or
    /// settle the Happening.
    /// </summary>
    public sealed class
        KvltHappeningPreparationResult
    {
        private readonly
            Happening[]
            preparedHappenings;

        private readonly
            SceneReleaseActivationAttempt[]
            activationAttempts;

        private readonly
            ActivationLegitimacyAssessment[]
            legitimacyAssessments;

        private readonly
            ActivationLegitimacyCrisisGroup[]
            crisisGroups;

        private readonly
            AllegianceCrisis[]
            openedCrises;

        public int GlobalTurn { get; }

        public int MaterializedBehaviorCount { get; }

        public int MaterializedHailCount { get; }

        public IReadOnlyList<Happening>
            PreparedHappenings =>
            preparedHappenings;

        public IReadOnlyList<
                SceneReleaseActivationAttempt>
            ActivationAttempts =>
            activationAttempts;

        public IReadOnlyList<
                ActivationLegitimacyAssessment>
            LegitimacyAssessments =>
            legitimacyAssessments;

        public IReadOnlyList<
                ActivationLegitimacyCrisisGroup>
            CrisisGroups =>
            crisisGroups;

        public IReadOnlyList<AllegianceCrisis>
            OpenedCrises =>
            openedCrises;

        public bool RequiresCrisisVoting =>
            openedCrises.Length > 0;

        public KvltHappeningPreparationResult(
            int globalTurn,
            IReadOnlyList<Happening>
                sourcePreparedHappenings,
            IReadOnlyList<
                SceneReleaseActivationAttempt>
                sourceActivationAttempts,
            IReadOnlyList<
                ActivationLegitimacyAssessment>
                sourceLegitimacyAssessments,
            IReadOnlyList<
                ActivationLegitimacyCrisisGroup>
                sourceCrisisGroups,
            IReadOnlyList<AllegianceCrisis>
                sourceOpenedCrises,
            int materializedBehaviorCount,
            int materializedHailCount)
        {
            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            if (materializedBehaviorCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(materializedBehaviorCount)
                );
            }

            if (materializedHailCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(materializedHailCount)
                );
            }

            GlobalTurn =
                globalTurn;

            MaterializedBehaviorCount =
                materializedBehaviorCount;

            MaterializedHailCount =
                materializedHailCount;

            preparedHappenings =
                Copy(
                    sourcePreparedHappenings,
                    nameof(sourcePreparedHappenings)
                );

            activationAttempts =
                Copy(
                    sourceActivationAttempts,
                    nameof(sourceActivationAttempts)
                );

            legitimacyAssessments =
                Copy(
                    sourceLegitimacyAssessments,
                    nameof(sourceLegitimacyAssessments)
                );

            crisisGroups =
                Copy(
                    sourceCrisisGroups,
                    nameof(sourceCrisisGroups)
                );

            openedCrises =
                Copy(
                    sourceOpenedCrises,
                    nameof(sourceOpenedCrises)
                );
        }

        private static T[] Copy<T>(
            IReadOnlyList<T> source,
            string parameterName)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(
                    parameterName
                );
            }

            T[] result =
                new T[source.Count];

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                result[index] =
                    source[index] ??
                    throw new ArgumentException(
                        "Happening preparation result " +
                        "cannot contain null.",
                        parameterName
                    );
            }

            return result;
        }
    }
}