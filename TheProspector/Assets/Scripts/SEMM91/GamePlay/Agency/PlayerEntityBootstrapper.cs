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
            
            AddStartingDemoTape(
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
        
        private void AddStartingDemoTape(
            GameEntity playerEntity,
            ulong clientId)
        {
            if (playerEntity == null)
                return;

            const float startingConveyance =
                0.60f;

            DemoTapeTrackSnapshot trackSnapshot =
                new DemoTapeTrackSnapshot(
                    sourceTrackId:
                    $"PRESESSION_TRACK_{clientId}",
                    displayName:
                    "Old Track",
                    sourceConveyance:
                    startingConveyance,
                    recordedConveyance:
                    startingConveyance
                );

            DemoTape startingDemo =
                new DemoTape(
                    demoTapeId:
                    $"PRESESSION_DEMO_{clientId}",
                    displayName:
                    "Demo_1",
                    sourceSetId:
                    $"PRESESSION_SET_{clientId}",
                    sourceSetName:
                    "Pre-Session Set",
                    recordedTurn:
                    -1,
                    takeCount:
                    1,
                    recordingInterest:
                    startingConveyance,
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
                $"avgConveyance=" +
                $"{startingDemo.AverageConveyance:F2}"
            );
        }
    }
}