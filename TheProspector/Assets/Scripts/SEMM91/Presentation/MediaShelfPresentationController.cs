using System.Collections.Generic;
using SEMM91.GamePlay.Actions;
using SEMM91.InputSystems;
using SEMM91.Networking;
using SEMM91.Networking.DebugSnapshots;
using Unity.Netcode;
using UnityEngine;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MediaShelfPresentationController :
        MonoBehaviour
    {
        [Header("Persistent Shelf")]
        [SerializeField]
        private Transform presentationRoot;

        [SerializeField]
        private Camera interactionCamera;

        [SerializeField]
        private Transform vhsItemRoot;

        [SerializeField]
        private Transform demoTapeItemRoot;

        [Header("Runtime Item Prefabs")]
        [SerializeField]
        private MediaShelfItemView vhsItemPrefab;

        [SerializeField]
        private MediaShelfItemView demoTapeItemPrefab;

        [Header("VHS Layout")]
        [SerializeField]
        private Vector3 vhsFirstLocalPosition;

        [SerializeField]
        private Vector3 vhsItemSpacing =
            new Vector3(0.35f, 0.0f, 0.0f);

        [Header("Demo Cassette Layout")]
        [SerializeField]
        private Vector3 demoFirstLocalPosition;

        [SerializeField]
        private Vector3 demoItemSpacing =
            new Vector3(0.22f, 0.0f, 0.0f);

        [SerializeField]
        [Range(0.05f, 1.0f)]
        private float unreleasedOpacity = 0.35f;

        [Header("Diagnostics")]
        [SerializeField]
        private bool logRefreshes;

        private Object _contextOwner;

        private MediaShelfInteractionMode
            _interactionMode =
                MediaShelfInteractionMode.Hidden;

        private PlayerActionController
            _playerActionController;

        private NetPlayerState
            _localPlayerState;

        private int _lastSnapshotVersion = -1;

        private readonly List<VhsBinding>
            _vhsBindings =
                new List<VhsBinding>();

        private readonly List<DemoBinding>
            _demoBindings =
                new List<DemoBinding>();

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
                MediaShelfInteractionMode.Hidden)
            {
                return;
            }

            if (presentationRoot == null ||
                !presentationRoot.gameObject
                    .activeSelf)
            {
                return;
            }

            if (!TryResolveLocalPlayer())
                return;

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

            RefreshDraftPresentation();
        }

        public void ApplyContext(
            Object contextOwner,
            Transform poseAnchor,
            MediaShelfInteractionMode
                interactionMode)
        {
            if (contextOwner == null)
            {
                Debug.LogError(
                    "MediaShelfPresentationController " +
                    "received a null context owner.",
                    this
                );

                return;
            }

            _contextOwner = contextOwner;
            _interactionMode = interactionMode;

            if (interactionMode ==
                MediaShelfInteractionMode.Hidden)
            {
                HidePresentation();
                return;
            }

            if (presentationRoot == null ||
                poseAnchor == null)
            {
                Debug.LogError(
                    "MediaShelfPresentationController " +
                    "is missing its PresentationRoot or " +
                    "the contextual pose anchor.",
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
                    "[MEDIA SHELF CONTEXT] " +
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
                MediaShelfInteractionMode.Hidden;

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

        private bool TryResolveLocalPlayer()
        {
            if (_playerActionController != null &&
                _playerActionController.IsClient &&
                _playerActionController.IsOwner &&
                _localPlayerState != null)
            {
                return true;
            }

            _playerActionController = null;
            _localPlayerState = null;

            PlayerActionController[] controllers =
                Object.FindObjectsByType<
                    PlayerActionController
                >(
                    FindObjectsSortMode.None
                );

            foreach (
                PlayerActionController controller
                in controllers)
            {
                if (controller == null ||
                    !controller.IsClient ||
                    !controller.IsOwner)
                {
                    continue;
                }

                _playerActionController =
                    controller;

                _localPlayerState =
                    controller.GetComponent<
                        NetPlayerState
                    >();

                break;
            }

            return
                _playerActionController != null &&
                _localPlayerState != null;
        }

        private void RebuildItems(
            DomainSnapshotReplicator snapshot)
        {
            ClearRuntimeItems();

            NetworkManager manager =
                NetworkManager.Singleton;

            if (manager == null)
                return;

            ulong localClientId =
                manager.LocalClientId;

            List<
                DomainSnapshotReplicator
                    .RehearsalSetDebugRow
            > rehearsalRows =
                new List<
                    DomainSnapshotReplicator
                        .RehearsalSetDebugRow
                >();

            for (int index = 0;
                 index <
                 snapshot.RehearsalSetRows.Count;
                 index++)
            {
                DomainSnapshotReplicator
                    .RehearsalSetDebugRow row =
                        snapshot.RehearsalSetRows[
                            index
                        ];

                if (row.OwnerClientId ==
                    localClientId)
                {
                    rehearsalRows.Add(row);
                }
            }

            rehearsalRows.Sort(
                (left, right) =>
                    left.SetIndex.CompareTo(
                        right.SetIndex
                    )
            );

            for (int index = 0;
                 index <
                 rehearsalRows.Count;
                 index++)
            {
                CreateVhsItem(
                    rehearsalRows[index],
                    index
                );
            }

            List<
                DomainSnapshotReplicator
                    .DemoTapeDebugRow
            > demoRows =
                new List<
                    DomainSnapshotReplicator
                        .DemoTapeDebugRow
                >();

            for (int index = 0;
                 index <
                 snapshot.DemoTapeRows.Count;
                 index++)
            {
                DomainSnapshotReplicator
                    .DemoTapeDebugRow row =
                        snapshot.DemoTapeRows[
                            index
                        ];

                if (row.OwnerClientId ==
                    localClientId)
                {
                    demoRows.Add(row);
                }
            }

            demoRows.Sort(
                (left, right) =>
                    left.DemoIndex.CompareTo(
                        right.DemoIndex
                    )
            );

            for (int index = 0;
                 index <
                 demoRows.Count;
                 index++)
            {
                CreateDemoItem(
                    demoRows[index],
                    index
                );
            }

            if (logRefreshes)
            {
                Debug.Log(
                    "[MEDIA SHELF REFRESH] " +
                    $"snapshot=" +
                    $"{snapshot.SnapshotVersion.Value} | " +
                    $"mode={_interactionMode} | " +
                    $"vhs={_vhsBindings.Count} | " +
                    $"demos={_demoBindings.Count}",
                    this
                );
            }
        }

        private void CreateVhsItem(
            DomainSnapshotReplicator
                .RehearsalSetDebugRow row,
            int itemIndex)
        {
            if (vhsItemPrefab == null ||
                vhsItemRoot == null)
            {
                return;
            }

            MediaShelfItemView view =
                Instantiate(
                    vhsItemPrefab,
                    vhsItemRoot
                );

            view.transform.localPosition =
                vhsFirstLocalPosition +
                vhsItemSpacing * itemIndex;

            view.transform.localRotation =
                Quaternion.identity;

            view.SetInteractionCamera(
                interactionCamera
            );

            string setId =
                row.SetId.ToString();

            view.BindClick(
                () =>
                {
                    if (_interactionMode !=
                        MediaShelfInteractionMode
                            .Rehearsal)
                    {
                        return;
                    }

                    _playerActionController
                        ?.RequestSelectRehearsalSet(
                            setId
                        );
                }
            );

            view.SetPresentation(
                _interactionMode ==
                MediaShelfInteractionMode
                    .Rehearsal,
                1.0f,
                row.IsActive
            );

            _vhsBindings.Add(
                new VhsBinding(view)
            );
        }

        private void CreateDemoItem(
            DomainSnapshotReplicator
                .DemoTapeDebugRow row,
            int itemIndex)
        {
            if (demoTapeItemPrefab == null ||
                demoTapeItemRoot == null)
            {
                return;
            }

            MediaShelfItemView view =
                Instantiate(
                    demoTapeItemPrefab,
                    demoTapeItemRoot
                );

            view.transform.localPosition =
                demoFirstLocalPosition +
                demoItemSpacing * itemIndex;

            view.transform.localRotation =
                Quaternion.identity;

            view.SetInteractionCamera(
                interactionCamera
            );

            string demoTapeId =
                row.DemoTapeId.ToString();

            view.BindClick(
                () =>
                {
                    if (_interactionMode !=
                        MediaShelfInteractionMode
                            .Promotion)
                    {
                        return;
                    }

                    _playerActionController
                        ?.RequestDraftDemoRelease(
                            demoTapeId
                        );
                }
            );

            _demoBindings.Add(
                new DemoBinding(
                    view,
                    demoTapeId,
                    row.IsReleased
                )
            );
        }

        private void RefreshDraftPresentation()
        {
            HashSet<string> draftedDemoIds =
                new HashSet<string>();

            for (int actionPosition = 1;
                 actionPosition <= 3;
                 actionPosition++)
            {
                DraftedActionSummary summary =
                    _localPlayerState
                        .GetDraftedActionSummary(
                            actionPosition
                        );

                if (!summary.IsOccupied ||
                    summary.ActionType !=
                    DraftedActionType
                        .ReleaseDemoTape ||
                    !summary.HasTargetDemoTape)
                {
                    continue;
                }

                draftedDemoIds.Add(
                    summary.TargetDemoTapeId
                        .ToString()
                );
            }

            foreach (
                DemoBinding binding
                in _demoBindings)
            {
                bool isDrafted =
                    draftedDemoIds.Contains(
                        binding.DemoTapeId
                    );

                bool appearsWhole =
                    binding.IsReleased ||
                    isDrafted;

                bool interactable =
                    _interactionMode ==
                    MediaShelfInteractionMode
                        .Promotion &&
                    !binding.IsReleased &&
                    !isDrafted;

                binding.View.SetPresentation(
                    interactable,
                    appearsWhole
                        ? 1.0f
                        : unreleasedOpacity,
                    isDrafted
                );
            }
        }

        private void ClearRuntimeItems()
        {
            foreach (
                VhsBinding binding
                in _vhsBindings)
            {
                DestroyView(binding.View);
            }

            foreach (
                DemoBinding binding
                in _demoBindings)
            {
                DestroyView(binding.View);
            }

            _vhsBindings.Clear();
            _demoBindings.Clear();
        }

        private static void DestroyView(
            MediaShelfItemView view)
        {
            if (view == null)
                return;

            view.gameObject.SetActive(false);

            Object.Destroy(
                view.gameObject
            );
        }

        private readonly struct VhsBinding
        {
            public readonly
                MediaShelfItemView View;

            public VhsBinding(
                MediaShelfItemView view)
            {
                View = view;
            }
        }

        private readonly struct DemoBinding
        {
            public readonly
                MediaShelfItemView View;

            public readonly string
                DemoTapeId;

            public readonly bool
                IsReleased;

            public DemoBinding(
                MediaShelfItemView view,
                string demoTapeId,
                bool isReleased)
            {
                View = view;
                DemoTapeId = demoTapeId;
                IsReleased = isReleased;
            }
        }
    }
}
