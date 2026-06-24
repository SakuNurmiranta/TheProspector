using SEMM91.Core.Collectives;
using SEMM91.Core.Entities;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Applies the collective-membership consequences of a
    /// classified Keeper transition.
    ///
    /// Current institutional representation:
    /// - regular leader: active in own band, passive in KVLT;
    /// - Keeper: passive in own band, active in KVLT;
    /// - Keeper's band collective is dormant;
    /// - Keeper's leader entity leads KVLT through The Hole.
    ///
    /// This resolver does not select the Keeper, mutate networking
    /// state, resolve canonization, or gate player commands.
    /// </summary>
    public sealed class KeeperRoleResolver
    {
        public KeeperRoleResolution Resolve(
            KeeperTransitionResult transition,
            SeededWorldState world)
        {
            if (!transition.HasResult)
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    "Keeper transition has no result."
                );
            }

            if (world == null)
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    "Seeded world state is unavailable."
                );
            }

            Collective kvlt =
                world.FindKvlt();

            if (kvlt == null)
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    "KVLT collective is unavailable."
                );
            }

            switch (transition.Reason)
            {
                case KeeperTransitionReason
                    .InitialAssignment:

                    return ResolveInitialAssignment(
                        transition,
                        world,
                        kvlt
                    );

                case KeeperTransitionReason
                    .YearEndRetained:

                    return ResolveRetention(
                        transition,
                        world,
                        kvlt
                    );

                case KeeperTransitionReason
                    .YearEndReplaced:

                    return ResolveReplacement(
                        transition,
                        world,
                        kvlt
                    );

                case KeeperTransitionReason
                    .DisconnectionFallback:

                    return ResolveDisconnectionFallback(
                        transition,
                        world,
                        kvlt
                    );

                case KeeperTransitionReason
                    .SceneCollapseLocked:

                    /*
                     * The incumbent remains in office during the
                     * apocalypse round. No role mutation occurs.
                     */
                    return KeeperRoleResolution.Success(
                        transition,
                        outgoingRoleReleased: false,
                        incomingRoleApplied: false
                    );

                default:

                    return KeeperRoleResolution.Failure(
                        transition,
                        $"Unsupported Keeper transition reason: " +
                        $"{transition.Reason}."
                    );
            }
        }

        private static KeeperRoleResolution
            ResolveInitialAssignment(
                KeeperTransitionResult transition,
                SeededWorldState world,
                Collective kvlt)
        {
            if (!transition.HasAssignedKeeper)
            {
                ClearKvltLeader(kvlt);

                return KeeperRoleResolution.Success(
                    transition,
                    outgoingRoleReleased: false,
                    incomingRoleApplied: false
                );
            }

            if (!TryResolveParticipantContext(
                    transition.NextKeeperClientId,
                    world,
                    kvlt,
                    out ParticipantRoleContext incoming,
                    out string failureReason
                ))
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    failureReason
                );
            }

            bool applied =
                TryEnterKeeperRole(
                    incoming,
                    kvlt
                );

            if (!applied)
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    "Incoming Keeper role could not be applied."
                );
            }

            return KeeperRoleResolution.Success(
                transition,
                outgoingRoleReleased: false,
                incomingRoleApplied: true
            );
        }

        private static KeeperRoleResolution
            ResolveRetention(
                KeeperTransitionResult transition,
                SeededWorldState world,
                Collective kvlt)
        {
            if (!transition.HasAssignedKeeper)
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    "Retained Keeper transition has no Keeper."
                );
            }

            if (!TryResolveParticipantContext(
                    transition.NextKeeperClientId,
                    world,
                    kvlt,
                    out ParticipantRoleContext incumbent,
                    out string failureReason
                ))
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    failureReason
                );
            }

            /*
             * This is an idempotent invariant repair. The Keeper
             * does not exit and re-enter the role.
             */
            bool applied =
                TryEnterKeeperRole(
                    incumbent,
                    kvlt
                );

            if (!applied)
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    "Retained Keeper role could not be preserved."
                );
            }

            return KeeperRoleResolution.Success(
                transition,
                outgoingRoleReleased: false,
                incomingRoleApplied: true
            );
        }

        private static KeeperRoleResolution
            ResolveReplacement(
                KeeperTransitionResult transition,
                SeededWorldState world,
                Collective kvlt)
        {
            if (!TryResolveParticipantContext(
                    transition.PreviousKeeperClientId,
                    world,
                    kvlt,
                    out ParticipantRoleContext outgoing,
                    out string outgoingFailure
                ))
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    outgoingFailure
                );
            }

            if (!transition.HasAssignedKeeper)
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    "Replacement transition has no incoming Keeper."
                );
            }

            if (!TryResolveParticipantContext(
                    transition.NextKeeperClientId,
                    world,
                    kvlt,
                    out ParticipantRoleContext incoming,
                    out string incomingFailure
                ))
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    incomingFailure
                );
            }

            bool released =
                TryExitKeeperRole(
                    outgoing,
                    kvlt
                );

            bool applied =
                released &&
                TryEnterKeeperRole(
                    incoming,
                    kvlt
                );

            if (!released || !applied)
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    "Keeper replacement role mutation failed."
                );
            }

            return KeeperRoleResolution.Success(
                transition,
                outgoingRoleReleased: true,
                incomingRoleApplied: true
            );
        }

        private static KeeperRoleResolution
            ResolveDisconnectionFallback(
                KeeperTransitionResult transition,
                SeededWorldState world,
                Collective kvlt)
        {
            bool released = false;

            if (transition.PreviousKeeperClientId !=
                ulong.MaxValue)
            {
                if (!TryResolveParticipantContext(
                        transition.PreviousKeeperClientId,
                        world,
                        kvlt,
                        out ParticipantRoleContext outgoing,
                        out string outgoingFailure
                    ))
                {
                    return KeeperRoleResolution.Failure(
                        transition,
                        outgoingFailure
                    );
                }

                released =
                    TryExitKeeperRole(
                        outgoing,
                        kvlt
                    );

                if (!released)
                {
                    return KeeperRoleResolution.Failure(
                        transition,
                        "Disconnected Keeper role could not be released."
                    );
                }
            }

            if (!transition.HasAssignedKeeper)
            {
                ClearKvltLeader(kvlt);

                return KeeperRoleResolution.Success(
                    transition,
                    outgoingRoleReleased: released,
                    incomingRoleApplied: false
                );
            }

            if (!TryResolveParticipantContext(
                    transition.NextKeeperClientId,
                    world,
                    kvlt,
                    out ParticipantRoleContext incoming,
                    out string incomingFailure
                ))
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    incomingFailure
                );
            }

            bool applied =
                TryEnterKeeperRole(
                    incoming,
                    kvlt
                );

            if (!applied)
            {
                return KeeperRoleResolution.Failure(
                    transition,
                    "Fallback Keeper role could not be applied."
                );
            }

            return KeeperRoleResolution.Success(
                transition,
                outgoingRoleReleased: released,
                incomingRoleApplied: true
            );
        }

        private static bool
            TryResolveParticipantContext(
                ulong clientId,
                SeededWorldState world,
                Collective kvlt,
                out ParticipantRoleContext context,
                out string failureReason)
        {
            context = default;
            failureReason = string.Empty;

            if (clientId == ulong.MaxValue)
            {
                failureReason =
                    "Cannot resolve an unassigned Keeper.";

                return false;
            }

            string bandId =
                StartingCollectiveBootstrapper
                    .PlayerBandIdPrefix +
                clientId;

            Collective band =
                world.FindCollective(
                    bandId
                );

            if (band == null)
            {
                failureReason =
                    $"Player band is unavailable: {bandId}.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    band.LeaderEntityId
                ))
            {
                failureReason =
                    $"Player band {bandId} has no leader.";

                return false;
            }

            GameEntity leader =
                world.FindEntity(
                    band.LeaderEntityId
                );

            if (leader == null)
            {
                failureReason =
                    $"Band leader entity is unavailable: " +
                    $"{band.LeaderEntityId}.";

                return false;
            }

            if (!leader.HasCollectiveMembership(
                    band.CollectiveId
                ) ||
                !leader.HasCollectiveMembership(
                    StartingCollectiveBootstrapper
                        .KvltId
                ))
            {
                failureReason =
                    $"Leader {leader.EntityId} lacks required " +
                    "band or KVLT membership.";

                return false;
            }

            if (!band.HasEntityMember(
                    leader.EntityId
                ) ||
                !kvlt.HasEntityMember(
                    leader.EntityId
                ))
            {
                failureReason =
                    $"Collective membership records are missing " +
                    $"for leader {leader.EntityId}.";

                return false;
            }

            context =
                new ParticipantRoleContext(
                    leader,
                    band
                );

            return true;
        }

        private static bool TryEnterKeeperRole(
            ParticipantRoleContext context,
            Collective kvlt)
        {
            bool leaderMembershipChanged =
                context.Leader
                    .SetActiveCollectiveMembership(
                        StartingCollectiveBootstrapper
                            .KvltId
                    );

            bool bandMembershipChanged =
                context.Band
                    .TrySetEntityMembershipMode(
                        context.Leader.EntityId,
                        CollectiveMembershipMode.Passive
                    );

            bool kvltMembershipChanged =
                kvlt.TrySetEntityMembershipMode(
                    context.Leader.EntityId,
                    CollectiveMembershipMode.Active
                );

            if (!leaderMembershipChanged ||
                !bandMembershipChanged ||
                !kvltMembershipChanged)
            {
                return false;
            }

            context.Band.SetActive(false);

            kvlt.SetLeaderEntity(
                context.Leader.EntityId
            );

            return true;
        }

        private static bool TryExitKeeperRole(
            ParticipantRoleContext context,
            Collective kvlt)
        {
            bool leaderMembershipChanged =
                context.Leader
                    .SetActiveCollectiveMembership(
                        context.Band.CollectiveId
                    );

            bool bandMembershipChanged =
                context.Band
                    .TrySetEntityMembershipMode(
                        context.Leader.EntityId,
                        CollectiveMembershipMode.Active
                    );

            bool kvltMembershipChanged =
                kvlt.TrySetEntityMembershipMode(
                    context.Leader.EntityId,
                    CollectiveMembershipMode.Passive
                );

            if (!leaderMembershipChanged ||
                !bandMembershipChanged ||
                !kvltMembershipChanged)
            {
                return false;
            }

            context.Band.SetActive(true);

            if (kvlt.LeaderEntityId ==
                context.Leader.EntityId)
            {
                ClearKvltLeader(kvlt);
            }

            return true;
        }

        private static void ClearKvltLeader(
            Collective kvlt)
        {
            kvlt?.SetLeaderEntity(
                string.Empty
            );
        }

        private readonly struct
            ParticipantRoleContext
        {
            public GameEntity Leader { get; }
            public Collective Band { get; }

            public ParticipantRoleContext(
                GameEntity leader,
                Collective band)
            {
                Leader = leader;
                Band = band;
            }
        }
    }
}