
using SEMM91.Domain;
using UnityEngine;

namespace SEMM91.Presentation
{
    public sealed class AutonomousCirculationView
        : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float secondsPerDay = 1f;
        
        [SerializeField, Min(0)]
        private int listeningDays = 2;

        [SerializeField]
        private bool bKnowsCIsMissing = true;

        [SerializeField]
        private bool cKnowsDIsMissing = true;

        [SerializeField]
        private bool cCopies = true;

        
        [SerializeField]
        private bool enableThreeWayReactions = false;

        [SerializeField]
        private ListeningReaction bReaction =
            ListeningReaction.SupportAndCopy;

        [SerializeField]
        private ListeningReaction cReaction =
            ListeningReaction.SupportAndCopy;

        
        private AutonomousCirculationScenario _scenario;

        private float _elapsed;
        private bool _playing;
        private Vector2 _scroll;

        private void Awake()
        {
            ResetWorld();
        }

        private void Update()
        {
            if (!_playing || _scenario == null)
                return;

            _elapsed += Time.deltaTime;

            float interval =
                Mathf.Max(0.1f, secondsPerDay);

            while (_elapsed >= interval)
            {
                _elapsed -= interval;

                // Tästä eteenpäin tapahtumat tulevat
                // SimulationWorldin omasta kierrosta.
                _scenario.World.AdvanceDay();
            }
        }

       
        private void ResetWorld()
        {
            // Kolmijakoinen päätöksenteko edellyttää
            // vähintään yhden päivän kuuntelua.
            int duration = enableThreeWayReactions
                ? Mathf.Max(1, listeningDays)
                : listeningDays;

            _scenario = new AutonomousCirculationScenario(
                bKnowsCIsMissing,
                cKnowsDIsMissing,
                cCopies,
                duration,
                enableThreeWayReactions
                    ? bReaction
                    : (ListeningReaction?)null,
                enableThreeWayReactions
                    ? cReaction
                    : (ListeningReaction?)null);

            _playing = false;
            _elapsed = 0f;
            _scroll = Vector2.zero;
        }


        private void OnGUI()
        {
            if (_scenario == null)
                return;

            float width = Mathf.Min(
                620f, Screen.width - 40f);

            float height = Screen.height - 40f;

            GUILayout.BeginArea(
                new Rect(20f, 20f, width, height),
                GUI.skin.box);

            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label(
                "SEMM91 — Autonomous circulation");

            GUILayout.Label(
                "Day: " + _scenario.World.CurrentDay);

            GUILayout.Label(
                "Pending events: " +
                _scenario.Clock.PendingCount);

            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(
                    _playing ? "Pause" : "Play"))
            {
                _playing = !_playing;
                _elapsed = 0f;
            }

            if (GUILayout.Button("Step +1"))
            {
                _playing = false;
                _elapsed = 0f;
                _scenario.World.AdvanceDay();
            }

            if (GUILayout.Button("Run +10"))
            {
                _playing = false;
                _elapsed = 0f;
                _scenario.World.AdvanceDays(10);
            }

            if (GUILayout.Button("Reset"))
            {
                ResetWorld();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("CULTURAL CAPITAL");

            foreach (string id in new[] { "A", "B", "C", "D" })
            {
                GUILayout.Label(
                    id + ": " +
                    _scenario.Capital.GetCapital(id));
            }

            GUILayout.Space(10f);
            GUILayout.Label("D1 RECEPTION HISTORY");

            foreach (string id in new[] { "A", "B", "C", "D" })
            {
                bool held =
                    _scenario.Inventory.HasEverHeldProduct(
                        id, "D1");

                GUILayout.Label(
                    id + ": " +
                    (held ? "received" : "not received"));
            }

            
            GUILayout.Space(10f);
            GUILayout.Label("D1 LISTENING PROGRESS");

            foreach (string id in new[] { "A", "B", "C", "D" })
            {
                var status =
                    _scenario.GetListeningStatus(id, "D1");

                if (status == null)
                {
                    GUILayout.Label(id + ": no listening record");
                    continue;
                }

                GUILayout.Label(
                    id + ": " +
                    status.Phase +
                    " (" + (status.Progress * 100f).ToString("F0") +
                    "%)");
            }

            
            GUILayout.Space(10f);
            GUILayout.Label("AUTOMATIC COPY DISTRIBUTION");

            foreach (string id in new[] { "A", "B", "C", "D" })
            {
                var policy = _scenario.GetPolicy(id);

                GUILayout.Label(
                    id + " — decisions: " +
                    policy.CopyDecisions +
                    ", shipments: " +
                    policy.Dispatched.Count);

                foreach (var shipment in policy.Dispatched)
                {
                    GUILayout.Label(
                        "  " + id + " -> " +
                        shipment.RecipientNodeId +
                        " [" + shipment.Shipment.Status + "]");
                }
            }
            
            
            GUILayout.Space(10f);
            GUILayout.Label("LISTENING REACTIONS");

            foreach (string id in new[] { "A", "B", "C", "D" })
            {
                var reactionPolicy =
                    _scenario.GetReactionPolicy(id);

                if (reactionPolicy == null)
                    continue;

                foreach (var decision in reactionPolicy.Decisions)
                {
                    GUILayout.Label(
                        "Day " + decision.Day +
                        " / " + decision.NodeId +
                        " / " + decision.ProductId +
                        " / " + decision.Reaction);
                }
            }

            GUILayout.Space(10f);
            GUILayout.Label("PHYSICAL CONTROL");

            foreach (var transfer in _scenario.Control.Transfers)
            {
                GUILayout.Label(
                    "TRANSFER: " +
                    transfer.SenderNodeId + " -> " +
                    transfer.RecipientNodeId +
                    " / " + transfer.Copy.Id +
                    " [" + transfer.Shipment.Status + "]");
            }

            foreach (var shelf in _scenario.Control.Shelves)
            {
                GUILayout.Label(
                    "SHELVED: " +
                    shelf.NodeId +
                    " / " + shelf.ProductId +
                    " / " + shelf.CopyId);
            }


            GUILayout.Space(10f);
            GUILayout.Label("OPENED MESSAGES");

            foreach (var opened in
                     _scenario.World.OpenedMessages)
            {
                GUILayout.Label(
                    "Day " + opened.Day +
                    " / " + opened.NodeId +
                    " / " + opened.PackageId);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
