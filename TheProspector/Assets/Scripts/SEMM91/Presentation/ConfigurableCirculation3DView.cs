using System;
using System.Collections.Generic;
using SEMM91.Domain;
using UnityEngine;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ConfigurableCirculation3DView
        : MonoBehaviour
    {
        [SerializeField] [Min(0.1f)] private float secondsPerDay = 1f;

        [SerializeField] [Min(1)] private int listeningDays = 2;

        private sealed class EdgeDisplay
        {
            public LineRenderer Shaft;
            public LineRenderer Arrow;
        }

        private ConfigurableCirculationScenario _scenario;
        private CirculationObservation _observation;

        private readonly Dictionary<string, GameObject>
            _nodes = new();

        private readonly Dictionary<string, Vector3>
            _positions = new();

        private readonly Dictionary<string, EdgeDisplay>
            _edges = new();

        private GameObject _visualRoot;
        private Camera _camera;
        private bool _ownsCamera;

        private Material _lineMaterial;
        private MaterialPropertyBlock _colorBlock;

        private string _selectedNodeId;
        private string _selectedProductId;

        private bool _running;
        private float _elapsed;
        private Vector2 _scroll;
        private GUIStyle _nodeLabelStyle;

        private void Awake()
        {
            _colorBlock = new MaterialPropertyBlock();

            ConfigureCamera();
            CreateLineMaterial();
            ResetSimulation();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                HandleNodeClick();

            if (!_running)
                return;

            _elapsed += Time.deltaTime;

            var interval = Mathf.Max(0.1f, secondsPerDay);
            var steps = 0;

            while (_elapsed >= interval && steps < 20)
            {
                _elapsed -= interval;
                _scenario.World.AdvanceDay();
                steps++;
            }

            if (steps > 0)
                RefreshVisuals();
        }

        private static CirculationWorldDefinition
            BuildDefinition()
        {
            var d = new CirculationWorldDefinition();

            d.AddNode("A", "FI");
            d.AddNode("B", "FI");
            d.AddNode("C", "FI");
            d.AddNode("D", "SE");
            d.AddNode("E", "SE");

            d.AddConnection("AB", "A", "B");
            d.AddConnection("BC", "B", "C");

            // vastakkaiset tunnetut suunnat.
            d.AddConnection("CB", "C", "B");

            d.AddConnection("CD", "C", "D");
            d.AddConnection("DE", "D", "E");
            d.AddConnection("DA", "D", "A");

            d.MarkKnownMissing("A", "B");
            d.MarkKnownMissing("B", "C");
            d.MarkKnownMissing("C", "D");
            d.MarkKnownMissing("D", "E");

            d.SetSeed("A", "D1");
            d.AddSeed("A", "D2");

            return d;
        }

        private void ResetSimulation()
        {
            _running = false;
            _elapsed = 0f;
            _scroll = Vector2.zero;

            _scenario = new ConfigurableCirculationScenario(
                BuildDefinition(),
                Mathf.Max(1, listeningDays));

            _selectedNodeId = null;
            _selectedProductId = _scenario.ProductIds[0];

            _observation = _scenario.Observation.Capture();

            BuildGeometry();
            RefreshVisuals();
        }

        private void ConfigureCamera()
        {
            _camera = Camera.main;

            if (_camera == null)
            {
                var cameraObject =
                    new GameObject("CirculationCamera");

                cameraObject.tag = "MainCamera";
                _camera = cameraObject.AddComponent<Camera>();
                _ownsCamera = true;
            }

            // Tämä komponentti on tarkoitettu
            // erilliseen testisceneen.
            _camera.orthographic = true;
            _camera.orthographicSize = 6.5f;

            _camera.transform.position =
                new Vector3(-1.8f, 0f, -25f);

            _camera.transform.rotation =
                Quaternion.identity;

            _camera.clearFlags =
                CameraClearFlags.SolidColor;

            _camera.backgroundColor =
                new Color(0.07f, 0.09f, 0.12f);
        }

        private void CreateLineMaterial()
        {
            var shader = Shader.Find(
                "Universal Render Pipeline/Particles/Unlit");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            if (shader == null)
                throw new InvalidOperationException(
                    "No suitable line shader found.");

            _lineMaterial = new Material(shader);
            _lineMaterial.color = Color.white;
        }

        private void BuildGeometry()
        {
            if (_visualRoot != null)
                Destroy(_visualRoot);

            _nodes.Clear();
            _positions.Clear();
            _edges.Clear();

            _visualRoot =
                new GameObject("CirculationVisualRoot");

            var count = _observation.Nodes.Count;
            var radius = Mathf.Max(
                2.8f, count * 0.55f);

            for (var i = 0; i < count; i++)
            {
                var id = _observation.Nodes[i].Id;

                var angle =
                    Mathf.PI * 0.5f +
                    i * Mathf.PI * 2f / count;

                var position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f);

                var sphere = GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);

                sphere.name = "Node_" + id;
                sphere.transform.SetParent(
                    _visualRoot.transform);

                sphere.transform.position = position;
                sphere.transform.localScale =
                    Vector3.one * 0.75f;

                _nodes.Add(id, sphere);
                _positions.Add(id, position);
            }

            BuildConnections();
        }

        private void BuildConnections()
        {
            var totals =
                new Dictionary<(string, string), int>();

            var indices =
                new Dictionary<(string, string), int>();

            foreach (var edge in _observation.Connections)
            {
                var key = (edge.SenderId, edge.RecipientId);

                totals.TryGetValue(key, out var count);
                totals[key] = count + 1;
            }

            foreach (var edge in _observation.Connections)
            {
                var key = (edge.SenderId, edge.RecipientId);

                indices.TryGetValue(key, out var index);
                indices[key] = index + 1;

                var total = totals[key];

                // Erottaa samansuuntaiset rinnakkaiset
                // kaaret toisistaan. Perussiirtymä
                // erottaa myös vastakkaiset suunnat.
                var offset =
                    0.12f +
                    (index - (total - 1) * 0.5f)
                    * 0.22f;

                var from = _positions[edge.SenderId];
                var to = _positions[edge.RecipientId];

                var direction = (to - from).normalized;
                var perpendicular = new Vector3(
                    -direction.y, direction.x, 0f);

                var start =
                    from + direction * 0.48f +
                    perpendicular * offset;

                var tip =
                    to - direction * 0.48f +
                    perpendicular * offset;

                start.z = 0.3f;
                tip.z = 0.3f;

                var shaft = MakeLine(
                    "Edge_" + edge.Id, 2);

                shaft.SetPosition(0, start);
                shaft.SetPosition(1, tip);

                var arrow = MakeLine(
                    "Arrow_" + edge.Id, 3);

                var basePoint =
                    tip - direction * 0.25f;

                arrow.SetPosition(
                    0, basePoint + perpendicular * 0.13f);

                arrow.SetPosition(1, tip);

                arrow.SetPosition(
                    2, basePoint - perpendicular * 0.13f);

                _edges.Add(
                    edge.Id,
                    new EdgeDisplay
                    {
                        Shaft = shaft,
                        Arrow = arrow
                    });
            }
        }

        private LineRenderer MakeLine(
            string objectName,
            int pointCount)
        {
            var go = new GameObject(objectName);

            go.transform.SetParent(
                _visualRoot.transform);

            var line = go.AddComponent<LineRenderer>();

            line.sharedMaterial = _lineMaterial;
            line.positionCount = pointCount;
            line.useWorldSpace = true;
            line.widthMultiplier = 0.045f;
            line.numCapVertices = 4;
            line.numCornerVertices = 3;

            return line;
        }

        private void RefreshVisuals()
        {
            _observation = _scenario.Observation.Capture();

            var visual = CirculationVisualProjection.Build(
                _observation,
                _selectedNodeId,
                _selectedProductId);

            foreach (var node in visual.Nodes)
            {
                var renderer =
                    _nodes[node.Id].GetComponent<Renderer>();

                var color = ColorFor(node);

                _colorBlock.Clear();
                _colorBlock.SetColor("_BaseColor", color);
                _colorBlock.SetColor("_Color", color);

                renderer.SetPropertyBlock(_colorBlock);
            }

            foreach (var edge in visual.Connections)
            {
                var display = _edges[edge.Id];

                var color = edge.IsHighlighted
                    ? new Color(0.25f, 0.88f, 0.48f)
                    : new Color(0.42f, 0.49f, 0.56f);

                var width = edge.IsHighlighted
                    ? 0.075f
                    : 0.045f;

                SetLineAppearance(
                    display.Shaft, color, width);

                SetLineAppearance(
                    display.Arrow, color, width);
            }
        }

        private static void SetLineAppearance(
            LineRenderer line,
            Color color,
            float width)
        {
            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier = width;
        }

        private static Color ColorFor(
            CirculationNodeVisual node)
        {
            switch (node.Tone)
            {
                case CirculationNodeTone.SelectedNode:
                    return new Color(1f, 0.82f, 0.18f);

                case CirculationNodeTone.OutgoingContact:
                    return new Color(0.28f, 0.86f, 0.44f);

                case CirculationNodeTone.NotReceived:
                    return new Color(0.32f, 0.38f, 0.42f);

                case CirculationNodeTone.ReceivedUnopened:
                    return new Color(0.72f, 0.93f, 0.78f);

                case CirculationNodeTone.Queued:
                    return new Color(0.38f, 0.69f, 0.53f);

                case CirculationNodeTone.Listening:
                    return Color.Lerp(
                        new Color(0.42f, 0.73f, 0.56f),
                        new Color(0.10f, 0.36f, 0.26f),
                        Mathf.Clamp01(node.Progress));

                case CirculationNodeTone.Completed:
                    return new Color(0.07f, 0.31f, 0.22f);

                default:
                    return new Color(0.55f, 0.60f, 0.66f);
            }
        }

        private void SelectNode(string id)
        {
            _selectedNodeId = id;
            _selectedProductId = null;
            RefreshVisuals();
        }

        private void SelectDemo(string id)
        {
            _selectedNodeId = null;
            _selectedProductId = id;
            RefreshVisuals();
        }

        private void HandleNodeClick()
        {
            if (_camera == null || _scenario == null)
                return;

            // IMGUI-ohjauspaneeli on vasemmalla.
            if (Input.mousePosition.x < 320f)
                return;

            var ray = _camera.ScreenPointToRay(
                Input.mousePosition);

            if (!Physics.Raycast(ray, out var hit))
                return;

            foreach (var pair in _nodes)
                if (hit.collider.gameObject == pair.Value)
                {
                    SelectNode(pair.Key);
                    return;
                }
        }

        private void OnGUI()
        {
            if (_scenario == null || _camera == null)
                return;

            DrawNodeLabels();

            GUILayout.BeginArea(
                new Rect(
                    12f, 12f,
                    290f, Screen.height - 24f),
                GUI.skin.box);

            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label(
                "SEMM91 — Circulation network");

            GUILayout.Label(
                "Day: " + _observation.Day);

            GUILayout.Label(
                "Pending events: " +
                _scenario.Clock.PendingCount);

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(
                    _running ? "Pause" : "Play"))
            {
                _running = !_running;
                _elapsed = 0f;
            }

            if (GUILayout.Button("Step +1"))
            {
                _running = false;
                _elapsed = 0f;

                _scenario.World.AdvanceDay();
                RefreshVisuals();
            }

            if (GUILayout.Button("Reset"))
                ResetSimulation();

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Clear selection"))
            {
                _selectedNodeId = null;
                _selectedProductId = null;
                RefreshVisuals();
            }

            GUILayout.Space(12f);
            GUILayout.Label("SELECT NODE");

            foreach (var node in _observation.Nodes)
            {
                var prefix =
                    _selectedNodeId == node.Id ? "[*] " : "";

                if (GUILayout.Button(prefix + node.Id))
                    SelectNode(node.Id);
            }

            GUILayout.Space(12f);
            GUILayout.Label("SELECT DEMO");

            foreach (var productId in
                     _observation.ProductIds)
            {
                var prefix =
                    _selectedProductId == productId
                        ? "[*] "
                        : "";

                if (GUILayout.Button(prefix + productId))
                    SelectDemo(productId);
            }

            GUILayout.Space(12f);
            GUILayout.Label("SELECTION DETAILS");


            if (_selectedNodeId != null)
            {
                var details =
                    CirculationInspectorProjection.ForNode(
                        _observation,
                        _selectedNodeId);

                GUILayout.Label("Node: " + details.NodeId);
                GUILayout.Label(
                    "Country: " + details.CountryCode);
                GUILayout.Label(
                    "Capital: " + details.Capital);

                GUILayout.Space(8f);
                GUILayout.Label(
                    "OUTGOING CONTACTS (" +
                    details.Outgoing.Count + ")");

                foreach (var edge in details.Outgoing)
                    GUILayout.Label(
                        edge.Id + ": " +
                        edge.SenderId + " -> " +
                        edge.RecipientId);

                GUILayout.Space(8f);
                GUILayout.Label(
                    "INCOMING CONTACTS (" +
                    details.Incoming.Count + ")");

                foreach (var edge in details.Incoming)
                    GUILayout.Label(
                        edge.Id + ": " +
                        edge.SenderId + " -> " +
                        edge.RecipientId);

                GUILayout.Space(8f);
                GUILayout.Label("CULTURAL PRODUCTS");

                foreach (var state in details.Products)
                {
                    GUILayout.Label(
                        state.ProductId + ": " +
                        state.Phase +
                        " / " +
                        (state.Progress * 100f).ToString("F0") +
                        "%");

                    if (state.Reaction.HasValue)
                        GUILayout.Label(
                            "  Private decision: " +
                            state.Reaction.Value +
                            " (day " + state.ReactionDay + ")");
                }

                GUILayout.Space(8f);
                GUILayout.Label("OPINIONS LEARNED");

                foreach (var opinion in details.OpinionsReceived)
                    GUILayout.Label(
                        "Heard " +
                        opinion.SpeakerNodeId +
                        " on " + opinion.ProductId +
                        ": " + opinion.Thumb);

                GUILayout.Space(8f);
                GUILayout.Label("OPINIONS KNOWN BY OTHERS");

                foreach (var opinion in
                         details.OpinionsKnownByOthers)
                    GUILayout.Label(
                        opinion.ObserverNodeId +
                        " heard " + details.NodeId +
                        " on " + opinion.ProductId +
                        ": " + opinion.Thumb);
            }
            else if (_selectedProductId != null)
            {
                var details =
                    CirculationInspectorProjection.ForProduct(
                        _observation,
                        _selectedProductId);

                GUILayout.Label("Demo: " + details.ProductId);

                GUILayout.Space(8f);
                GUILayout.Label("EXPLICIT NETWORK REACH");

                GUILayout.Label(
                    "Ever received: " +
                    details.EverReceivedCount +
                    " / " + details.NodeStates.Count);

                GUILayout.Label(
                    "Not received: " +
                    details.NotReceivedCount);

                GUILayout.Label(
                    "Received, unopened: " +
                    details.ReceivedUnopenedCount);

                GUILayout.Label(
                    "Queued: " + details.QueuedCount);

                GUILayout.Label(
                    "Listening: " + details.ListeningCount);

                GUILayout.Label(
                    "Completed: " + details.CompletedCount);

                GUILayout.Space(8f);
                GUILayout.Label("LISTENING REACTIONS");

                GUILayout.Label(
                    "Support and copy: " +
                    details.SupportCount);

                GUILayout.Label(
                    "Oppose and transfer: " +
                    details.TransferCount);

                GUILayout.Label(
                    "Oppose and shelve: " +
                    details.ShelveCount);

                GUILayout.Space(8f);
                GUILayout.Label("NODE STATES");

                foreach (var state in details.NodeStates)
                {
                    GUILayout.Label(
                        state.NodeId + ": " +
                        state.Phase +
                        " / " +
                        (state.Progress * 100f).ToString("F0") +
                        "%");

                    if (state.Reaction.HasValue)
                        GUILayout.Label(
                            "  Decision: " +
                            state.Reaction.Value);
                }

                GUILayout.Space(8f);
                GUILayout.Label("KNOWN COMMUNICATIONS");

                foreach (var opinion in
                         details.KnownCommunications)
                    GUILayout.Label(
                        opinion.SpeakerNodeId +
                        " -> " +
                        opinion.ObserverNodeId +
                        ": " +
                        opinion.Thumb);
            }
            else
            {
                GUILayout.Label("No selection");
            }


            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawNodeLabels()
        {
            if (_nodeLabelStyle == null)
            {
                _nodeLabelStyle =
                    new GUIStyle(GUI.skin.label);

                _nodeLabelStyle.alignment =
                    TextAnchor.MiddleCenter;

                _nodeLabelStyle.fontStyle =
                    FontStyle.Bold;

                _nodeLabelStyle.normal.textColor =
                    Color.white;
            }

            foreach (var pair in _positions)
            {
                var screen =
                    _camera.WorldToScreenPoint(pair.Value);

                if (screen.z <= 0f)
                    continue;

                
                GUI.Label(
                    new Rect(
                        screen.x - 30f,
                        Screen.height - screen.y - 11f,
                        60f,
                        22f),
                    pair.Key,
                    _nodeLabelStyle);

            }
        }

        private void OnDestroy()
        {
            if (_visualRoot != null)
                Destroy(_visualRoot);

            if (_lineMaterial != null)
                Destroy(_lineMaterial);

            if (_ownsCamera && _camera != null)
                Destroy(_camera.gameObject);
        }
    }
}