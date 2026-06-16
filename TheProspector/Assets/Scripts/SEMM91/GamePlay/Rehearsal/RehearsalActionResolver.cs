using System;
using System.Collections.Generic;
using System.Linq;
using SEMM91.Core.Recordings;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tracks;
using SEMM91.GamePlay.Entities;
using UnityEngine;

namespace SEMM91.GamePlay.Rehearsal
{
    public class RehearsalActionResolver
    {
        private const float RecordingFluctuationMin = -0.05f;
        private const float RecordingFluctuationMax = 0.05f;
        
        private readonly Func<int> _getCurrentTurn;
        private readonly Action<string> _log;

        public RehearsalActionResolver(
            Func<int> getCurrentTurn,
            Action<string> log = null)
        {
            _getCurrentTurn = getCurrentTurn;
            _log = log ?? Debug.Log;
        }

        public void ResolveRehearseActiveSet(
            ulong clientId,
            GameEntity controller,
            byte committedActions)
        {
            if (controller == null)
            {
                _log?.Invoke($"[BLOCKED] Client {clientId} has no controller entity.");
                return;
            }

            if (controller.Ideas.Count == 0)
            {
                RehearseActiveVhsSet(clientId, controller);
                return;
            }

            RehearsalSet activeSet = GetOrCreateActiveVhsSet(clientId, controller);

            string vhsTrackId = Guid.NewGuid().ToString();
            string vhsTrackName = $"Track_{activeSet.VhsTracks.Count + 1}";

            float conveyance = 0.35f;

            Track track = new Track(
                vhsTrackId,
                vhsTrackName,
                conveyance,
                _getCurrentTurn()
            );

            Idea idea = controller.Ideas[0];

            track.AddIdea(idea);

            if (!controller.RemoveIdea(idea))
            {
                _log?.Invoke($"[BLOCKED] Client {clientId} could not remove idea from controller.");
                return;
            }

            activeSet.AddTrack(track);

            _log?.Invoke(
                $"[REHEARSE CREATED] Client {clientId} controller={controller.DisplayName} " +
                $"set={activeSet.DisplayName} vhsTrack={track.DisplayName} " +
                $"ideasInVhs={track.Ideas.Count} conveyance={track.Conveyance:0.00} " +
                $"rehearsals={track.RehearsalCount} raw={track.IsRaw} " +
                $"honed={track.IsHoned} totalVhsTracks={controller.GetTotalVhsTrackCountFromSets()}"
            );
        }

        private void RehearseActiveVhsSet(
            ulong clientId,
            GameEntity controller)
        {
            RehearsalSet activeRehearsalSet = controller.GetActiveVhsSet();

            if (activeRehearsalSet == null)
            {
                _log?.Invoke($"[BLOCKED] Client {clientId} has no vhs set.");
                return;
            }

            if (activeRehearsalSet.VhsTracks.Count == 0)
            {
                _log?.Invoke($"[BLOCKED] Client {clientId} has no vhs tracks.");
                return;
            }

            float gain = GetSingleRehearsalPayloadConveyanceGain();

            activeRehearsalSet.RehearseAll(gain, _getCurrentTurn());

            _log?.Invoke(
                $"[REHEARSE SET UPDATED] Client {clientId} controller={controller.DisplayName} " +
                $"set={activeRehearsalSet.DisplayName} tracks={activeRehearsalSet.VhsTracks.Count} " +
                $"gain={gain:0.00} lastRehearsedTurn={activeRehearsalSet.LastRehearsedTurn}"
            );
        }

        private RehearsalSet GetOrCreateActiveVhsSet(
            ulong clientId,
            GameEntity controller)
        {
            RehearsalSet activeSet = controller.GetActiveVhsSet();

            if (activeSet != null)
                return activeSet;

            string setId = Guid.NewGuid().ToString();
            string setName = $"Set_{controller.VhsSets.Count + 1}";

            activeSet = new RehearsalSet(setId, setName, _getCurrentTurn());

            controller.AddVhsSet(activeSet);
            controller.SetActiveVhsSet(activeSet);

            _log?.Invoke(
                $"[VHS SET CREATED] Client {clientId} controller={controller.DisplayName} " +
                $"set={activeSet.DisplayName} totalSets={controller.VhsSets.Count}"
            );

            return activeSet;
        }

        private float GetSingleRehearsalPayloadConveyanceGain()
        {
            return 0.10f;
        }
        
        public bool TryCreateNewActiveEmptyVhsSet(
            ulong clientId,
            GameEntity controller,
            out string message)
        {
            message = "";

            if (controller == null)
            {
                message = $"[CREATE SET BLOCKED] Client {clientId} has no controller entity.";
                return false;
            }

            foreach (RehearsalSet existingSet in controller.VhsSets)
            {
                if (existingSet == null)
                    continue;

                if (existingSet.VhsTracks.Count == 0)
                {
                    message =
                        $"[CREATE SET BLOCKED] Client {clientId} already has an empty set: " +
                        $"{existingSet.DisplayName}.";

                    return false;
                }
            }

            string setId = Guid.NewGuid().ToString();
            string setName = $"Set_{controller.VhsSets.Count + 1}";

            RehearsalSet newSet = new RehearsalSet(
                setId,
                setName,
                _getCurrentTurn()
            );

            controller.AddVhsSet(newSet);
            controller.SetActiveVhsSet(newSet);

            message =
                $"[CREATE SET] Client {clientId} controller={controller.DisplayName} " +
                $"activeSet={newSet.DisplayName} totalSets={controller.VhsSets.Count}";

            _log?.Invoke(message);

            return true;
        }
        
        public bool TryRecordActiveSetToDemo(
            ulong clientId,
            GameEntity playerEntity,
            int takeCount,
            out string message)
        {
            message = "";

            if (playerEntity == null)
            {
                message = $"[RECORD BLOCKED] Client {clientId} has no player entity.";
                return false;
            }

            if (takeCount <= 0)
            {
                message = $"[RECORD BLOCKED] Client {clientId} has no recording takes.";
                return false;
            }

            RehearsalSet activeSet = playerEntity.GetActiveVhsSet();

            if (activeSet == null)
            {
                message = $"[RECORD BLOCKED] Client {clientId} has no active set.";
                return false;
            }

            if (activeSet.VhsTracks.Count == 0)
            {
                message = $"[RECORD BLOCKED] Client {clientId} active set {activeSet.DisplayName} has no tracks.";
                return false;
            }

            RecordingTake bestTake = null;

            for (int i = 0; i < takeCount; i++)
            {
                RecordingTake take = CreateTake(activeSet, i + 1);

                if (bestTake == null || take.RecordingInterest > bestTake.RecordingInterest)
                    bestTake = take;
            }

            if (bestTake == null)
            {
                message = $"[RECORD BLOCKED] Client {clientId} could not create a take.";
                return false;
            }

            string demoId = Guid.NewGuid().ToString();
            string demoName = $"Demo_{playerEntity.DemoTapes.Count + 1}";

            DemoTape demoTape = new DemoTape(
                demoId,
                demoName,
                activeSet.VhsSetId,
                activeSet.DisplayName,
                _getCurrentTurn(),
                takeCount,
                bestTake.RecordingInterest,
                bestTake.TrackSnapshots
            );

            playerEntity.AddDemoTape(demoTape);

            message =
                $"[RECORDED] Client {clientId} demo={demoTape.DisplayName} " +
                $"sourceSet={activeSet.DisplayName} takes={takeCount} " +
                $"interest={demoTape.RecordingInterest:0.00} " +
                $"avgC={demoTape.AverageConveyance:0.00} tracks={demoTape.TrackSnapshots.Count}";

            _log?.Invoke(message);

            return true;
        }
        
        private RecordingTake CreateTake(
            RehearsalSet sourceSet,
            int takeNumber)
        {
            List<DemoTapeTrackSnapshot> snapshots = new();

            foreach (Track sourceTrack in sourceSet.VhsTracks)
            {
                float sourceConveyance = sourceTrack.Conveyance;

                float fluctuation = UnityEngine.Random.Range(
                    RecordingFluctuationMin,
                    RecordingFluctuationMax
                );

                float recordedConveyance = Mathf.Clamp01(sourceConveyance + fluctuation);

                snapshots.Add(new DemoTapeTrackSnapshot(
                    sourceTrack.VhsTrackId,
                    sourceTrack.DisplayName,
                    sourceConveyance,
                    recordedConveyance
                ));
            }

            float interest = CalculateRecordingInterest(snapshots);

            return new RecordingTake(
                takeNumber,
                interest,
                snapshots
            );
        }

        private float CalculateRecordingInterest(
            IReadOnlyList<DemoTapeTrackSnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
                return 0f;

            float average = snapshots.Average(t => t.RecordedConveyance);
            float peak = snapshots.Max(t => t.RecordedConveyance);

            List<float> breakouts = snapshots
                .Where(t => t.RecordedConveyance > t.SourceConveyance)
                .Select(t => t.RecordedConveyance - t.SourceConveyance)
                .ToList();

            List<float> collapses = snapshots
                .Where(t => t.RecordedConveyance < t.SourceConveyance)
                .Select(t => t.SourceConveyance - t.RecordedConveyance)
                .ToList();

            float averageBreakout = breakouts.Count == 0
                ? 0f
                : breakouts.Average();

            float averageCollapse = collapses.Count == 0
                ? 0f
                : collapses.Average();

            return average + peak + averageBreakout - averageCollapse;
        }

        private class RecordingTake
        {
            public int TakeNumber { get; }
            public float RecordingInterest { get; }
            public IReadOnlyList<DemoTapeTrackSnapshot> TrackSnapshots { get; }

            public RecordingTake(
                int takeNumber,
                float recordingInterest,
                IReadOnlyList<DemoTapeTrackSnapshot> trackSnapshots)
            {
                TakeNumber = takeNumber;
                RecordingInterest = recordingInterest;
                TrackSnapshots = trackSnapshots;
            }
        }
    }
    
}