using System;
using System.Linq;
using SEMM91.Core.Entities;
using SEMM91.Core.Tracks;
using SEMM91.GamePlay;
using SEMM91.GamePlay.Actions;
using SEMM91.Networking;
using UnityEngine;

namespace SEMM91.GamePlay.Pressure
{
    public class SeasonPressureResolver
    {
        private const float ForgetfulnessConveyanceModHard = 0.95f;
        private const float ForgetfulnessConveyanceModSoft = 0.98f;
        private const float MinimumVhsConveyance = 0.1f;

        private readonly Action<string> _log;

        public SeasonPressureResolver(Action<string> log = null)
        {
            _log = log ?? Debug.Log;
        }

        public void ApplySeasonPressure(
            ulong clientId,
            NetPlayerState state)
        {
            ApplyForgetfulnessIfNeeded(clientId, state);

            // Later:
            // ApplyColdWind(...)
            // ApplyRelaxedContinuity(...)
            // ApplyOverexertionPressure(...)
        }

        private void ApplyForgetfulnessIfNeeded(
            ulong clientId,
            NetPlayerState state)
        {
            if (state == null)
                return;

            GameEntity controller = state.PlayerEntity;

            if (controller == null)
            {
                _log?.Invoke($"Forgetfulness: Client {clientId} has no controller entity.");
                return;
            }

            RehearsalSet activeSet = controller.GetActiveVhsSet();

            if (activeSet == null)
            {
                _log?.Invoke($"[FORGETFULNESS BLOCKED] Client {clientId} has no VHS set.");
                return;
            }

            if (activeSet.VhsTracks.Count == 0)
            {
                _log?.Invoke($"[FORGETFULNESS NOTE] Client {clientId} active VHS set has no tracks.");
            }

            float decayMod = state.CurrentStanceValue == BandStance.Rehearse
                ? ForgetfulnessConveyanceModSoft
                : ForgetfulnessConveyanceModHard;

            foreach (RehearsalSet vhsSet in controller.VhsSets)
            {
                if (vhsSet == null)
                    continue;

                if (vhsSet == activeSet)
                {
                    _log?.Invoke($"No forgetfulness for active set {activeSet.DisplayName}");
                    continue;
                }

                foreach (Track vhsTrack in vhsSet.VhsTracks)
                {
                    if (vhsTrack == null)
                        continue;

                    if (activeSet.VhsTracks.Contains(vhsTrack))
                    {
                        _log?.Invoke(
                            $"[FORGETFULNESS BYPASSED] Client {clientId} " +
                            $"set={vhsSet.DisplayName} track={vhsTrack.DisplayName} sharedWithActiveSet=True"
                        );
                        continue;
                    }

                    float before = vhsTrack.Conveyance;

                    bool hitFloor = vhsTrack.ApplyConveyanceMultiplier(
                        decayMod,
                        MinimumVhsConveyance
                    );

                    _log?.Invoke(
                        $"[FORGETFULNESS] Client {clientId} set={vhsSet.DisplayName} {vhsTrack.DisplayName} " +
                        $"mod={decayMod:0.00} c={before:0.00}->{vhsTrack.Conveyance:0.00} floorHit={hitFloor}"
                    );
                }
            }
        }
    }
}