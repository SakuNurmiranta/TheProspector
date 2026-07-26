using UnityEngine;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SkullIdleAnimator : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField]
        private Transform modelMotionRoot;

        [SerializeField]
        private Light skullPointLight;

        [SerializeField]
        private LocalHingeRotation jawHinge;

        [Header("Time")]
        [SerializeField]
        private bool useUnscaledTime = true;

        [Header("Model Undulation")]
        [SerializeField]
        private bool animateModel = true;

        [SerializeField, Min(0f)]
        private float modelCyclesPerSecond = 0.18f;

        [SerializeField]
        private float modelVerticalAmplitude = 0.015f;

        [SerializeField]
        private float modelXTiltAmplitude = 1.25f;

        [SerializeField, Range(0f, 1f)]
        private float modelTiltPhaseOffset = 0.25f;

        [SerializeField]
        private AnimationCurve modelEase =
            AnimationCurve.EaseInOut(
                0f,
                0f,
                1f,
                1f
            );

        [Header("Jaw Breathing")]
        [SerializeField]
        private bool animateJaw = true;

        [SerializeField, Min(0f)]
        private float jawCyclesPerSecond = 0.3f;

        [Tooltip(
            "Use a negative value if the jaw opens toward " +
            "negative hinge angles."
        )]
        [SerializeField]
        private float jawAngleAmplitude = -1.5f;

        [SerializeField, Range(0f, 1f)]
        private float jawPhaseOffset;

        [SerializeField]
        private AnimationCurve jawEase =
            AnimationCurve.EaseInOut(
                0f,
                0f,
                1f,
                1f
            );

        [Header("Point Light Movement")]
        [SerializeField]
        private bool animateLightMovement = true;

        [SerializeField, Min(0f)]
        private float lightMovementCyclesPerSecond = 2.4f;

        [SerializeField]
        private Vector3 lightLocalPositionAmplitude =
            new Vector3(
                0.02f,
                0.03f,
                0.015f
            );

        [SerializeField, Range(0f, 1f)]
        private float lightMovementPhaseOffset = 0.25f;

        [SerializeField]
        private AnimationCurve lightMovementEase =
            AnimationCurve.EaseInOut(
                0f,
                0f,
                1f,
                1f
            );

        [Header("Point Light Intensity")]
        [SerializeField]
        private bool animateLightIntensity = true;

        [SerializeField, Min(0f)]
        private float lightIntensityCyclesPerSecond = 3.2f;

        [Tooltip(
            "Fraction of the captured baseline intensity. " +
            "0.08 means plus or minus 8 percent."
        )]
        [SerializeField, Range(0f, 0.95f)]
        private float lightIntensityFraction = 0.08f;

        [SerializeField, Range(0f, 1f)]
        private float lightIntensityPhaseOffset = 0.1f;

        [SerializeField]
        private AnimationCurve lightIntensityEase =
            AnimationCurve.EaseInOut(
                0f,
                0f,
                1f,
                1f
            );

        private Vector3 _modelRestLocalPosition;
        private Quaternion _modelRestLocalRotation;

        private Vector3 _lightRestLocalPosition;
        private float _lightRestIntensity;

        private bool _hasModelRestState;
        private bool _hasLightRestState;

        private void Awake()
        {
            CaptureRestState();
        }

        private void LateUpdate()
        {
            float time =
                useUnscaledTime
                    ? Time.unscaledTime
                    : Time.time;

            ApplyModelMotion(time);
            ApplyJawMotion(time);
            ApplyLightMotion(time);
            ApplyLightIntensity(time);
        }

        private void OnDisable()
        {
            RestoreRestState();
        }

        private void CaptureRestState()
        {
            if (modelMotionRoot != null)
            {
                _modelRestLocalPosition =
                    modelMotionRoot.localPosition;

                _modelRestLocalRotation =
                    modelMotionRoot.localRotation;

                _hasModelRestState = true;
            }

            if (skullPointLight != null)
            {
                _lightRestLocalPosition =
                    skullPointLight.transform.localPosition;

                _lightRestIntensity =
                    skullPointLight.intensity;

                _hasLightRestState = true;
            }
        }

        private void ApplyModelMotion(float time)
        {
            if (!animateModel ||
                !_hasModelRestState ||
                modelMotionRoot == null)
            {
                return;
            }

            float verticalSignal =
                EvaluateBipolar(
                    time,
                    modelCyclesPerSecond,
                    0f,
                    modelEase
                );

            float tiltSignal =
                EvaluateBipolar(
                    time,
                    modelCyclesPerSecond,
                    modelTiltPhaseOffset,
                    modelEase
                );

            modelMotionRoot.localPosition =
                _modelRestLocalPosition +
                Vector3.up *
                (
                    modelVerticalAmplitude *
                    verticalSignal
                );

            modelMotionRoot.localRotation =
                _modelRestLocalRotation *
                Quaternion.AngleAxis(
                    modelXTiltAmplitude *
                    tiltSignal,
                    Vector3.right
                );
        }

        private void ApplyJawMotion(float time)
        {
            if (!animateJaw ||
                jawHinge == null)
            {
                return;
            }

            // One-directional breath:
            // base angle -> base + amplitude -> base.
            float breath =
                Evaluate01(
                    time,
                    jawCyclesPerSecond,
                    jawPhaseOffset,
                    jawEase
                );

            jawHinge.SetAdditiveAngle(
                jawAngleAmplitude *
                breath
            );
        }

        private void ApplyLightMotion(float time)
        {
            if (!animateLightMovement ||
                !_hasLightRestState ||
                skullPointLight == null)
            {
                return;
            }

            float x =
                EvaluateBipolar(
                    time,
                    lightMovementCyclesPerSecond,
                    lightMovementPhaseOffset,
                    lightMovementEase
                );

            float y =
                EvaluateBipolar(
                    time,
                    lightMovementCyclesPerSecond * 1.07f,
                    lightMovementPhaseOffset + 0.31f,
                    lightMovementEase
                );

            float z =
                EvaluateBipolar(
                    time,
                    lightMovementCyclesPerSecond * 0.91f,
                    lightMovementPhaseOffset + 0.63f,
                    lightMovementEase
                );

            Vector3 offset =
                Vector3.Scale(
                    lightLocalPositionAmplitude,
                    new Vector3(
                        x,
                        y,
                        z
                    )
                );

            skullPointLight.transform.localPosition =
                _lightRestLocalPosition +
                offset;
        }

        private void ApplyLightIntensity(float time)
        {
            if (!animateLightIntensity ||
                !_hasLightRestState ||
                skullPointLight == null)
            {
                return;
            }

            float signal =
                EvaluateBipolar(
                    time,
                    lightIntensityCyclesPerSecond,
                    lightIntensityPhaseOffset,
                    lightIntensityEase
                );

            float multiplier =
                1f +
                lightIntensityFraction *
                signal;

            skullPointLight.intensity =
                Mathf.Max(
                    0f,
                    _lightRestIntensity *
                    multiplier
                );
        }

        private void RestoreRestState()
        {
            if (_hasModelRestState &&
                modelMotionRoot != null)
            {
                modelMotionRoot.localPosition =
                    _modelRestLocalPosition;

                modelMotionRoot.localRotation =
                    _modelRestLocalRotation;
            }

            if (_hasLightRestState &&
                skullPointLight != null)
            {
                skullPointLight.transform.localPosition =
                    _lightRestLocalPosition;

                skullPointLight.intensity =
                    _lightRestIntensity;
            }

            if (jawHinge != null)
            {
                jawHinge.SetAdditiveAngle(0f);
            }
        }

        private static float Evaluate01(
            float time,
            float cyclesPerSecond,
            float phaseOffset,
            AnimationCurve ease)
        {
            if (cyclesPerSecond <= 0f)
            {
                return 0f;
            }

            float phase =
                Mathf.Repeat(
                    time * cyclesPerSecond +
                    phaseOffset,
                    1f
                );

            // 0 -> 1 -> 0 over one cycle.
            float pingPong =
                1f -
                Mathf.Abs(
                    phase * 2f - 1f
                );

            float value =
                ease != null
                    ? ease.Evaluate(pingPong)
                    : pingPong;

            return Mathf.Clamp01(value);
        }

        private static float EvaluateBipolar(
            float time,
            float cyclesPerSecond,
            float phaseOffset,
            AnimationCurve ease)
        {
            return
                Evaluate01(
                    time,
                    cyclesPerSecond,
                    phaseOffset,
                    ease
                ) *
                2f -
                1f;
        }
    }
}