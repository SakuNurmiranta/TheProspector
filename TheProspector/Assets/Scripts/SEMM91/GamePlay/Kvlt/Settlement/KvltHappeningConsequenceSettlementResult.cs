using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Paradigm;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Immutable inspectable output of one
    /// post-voting Happening consequence pass.
    ///
    /// This result describes semantic/public state
    /// produced during HappeningSettlement.
    ///
    /// It does not contain movement, Canonization,
    /// recurring Gravity, Pressure or Keeper results.
    /// </summary>
    public sealed class
        KvltHappeningConsequenceSettlementResult
    {
        private readonly
            Happening[]
            settledHappenings;

        private readonly
            ActivationCrisisSettlement[]
            crisisSettlements;

        private readonly
            SceneReleaseLegitimacyEvaluation[]
            legitimacyEvaluations;

        private readonly
            Dictionary<
                string,
                SceneReleaseLegitimacyEvaluation>
            legitimacyByRelease;

        private readonly
            Dictionary<
                string,
                SceneReleaseFetteringResult>
            fetteringByRelease;

        private readonly KvltParadigmContestResult[]
            paradigmContestResults;

        public int GlobalTurn { get; }

        public int CoveredActivationApplications
        {
            get;
        }

        public int CrisisLegitimizedApplications
        {
            get;
        }

        public int PendingStoredCount { get; }

        public int PendingRedeemedCount { get; }

        public int AcceptedPrecedentsRaised { get; }

        public IReadOnlyList<Happening>
            SettledHappenings =>
            settledHappenings;

        public IReadOnlyList<
                ActivationCrisisSettlement>
            CrisisSettlements =>
            crisisSettlements;

        public IReadOnlyList<
                SceneReleaseLegitimacyEvaluation>
            LegitimacyEvaluations =>
            legitimacyEvaluations;

        public IReadOnlyDictionary<
                string,
                SceneReleaseLegitimacyEvaluation>
            LegitimacyByRelease =>
            legitimacyByRelease;

        public IReadOnlyDictionary<
                string,
                SceneReleaseFetteringResult>
            FetteringByRelease =>
            fetteringByRelease;

        public IReadOnlyList<KvltParadigmContestResult>
            ParadigmContestResults =>
            paradigmContestResults;

        public int ParadigmBeefCount
        {
            get
            {
                int count = 0;
                foreach (KvltParadigmContestResult result
                         in paradigmContestResults)
                {
                    if (result.IsBeef)
                        count++;
                }

                return count;
            }
        }

        public int NewPoserDeclarationCount
        {
            get
            {
                int count = 0;
                foreach (KvltParadigmContestResult result
                         in paradigmContestResults)
                    count += result.NewPoserDeclarations.Count;

                return count;
            }
        }

        public KvltHappeningConsequenceSettlementResult(
            int globalTurn,
            IReadOnlyList<Happening>
                sourceSettledHappenings,
            IReadOnlyList<
                ActivationCrisisSettlement>
                sourceCrisisSettlements,
            IReadOnlyList<
                SceneReleaseLegitimacyEvaluation>
                sourceLegitimacyEvaluations,
            IReadOnlyDictionary<
                string,
                SceneReleaseLegitimacyEvaluation>
                sourceLegitimacyByRelease,
            IReadOnlyDictionary<
                string,
                SceneReleaseFetteringResult>
                sourceFetteringByRelease,
            int coveredActivationApplications,
            int crisisLegitimizedApplications,
            int pendingStoredCount,
            int pendingRedeemedCount,
            int acceptedPrecedentsRaised)
            : this(
                globalTurn,
                sourceSettledHappenings,
                sourceCrisisSettlements,
                sourceLegitimacyEvaluations,
                sourceLegitimacyByRelease,
                sourceFetteringByRelease,
                coveredActivationApplications,
                crisisLegitimizedApplications,
                pendingStoredCount,
                pendingRedeemedCount,
                acceptedPrecedentsRaised,
                Array.Empty<KvltParadigmContestResult>())
        {
        }

        public KvltHappeningConsequenceSettlementResult(
            int globalTurn,
            IReadOnlyList<Happening>
                sourceSettledHappenings,
            IReadOnlyList<ActivationCrisisSettlement>
                sourceCrisisSettlements,
            IReadOnlyList<SceneReleaseLegitimacyEvaluation>
                sourceLegitimacyEvaluations,
            IReadOnlyDictionary<
                string,
                SceneReleaseLegitimacyEvaluation>
                sourceLegitimacyByRelease,
            IReadOnlyDictionary<
                string,
                SceneReleaseFetteringResult>
                sourceFetteringByRelease,
            int coveredActivationApplications,
            int crisisLegitimizedApplications,
            int pendingStoredCount,
            int pendingRedeemedCount,
            int acceptedPrecedentsRaised,
            IReadOnlyList<KvltParadigmContestResult>
                sourceParadigmContestResults)
        {
            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            ValidateCount(
                coveredActivationApplications,
                nameof(
                    coveredActivationApplications
                )
            );

            ValidateCount(
                crisisLegitimizedApplications,
                nameof(
                    crisisLegitimizedApplications
                )
            );

            ValidateCount(
                pendingStoredCount,
                nameof(pendingStoredCount)
            );

            ValidateCount(
                pendingRedeemedCount,
                nameof(pendingRedeemedCount)
            );

            ValidateCount(
                acceptedPrecedentsRaised,
                nameof(acceptedPrecedentsRaised)
            );

            GlobalTurn =
                globalTurn;

            CoveredActivationApplications =
                coveredActivationApplications;

            CrisisLegitimizedApplications =
                crisisLegitimizedApplications;

            PendingStoredCount =
                pendingStoredCount;

            PendingRedeemedCount =
                pendingRedeemedCount;

            AcceptedPrecedentsRaised =
                acceptedPrecedentsRaised;

            settledHappenings =
                Copy(
                    sourceSettledHappenings,
                    nameof(sourceSettledHappenings)
                );

            crisisSettlements =
                Copy(
                    sourceCrisisSettlements,
                    nameof(sourceCrisisSettlements)
                );

            legitimacyEvaluations =
                Copy(
                    sourceLegitimacyEvaluations,
                    nameof(sourceLegitimacyEvaluations)
                );

            legitimacyByRelease =
                CopyDictionary(
                    sourceLegitimacyByRelease,
                    nameof(sourceLegitimacyByRelease)
                );

            fetteringByRelease =
                CopyFetteringDictionary(
                    sourceFetteringByRelease,
                    nameof(sourceFetteringByRelease)
                );

            paradigmContestResults =
                Copy(
                    sourceParadigmContestResults,
                    nameof(sourceParadigmContestResults)
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
                        "Happening consequence result " +
                        "cannot contain null.",
                        parameterName
                    );
            }

            return result;
        }

        private static Dictionary<string, T>
            CopyDictionary<T>(
                IReadOnlyDictionary<string, T> source,
                string parameterName)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(
                    parameterName
                );
            }

            Dictionary<string, T> result =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                KeyValuePair<string, T> pair
                in source)
            {
                if (string.IsNullOrWhiteSpace(
                        pair.Key) ||
                    pair.Value == null)
                {
                    throw new ArgumentException(
                        "Happening consequence lookup " +
                        "contains invalid provenance.",
                        parameterName
                    );
                }

                result.Add(
                    pair.Key,
                    pair.Value
                );
            }

            return result;
        }

        private static
            Dictionary<
                string,
                SceneReleaseFetteringResult>
            CopyFetteringDictionary(
                IReadOnlyDictionary<
                    string,
                    SceneReleaseFetteringResult>
                    source,
                string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(
                    parameterName
                );
            }

            Dictionary<
                    string,
                    SceneReleaseFetteringResult>
                result =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                KeyValuePair<
                    string,
                    SceneReleaseFetteringResult>
                    pair
                in source)
            {
                if (string.IsNullOrWhiteSpace(
                        pair.Key))
                {
                    throw new ArgumentException(
                        "Fettering result identity " +
                        "cannot be empty.",
                        parameterName
                    );
                }

                if (!Enum.IsDefined(
                        typeof(
                            SceneReleaseFetteringResult
                        ),
                        pair.Value))
                {
                    throw new ArgumentException(
                        "Fettering result contains an " +
                        "undefined disposition.",
                        parameterName
                    );
                }

                result.Add(
                    pair.Key,
                    pair.Value
                );
            }

            return result;
        }

        private static void ValidateCount(
            int value,
            string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }
    }
}
