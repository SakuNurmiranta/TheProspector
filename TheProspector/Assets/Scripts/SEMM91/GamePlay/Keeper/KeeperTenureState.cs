using System;

namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Server-domain state belonging to one Keeper tenure.
    ///
    /// This is not a player trait. Pull and the canonization
    /// subject cease to belong to the player when the tenure ends.
    /// </summary>
    public sealed class KeeperTenureState
    {
        public ulong KeeperClientId { get; }
        public int StartedRound { get; }

        public string CanonizationSubjectReleaseId
        {
            get;
        }

        public int SubjectTenureYears { get; }
        public float Pull { get; }

        public KeeperTenureState(
            ulong keeperClientId,
            int startedRound,
            string canonizationSubjectReleaseId,
            int subjectTenureYears,
            float pull)
        {
            if (keeperClientId == ulong.MaxValue)
            {
                throw new ArgumentException(
                    "An unassigned client cannot own a " +
                    "Keeper tenure.",
                    nameof(keeperClientId)
                );
            }

            KeeperClientId =
                keeperClientId;

            StartedRound =
                Math.Max(0, startedRound);

            CanonizationSubjectReleaseId =
                canonizationSubjectReleaseId ??
                string.Empty;

            SubjectTenureYears =
                Math.Max(0, subjectTenureYears);

            Pull =
                KeeperPullRules.NormalizeAmount(
                    pull
                );
        }

        public static KeeperTenureState Create(
            ulong keeperClientId,
            int startedRound,
            float initialPull = 0.0f)
        {
            return new KeeperTenureState(
                keeperClientId,
                startedRound,
                canonizationSubjectReleaseId:
                    string.Empty,
                subjectTenureYears: 0,
                pull: initialPull
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
                subjectTenureYears: 0,
                Pull
            );
        }
        
        public KeeperTenureState
            AdvanceCanonizationSubjectYear()
        {
            if (string.IsNullOrWhiteSpace(
                    CanonizationSubjectReleaseId
                ))
            {
                return this;
            }

            return new KeeperTenureState(
                KeeperClientId,
                StartedRound,
                CanonizationSubjectReleaseId,
                SubjectTenureYears + 1,
                Pull
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
                return this;

            return new KeeperTenureState(
                KeeperClientId,
                StartedRound,
                CanonizationSubjectReleaseId,
                SubjectTenureYears,
                Pull + normalizedAmount
            );
        }

        public bool TrySpendPull(
            float cost,
            out KeeperTenureState nextState)
        {
            nextState = this;

            if (!KeeperPullRules.CanAfford(
                    Pull,
                    cost
                ))
            {
                return false;
            }

            float remainingPull =
                Pull - cost;

            if (remainingPull <
                KeeperPullRules.ComparisonTolerance)
            {
                remainingPull = 0.0f;
            }

            nextState =
                new KeeperTenureState(
                    KeeperClientId,
                    StartedRound,
                    CanonizationSubjectReleaseId,
                    SubjectTenureYears,
                    remainingPull
                );

            return true;
        }
        
        /// <summary>
        /// Emergency transfer preserves institutional state,
        /// but records when the replacement Keeper took office.
        /// It does not itself trigger canonization.
        /// </summary>
        public KeeperTenureState TransferTo(
            ulong nextKeeperClientId,
            int transferRound)
        {
            return new KeeperTenureState(
                nextKeeperClientId,
                transferRound,
                CanonizationSubjectReleaseId,
                SubjectTenureYears,
                Pull
            );
        }
    }
}