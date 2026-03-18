using TMPro;
using UnityEngine;

namespace SEMM91.UI
{
    public class GlobalOverlayView : PersistentUIView
    {
        [Header("Status Text")]
        [SerializeField] private TextMeshProUGUI yearText;
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI keeperText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private TextMeshProUGUI stateText;

        public override void Refresh(UIContext context)
        {
            if (yearText != null)
                yearText.text = $"Year: {context.CurrentRound}";

            if (turnText != null)
                turnText.text = $"Turn: {context.CurrentTurn}";

            if (keeperText != null)
                keeperText.text = $"Keeper: {context.KeeperClientId}";

            if (roleText != null)
                roleText.text = context.IsKeeper ? "Role: Keeper" : "Role: Regular";

            if (stateText != null)
                stateText.text = $"View: {context.ActiveState}";
        }
    }
}