using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MediaShelfItemView :
        MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField]
        private Canvas interactionCanvas;

        [SerializeField]
        private Button interactionButton;

        [Header("Visual State")]
        [SerializeField]
        private Renderer[] targetRenderers;

        [SerializeField]
        private GameObject selectedIndicator;

        [SerializeField]
        [Min(1.0f)]
        private float selectedScaleMultiplier = 1.05f;

        private Action _clickAction;
        private Vector3 _baseLocalScale;
        private Material[][] _runtimeMaterials;

        private void Awake()
        {
            _baseLocalScale = transform.localScale;
            CacheRuntimeMaterials();

            if (interactionButton != null)
            {
                interactionButton.onClick.AddListener(
                    HandleClick
                );
            }
        }

        private void OnDestroy()
        {
            if (interactionButton != null)
            {
                interactionButton.onClick.RemoveListener(
                    HandleClick
                );
            }

            _clickAction = null;
        }

        public void SetInteractionCamera(
            Camera eventCamera)
        {
            if (interactionCanvas == null)
                return;

            interactionCanvas.worldCamera =
                eventCamera;
        }

        public void BindClick(
            Action clickAction)
        {
            _clickAction = clickAction;
        }

        public void SetPresentation(
            bool interactable,
            float opacity,
            bool selected)
        {
            if (interactionButton != null)
            {
                interactionButton.interactable =
                    interactable;

                interactionButton.gameObject.SetActive(
                    interactable
                );
            }

            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(
                    selected
                );
            }

            transform.localScale =
                selected
                    ? _baseLocalScale *
                      selectedScaleMultiplier
                    : _baseLocalScale;

            SetOpacity(
                Mathf.Clamp01(opacity)
            );
        }

        private void HandleClick()
        {
            _clickAction?.Invoke();
        }

        private void CacheRuntimeMaterials()
        {
            if (targetRenderers == null)
            {
                _runtimeMaterials =
                    Array.Empty<Material[]>();

                return;
            }

            _runtimeMaterials =
                new Material[
                    targetRenderers.Length
                ][];

            for (int rendererIndex = 0;
                 rendererIndex <
                 targetRenderers.Length;
                 rendererIndex++)
            {
                Renderer targetRenderer =
                    targetRenderers[
                        rendererIndex
                    ];

                _runtimeMaterials[
                    rendererIndex
                ] =
                    targetRenderer != null
                        ? targetRenderer.materials
                        : Array.Empty<Material>();
            }
        }

        private void SetOpacity(
            float opacity)
        {
            bool transparent =
                opacity < 0.999f;

            for (int rendererIndex = 0;
                 rendererIndex <
                 _runtimeMaterials.Length;
                 rendererIndex++)
            {
                Material[] materials =
                    _runtimeMaterials[
                        rendererIndex
                    ];

                for (int materialIndex = 0;
                     materialIndex <
                     materials.Length;
                     materialIndex++)
                {
                    ApplyOpacity(
                        materials[materialIndex],
                        opacity,
                        transparent
                    );
                }
            }
        }

        private static void ApplyOpacity(
            Material material,
            float opacity,
            bool transparent)
        {
            if (material == null)
                return;

            if (material.HasProperty(
                    "_BaseColor"
                ))
            {
                Color color =
                    material.GetColor(
                        "_BaseColor"
                    );

                color.a = opacity;

                material.SetColor(
                    "_BaseColor",
                    color
                );
            }

            if (material.HasProperty(
                    "_Color"
                ))
            {
                Color color =
                    material.GetColor(
                        "_Color"
                    );

                color.a = opacity;

                material.SetColor(
                    "_Color",
                    color
                );
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat(
                    "_Surface",
                    transparent ? 1.0f : 0.0f
                );
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat(
                    "_Mode",
                    transparent ? 3.0f : 0.0f
                );
            }

            material.SetOverrideTag(
                "RenderType",
                transparent
                    ? "Transparent"
                    : "Opaque"
            );

            material.SetInt(
                "_SrcBlend",
                transparent
                    ? (int)BlendMode.SrcAlpha
                    : (int)BlendMode.One
            );

            material.SetInt(
                "_DstBlend",
                transparent
                    ? (int)BlendMode.OneMinusSrcAlpha
                    : (int)BlendMode.Zero
            );

            material.SetInt(
                "_ZWrite",
                transparent ? 0 : 1
            );

            if (transparent)
            {
                material.EnableKeyword(
                    "_SURFACE_TYPE_TRANSPARENT"
                );

                material.EnableKeyword(
                    "_ALPHABLEND_ON"
                );

                material.renderQueue = 3000;
            }
            else
            {
                material.DisableKeyword(
                    "_SURFACE_TYPE_TRANSPARENT"
                );

                material.DisableKeyword(
                    "_ALPHABLEND_ON"
                );

                material.renderQueue = -1;
            }
        }
    }
}
