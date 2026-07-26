using System;
using SEMM91.Core.Entities;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
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
            
            AddStartingDemoTapes(
                playerEntity,
                clientId
            );

            _log?.Invoke(
                $"[ENTITY SEED] {playerEntity.DisplayName} " +
                $"aspects={playerEntity.AspectIds.Count} " +
                $"tags={playerEntity.TagContainers.Count}"
            );

            return playerEntity;
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