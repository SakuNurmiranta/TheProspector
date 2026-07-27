using System.Collections.Generic;
using SEMM91.Networking.DebugSnapshots;
using SEMM91.UI;
using UnityEngine;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class DemoTowerPresentationController :
        MonoBehaviour
    {
        [Header("Persistent Demo Tower")]
        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private Camera interactionCamera;

        [SerializeField]
        private Transform releaseItemRoot;

        [SerializeField]
        private MediaShelfItemView releaseItemPrefab;

        [Header("Released Cassette Layout")]
        [SerializeField]
        private Vector3 firstLocalPosition;

        [SerializeField]
        private Vector3 itemSpacing =
            new Vector3(0.0f, 5.0f, 0.0f);

        [Header("Diagnostics")]
        [SerializeField]
        private bool logRefreshes;

        private Object _contextOwner;

        private DemoTowerInteractionMode
            _interactionMode =
                DemoTowerInteractionMode.Hidden;

        private int _lastSnapshotVersion = -1;

        private string _gestationSelectedReleaseId =
            string.Empty;

        private KeeperView _keeperView;

        private readonly List<ReleaseBinding>
            _releaseBindings =
                new List<ReleaseBinding>();

        private void Awake()
        {
            if (presentationRoot != null)
            {
                presentationRoot.gameObject
                    .SetActive(false);
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

            string releaseId =
                row.ReleaseId.ToString();

            view.BindClick(
                () => HandleReleaseClicked(
                    releaseId
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
            string releaseId)
        {
            if (string.IsNullOrWhiteSpace(
                    releaseId
                ))
            {
                return;
            }

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
    }
}

