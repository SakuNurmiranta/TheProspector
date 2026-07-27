using System;
using SEMM91.Core.Entities;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;
using UnityEngine;

namespace SEMM91.GamePlay.Agency
{
    public class PlayerEntityBootstrapper
    {
        private readonly Action<string> _log;

        public PlayerEntityBootstrapper(Action<string> log = null)
        {
            _log = log ?? Debug.Log;
        }

        public GameEntity CreateStartingPlayerEntity(ulong clientId)
        {
            GameObject playerEntityObj = new GameObject($"Leader_{clientId}");
            GameEntity playerEntity = playerEntityObj.AddComponent<GameEntity>();

            playerEntity.InitializeIdentity(
                $"Player {clientId}",
                GameEntityType.Character
            );

            playerEntity.AddAspectId("ASPECT_KNOWS_GUITAR");
            playerEntity.AddAspectId("ASPECT_HAS_GUITAR");

            playerEntity.AddTagContainer(TagContainerType.Resonance);
            playerEntity.AddTagContainer(TagContainerType.Conviction);
            playerEntity.AddTagContainer(TagContainerType.Mood);
            playerEntity.AddTagContainer(TagContainerType.Transient);
            
            playerEntity.TrySetTag(
                TagContainerType.Resonance,
                new TagInstance(TagAxis.Physical, TagPole.Negative, TagDegree.Weak)
            );

            playerEntity.TrySetTag(
                TagContainerType.Conviction,
                new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak)
            );
            playerEntity.TrySetTag(
                TagContainerType.Mood,
                new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak)
                );
            
            AddStartingRehearsalTapes(
                playerEntity,
                clientId
            );

            AddStartingDemoTapes(
                playerEntity,
                clientId
            );

            _log?.Invoke(
                $"[ENTITY SEED] {playerEntity.DisplayName} " +
                $"aspects={playerEntity.AspectIds.Count} " +
                $"tags={playerEntity.TagContainers.Count} " +
                $"vhsSets={playerEntity.VhsSets.Count} " +
                $"vhsTracks=" +
                $"{playerEntity.GetTotalVhsTrackCountFromSets()} " +
                $"demoTapes={playerEntity.DemoTapes.Count}"
            );

            return playerEntity;
        }
        
        private void AddStartingRehearsalTapes(
            GameEntity playerEntity,
            ulong clientId)
        {
            if (playerEntity == null)
                return;

            RehearsalSet firstSet =
                AddStartingRehearsalTape(
                    playerEntity,
                    clientId,
                    setSuffix: "A",
                    displayName:
                    "Rehearsal VHS A",
                    createdTurn: -4,
                    trackCount: 3,
                    baseConveyance: 0.45f
                );

            AddStartingRehearsalTape(
                playerEntity,
                clientId,
                setSuffix: "B",
                displayName:
                "Rehearsal VHS B",
                createdTurn: -3,
                trackCount: 4,
                baseConveyance: 0.55f
            );

            /*
             * AddVhsSet already makes the first set active when
             * no active set exists. Set it explicitly so the
             * bootstrap result remains deterministic.
             */
            if (firstSet != null)
            {
                playerEntity.SetActiveVhsSet(
                    firstSet
                );
            }

            _log?.Invoke(
                "[ENTITY SEED VHS READY] " +
                $"client={clientId} " +
                $"entity={playerEntity.DisplayName} " +
                $"sets={playerEntity.VhsSets.Count} " +
                $"tracks=" +
                $"{playerEntity.GetTotalVhsTrackCountFromSets()} " +
                $"activeSet=" +
                $"{playerEntity.ActiveVhsSetId}"
            );
        }

        private RehearsalSet
            AddStartingRehearsalTape(
                GameEntity playerEntity,
                ulong clientId,
                string setSuffix,
                string displayName,
                int createdTurn,
                int trackCount,
                float baseConveyance)
        {
            if (playerEntity == null ||
                trackCount <= 0)
            {
                return null;
            }

            RehearsalSet rehearsalSet =
                new RehearsalSet(
                    vhsSetId:
                    $"PRESESSION_VHS_SET_" +
                    $"{clientId}_{setSuffix}",
                    displayName:
                    displayName,
                    createdTurn:
                    createdTurn
                );

            for (int trackIndex = 0;
                 trackIndex < trackCount;
                 trackIndex++)
            {
                int displayIndex =
                    trackIndex + 1;

                float conveyance =
                    Mathf.Clamp01(
                        baseConveyance +
                        trackIndex * 0.05f
                    );

                Track track =
                    new Track(
                        vhsTrackId:
                        $"PRESESSION_VHS_TRACK_" +
                        $"{clientId}_{setSuffix}_" +
                        $"{displayIndex}",
                        displayName:
                        $"{displayName} Track " +
                        $"{displayIndex}",
                        initialConveyance:
                        conveyance,
                        createdTurn:
                        createdTurn
                    );

                rehearsalSet.AddTrack(
                    track
                );
            }

            playerEntity.AddVhsSet(
                rehearsalSet
            );

            _log?.Invoke(
                "[ENTITY SEED VHS] " +
                $"client={clientId} " +
                $"entity={playerEntity.DisplayName} " +
                $"set={rehearsalSet.DisplayName} " +
                $"setId={rehearsalSet.VhsSetId} " +
                $"tracks={rehearsalSet.VhsTracks.Count} " +
                $"createdTurn=" +
                $"{rehearsalSet.CreatedTurn}"
            );

            return rehearsalSet;
        }

        private void AddStartingDemoTapes(
            GameEntity playerEntity,
            ulong clientId)
        {
            if (playerEntity == null)
                return;

            /*
             * Startup scene publication consumes the latest
             * unreleased pre-session demo. Add the two test
             * cassettes first and the automatic scene-release
             * cassette last.
             *
             * Result after session initialization:
             * - Demo_1_TEST_A remains Unreleased.
             * - Demo_2_TEST_B remains Unreleased.
             * - Demo_3_STARTING_RELEASE becomes Hosted.
             */
            AddStartingDemoTape(
                playerEntity,
                clientId,
                cassetteSuffix: "TEST_A",
                displayName: "Demo_1_TEST_A",
                trackDisplayName: "Test Track A",
                recordedTurn: -3,
                takeCount: 1,
                conveyance: 0.55f
            );

            AddStartingDemoTape(
                playerEntity,
                clientId,
                cassetteSuffix: "TEST_B",
                displayName: "Demo_2_TEST_B",
                trackDisplayName: "Test Track B",
                recordedTurn: -2,
                takeCount: 2,
                conveyance: 0.65f
            );

            AddStartingDemoTape(
                playerEntity,
                clientId,
                cassetteSuffix: "STARTING_RELEASE",
                displayName: "Demo_3_STARTING_RELEASE",
                trackDisplayName: "Old Track",
                recordedTurn: -1,
                takeCount: 1,
                conveyance: 0.60f
            );

            _log?.Invoke(
                "[ENTITY SEED DEMOS READY] " +
                $"client={clientId} " +
                $"entity={playerEntity.DisplayName} " +
                $"total={playerEntity.DemoTapes.Count} " +
                "expectedUnreleasedAfterStartup=2"
            );
        }

        private void AddStartingDemoTape(
            GameEntity playerEntity,
            ulong clientId,
            string cassetteSuffix,
            string displayName,
            string trackDisplayName,
            int recordedTurn,
            int takeCount,
            float conveyance)
        {
            if (playerEntity == null)
                return;

            string sourceSetId =
                $"PRESESSION_SET_{clientId}_{cassetteSuffix}";

            DemoTapeTrackSnapshot trackSnapshot =
                new DemoTapeTrackSnapshot(
                    sourceTrackId:
                    $"PRESESSION_TRACK_{clientId}_{cassetteSuffix}",
                    displayName:
                    trackDisplayName,
                    sourceConveyance:
                    conveyance,
                    recordedConveyance:
                    conveyance
                );

            DemoTape startingDemo =
                new DemoTape(
                    demoTapeId:
                    $"PRESESSION_DEMO_{clientId}_{cassetteSuffix}",
                    displayName:
                    displayName,
                    sourceSetId:
                    sourceSetId,
                    sourceSetName:
                    $"Pre-Session Set {cassetteSuffix}",
                    recordedTurn:
                    recordedTurn,
                    takeCount:
                    takeCount,
                    recordingInterest:
                    conveyance,
                    snapshots:
                    new[]
                    {
                        trackSnapshot
                    }
                );

            playerEntity.AddDemoTape(
                startingDemo
            );

            _log?.Invoke(
                "[ENTITY SEED DEMO] " +
                $"client={clientId} " +
                $"entity={playerEntity.DisplayName} " +
                $"demo={startingDemo.DisplayName} " +
                $"demoId={startingDemo.DemoTapeId} " +
                $"recordedTurn={startingDemo.RecordedTurn} " +
                $"takes={startingDemo.TakeCount} " +
                $"avgConveyance=" +
                $"{startingDemo.AverageConveyance:F2}"
            );
        }

    }
}