using SEMM91.Networking;
using TMPro;
using UnityEngine;
using SEMM91.Networking.DebugSnapshots;
using SEMM91.Presentation;
using SEMM91.InputSystems;
using UnityEngine.UI;

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
        
        [Header("Gestation Controls")]
        [SerializeField]
        private Button dreamButton;

        private TextMeshProUGUI _dreamButtonText;
        private PlayerActionController _actionController;
        
        private void Awake()
        {
            if (dreamButton == null)
                return;

            _dreamButtonText =
                dreamButton.GetComponentInChildren<
                    TextMeshProUGUI
                >(true);

            dreamButton.onClick.AddListener(
                RequestDream
            );
        }

        private void OnDestroy()
        {
            if (dreamButton != null)
            {
                dreamButton.onClick.RemoveListener(
                    RequestDream
                );
            }
        }
        
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
                _actionController = null;

                if (dreamButton != null)
                {
                    dreamButton.interactable = false;
                }

                SetText(
                    _dreamButtonText,
                    "DREAM\nCONNECTING"
                );
                
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
            
            _actionController =
                state.GetComponent<PlayerActionController>();
            
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
            RefreshDreamButton();
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
        
        private void RefreshDreamButton()
        {
            if (dreamButton == null)
                return;

            if (_actionController == null)
            {
                dreamButton.interactable = false;

                SetText(
                    _dreamButtonText,
                    "DREAM\nUNAVAILABLE"
                );

                return;
            }

            PlayerActionPresentation presentation =
                _actionController.GetPresentation(
                    PlayerCommand.Dream
                );

            dreamButton.interactable =
                presentation.IsAvailable;

            if (presentation.IsAvailable)
            {
                SetText(
                    _dreamButtonText,
                    presentation.Label.ToUpperInvariant()
                );

                return;
            }

            string unavailableLabel =
                presentation.UnavailableReason switch
                {
                    ActionUnavailableReason.DreamUnavailable =>
                        "USED",

                    ActionUnavailableReason.PlayerInactive =>
                        "INACTIVE",

                    ActionUnavailableReason.TurnAlreadyCommitted =>
                        "COMMITTED",

                    ActionUnavailableReason.MissingPlayerState =>
                        "CONNECTING",

                    ActionUnavailableReason.MissingCoordinator =>
                        "UNAVAILABLE",

                    _ =>
                        "UNAVAILABLE"
                };

            SetText(
                _dreamButtonText,
                $"{presentation.Label.ToUpperInvariant()}\n" +
                unavailableLabel
            );
        }
        
        private void RequestDream()
        {
            if (_actionController == null)
                return;

            if (!_actionController.CanRequest(
                    PlayerCommand.Dream
                ))
            {
                return;
            }

            _actionController.Request(
                PlayerCommand.Dream
            );
        }
    }
}