using System.Collections.Generic;
using SEMM91.GamePlay.InfoScope;
using SEMM91.GamePlay.World;
using SEMM91.Networking;
using Unity.Netcode;
using UnityEngine;

namespace SEMM91.UI
{
    /// <summary>
    /// Renders the local player's InfoScope-approved physical-event
    /// projections over the seasonal map image.
    ///
    /// This view does not inspect authoritative action payloads.
    /// </summary>
    public sealed class PhysicalEventMapView :
        PersistentUIView
    {
        [Header("Map marker presentation")]
        [SerializeField]
        private RectTransform markerLayer;

        [SerializeField]
        private RectTransform draftedMarkerTemplate;

        [SerializeField]
        private RectTransform committedMarkerTemplate;

        private readonly List<RectTransform>
            _activeMarkers =
                new List<RectTransform>();

        /*
         * The physical map is deterministic and contains only
         * one hundred nodes. This local lookup copy is read-only
         * presentation support.
         */
        private readonly PhysicalMapGrid
            _physicalMapGrid =
                new PhysicalMapGrid();

        private NetPlayerState
            _observedPlayerState;

        private bool _hasWarnedMissingReferences;

        private void Awake()
        {
            if (draftedMarkerTemplate != null)
            {
                draftedMarkerTemplate
                    .gameObject
                    .SetActive(false);
            }

            if (committedMarkerTemplate != null)
            {
                committedMarkerTemplate
                    .gameObject
                    .SetActive(false);
            }
        }

        public override void Refresh(
            UIContext context)
        {
            NetPlayerState localPlayerState =
                context.LocalPlayerState;

            if (_observedPlayerState ==
                localPlayerState)
            {
                return;
            }

            BindObservedPlayerState(
                localPlayerState
            );
        }

        private void BindObservedPlayerState(
            NetPlayerState nextState)
        {
            UnbindObservedPlayerState();

            _observedPlayerState =
                nextState;

            if (_observedPlayerState != null)
            {
                _observedPlayerState
                    .ObservedPhysicalEvents
                    .OnListChanged +=
                    HandleObservedEventsChanged;
            }

            RebuildMarkers();
        }

        private void UnbindObservedPlayerState()
        {
            if (_observedPlayerState != null)
            {
                _observedPlayerState
                    .ObservedPhysicalEvents
                    .OnListChanged -=
                    HandleObservedEventsChanged;
            }

            _observedPlayerState = null;
        }

        private void HandleObservedEventsChanged(
            NetworkListEvent<
                ObservedPhysicalEventSummary
            > changeEvent)
        {
            RebuildMarkers();
        }

        private void RebuildMarkers()
        {
            ClearMarkers();

            if (!ValidateReferences())
                return;

            if (_observedPlayerState == null)
                return;

            NetworkList<
                ObservedPhysicalEventSummary
            > observedEvents =
                _observedPlayerState
                    .ObservedPhysicalEvents;

            for (int i = 0;
                 i < observedEvents.Count;
                 i++)
            {
                TryCreateMarker(
                    observedEvents[i]
                );
            }
        }

        private void TryCreateMarker(
            ObservedPhysicalEventSummary summary)
        {
            string nodeId =
                summary.PhysicalNodeId
                    .ToString();

            if (!_physicalMapGrid.TryGetNode(
                    nodeId,
                    out PhysicalMapNode node
                ))
            {
                Debug.LogWarning(
                    "[PhysicalEventMapView] " +
                    "Observed event references an unknown " +
                    $"physical node | " +
                    $"event={summary.EventId} | " +
                    $"node={nodeId}",
                    this
                );

                return;
            }

            Vector2 normalizedPoint =
                PhysicalMapCoordinateProjection
                    .ToNormalizedPoint(
                        node.Coordinate
                    );
            
            RectTransform selectedTemplate =
                summary.PlanState switch
                {
                    ObservedPhysicalEventPlanState.Drafted =>
                        draftedMarkerTemplate,

                    ObservedPhysicalEventPlanState.Committed =>
                        committedMarkerTemplate,

                    _ => null
                };

            if (selectedTemplate == null)
            {
                Debug.LogWarning(
                    "[PhysicalEventMapView] " +
                    "Observed event has no supported marker state | " +
                    $"event={summary.EventId} | " +
                    $"state={summary.PlanState}",
                    this
                );

                return;
            }
            
            RectTransform marker =
                Instantiate(
                    selectedTemplate,
                    markerLayer
                );

            marker.name =
                $"PhysicalEventMarker_" +
                $"{summary.PlanState}_" +
                $"{summary.EventId}";

            marker.anchorMin =
                normalizedPoint;

            marker.anchorMax =
                normalizedPoint;

            marker.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            marker.anchoredPosition =
                Vector2.zero;

            marker.localScale =
                Vector3.one;

            marker.gameObject.SetActive(
                true
            );

            _activeMarkers.Add(
                marker
            );
        }

        private bool ValidateReferences()
        {
            if (markerLayer != null &&
                draftedMarkerTemplate != null &&
                committedMarkerTemplate != null)
            {
                return true;
            }

            if (!_hasWarnedMissingReferences)
            {
                Debug.LogWarning(
                    "[PhysicalEventMapView] " +
                    "Marker Layer, Drafted Marker Template, or " +
                    "Committed Marker Template is not assigned.",
                    this
                );

                _hasWarnedMissingReferences =
                    true;
            }

            return false;
        }

        private void ClearMarkers()
        {
            for (int i =
                     _activeMarkers.Count - 1;
                 i >= 0;
                 i--)
            {
                RectTransform marker =
                    _activeMarkers[i];

                if (marker == null)
                    continue;

                marker.gameObject.SetActive(
                    false
                );

                Destroy(
                    marker.gameObject
                );
            }

            _activeMarkers.Clear();
        }

        private void OnDestroy()
        {
            UnbindObservedPlayerState();
            ClearMarkers();
        }
    }
}