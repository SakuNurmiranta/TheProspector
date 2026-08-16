using System;
using System.Collections.Generic;
using System.Linq;
using SEMM91.Core.Entities;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tracks;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    /// <summary>
    /// Applies the player/media portion of the
    /// configured Peak-2 initial state.
    ///
    /// Networking ownership and Keeper replication
    /// remain GameCoordinator responsibilities.
    /// </summary>
    public sealed class
        Peak2StartingScenarioBootstrapper
    {
        public const string FoundingBandName =
            "Mayhem";

        public KvltStartingScenarioBootstrapResult
            Bootstrap(
                KvltScenarioProfile profile,
                IReadOnlyDictionary<
                    ulong,
                    GameEntity>
                    participantsByClientId,
                IReadOnlyCollection<ulong>
                    humanClientIds,
                IReadOnlyCollection<ulong>
                    botClientIds,
                string foundingDemoTapeJson,
                int startingTurn,
                bool allowSoloDevelopmentMode =
                    false)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(
                    nameof(profile)
                );
            }

            if (participantsByClientId == null)
            {
                throw new ArgumentNullException(
                    nameof(participantsByClientId)
                );
            }

            if (humanClientIds == null)
            {
                throw new ArgumentNullException(
                    nameof(humanClientIds)
                );
            }

            if (botClientIds == null)
            {
                throw new ArgumentNullException(
                    nameof(botClientIds)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    foundingDemoTapeJson))
            {
                throw new ArgumentException(
                    "Founding authored DemoTape JSON " +
                    "cannot be empty.",
                    nameof(foundingDemoTapeJson)
                );
            }

            if (startingTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingTurn)
                );
            }

            bool solo =
                allowSoloDevelopmentMode &&
                participantsByClientId.Count == 1 &&
                humanClientIds.Count == 1 &&
                botClientIds.Count == 0;

            if (!solo)
            {
                if (participantsByClientId.Count !=
                    profile.RequiredKvltPlayerCount)
                {
                    throw new ArgumentException(
                        "Peak-2 participant population " +
                        "does not match Scenario Profile | " +
                        $"expected=" +
                        $"{profile.RequiredKvltPlayerCount} " +
                        $"actual=" +
                        $"{participantsByClientId.Count}"
                    );
                }

                if (humanClientIds.Count != 1)
                {
                    throw new ArgumentException(
                        "Peak-2 requires exactly one " +
                        "human participant."
                    );
                }

                if (botClientIds.Count !=
                    profile.RequiredKvltPlayerCount -
                    1)
                {
                    throw new ArgumentException(
                        "Peak-2 requires four " +
                        "tabula-rasa bot participants."
                    );
                }
            }

            if (humanClientIds.Count != 1)
            {
                throw new ArgumentException(
                    "A founding human participant is " +
                    "required."
                );
            }

            ulong humanClientId =
                humanClientIds.First();

            HashSet<ulong> claimedIds =
                new();

            if (!claimedIds.Add(
                    humanClientId))
            {
                throw new ArgumentException(
                    "Duplicate participant identity."
                );
            }

            foreach (
                ulong botClientId
                in botClientIds)
            {
                if (!claimedIds.Add(
                        botClientId))
                {
                    throw new ArgumentException(
                        "Human/bot participant roles " +
                        "overlap or contain duplicates."
                    );
                }
            }

            if (claimedIds.Count !=
                participantsByClientId.Count)
            {
                throw new ArgumentException(
                    "Ready-role population does not " +
                    "exactly match playable participants."
                );
            }

            foreach (
                ulong clientId
                in claimedIds)
            {
                if (!participantsByClientId
                        .TryGetValue(
                            clientId,
                            out GameEntity entity) ||
                    entity == null)
                {
                    throw new ArgumentException(
                        "Ready participant has no " +
                        "playable GameEntity | " +
                        $"client={clientId}"
                    );
                }

                /*
                 * Scenario bootstrap is one-shot.
                 *
                 * A starting authored recording must be
                 * installed together with its logical
                 * source RehearsalSet. Therefore bootstrap
                 * begins only over clean media state.
                 */
                if (entity.DemoTapes.Count != 0 ||
                    entity.VhsSets.Count != 0)
                {
                    throw new InvalidOperationException(
                        "Starting Scenario cannot be " +
                        "applied over existing media state | " +
                        $"client={clientId} | " +
                        $"vhsSets={entity.VhsSets.Count} | " +
                        $"demoTapes={entity.DemoTapes.Count}"
                    );
                }
            }

            GameEntity mayhem =
                participantsByClientId[
                    humanClientId
                ];

            mayhem.InitializeIdentity(
                FoundingBandName,
                mayhem.EntityType
            );

            /*
             * Bot ordering is deterministic and does not
             * depend on connection order.
             */
            ulong[] orderedBots =
                botClientIds
                    .OrderBy(id => id)
                    .ToArray();

            for (int index = 0;
                 index < orderedBots.Length;
                 index++)
            {
                ulong botClientId =
                    orderedBots[index];

                GameEntity bot =
                    participantsByClientId[
                        botClientId
                    ];

                bot.InitializeIdentity(
                    $"Band {index + 1}",
                    bot.EntityType
                );
            }

            /*
             * Build both the authored cassette and the
             * exact rehearsal source from which its
             * immutable snapshots are produced.
             */
            DemoTape foundingDemoTape =
                AuthoredDemoTapeJsonLoader.Load(
                    foundingDemoTapeJson,
                    mayhem.EntityId,
                    startingTurn,
                    out RehearsalSet
                        foundingRehearsalSet
                );

            if (foundingDemoTape.DemoTapeId !=
                Peak2KvltScenarioProfileFactory
                    .FoundingDemoTapeId)
            {
                throw new InvalidOperationException(
                    "Configured founding DemoTape does " +
                    "not match the Peak-2 profile | " +
                    $"expected=" +
                    $"{Peak2KvltScenarioProfileFactory.FoundingDemoTapeId} " +
                    $"actual=" +
                    $"{foundingDemoTape.DemoTapeId}"
                );
            }

            /*
             * Defensive causal consistency check.
             *
             * The loader should already guarantee this,
             * but scenario bootstrap must never install
             * a DemoTape whose declared source set does
             * not match the accompanying rehearsal VHS.
             */
            if (foundingDemoTape.SourceSetId !=
                foundingRehearsalSet.VhsSetId)
            {
                throw new InvalidOperationException(
                    "Founding DemoTape source does not " +
                    "match its RehearsalSet | " +
                    $"demoSourceSet=" +
                    $"{foundingDemoTape.SourceSetId} | " +
                    $"rehearsalSet=" +
                    $"{foundingRehearsalSet.VhsSetId}"
                );
            }

            /*
             * Source first, recording second.
             *
             * Mayhem therefore begins with the exact
             * rehearsal VHS logically prior to the
             * Freezing Moon cassette.
             */
            mayhem.AddVhsSet(
                foundingRehearsalSet
            );

            mayhem.AddDemoTape(
                foundingDemoTape
            );

            return new
                KvltStartingScenarioBootstrapResult(
                    humanClientId,
                    mayhem.EntityId,
                    foundingDemoTape.DemoTapeId,
                    participantsByClientId.Count
                );
        }
    }
}