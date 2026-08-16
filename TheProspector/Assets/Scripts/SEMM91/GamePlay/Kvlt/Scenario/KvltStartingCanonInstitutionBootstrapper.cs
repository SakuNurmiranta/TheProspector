using System;
using System.Collections.Generic;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    /// <summary>
    /// Materializes the institutional SceneRelease
    /// portion of an already-established Scenario
    /// starting state.
    ///
    /// Preconditions:
    ///
    /// - starting Canon/Society/Normative state exists;
    /// - the founding DemoTape exists;
    /// - the founding Keeper tenure already exists.
    ///
    /// This operation does not replay any historical
    /// Promotion, Happening, activation, movement or
    /// Canon-settlement event.
    /// </summary>
    public sealed class
        KvltStartingCanonInstitutionBootstrapper
    {
        private readonly
            KvltStartingCanonReleaseBootstrapper
            releaseBootstrapper =
                new();

        private readonly
            KvltStartingCanonFrontierResolver
            frontierResolver =
                new();

        private readonly
            SceneReleaseCanonGravityDecompositionService
            decompositionService =
                new();

        public
            KvltStartingCanonInstitutionBootstrapResult
            Apply(
                SeededWorldState worldState,
                DemoTape foundingDemoTape,
                string foundingOwnerEntityId,
                string sceneId,
                string keeperTenureId,
                float startingCanonScenePosition,
                int startingTurn)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(
                    nameof(worldState)
                );
            }

            if (foundingDemoTape == null)
            {
                throw new ArgumentNullException(
                    nameof(foundingDemoTape)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    foundingOwnerEntityId))
            {
                throw new ArgumentException(
                    "Founding owner entity identity " +
                    "cannot be empty.",
                    nameof(foundingOwnerEntityId)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    sceneId))
            {
                throw new ArgumentException(
                    "Starting Scene identity cannot be " +
                    "empty.",
                    nameof(sceneId)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    keeperTenureId))
            {
                throw new ArgumentException(
                    "Starting canonical institution " +
                    "requires an active Keeper tenure.",
                    nameof(keeperTenureId)
                );
            }

            if (startingTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingTurn)
                );
            }

            /*
             * Entry 6 is a prerequisite.
             *
             * This service does not own construction of
             * the starting semantic environment because
             * Keeper assignment occurs between those two
             * bootstrap phases at runtime.
             */
            if (worldState.KvltCanon.Records.Count == 0)
            {
                throw new InvalidOperationException(
                    "Starting canonical institution " +
                    "requires an established starting Canon."
                );
            }

            KvltStartingCanonReleaseBootstrapResult
                releaseBootstrap =
                    releaseBootstrapper.Apply(
                        worldState,
                        foundingDemoTape,
                        foundingOwnerEntityId,
                        sceneId,
                        keeperTenureId,
                        startingCanonScenePosition,
                        startingTurn
                    );

            IReadOnlyList<
                    SceneReleaseCanonFrontierClaim>
                frontierClaims =
                    frontierResolver.Resolve(
                        worldState,
                        foundingDemoTape,
                        releaseBootstrap.Release,
                        foundingOwnerEntityId
                    );

            SceneReleaseCanonGravityDecompositionState
                decomposition =
                    decompositionService.Apply(
                        releaseBootstrap.Release,
                        releaseBootstrap
                            .FinalLegitimacy,
                        frontierClaims
                    );

            /*
             * At this point the SceneRelease is a
             * complete starting Canon institution:
             *
             * - CanonRetained
             * - immutable activation/freeze
             * - exact frontier provenance
             * - claim-level Gravity decomposition
             *
             * No Score is paid here. Turn settlement
             * owns all payouts.
             */
            return new
                KvltStartingCanonInstitutionBootstrapResult(
                    releaseBootstrap.Release,
                    releaseBootstrap.FinalLegitimacy,
                    releaseBootstrap.FreezeState,
                    frontierClaims,
                    decomposition
                );
        }
    }
}
