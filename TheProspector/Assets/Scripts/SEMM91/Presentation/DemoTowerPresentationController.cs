using System.Collections.Generic;
using SEMM91.Networking.DebugSnapshots;
using SEMM91.UI;
using UnityEngine;
using System.Text;
using TMPro;
using UnityEngine.UI;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class DemoTowerPresentationController :
        MonoBehaviour
    {
        [Header("Persistent Demo Tower")] [SerializeField]
        private Transform presentationRoot;

        [SerializeField] private Camera interactionCamera;

        [SerializeField] private Transform releaseItemRoot;

        [SerializeField] private MediaShelfItemView releaseItemPrefab;

        [Header("Released Cassette Layout")] [SerializeField]
        private Vector3 firstLocalPosition;

        [SerializeField] private Vector3 itemSpacing =
            new Vector3(0.0f, 5.0f, 0.0f);

        [Header("Diagnostics")] [SerializeField]
        private bool logRefreshes;

        [Header("Release Contents")] [SerializeField]
        private Canvas contentsCanvas;

        [SerializeField] private GameObject contentsPanelRoot;

        [SerializeField] private TextMeshProUGUI contentsTitleText;

        [SerializeField] private TextMeshProUGUI contentsTrackListText;

        [SerializeField] private Button contentsCloseButton;

        private Object _contextOwner;

        private DemoTowerInteractionMode
            _interactionMode =
                DemoTowerInteractionMode.Hidden;

        private int _lastSnapshotVersion = -1;

        private string _gestationSelectedReleaseId =
            string.Empty;

        private MediaShelfItemView _presentedTowerView;
        
        private KeeperView _keeperView;

        private readonly List<ReleaseBinding>
            _releaseBindings =
                new List<ReleaseBinding>();

        [Header("Cassette Hero Presentation")]
        [SerializeField]
        private CassettePresentationController
            cassettePresentationController;
        
        private void Awake()
        {
            if (presentationRoot != null)
            {
                presentationRoot.gameObject
                    .SetActive(false);
            }

            if (contentsCloseButton != null)
            {
                contentsCloseButton.onClick.AddListener(
                    HideContentsPanel
                );
            }

            HideContentsPanel();
        }

        private void OnDestroy()
        {
            if (contentsCloseButton != null)
            {
                contentsCloseButton.onClick.RemoveListener(
                    HideContentsPanel
                );
            }
        }

        private void Update()
        {
            if (_interactionMode ==
                DemoTowerInteractionMode.Hidden)
            {
                return;
            }

            if (presentationRoot == null ||
                !presentationRoot.gameObject
                    .activeSelf)
            {
                return;
            }

            DomainSnapshotReplicator snapshot =
                DomainSnapshotReplicator.Instance;

            if (snapshot == null ||
                !snapshot.IsSnapshotNetworkReady)
            {
                return;
            }

            int snapshotVersion =
                snapshot.SnapshotVersion.Value;

            if (_lastSnapshotVersion !=
                snapshotVersion)
            {
                RebuildItems(snapshot);
                _lastSnapshotVersion =
                    snapshotVersion;
            }

            RefreshSelectionPresentation();
        }

        public void ApplyContext(
            Object contextOwner,
            Transform poseAnchor,
            DemoTowerInteractionMode interactionMode)
        {
            if (contextOwner == null)
            {
                Debug.LogError(
                    "DemoTowerPresentationController " +
                    "received a null context owner.",
                    this
                );

                return;
            }

            _contextOwner = contextOwner;
            _interactionMode = interactionMode;
            _keeperView = null;

            if (interactionMode ==
                DemoTowerInteractionMode.Hidden)
            {
                HidePresentation();
                return;
            }

            if (presentationRoot == null ||
                poseAnchor == null)
            {
                Debug.LogError(
                    "DemoTowerPresentationController is " +
                    "missing its PresentationRoot or the " +
                    "contextual pose anchor.",
                    this
                );

                HidePresentation();
                return;
            }

            if (contentsCanvas != null)
            {
                contentsCanvas.worldCamera =
                    interactionCamera;
            }

            HideContentsPanel();

            presentationRoot.SetPositionAndRotation(
                poseAnchor.position,
                poseAnchor.rotation
            );

            presentationRoot.localScale =
                poseAnchor.localScale;

            presentationRoot.gameObject
                .SetActive(true);

            _lastSnapshotVersion = -1;

            if (logRefreshes)
            {
                Debug.Log(
                    "[DEMO TOWER CONTEXT] " +
                    $"owner={contextOwner.name} | " +
                    $"mode={interactionMode} | " +
                    $"anchor={poseAnchor.name}",
                    this
                );
            }
        }

        public void ReleaseContext(
            Object contextOwner)
        {
            if (_contextOwner != contextOwner)
                return;

            _contextOwner = null;
            _interactionMode =
                DemoTowerInteractionMode.Hidden;

            _keeperView = null;
            HidePresentation();
        }

        private void HidePresentation()
        {
            HideContentsPanel();
            if (presentationRoot != null)
            {
                presentationRoot.gameObject
                    .SetActive(false);
            }
        }

        private void RebuildItems(
            DomainSnapshotReplicator snapshot)
        {
            ClearRuntimeItems();

            if (releaseItemPrefab == null ||
                releaseItemRoot == null ||
                snapshot.KeeperReleaseRows == null)
            {
                return;
            }

            for (int index = 0;
                 index <
                 snapshot.KeeperReleaseRows.Count;
                 index++)
            {
                DomainSnapshotReplicator
                    .KeeperReleaseDebugRow row =
                        snapshot.KeeperReleaseRows[index];

                CreateReleaseItem(
                    snapshot,
                    row,
                    index
                );
            }

            if (logRefreshes)
            {
                Debug.Log(
                    "[DEMO TOWER REFRESH] " +
                    $"snapshot=" +
                    $"{snapshot.SnapshotVersion.Value} | " +
                    $"mode={_interactionMode} | " +
                    $"releases={_releaseBindings.Count}",
                    this
                );
            }
        }

        private void CreateReleaseItem(
            DomainSnapshotReplicator snapshot,
            DomainSnapshotReplicator
                .KeeperReleaseDebugRow row,
            int itemIndex)
        {
            MediaShelfItemView view =
                Instantiate(
                    releaseItemPrefab,
                    releaseItemRoot
                );

            view.transform.localPosition =
                firstLocalPosition +
                itemSpacing * itemIndex;

            view.transform.localRotation =
                Quaternion.identity;

            view.SetInteractionCamera(
                interactionCamera
            );

            string demoTitle =
                ResolveDemoTapeTitle(
                    snapshot,
                    row.SourceDemoTapeId
                );

            view.SetTitle(
                demoTitle
            );

            string releaseId =
                row.ReleaseId.ToString();

            string sourceDemoTapeId =
                row.SourceDemoTapeId.ToString();

            view.BindClick(
                () => HandleReleaseClicked(
                    view,
                    releaseId,
                    sourceDemoTapeId
                )
            );

            view.SetPresentation(
                interactable: true,
                opacity: 1.0f,
                selected: false
            );

            _releaseBindings.Add(
                new ReleaseBinding(
                    view,
                    releaseId
                )
            );
        }

        private void HandleReleaseClicked(
            MediaShelfItemView view,
            string releaseId,
            string sourceDemoTapeId)
        {
            if (string.IsNullOrWhiteSpace(
                    releaseId
                ))
            {
                return;
            }

            /*ShowReleaseContents(
                sourceDemoTapeId
            );*/

            switch (_interactionMode)
            {
                case DemoTowerInteractionMode
                    .Gestation:

                    _gestationSelectedReleaseId =
                        releaseId;

                    Debug.Log(
                        "[DEMO TOWER GESTATION TARGET] " +
                        $"releaseId={releaseId}",
                        this
                    );

                    break;

                case DemoTowerInteractionMode.Keeper:
                {
                    KeeperView keeperView =
                        ResolveKeeperView();

                    if (keeperView == null)
                    {
                        Debug.LogWarning(
                            "[DEMO TOWER] No active " +
                            "KeeperView could receive " +
                            $"releaseId={releaseId}.",
                            this
                        );

                        return;
                    }

                    keeperView.SelectReleaseById(
                        releaseId
                    );

                    break;
                }
            }
            
            if (cassettePresentationController != null)
            {
                if (_presentedTowerView != null &&
                    _presentedTowerView != view)
                {
                    _presentedTowerView.SetModelVisible(true);
                }

                _presentedTowerView = view;

                view.SetModelVisible(false);
                cassettePresentationController
                    .PresentToReady(sourceDemoTapeId);
            }
            else
            {
                Debug.LogWarning(
                    "[DEMO TOWER] " +
                    "CassettePresentationController " +
                    "is not assigned.",
                    this
                );
            }
        }

        private void RefreshSelectionPresentation()
        {
            string selectedReleaseId =
                string.Empty;

            if (_interactionMode ==
                DemoTowerInteractionMode.Gestation)
            {
                selectedReleaseId =
                    _gestationSelectedReleaseId;
            }
            else if (_interactionMode ==
                     DemoTowerInteractionMode.Keeper)
            {
                KeeperView keeperView =
                    ResolveKeeperView();

                if (keeperView != null)
                {
                    selectedReleaseId =
                        keeperView.SelectedReleaseId;
                }
            }

            foreach (
                ReleaseBinding binding
                in _releaseBindings)
            {
                binding.View.SetPresentation(
                    interactable: true,
                    opacity: 1.0f,
                    selected:
                    binding.ReleaseId ==
                    selectedReleaseId
                );
            }
        }

        private KeeperView ResolveKeeperView()
        {
            if (_keeperView != null)
                return _keeperView;

            KeeperView[] views =
                Object.FindObjectsByType<KeeperView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            foreach (KeeperView view in views)
            {
                if (view == null)
                    continue;

                _keeperView = view;
                break;
            }

            return _keeperView;
        }

        private void ClearRuntimeItems()
        {
            foreach (
                ReleaseBinding binding
                in _releaseBindings)
            {
                if (binding.View == null)
                    continue;

                binding.View.gameObject
                    .SetActive(false);

                Object.Destroy(
                    binding.View.gameObject
                );
            }

            _releaseBindings.Clear();
        }

        private void HideContentsPanel()
        {
            if (contentsPanelRoot != null)
            {
                contentsPanelRoot.SetActive(false);
            }
        }
        
        private void ShowReleaseContents(
            string sourceDemoTapeId)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceDemoTapeId
                ))
            {
                return;
            }

            DomainSnapshotReplicator snapshot =
                DomainSnapshotReplicator.Instance;

            if (snapshot == null ||
                !snapshot.IsSnapshotNetworkReady)
            {
                return;
            }

            string tapeTitle =
                sourceDemoTapeId;

            for (int index = 0;
                 index < snapshot.DemoTapeRows.Count;
                 index++)
            {
                DomainSnapshotReplicator
                    .DemoTapeDebugRow demoRow =
                        snapshot.DemoTapeRows[index];

                if (demoRow.DemoTapeId.ToString() !=
                    sourceDemoTapeId)
                {
                    continue;
                }

                tapeTitle =
                    demoRow.DisplayName.ToString();

                break;
            }

            List<
                DemoTapeTrackSemanticDebugRow
            > trackRows =
                new List<
                    DemoTapeTrackSemanticDebugRow
                >();

            for (int index = 0;
                 index <
                 snapshot.DemoTapeTrackSemanticRows.Count;
                 index++)
            {
                DemoTapeTrackSemanticDebugRow row =
                    snapshot
                        .DemoTapeTrackSemanticRows[index];

                if (row.DemoTapeId.ToString() !=
                    sourceDemoTapeId)
                {
                    continue;
                }

                trackRows.Add(row);
            }

            trackRows.Sort(
                (left, right) =>
                    left.TrackIndex.CompareTo(
                        right.TrackIndex
                    )
            );

            StringBuilder trackList =
                new StringBuilder();

            for (int index = 0;
                 index < trackRows.Count;
                 index++)
            {
                string trackTitle =
                    trackRows[index]
                        .DisplayName
                        .ToString();

                trackList
                    .Append(index + 1)
                    .Append(". ")
                    .Append(trackTitle);

                if (index <
                    trackRows.Count - 1)
                {
                    trackList.AppendLine();
                }
            }

            if (contentsTitleText != null)
            {
                contentsTitleText.text =
                    tapeTitle;
            }

            if (contentsTrackListText != null)
            {
                contentsTrackListText.text =
                    trackRows.Count > 0
                        ? trackList.ToString()
                        : "No recorded tracks.";
            }

            if (contentsPanelRoot != null)
            {
                contentsPanelRoot.SetActive(true);
            }
        }

        private readonly struct ReleaseBinding
        {
            public readonly MediaShelfItemView View;
            public readonly string ReleaseId;

            public ReleaseBinding(
                MediaShelfItemView view,
                string releaseId)
            {
                View = view;
                ReleaseId = releaseId;
            }
        }

        private static string ResolveDemoTapeTitle(
            DomainSnapshotReplicator snapshot,
            Unity.Collections.FixedString64Bytes
                sourceDemoTapeId)
        {
            if (snapshot == null ||
                snapshot.DemoTapeRows == null)
            {
                return string.Empty;
            }

            for (int index = 0;
                 index < snapshot.DemoTapeRows.Count;
                 index++)
            {
                DomainSnapshotReplicator
                    .DemoTapeDebugRow demoRow =
                        snapshot.DemoTapeRows[index];

                if (!demoRow.DemoTapeId.Equals(
                        sourceDemoTapeId))
                {
                    continue;
                }

                return demoRow.DisplayName.ToString();
            }

            return string.Empty;
        }
        
        public void RestorePresentedTowerView()
        {
            if (_presentedTowerView != null)
            {
                _presentedTowerView.SetModelVisible(true);
            }

            _presentedTowerView = null;
        }
    }
    
}