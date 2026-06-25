using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Applies SceneRelease legacy and Keeper-tenure consequences
    /// after Keeper selection has already been classified.
    ///
    /// The resolver does not select the Keeper and does not write
    /// networking state.
    /// </summary>
    public sealed class KeeperLegacyResolver
    {
        public KeeperLegacyResolution Resolve(
            KeeperTransitionResult transition,
            KeeperTenureState currentTenure,
            SeededWorldState world,
            string nextKeeperOwnerEntityId)
        {
            if (!transition.HasResult)
            {
                return new KeeperLegacyResolution(
                    transition,
                    currentTenure
                );
            }

            switch (transition.Reason)
            {
                case KeeperTransitionReason
                    .InitialAssignment:

                    return ResolveInitialAssignment(
                        transition,
                        world,
                        nextKeeperOwnerEntityId
                    );

                case KeeperTransitionReason
                    .YearEndRetained:

                    return ResolveYearEndRetained(
                        transition,
                        currentTenure,
                        world,
                        nextKeeperOwnerEntityId
                    );

                case KeeperTransitionReason
                    .YearEndReplaced:

                    return ResolveYearEndReplaced(
                        transition,
                        currentTenure,
                        world,
                        nextKeeperOwnerEntityId
                    );

                case KeeperTransitionReason
                    .DisconnectionFallback:

                    return ResolveDisconnectionFallback(
                        transition,
                        currentTenure,
                        world,
                        nextKeeperOwnerEntityId
                    );

                case KeeperTransitionReason
                    .SceneCollapseLocked:

                    return ResolveSceneCollapseLock(
                        transition,
                        currentTenure
                    );

                default:

                    return new KeeperLegacyResolution(
                        transition,
                        currentTenure
                    );
            }
        }

        private static KeeperLegacyResolution
            ResolveInitialAssignment(
                KeeperTransitionResult transition,
                SeededWorldState world,
                string nextKeeperOwnerEntityId)
        {
            if (!transition.HasAssignedKeeper)
            {
                return new KeeperLegacyResolution(
                    transition,
                    nextTenure: null
                );
            }

            KeeperTenureState nextTenure =
                KeeperTenureState.Create(
                    transition.NextKeeperClientId,
                    transition.ResolvedRound,
                    transition.PullGrant
                );

            string incomingSubjectReleaseId =
                TryBeginNewSubject(
                    world,
                    nextKeeperOwnerEntityId,
                    transition.ResolvedRound
                );

            if (!string.IsNullOrWhiteSpace(
                    incomingSubjectReleaseId
                ))
            {
                nextTenure =
                    nextTenure
                        .WithCanonizationSubject(
                            incomingSubjectReleaseId
                        );
            }

            KeeperTransitionResult enriched =
                transition.WithLegacy(
                    previousSubjectReleaseId:
                        string.Empty,
                    canonizedReleaseId:
                        string.Empty,
                    incomingSubjectReleaseId:
                        incomingSubjectReleaseId
                );

            return new KeeperLegacyResolution(
                enriched,
                nextTenure
            );
        }

        private static KeeperLegacyResolution
            ResolveYearEndRetained(
                KeeperTransitionResult transition,
                KeeperTenureState currentTenure,
                SeededWorldState world,
                string nextKeeperOwnerEntityId)
        {
            bool existingTenureMatchesKeeper =
                currentTenure != null &&
                currentTenure.KeeperClientId ==
                transition.NextKeeperClientId;
            
            KeeperTenureState nextTenure =
                EnsureTenureForAssignedKeeper(
                    transition,
                    currentTenure
                );

            if (nextTenure == null)
            {
                return new KeeperLegacyResolution(
                    transition,
                    nextTenure: null
                );
            }
            
            if (existingTenureMatchesKeeper)
            {
                nextTenure =
                    nextTenure.AddPull(
                        transition.PullGrant
                    );
            }

            string previousSubjectReleaseId =
                nextTenure
                    .CanonizationSubjectReleaseId;

            string incomingSubjectReleaseId =
                previousSubjectReleaseId;

            if (IsCurrentCanonizationSubject(
                    world,
                    previousSubjectReleaseId
                ))
            {
                bool worldAdvanced =
                    world
                        .TryAdvanceCanonizationSubjectYear(
                            previousSubjectReleaseId
                        );

                if (worldAdvanced)
                {
                    nextTenure =
                        nextTenure
                            .AdvanceCanonizationSubjectYear();
                }
            }
            else
            {
                incomingSubjectReleaseId =
                    TryBeginNewSubject(
                        world,
                        nextKeeperOwnerEntityId,
                        transition.ResolvedRound
                    );

                if (!string.IsNullOrWhiteSpace(
                        incomingSubjectReleaseId
                    ))
                {
                    nextTenure =
                        nextTenure
                            .WithCanonizationSubject(
                                incomingSubjectReleaseId
                            );
                }
            }

            KeeperTransitionResult enriched =
                transition.WithLegacy(
                    previousSubjectReleaseId,
                    canonizedReleaseId:
                        string.Empty,
                    incomingSubjectReleaseId
                );

            return new KeeperLegacyResolution(
                enriched,
                nextTenure
            );
        }

        private static KeeperLegacyResolution
            ResolveYearEndReplaced(
                KeeperTransitionResult transition,
                KeeperTenureState currentTenure,
                SeededWorldState world,
                string nextKeeperOwnerEntityId)
        {
            string previousSubjectReleaseId =
                currentTenure?
                    .CanonizationSubjectReleaseId ??
                string.Empty;

            string canonizedReleaseId =
                string.Empty;

            if (IsCurrentCanonizationSubject(
                    world,
                    previousSubjectReleaseId
                ))
            {
                bool canonized =
                    world.TryCanonizeSceneRelease(
                        previousSubjectReleaseId,
                        transition.ResolvedRound,
                        transition.NextKeeperClientId
                    );

                if (canonized)
                {
                    canonizedReleaseId =
                        previousSubjectReleaseId;
                }
            }

            KeeperTenureState nextTenure =
                transition.HasAssignedKeeper
                    ? KeeperTenureState.Create(
                        transition.NextKeeperClientId,
                        transition.ResolvedRound,
                        transition.PullGrant
                    )
                    : null;

            string incomingSubjectReleaseId =
                string.Empty;

            if (nextTenure != null)
            {
                incomingSubjectReleaseId =
                    TryBeginNewSubject(
                        world,
                        nextKeeperOwnerEntityId,
                        transition.ResolvedRound
                    );

                if (!string.IsNullOrWhiteSpace(
                        incomingSubjectReleaseId
                    ))
                {
                    nextTenure =
                        nextTenure
                            .WithCanonizationSubject(
                                incomingSubjectReleaseId
                            );
                }
            }

            KeeperTransitionResult enriched =
                transition.WithLegacy(
                    previousSubjectReleaseId,
                    canonizedReleaseId,
                    incomingSubjectReleaseId
                );

            return new KeeperLegacyResolution(
                enriched,
                nextTenure
            );
        }

        private static KeeperLegacyResolution
            ResolveDisconnectionFallback(
                KeeperTransitionResult transition,
                KeeperTenureState currentTenure,
                SeededWorldState world,
                string nextKeeperOwnerEntityId)
        {
            string previousSubjectReleaseId =
                currentTenure?
                    .CanonizationSubjectReleaseId ??
                string.Empty;

            if (!transition.HasAssignedKeeper)
            {
                KeeperTransitionResult unassigned =
                    transition.WithLegacy(
                        previousSubjectReleaseId,
                        canonizedReleaseId:
                            string.Empty,
                        incomingSubjectReleaseId:
                            string.Empty
                    );

                return new KeeperLegacyResolution(
                    unassigned,
                    nextTenure: null
                );
            }

            KeeperTenureState nextTenure;

            string incomingSubjectReleaseId =
                previousSubjectReleaseId;

            if (currentTenure != null)
            {
                /*
                 * Emergency succession preserves the institutional
                 * subject and Pull. It does not canonize anything.
                 */
                nextTenure =
                    currentTenure.TransferTo(
                        transition.NextKeeperClientId,
                        transition.ResolvedRound
                    );
            }
            else
            {
                nextTenure =
                    KeeperTenureState.Create(
                        transition.NextKeeperClientId,
                        transition.ResolvedRound,
                        transition.PullGrant
                    );

                incomingSubjectReleaseId =
                    TryBeginNewSubject(
                        world,
                        nextKeeperOwnerEntityId,
                        transition.ResolvedRound
                    );

                if (!string.IsNullOrWhiteSpace(
                        incomingSubjectReleaseId
                    ))
                {
                    nextTenure =
                        nextTenure
                            .WithCanonizationSubject(
                                incomingSubjectReleaseId
                            );
                }
            }

            KeeperTransitionResult enriched =
                transition.WithLegacy(
                    previousSubjectReleaseId,
                    canonizedReleaseId:
                        string.Empty,
                    incomingSubjectReleaseId
                );

            return new KeeperLegacyResolution(
                enriched,
                nextTenure
            );
        }

        private static KeeperLegacyResolution
            ResolveSceneCollapseLock(
                KeeperTransitionResult transition,
                KeeperTenureState currentTenure)
        {
            string preservedSubjectReleaseId =
                currentTenure?
                    .CanonizationSubjectReleaseId ??
                string.Empty;

            KeeperTransitionResult enriched =
                transition.WithLegacy(
                    previousSubjectReleaseId:
                        preservedSubjectReleaseId,
                    canonizedReleaseId:
                        string.Empty,
                    incomingSubjectReleaseId:
                        preservedSubjectReleaseId
                );

            /*
             * The apocalypse round preserves the incumbent,
             * their tenure, subject age and Pull exactly.
             */
            return new KeeperLegacyResolution(
                enriched,
                currentTenure
            );
        }

        private static KeeperTenureState
            EnsureTenureForAssignedKeeper(
                KeeperTransitionResult transition,
                KeeperTenureState currentTenure)
        {
            if (!transition.HasAssignedKeeper)
                return null;

            if (currentTenure != null &&
                currentTenure.KeeperClientId ==
                transition.NextKeeperClientId)
            {
                return currentTenure;
            }

            return KeeperTenureState.Create(
                transition.NextKeeperClientId,
                transition.ResolvedRound,
                transition.PullGrant
            );
        }

        private static bool
            IsCurrentCanonizationSubject(
                SeededWorldState world,
                string releaseId)
        {
            if (world == null ||
                string.IsNullOrWhiteSpace(
                    releaseId
                ))
            {
                return false;
            }

            SceneRelease release =
                world.FindSceneRelease(
                    releaseId
                );

            return release != null &&
                   release.LegacyState ==
                   SceneLegacyState
                       .CanonizationSubject;
        }

        private static string
            TryBeginNewSubject(
                SeededWorldState world,
                string ownerEntityId,
                int startedRound)
        {
            if (world == null ||
                string.IsNullOrWhiteSpace(
                    ownerEntityId
                ))
            {
                return string.Empty;
            }

            bool found =
                world
                    .TrySelectPrimaryWorkProxyForOwner(
                        ownerEntityId,
                        out SceneRelease selected
                    );

            if (!found ||
                selected == null)
            {
                return string.Empty;
            }

            bool beganSubject =
                world.TryBeginCanonizationSubject(
                    selected.ReleaseId,
                    startedRound
                );

            return beganSubject
                ? selected.ReleaseId
                : string.Empty;
        }
    }
}