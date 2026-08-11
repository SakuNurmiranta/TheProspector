using System;

namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Server-domain state belonging to one continuous
    /// Keeper tenure.
    ///
    /// KeeperTenureId identifies the uninterrupted
    /// reign rather than the player. The same Keeper
    /// retained in office preserves the ID. Any actual
    /// succession starts a new ID, including a former
    /// Keeper returning later.
    ///
    /// This is not a player trait. Pull and the legacy
    /// canonization subject cease to belong to the
    /// player when the tenure ends.
    /// </summary>
    public sealed class KeeperTenureState
    {
        public string KeeperTenureId { get; }

        public ulong KeeperClientId { get; }

        public int StartedRound { get; }

        public string CanonizationSubjectReleaseId
        {
            get;
        }

        public int SubjectTenureYears { get; }

        public float Pull { get; }

        public int LastInterventionTurn { get; }

        public bool
            BoostUsedOnLastInterventionTurn
        {
            get;
        }

        public bool
            SuppressUsedOnLastInterventionTurn
        {
            get;
        }

        public KeeperTenureState(
            ulong keeperClientId,
            int startedRound,
            string canonizationSubjectReleaseId,
            int subjectTenureYears,
            float pull,
            int lastInterventionTurn = -1,
            bool boostUsedOnLastInterventionTurn = false,
            bool suppressUsedOnLastInterventionTurn = false,
            string keeperTenureId = null)
        {
            if (keeperClientId == ulong.MaxValue)
            {
                throw new ArgumentException(
                    "An unassigned client cannot own a " +
                    "Keeper tenure.",
                    nameof(keeperClientId)
                );
            }

            if (keeperTenureId != null &&
                string.IsNullOrWhiteSpace(
                    keeperTenureId))
            {
                throw new ArgumentException(
                    "Keeper tenure identity cannot be " +
                    "empty.",
                    nameof(keeperTenureId)
                );
            }

            KeeperTenureId =
                keeperTenureId == null
                    ? CreateNewTenureId()
                    : keeperTenureId.Trim();

            KeeperClientId =
                keeperClientId;

            StartedRound =
                Math.Max(
                    0,
                    startedRound
                );

            CanonizationSubjectReleaseId =
                canonizationSubjectReleaseId ??
                string.Empty;

            SubjectTenureYears =
                Math.Max(
                    0,
                    subjectTenureYears
                );

            Pull =
                KeeperPullRules.NormalizeAmount(
                    pull
                );

            LastInterventionTurn =
                Math.Max(
                    -1,
                    lastInterventionTurn
                );

            BoostUsedOnLastInterventionTurn =
                LastInterventionTurn >= 0 &&
                boostUsedOnLastInterventionTurn;

            SuppressUsedOnLastInterventionTurn =
                LastInterventionTurn >= 0 &&
                suppressUsedOnLastInterventionTurn;
        }

        public static KeeperTenureState Create(
            ulong keeperClientId,
            int startedRound,
            float pullGrant = 0.0f)
        {
            return new KeeperTenureState(
                keeperClientId,
                startedRound,
                canonizationSubjectReleaseId:
                    string.Empty,
                subjectTenureYears:
                    0,
                pull:
                    pullGrant,
                keeperTenureId:
                    CreateNewTenureId()
            );
        }

        public KeeperTenureState
            WithCanonizationSubject(
                string releaseId)
        {
            return new KeeperTenureState(
                KeeperClientId,
                StartedRound,
                releaseId,
                subjectTenureYears:
                    0,
                Pull,
                lastInterventionTurn:
                    LastInterventionTurn,
                boostUsedOnLastInterventionTurn:
                    BoostUsedOnLastInterventionTurn,
                suppressUsedOnLastInterventionTurn:
                    SuppressUsedOnLastInterventionTurn,
                keeperTenureId:
                    KeeperTenureId
            );
        }

        public KeeperTenureState
            AdvanceCanonizationSubjectYear()
        {
            if (string.IsNullOrWhiteSpace(
                    CanonizationSubjectReleaseId))
            {
                return this;
            }

            return new KeeperTenureState(
                KeeperClientId,
                StartedRound,
                CanonizationSubjectReleaseId,
                SubjectTenureYears + 1,
                Pull,
                lastInterventionTurn:
                    LastInterventionTurn,
                boostUsedOnLastInterventionTurn:
                    BoostUsedOnLastInterventionTurn,
                suppressUsedOnLastInterventionTurn:
                    SuppressUsedOnLastInterventionTurn,
                keeperTenureId:
                    KeeperTenureId
            );
        }

        public KeeperTenureState AddPull(
            float amount)
        {
            float normalizedAmount =
                KeeperPullRules.NormalizeAmount(
                    amount
                );

            if (normalizedAmount <= 0.0f)
            {
                return this;
            }

            return new KeeperTenureState(
                KeeperClientId,
                StartedRound,
                CanonizationSubjectReleaseId,
                SubjectTenureYears,
                Pull + normalizedAmount,
                lastInterventionTurn:
                    LastInterventionTurn,
                boostUsedOnLastInterventionTurn:
                    BoostUsedOnLastInterventionTurn,
                suppressUsedOnLastInterventionTurn:
                    SuppressUsedOnLastInterventionTurn,
                keeperTenureId:
                    KeeperTenureId
            );
        }

        public bool TrySpendPull(
            float cost,
            out KeeperTenureState nextState)
        {
            nextState =
                this;

            if (!KeeperPullRules.CanAfford(
                    Pull,
                    cost))
            {
                return false;
            }

            float remainingPull =
                Pull -
                cost;

            if (remainingPull <
                KeeperPullRules.ComparisonTolerance)
            {
                remainingPull =
                    0.0f;
            }

            nextState =
                new KeeperTenureState(
                    KeeperClientId,
                    StartedRound,
                    CanonizationSubjectReleaseId,
                    SubjectTenureYears,
                    remainingPull,
                    lastInterventionTurn:
                        LastInterventionTurn,
                    boostUsedOnLastInterventionTurn:
                        BoostUsedOnLastInterventionTurn,
                    suppressUsedOnLastInterventionTurn:
                        SuppressUsedOnLastInterventionTurn,
                    keeperTenureId:
                        KeeperTenureId
                );

            return true;
        }

        public bool HasUsedIntervention(
            KeeperInterventionType type,
            int turn)
        {
            if (turn < 0 ||
                LastInterventionTurn !=
                turn)
            {
                return false;
            }

            return type switch
            {
                KeeperInterventionType
                        .BoostVisibility =>
                    BoostUsedOnLastInterventionTurn,

                KeeperInterventionType
                        .SuppressVisibility =>
                    SuppressUsedOnLastInterventionTurn,

                _ => false
            };
        }

        public KeeperTenureState RecordIntervention(
            KeeperInterventionType type,
            int turn)
        {
            if (turn < 0)
            {
                return this;
            }

            bool sameTurn =
                LastInterventionTurn ==
                turn;

            bool boostUsed =
                sameTurn &&
                BoostUsedOnLastInterventionTurn;

            bool suppressUsed =
                sameTurn &&
                SuppressUsedOnLastInterventionTurn;

            switch (type)
            {
                case KeeperInterventionType
                    .BoostVisibility:

                    boostUsed =
                        true;

                    break;

                case KeeperInterventionType
                    .SuppressVisibility:

                    suppressUsed =
                        true;

                    break;

                default:

                    return this;
            }

            return new KeeperTenureState(
                KeeperClientId,
                StartedRound,
                CanonizationSubjectReleaseId,
                SubjectTenureYears,
                Pull,
                lastInterventionTurn:
                    turn,
                boostUsedOnLastInterventionTurn:
                    boostUsed,
                suppressUsedOnLastInterventionTurn:
                    suppressUsed,
                keeperTenureId:
                    KeeperTenureId
            );
        }

        /// <summary>
        /// Emergency succession preserves institutional
        /// state such as Pull and the legacy subject,
        /// but a different office-holder begins a new
        /// continuous Keeper tenure.
        ///
        /// Supplying the same Keeper is idempotent and
        /// therefore preserves the existing tenure.
        /// </summary>
        public KeeperTenureState TransferTo(
            ulong nextKeeperClientId,
            int transferRound)
        {
            if (nextKeeperClientId ==
                KeeperClientId)
            {
                return this;
            }

            return new KeeperTenureState(
                nextKeeperClientId,
                transferRound,
                CanonizationSubjectReleaseId,
                SubjectTenureYears,
                Pull,
                keeperTenureId:
                    CreateNewTenureId()
            );
        }

        private static string CreateNewTenureId()
        {
            return Guid
                .NewGuid()
                .ToString();
        }
    }
}