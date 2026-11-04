
using SEMM91.Domain;
using UnityEngine;

namespace SEMM91.Presentation
{
    public sealed class ThreeNodeScenarioView : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float secondsPerDay = 1f;

        private ThreeNodeScenarioPlayback _playback;

        private bool _playing;
        private float _accumulator;
        private Vector2 _scrollPosition;

        private void Awake()
        {
            ResetScenario();
        }

        private void Update()
        {
            if (!_playing || _playback == null)
                return;

            float interval =
                Mathf.Max(0.1f, secondsPerDay);

            _accumulator += Time.deltaTime;

            while (_accumulator >= interval &&
                   !_playback.IsComplete)
            {
                _accumulator -= interval;
                _playback.Step();
            }

            if (_playback.IsComplete)
            {
                _playing = false;
                _accumulator = 0f;
            }
        }

        private void ResetScenario()
        {
            _playback = new ThreeNodeScenarioPlayback();
            _playing = false;
            _accumulator = 0f;
            _scrollPosition = Vector2.zero;
        }

        private void OnGUI()
        {
            if (_playback == null)
                return;

            var current = _playback.Current;

            GUILayout.BeginArea(
                new Rect(
                    20f,
                    20f,
                    Mathf.Min(550f, Screen.width - 40f),
                    Mathf.Max(100f, Screen.height - 40f)),
                GUI.skin.box);

            _scrollPosition =
                GUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label("SEMM91 — Three-node demonstration");

            GUILayout.Space(8f);

            GUILayout.Label(
                $"Day: {current.Day} / " +
                $"{ThreeNodeScenarioPlayback.FinalDay}");

            GUILayout.Label(
                $"Pending events: {current.PendingEvents}");

            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();

            GUI.enabled = !_playback.IsComplete;

            if (GUILayout.Button("Step +1 day"))
            {
                _playing = false;
                _accumulator = 0f;
                _playback.Step();
            }

            if (GUILayout.Button(
                    _playing ? "Pause" : "Play"))
            {
                _playing = !_playing;
                _accumulator = 0f;
            }

            if (GUILayout.Button("Run to end"))
            {
                _playing = false;
                _accumulator = 0f;
                _playback.RunToEnd();
            }

            GUI.enabled = true;

            if (GUILayout.Button("Reset"))
            {
                ResetScenario();
                current = _playback.Current;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(12f);

            GUILayout.Label("CULTURAL CAPITAL");

            GUILayout.Label(
                $"A: {current.CapitalA}    " +
                $"B: {current.CapitalB}    " +
                $"C: {current.CapitalC}");

            GUILayout.Space(10f);

            GUILayout.Label("CASSETTE HOLDERS");

            GUILayout.Label(
                "Original: " +
                ShowHolder(current.OriginalHolder));

            GUILayout.Label(
                "A to B copy: " +
                ShowHolder(current.ABCopyHolder));

            GUILayout.Label(
                "B to C copy: " +
                ShowHolder(current.BCCopyHolder));

            GUILayout.Space(10f);

            GUILayout.Label("POSTAL SHIPMENTS");

            GUILayout.Label(
                "A to B: " + current.ShipABStatus);

            GUILayout.Label(
                "B to A: " +
                ShowStatus(current.ShipBAStatus));

            GUILayout.Label(
                "B to C: " +
                ShowStatus(current.ShipBCStatus));

            GUILayout.Space(10f);

            GUILayout.Label("BACKLOG");

            GUILayout.Label(
                $"A: {current.BacklogA}    " +
                $"B: {current.BacklogB}    " +
                $"C: {current.BacklogC}");

            GUILayout.Space(10f);

            GUILayout.Label("EVENT TRACE");

            foreach (string entry in current.Trace)
            {
                GUILayout.Label(entry);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static string ShowHolder(string holder)
        {
            return holder ?? "not held / in transit";
        }

        private static string ShowStatus(
            PostalShipmentStatus? status)
        {
            return status.HasValue
                ? status.Value.ToString()
                : "not dispatched";
        }
    }
}
