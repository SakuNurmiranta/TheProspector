using System;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tracks;
using SEMM91.GamePlay.Entities;
using UnityEngine;

namespace SEMM91.GamePlay.Rehearsal
{
    public class ActionResolver
    {
        private readonly Func<int> _getCurrentTurn;
        private readonly Action<string> _log;

        public ActionResolver(
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

            VhsSet activeSet = GetOrCreateActiveVhsSet(clientId, controller);

            string vhsTrackId = Guid.NewGuid().ToString();
            string vhsTrackName = $"Track_{activeSet.VhsTracks.Count + 1}";

            float conveyance = 0.35f;

            VhsTrack vhsTrack = new VhsTrack(
                vhsTrackId,
                vhsTrackName,
                conveyance,
                _getCurrentTurn()
            );

            Idea idea = controller.Ideas[0];

            vhsTrack.AddIdea(idea);

            if (!controller.RemoveIdea(idea))
            {
                _log?.Invoke($"[BLOCKED] Client {clientId} could not remove idea from controller.");
                return;
            }

            activeSet.AddTrack(vhsTrack);

            _log?.Invoke(
                $"[REHEARSE CREATED] Client {clientId} controller={controller.DisplayName} " +
                $"set={activeSet.DisplayName} vhsTrack={vhsTrack.DisplayName} " +
                $"ideasInVhs={vhsTrack.Ideas.Count} conveyance={vhsTrack.Conveyance:0.00} " +
                $"rehearsals={vhsTrack.RehearsalCount} raw={vhsTrack.IsRaw} " +
                $"honed={vhsTrack.IsHoned} totalVhsTracks={controller.GetTotalVhsTrackCountFromSets()}"
            );
        }

        private void RehearseActiveVhsSet(
            ulong clientId,
            GameEntity controller)
        {
            VhsSet activeVhsSet = controller.GetActiveVhsSet();

            if (activeVhsSet == null)
            {
                _log?.Invoke($"[BLOCKED] Client {clientId} has no vhs set.");
                return;
            }

            if (activeVhsSet.VhsTracks.Count == 0)
            {
                _log?.Invoke($"[BLOCKED] Client {clientId} has no vhs tracks.");
                return;
            }

            float gain = GetSingleRehearsalPayloadConveyanceGain();

            activeVhsSet.RehearseAll(gain, _getCurrentTurn());

            _log?.Invoke(
                $"[REHEARSE SET UPDATED] Client {clientId} controller={controller.DisplayName} " +
                $"set={activeVhsSet.DisplayName} tracks={activeVhsSet.VhsTracks.Count} " +
                $"gain={gain:0.00} lastRehearsedTurn={activeVhsSet.LastRehearsedTurn}"
            );
        }

        private VhsSet GetOrCreateActiveVhsSet(
            ulong clientId,
            GameEntity controller)
        {
            VhsSet activeSet = controller.GetActiveVhsSet();

            if (activeSet != null)
                return activeSet;

            string setId = Guid.NewGuid().ToString();
            string setName = $"Set_{controller.VhsSets.Count + 1}";

            activeSet = new VhsSet(setId, setName, _getCurrentTurn());

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

            foreach (VhsSet existingSet in controller.VhsSets)
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

            VhsSet newSet = new VhsSet(
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
    }
}