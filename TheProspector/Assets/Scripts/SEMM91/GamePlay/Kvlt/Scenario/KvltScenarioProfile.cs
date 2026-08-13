using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Pressure;
using SEMM91.GamePlay.Kvlt.Settlement;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    /// <summary>
    /// Versioned authoritative configuration contract
    /// for one Peak-2 KVLT scenario.
    ///
    /// This is a pure domain object, not yet a
    /// ScriptableObject or runtime bootstrapper.
    /// </summary>
    public sealed class KvltScenarioProfile
    {
        private readonly
            CanonPrecedentRecord[]
            startingCanon;

        private readonly
            KvltSocietyNormSeed[]
            startingSocietyNorms;

        private readonly
            KvltAcceptedTransgressionSeed[]
            startingAcceptedTransgressions;

        private readonly
            KvltHistoricalHailPraxisSeed[]
            startingHailPraxisHistory;

        public string ProfileId { get; }

        public int Version { get; }

        public int RequiredKvltPlayerCount { get; }

        public int TurnsPerYear { get; }

        public float
            SharedHappeningWindowSeconds { get; }

        public float FreshReleasePosition { get; }

        public float InnerFieldEntryCeiling { get; }

        public float OuterBoundary { get; }

        public float NexusBoundary { get; }

        public float FieldDriftScale { get; }

        public float
            BreakthroughDriftMultiplier { get; }

        public float InitialSceneDamage { get; }

        public string FoundingKeeperSplatId { get; }

        public float StartingKeeperPull { get; }

        public SceneStandingProjectionPolicy
            StandingProjectionPolicy { get; }

        public ScenePressureRebuildPolicy
            PressureRebuildPolicy { get; }

        public NormativePressureBlendPolicy
            NormativePressureBlendPolicy { get; }

        public KvltPullEconomyProfile
            PullEconomy { get; }

        public IReadOnlyList<
                CanonPrecedentRecord>
            StartingCanon =>
            startingCanon;

        public IReadOnlyList<
                KvltSocietyNormSeed>
            StartingSocietyNorms =>
            startingSocietyNorms;

        public IReadOnlyList<
                KvltAcceptedTransgressionSeed>
            StartingAcceptedTransgressions =>
            startingAcceptedTransgressions;

        public IReadOnlyList<
                KvltHistoricalHailPraxisSeed>
            StartingHailPraxisHistory =>
            startingHailPraxisHistory;

        public KvltScenarioProfile(
            string profileId,
            int version,
            int requiredKvltPlayerCount,
            int turnsPerYear,
            float sharedHappeningWindowSeconds,
            float freshReleasePosition,
            float innerFieldEntryCeiling,
            float outerBoundary,
            float nexusBoundary,
            float fieldDriftScale,
            float breakthroughDriftMultiplier,
            float initialSceneDamage,
            string foundingKeeperSplatId,
            float startingKeeperPull,
            SceneStandingProjectionPolicy
                standingProjectionPolicy,
            ScenePressureRebuildPolicy
                pressureRebuildPolicy,
            NormativePressureBlendPolicy
                normativePressureBlendPolicy,
            KvltPullEconomyProfile
                pullEconomy,
            IReadOnlyList<
                CanonPrecedentRecord>
                sourceStartingCanon,
            IReadOnlyList<
                KvltSocietyNormSeed>
                sourceStartingSocietyNorms,
            IReadOnlyList<
                KvltAcceptedTransgressionSeed>
                sourceStartingAcceptedTransgressions,
            IReadOnlyList<
                KvltHistoricalHailPraxisSeed>
                sourceStartingHailPraxisHistory)
        {
            ProfileId =
                RequireText(
                    profileId,
                    nameof(profileId)
                );

            FoundingKeeperSplatId =
                RequireText(
                    foundingKeeperSplatId,
                    nameof(foundingKeeperSplatId)
                );

            if (version <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(version)
                );
            }

            if (requiredKvltPlayerCount < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredKvltPlayerCount)
                );
            }

            if (turnsPerYear <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnsPerYear)
                );
            }

            RequirePositiveFinite(
                sharedHappeningWindowSeconds,
                nameof(
                    sharedHappeningWindowSeconds
                )
            );

            RequireFinite(
                freshReleasePosition,
                nameof(freshReleasePosition)
            );

            RequireFinite(
                innerFieldEntryCeiling,
                nameof(innerFieldEntryCeiling)
            );

            RequireFinite(
                outerBoundary,
                nameof(outerBoundary)
            );

            RequireFinite(
                nexusBoundary,
                nameof(nexusBoundary)
            );

            /*
             * Position increases inward.
             *
             * A fresh release must begin inside the
             * Fringe boundary but below any privileged
             * entry ceiling, and that ceiling must
             * remain below Nexus.
             */
            if (outerBoundary >=
                    freshReleasePosition ||
                freshReleasePosition >
                    innerFieldEntryCeiling ||
                innerFieldEntryCeiling >=
                    nexusBoundary)
            {
                throw new ArgumentException(
                    "Scenario Field geometry must obey " +
                    "Outer < Fresh <= EntryCeiling < " +
                    "Nexus."
                );
            }

            RequireNonNegativeFinite(
                fieldDriftScale,
                nameof(fieldDriftScale)
            );

            RequireNonNegativeFinite(
                breakthroughDriftMultiplier,
                nameof(
                    breakthroughDriftMultiplier
                )
            );

            RequireNonNegativeFinite(
                initialSceneDamage,
                nameof(initialSceneDamage)
            );

            RequireNonNegativeFinite(
                startingKeeperPull,
                nameof(startingKeeperPull)
            );

            StandingProjectionPolicy =
                standingProjectionPolicy ??
                throw new ArgumentNullException(
                    nameof(
                        standingProjectionPolicy
                    )
                );

            PressureRebuildPolicy =
                pressureRebuildPolicy ??
                throw new ArgumentNullException(
                    nameof(pressureRebuildPolicy)
                );

            NormativePressureBlendPolicy =
                normativePressureBlendPolicy ??
                throw new ArgumentNullException(
                    nameof(
                        normativePressureBlendPolicy
                    )
                );

            PullEconomy =
                pullEconomy ??
                throw new ArgumentNullException(
                    nameof(pullEconomy)
                );

            startingCanon =
                Copy(
                    sourceStartingCanon,
                    nameof(sourceStartingCanon)
                );

            startingSocietyNorms =
                CopyUniqueSocietyNorms(
                    sourceStartingSocietyNorms
                );

            startingAcceptedTransgressions =
                CopyUniqueById(
                    sourceStartingAcceptedTransgressions,
                    nameof(
                        sourceStartingAcceptedTransgressions
                    ),
                    seed =>
                        seed.SeedId
                );

            startingHailPraxisHistory =
                CopyUniqueById(
                    sourceStartingHailPraxisHistory,
                    nameof(
                        sourceStartingHailPraxisHistory
                    ),
                    seed =>
                        seed.SeedId
                );

            Version =
                version;

            RequiredKvltPlayerCount =
                requiredKvltPlayerCount;

            TurnsPerYear =
                turnsPerYear;

            SharedHappeningWindowSeconds =
                sharedHappeningWindowSeconds;

            FreshReleasePosition =
                freshReleasePosition;

            InnerFieldEntryCeiling =
                innerFieldEntryCeiling;

            OuterBoundary =
                outerBoundary;

            NexusBoundary =
                nexusBoundary;

            FieldDriftScale =
                fieldDriftScale;

            BreakthroughDriftMultiplier =
                breakthroughDriftMultiplier;

            InitialSceneDamage =
                initialSceneDamage;

            StartingKeeperPull =
                startingKeeperPull;
        }

        public KvltSceneSettlementPolicy
            CreateSceneSettlementPolicy()
        {
            return new KvltSceneSettlementPolicy(
                FieldDriftScale,
                BreakthroughDriftMultiplier,
                NexusBoundary,
                OuterBoundary,
                StandingProjectionPolicy,
                PressureRebuildPolicy,
                NormativePressureBlendPolicy
            );
        }

        private static
            KvltSocietyNormSeed[]
            CopyUniqueSocietyNorms(
                IReadOnlyList<
                    KvltSocietyNormSeed>
                    source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(
                    nameof(source)
                );
            }

            KvltSocietyNormSeed[] result =
                new KvltSocietyNormSeed[
                    source.Count
                ];

            HashSet<
                (
                    Core.Tags.TagAxis Axis,
                    Core.Tags.TagPole Pole
                )>
                seen =
                    new();

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                KvltSocietyNormSeed seed =
                    source[index] ??
                    throw new ArgumentException(
                        "Scenario Society norms cannot " +
                        "contain null.",
                        nameof(source)
                    );

                if (!seen.Add(
                        (
                            seed.Axis,
                            seed.Pole
                        )))
                {
                    throw new ArgumentException(
                        "Scenario Society norms contain " +
                        "duplicate Tag polarity.",
                        nameof(source)
                    );
                }

                result[index] =
                    seed;
            }

            return result;
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
                        "Scenario seed collection cannot " +
                        "contain null.",
                        parameterName
                    );
            }

            return result;
        }

        private static T[] CopyUniqueById<T>(
            IReadOnlyList<T> source,
            string parameterName,
            Func<T, string> idSelector)
            where T : class
        {
            T[] result =
                Copy(
                    source,
                    parameterName
                );

            HashSet<string> seen =
                new(
                    StringComparer.Ordinal
                );

            foreach (T item in result)
            {
                string id =
                    idSelector(item);

                if (!seen.Add(id))
                {
                    throw new ArgumentException(
                        "Scenario seed collection " +
                        "contains duplicate identity | " +
                        $"id={id}",
                        parameterName
                    );
                }
            }

            return result;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Scenario profile identity cannot " +
                    "be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }

        private static void
            RequirePositiveFinite(
                float value,
                string parameterName)
        {
            if (!IsFinite(value) ||
                value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static void
            RequireNonNegativeFinite(
                float value,
                string parameterName)
        {
            if (!IsFinite(value) ||
                value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static void RequireFinite(
            float value,
            string parameterName)
        {
            if (!IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static bool IsFinite(
            float value)
        {
            return
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}