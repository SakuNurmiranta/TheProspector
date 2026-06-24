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
        public int Pull { get; }

        public KeeperTenureState(
            ulong keeperClientId,
            int startedRound,
            string canonizationSubjectReleaseId,
            int subjectTenureYears,
            int pull)
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
                Math.Max(0, pull);
        }

        public static KeeperTenureState Create(
            ulong keeperClientId,
            int startedRound,
            int initialPull = 0)
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