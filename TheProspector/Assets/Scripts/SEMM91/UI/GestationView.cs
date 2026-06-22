using SEMM91.Networking;
using TMPro;
using UnityEngine;
using SEMM91.Networking.DebugSnapshots;
using SEMM91.Presentation;

namespace SEMM91.UI
{
    public class GestationView : ContextualUIView
    {
        [Header("Gestation State")]
        [SerializeField]
        private TextMeshProUGUI selectedIdeaSourceText;

        [SerializeField]
        private TextMeshProUGUI dreamAvailabilityText;

        [SerializeField]
        private TextMeshProUGUI transientIdeaText;

        [SerializeField]
        private TextMeshProUGUI persistentIdeaSourcesText;
        
        public override bool Supports(
            GameUIState state)
        {
            return state == GameUIState.Gestation;
        }

        public override void Refresh(
            UIContext context)
        {
            NetPlayerState state =
                context.LocalPlayerState;

            if (state == null)
            {
                SetText(
                    selectedIdeaSourceText,
                    "Selected idea source: Connecting..."
                );

                SetText(
                    dreamAvailabilityText,
                    "Dream this turn: Connecting..."
                );

                SetText(
                    transientIdeaText,
                    "Transient idea: Connecting..."
                );

                SetText(
                    persistentIdeaSourcesText,
                    "Persistent idea sources: Connecting..."
                );

                return;
            }
            SetText(
                selectedIdeaSourceText,
                $"Selected idea source: " +
                $"{state.SelectedIdeaSourceValue}"
            );

            string dreamState;

            if (!state.ActiveValue)
            {
                dreamState = "Unavailable";
            }
            else
            {
                dreamState =
                    state.CanDreamValue
                        ? "Available"
                        : "Used";
            }

            SetText(
                dreamAvailabilityText,
                $"Dream this turn: {dreamState}"
            );
            
            RefreshTagSummaries(context);
            
        }

        private static void SetText(
            TextMeshProUGUI target,
            string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
        
        private void RefreshTagSummaries(
            UIContext context)
        {
            if (!context.HasLocalInventorySnapshot)
            {
                SetText(
                    transientIdeaText,
                    "Transient idea: Synchronizing..."
                );

                SetText(
                    persistentIdeaSourcesText,
                    "Persistent idea sources: Synchronizing..."
                );

                return;
            }

            DomainSnapshotReplicator.PlayerInventoryDebugRow
                snapshot =
                    context.LocalInventorySnapshot;

            SetText(
                transientIdeaText,
                "Transient idea: " +
                FormatTag(snapshot.TransientTag)
            );

            SetText(
                persistentIdeaSourcesText,
                "Persistent idea sources:\n" +
                $"Conviction: " +
                $"{FormatTag(snapshot.ConvictionTag)}\n" +
                $"Mood: " +
                $"{FormatTag(snapshot.MoodTag)}\n" +
                $"Resonance: " +
                $"{FormatTag(snapshot.ResonanceTag)}"
            );
        }

        private static string FormatTag(
            DomainSnapshotReplicator.TagDebugSnapshot
                snapshot)
        {
            if (!snapshot.HasTag)
                return "Empty";

            return TagPresentationText.FormatTag(
                snapshot.Axis,
                snapshot.Pole,
                snapshot.Degree
            );
        }
    }
}