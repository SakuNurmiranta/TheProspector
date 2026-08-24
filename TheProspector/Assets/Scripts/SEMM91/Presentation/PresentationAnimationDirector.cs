using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PresentationAnimationDirector : MonoBehaviour
    {
        [Serializable]
        private sealed class AnimationBinding
        {
            [SerializeField] private string id = string.Empty;
            [SerializeField] private SplineAnimate splineAnimate;
            [SerializeField] private SplineContainer spline;
            [SerializeField] private Animator animator;
            [SerializeField] private AnimationClip animationClip;
            [SerializeField] private string animatorStateName = string.Empty;

            public string Id => id;
            public SplineAnimate SplineAnimate => splineAnimate;
            public SplineContainer Spline => spline;
            public Animator Animator => animator;
            public AnimationClip AnimationClip => animationClip;
            public string AnimatorStateName => animatorStateName;
        }

        public static PresentationAnimationDirector Instance { get; private set; }

        [Header("Bindings")]
        [SerializeField] private List<AnimationBinding> bindings =
            new List<AnimationBinding>();

        [Header("Validation")]
        [SerializeField] private float durationToleranceSeconds = 0.01f;
        [SerializeField] private bool allDurationsMatch;
        [SerializeField] private bool transformOwnershipValid;

        public bool AllDurationsMatch => allDurationsMatch;
        public bool TransformOwnershipValid => transformOwnershipValid;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            RefreshValidation();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool Play(string bindingId)
        {
            AnimationBinding binding = FindBinding(bindingId);

            if (binding == null)
            {
                Debug.LogError(
                    $"[PRESENTATION ANIMATION] No binding found for id '{bindingId}'.",
                    this
                );
                return false;
            }

            if (!IsRuntimeBindingComplete(binding))
            {
                Debug.LogError(
                    $"[PRESENTATION ANIMATION] Binding '{bindingId}' is incomplete.",
                    this
                );
                return false;
            }

            binding.SplineAnimate.Pause();
            binding.SplineAnimate.Container = binding.Spline;
            binding.SplineAnimate.Restart(false);

            binding.Animator.Play(
                binding.AnimatorStateName,
                0,
                0.0f
            );

            binding.SplineAnimate.Play();
            return true;
        }

        public bool TryGetDuration(
            string bindingId,
            out float durationSeconds)
        {
            durationSeconds = 0.0f;

            AnimationBinding binding =
                FindBinding(bindingId);

            if (binding == null ||
                binding.AnimationClip == null)
            {
                return false;
            }

            durationSeconds =
                binding.AnimationClip.length;

            return durationSeconds > 0.0f;
        }
        
        public bool Stop(string bindingId)
        {
            AnimationBinding binding = FindBinding(bindingId);

            if (binding == null || binding.SplineAnimate == null)
                return false;

            binding.SplineAnimate.Pause();
            return true;
        }

        [ContextMenu("Refresh Validation")]
        public void RefreshValidation()
        {
            allDurationsMatch = true;
            transformOwnershipValid = true;

            if (bindings == null || bindings.Count == 0)
            {
                allDurationsMatch = false;
                transformOwnershipValid = false;
                return;
            }

            foreach (AnimationBinding binding in bindings)
            {
                if (!DurationMatches(binding))
                    allDurationsMatch = false;

                if (!OwnershipIsValid(binding))
                    transformOwnershipValid = false;
            }
        }

        private AnimationBinding FindBinding(string bindingId)
        {
            if (string.IsNullOrWhiteSpace(bindingId))
                return null;

            foreach (AnimationBinding binding in bindings)
            {
                if (binding != null && binding.Id == bindingId)
                    return binding;
            }

            return null;
        }

        private bool DurationMatches(
            AnimationBinding binding)
        {
            if (binding == null ||
                binding.SplineAnimate == null ||
                binding.AnimationClip == null)
            {
                return false;
            }

            float animationDuration =
                binding.AnimationClip.length;

#if UNITY_EDITOR
            float stateSpeed =
                GetAnimatorStateSpeed(binding);

            if (Mathf.Abs(stateSpeed) > 0.0001f)
            {
                animationDuration /=
                    Mathf.Abs(stateSpeed);
            }
#endif
            
            return Mathf.Abs(
                binding.SplineAnimate.Duration -
                animationDuration
            ) <= durationToleranceSeconds;
        }
        
#if UNITY_EDITOR
        private float GetAnimatorStateSpeed(
            AnimationBinding binding)
        {
            if (binding.Animator == null ||
                binding.Animator.runtimeAnimatorController == null)
            {
                return 1.0f;
            }

            AnimatorController controller =
                binding.Animator
                        .runtimeAnimatorController
                    as AnimatorController;

            if (controller == null)
            {
                return 1.0f;
            }

            for (int layerIndex = 0;
                 layerIndex < controller.layers.Length;
                 layerIndex++)
            {
                AnimatorState state =
                    FindState(
                        controller.layers[layerIndex]
                            .stateMachine,
                        binding.AnimatorStateName
                    );

                if (state != null)
                {
                    return state.speed;
                }
            }

            return 1.0f;
        }

        private static AnimatorState FindState(
            AnimatorStateMachine stateMachine,
            string stateName)
        {
            foreach (
                ChildAnimatorState childState
                in stateMachine.states)
            {
                if (childState.state.name ==
                    stateName)
                {
                    return childState.state;
                }
            }

            foreach (
                ChildAnimatorStateMachine childMachine
                in stateMachine.stateMachines)
            {
                AnimatorState found =
                    FindState(
                        childMachine.stateMachine,
                        stateName
                    );

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
#endif
        
        private bool OwnershipIsValid(
            AnimationBinding binding)
        {
            if (binding == null ||
                binding.SplineAnimate == null ||
                binding.Spline == null ||
                binding.Animator == null ||
                binding.AnimationClip == null)
            {
                return false;
            }

            Transform motionRoot =
                binding.SplineAnimate.transform;

            Transform animatedRoot =
                binding.Animator.transform;

            /*
             * The spline owns MotionRoot.
             * Animator must live beneath MotionRoot.
             */
            if (motionRoot == animatedRoot ||
                !animatedRoot.IsChildOf(motionRoot))
            {
                return false;
            }

            /*
             * Spline owns position only.
             * Rotation remains available to Animator.
             */
            if (binding.SplineAnimate.Alignment !=
                SplineAnimate.AlignmentMode.None)
            {
                return false;
            }

            /*
             * Avoid Mecanim root-motion translation
             * bypassing our explicit hierarchy contract.
             *
             * Ordinary keyed local position on AnimatedRoot
             * and its children remains completely legal.
             */
            if (binding.Animator.applyRootMotion)
            {
                return false;
            }

            return true;
        }
        private static bool IsRuntimeBindingComplete(
            AnimationBinding binding)
        {
            return binding != null &&
                   !string.IsNullOrWhiteSpace(binding.Id) &&
                   binding.SplineAnimate != null &&
                   binding.Spline != null &&
                   binding.Animator != null &&
                   binding.AnimationClip != null &&
                   !string.IsNullOrWhiteSpace(binding.AnimatorStateName);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            durationToleranceSeconds =
                Mathf.Max(0.0001f, durationToleranceSeconds);

            RefreshValidation();
        }
#endif
    }
}