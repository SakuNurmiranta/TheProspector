using System.Text;
using TMPro;
using UnityEngine;

namespace SEMM91.UI
{
    public class UIDebugPanelView : PersistentUIView
    {
        [SerializeField] private TextMeshProUGUI diagnosticsText;

        public override void Refresh(UIContext context)
        {
            if (diagnosticsText == null)
                return;

            var playerStates = FindObjectsOfType<SEMM91.Networking.NetPlayerState>();
            StringBuilder sb = new();

            sb.AppendLine("Players:");
            foreach (var state in playerStates)
            {
                ulong clientId = state.OwnerClientIdCached != ulong.MaxValue
                    ? state.OwnerClientIdCached
                    : state.OwnerClientId;

                bool isKeeper = clientId == context.KeeperClientId;
                string role = isKeeper ? "Keeper" : "Regular";

                sb.AppendLine(
                    $"Client {clientId} | {state.DisplayNameStr} | Role: {role} | Score: {state.ScoreValue} | Exhausted: {state.ExhaustedValue} | Active: {state.ActiveValue}"
                );
            }

            diagnosticsText.text = sb.ToString();
        }
    }
}