using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Gestation.Questing
{
    /// <summary>
    /// Server-side record of the last global turn on which each
    /// connected player consumed their optional Dream opportunity.
    ///
    /// This class models usage only. Other eligibility conditions
    /// remain the responsibility of the server coordinator.
    /// </summary>
    public sealed class QuestingTurnUsageRegistry
    {
        private readonly Dictionary<ulong, int>
            _lastDreamTurnByClient = new();

        public bool HasUnusedDreamForTurn(
            ulong clientId,
            int globalTurn)
        {
            ValidateGlobalTurn(globalTurn);

            if (!_lastDreamTurnByClient.TryGetValue(
                    clientId,
                    out int lastDreamTurn))
            {
                return true;
            }

            return globalTurn > lastDreamTurn;
        }

        public bool TryConsumeDreamForTurn(
            ulong clientId,
            int globalTurn)
        {
            ValidateGlobalTurn(globalTurn);

            if (_lastDreamTurnByClient.TryGetValue(
                    clientId,
                    out int lastDreamTurn) &&
                globalTurn <= lastDreamTurn)
            {
                return false;
            }

            _lastDreamTurnByClient[clientId] = globalTurn;
            return true;
        }

        public bool TryGetLastDreamTurn(
            ulong clientId,
            out int globalTurn)
        {
            return _lastDreamTurnByClient.TryGetValue(
                clientId,
                out globalTurn
            );
        }

        public void ClearClient(ulong clientId)
        {
            _lastDreamTurnByClient.Remove(clientId);
        }

        public void ClearAll()
        {
            _lastDreamTurnByClient.Clear();
        }

        private static void ValidateGlobalTurn(int globalTurn)
        {
            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn),
                    globalTurn,
                    "Global turn cannot be negative."
                );
            }
        }
    }
}