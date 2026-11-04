
using SEMM91.Domain;
using UnityEngine;

namespace SEMM91.Presentation
{
    public sealed class KnowledgeDrivenScenarioView
        : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float secondsPerDay = 1f;

        private static readonly string[] VariantNames =
        {
            "C: D1 puuttuu",
            "C: tuntematon",
            "C: torjunta"
        };

        private KnowledgeDrivenScenarioPlayback _playback;

        private int _variantIndex;
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
            bool knowsMissing = _variantIndex != 1;
            bool communicatedDislike = _variantIndex == 2;

            _playback = new KnowledgeDrivenScenarioPlayback(
                bKnowsCIsMissing: knowsMissing,
                cCommunicatedDislike: communicatedDislike);

            _playing = false;
            _accumulator = 0f;
            _scrollPosition = Vector2.zero;
        }

        private void OnGUI()
        {
            if (_playback == null)
                return;

            float width = Mathf.Max(
                240f,
                Mathf.Min(640f, Screen.width - 40f));

            float height = Mathf.Max(
                120f,
                Screen.height - 40f);

            GUILayout.BeginArea(
                new Rect(20f, 20f, width, height),
                GUI.skin.box);

            _scrollPosition =
                GUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label(
                "SEMM91 — Knowledge-driven circulation");

            GUILayout.Space(10f);
            GUILayout.Label("SCENARIO");

            int selected = GUILayout.Toolbar(
                _variantIndex,
                VariantNames);

            if (selected != _variantIndex)
            {
                _variantIndex = selected;
                ResetScenario();
            }

            var current = _playback.Current;

            GUILayout.Space(8f);

            switch (_variantIndex)
            {
                case 0:
                    GUILayout.Label(
                        "B knows that C has not received D1.");
                    break;

                case 1:
                    GUILayout.Label(
                        "B does not know C's D1 history.");
                    break;

                case 2:
                    GUILayout.Label(
                        "B knows D1 is missing, but C " +
                        "has communicated a negative stance.");
                    break;
            }

            GUILayout.Space(10f);

            GUILayout.Label(
                $"Simulation day: {current.Day} / " +
                $"{KnowledgeDrivenScenarioPlayback.FinalDay}");

            GUILayout.Label(
                $"Pending events: {current.PendingEvents}");

            GUILayout.BeginHorizontal();

            GUI.enabled = !_playback.IsComplete;

            if (GUILayout.Button("Step +1"))
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
            }

            GUILayout.EndHorizontal();

            // Toiminnot ovat voineet muuttaa
            // simulaation tilaa tämän OnGUI-kutsun
            // aikana. Luetaan uusin tilannekuva.
            current = _playback.Current;

            GUILayout.Space(12f);
            GUILayout.Label("CULTURAL CAPITAL");

            GUILayout.Label(
                $"A: {current.CapitalA}     " +
                $"B: {current.CapitalB}     " +
                $"C: {current.CapitalC}");

            GUILayout.Space(10f);
            GUILayout.Label("D1 DISTRIBUTION");

            GUILayout.Label(
                $"First wave: {current.FirstWaveCount}");

            GUILayout.Label(
                $"Second wave: {current.SecondWaveCount}");

            GUILayout.Label(
                "Original holder: " +
                Holder(current.OriginalHolder));

            GUILayout.Label(
                "A to B copy: " +
                Holder(current.ABCopyHolder));

            GUILayout.Label(
                "B to C copy: " +
                Holder(current.BCCopyHolder));

            GUILayout.Label(
                "C has received D1: " +
                (current.CHasEverHeldD1 ? "YES" : "NO"));

            GUILayout.Space(10f);
            GUILayout.Label("NODE KNOWLEDGE");

            GUILayout.Label(
                "B's knowledge of A: " +
                Knowledge(current.BThinksAHasD1));

            GUILayout.Label(
                "B's knowledge of C: " +
                Knowledge(current.BThinksCHasD1));

            GUILayout.Label(
                "A's knowledge of B: " +
                Knowledge(current.AThinksBHasD1));

            GUILayout.Label(
                "C's knowledge of B: " +
                Knowledge(current.CThinksBHasD1));

            
            GUILayout.Label(
                "B knows C dislikes D1: " +
                (current.BKnowsCDislikesD1 ? "YES" : "NO"));

            
            GUILayout.Space(10f);
            GUILayout.Label("POSTAL SHIPMENTS");

            GUILayout.Label(
                "A to B: " + current.ShipABStatus);

            GUILayout.Label(
                "B to A: " +
                Status(current.ShipBAStatus));

            GUILayout.Label(
                "B to C: " +
                Status(current.ShipBCStatus));
            
            
            GUILayout.Label(
                "C to B: " +
                Status(current.ShipCBStatus));


            GUILayout.Space(10f);
            GUILayout.Label("BACKLOG");

            GUILayout.Label(
                $"A: {current.BacklogA}     " +
                $"B: {current.BacklogB}     " +
                $"C: {current.BacklogC}");

            GUILayout.Space(10f);
            GUILayout.Label("EVENT TRACE");

            foreach (string entry in current.Trace)
            {
                GUILayout.Label(entry);
            }

            GUILayout.Space(10f);
            GUILayout.Label(
                $"Recorded days: {_playback.History.Count}");

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static string Holder(string holder)
        {
            return holder ?? "not held / in transit";
        }

        private static string Status(
            PostalShipmentStatus? status)
        {
            return status.HasValue
                ? status.Value.ToString()
                : "not dispatched";
        }

        private static string Knowledge(bool? value)
        {
            if (!value.HasValue)
                return "unknown";

            return value.Value
                ? "D1 received"
                : "D1 not received";
        }
    }
}
