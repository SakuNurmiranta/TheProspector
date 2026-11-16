
using System;
using System.Collections.Generic;
using SEMM91.Domain;
using UnityEngine;

namespace SEMM91.Presentation
{
    public sealed partial class ConfigurableCirculation3DView
    {
        [Header("Crucible 9.2.19 — Research layout")]
        [SerializeField]
        private bool researchLayoutEnabled = true;

        [SerializeField, Min(1)]
        private int layoutRebuildEveryDays = 7;

        [SerializeField, Min(0.1f)]
        private float layoutTransitionSeconds = 0.8f;

        private SimulationWorld _researchWorld;
        private RollingCapitalGains _researchGains;
        private ResearchLayoutPlan _researchPlan;
        private Dictionary<string, float> _researchSizes;

        private readonly List<string> _researchIds =
            new List<string>();

        private readonly List<ResearchMail> _researchMessages =
            new List<ResearchMail>();

        private int _researchLastLayoutDay;
        private bool _researchPlanDirty;
        private bool _researchSizesDirty;

        private void InitializeResearchLayout()
        {
            DisposeResearchLayout();

            _researchIds.Clear();

            foreach (var node in _observation.Nodes)
                _researchIds.Add(node.Id);

            _researchWorld = _scenario.World;

            _researchGains = new RollingCapitalGains(
                _researchIds,
                id => _scenario.Capital.GetCapital(id),
                _researchWorld.CurrentDay);

            _researchSizes =
                ResearchLayoutModel.CapitalSizeFactors(
                    _scenario.Capital.Snapshot());

            _researchLastLayoutDay =
                _researchWorld.CurrentDay;

            RebuildResearchPlan();

            _researchPlanDirty = false;
            _researchSizesDirty = false;

            _researchWorld.DayCompleted +=
                OnResearchDayCompleted;
        }

        private void DisposeResearchLayout()
        {
            if (_researchWorld != null)
            {
                _researchWorld.DayCompleted -=
                    OnResearchDayCompleted;
            }

            _researchWorld = null;
            _researchGains = null;
            _researchPlan = null;
        }

        private void OnResearchDayCompleted(int day)
        {
            if (_researchGains == null)
                return;

            // Cheap daily balance sampling.
            _researchGains.RecordDay(day);

            _researchSizesDirty = true;

            // Do not rebuild here. A fast simulation
            // can advance 20 days in one Unity frame.
            if (day - _researchLastLayoutDay >=
                layoutRebuildEveryDays)
            {
                _researchPlanDirty = true;
            }
        }

        private void RebuildResearchPlan()
        {
            if (_scenario == null ||
                _researchGains == null)
            {
                return;
            }

            _researchMessages.Clear();

            foreach (var e in
                     _scenario.CommunicationArcs.Events)
            {
                _researchMessages.Add(
                    new ResearchMail(
                        e.SourceNodeId,
                        e.TargetNodeId,
                        e.SentDay));
            }

            _researchPlan = ResearchLayoutModel.Build(
                _researchIds,
                _researchGains.RecentGains,
                _researchMessages,
                _scenario.World.CurrentDay,
                _layoutRadius);
        }

        public bool TryGetResearchNodePosition(
            string nodeId,
            out Vector3 position)
        {
            return _positions.TryGetValue(
                nodeId, out position);
        }

        private void LateUpdate()
        {
            if (!researchLayoutEnabled ||
                _researchPlan == null ||
                _scenario == null ||
                _visualRoot == null)
            {
                return;
            }

            // At most one size and cluster update
            // per rendered frame.
            if (_researchSizesDirty)
            {
                _researchSizes =
                    ResearchLayoutModel.CapitalSizeFactors(
                        _scenario.Capital.Snapshot());

                _researchSizesDirty = false;
            }

            if (_researchPlanDirty)
            {
                RebuildResearchPlan();

                _researchLastLayoutDay =
                    _scenario.World.CurrentDay;

                _researchPlanDirty = false;
            }

            // Presentation-only smoothing.
            // No simulation days are advanced here.
            float factor = 1f - Mathf.Exp(
                -Mathf.Max(
                    0f, Time.unscaledDeltaTime) /
                Mathf.Max(
                    0.1f, layoutTransitionSeconds));

            bool moved = false;

            foreach (string id in _researchIds)
            {
                if (!_nodes.TryGetValue(
                        id, out var go) ||
                    go == null ||
                    !_researchPlan.Targets.TryGetValue(
                        id, out var target))
                {
                    continue;
                }

                Vector3 previous = _positions[id];

                Vector3 desired = new Vector3(
                    target.x,
                    target.y,
                    0f);

                Vector3 next = Vector3.Lerp(
                    previous, desired, factor);

                if ((next - desired).sqrMagnitude <
                    0.000001f)
                {
                    next = desired;
                }

                if ((next - previous).sqrMagnitude >
                    0.00000001f)
                {
                    _positions[id] = next;
                    go.transform.position = next;
                    moved = true;
                }

                float size =
                    _researchSizes != null &&
                    _researchSizes.TryGetValue(
                        id, out float rank)
                    ? rank
                    : 1f;

                Vector3 scale =
                    Vector3.one * (0.75f * size);

                go.transform.localScale =
                    Vector3.Lerp(
                        go.transform.localScale,
                        scale,
                        factor);
            }

            // Geometry is refreshed only while
            // at least one node is actually moving.
            if (!moved)
                return;

            MoveResearchArrows();

            // The pending reply guides follow the
            // same current coordinates as arrows.
            RefreshReplyGuides();
        }

        private void MoveResearchArrows()
        {
            var live = GetLiveConnections();

            var totals =
                new Dictionary<(string, string), int>();

            var indices =
                new Dictionary<(string, string), int>();

            foreach (var arc in live)
            {
                var pair = (
                    arc.SourceNodeId,
                    arc.TargetNodeId);

                totals.TryGetValue(
                    pair, out int count);

                totals[pair] = count + 1;
            }

            foreach (var arc in live)
            {
                var pair = (
                    arc.SourceNodeId,
                    arc.TargetNodeId);

                indices.TryGetValue(
                    pair, out int index);

                indices[pair] = index + 1;

                if (!_edges.TryGetValue(
                        arc.Id, out var display) ||
                    display.Shaft == null ||
                    display.Arrow == null ||
                    !_positions.TryGetValue(
                        arc.SourceNodeId,
                        out Vector3 from) ||
                    !_positions.TryGetValue(
                        arc.TargetNodeId,
                        out Vector3 to))
                {
                    continue;
                }

                Vector3 difference = to - from;

                if (difference.sqrMagnitude <
                    0.000001f)
                {
                    continue;
                }

                Vector3 direction =
                    difference.normalized;

                Vector3 perpendicular = new Vector3(
                    -direction.y,
                    direction.x,
                    0f);

                // 0-based indexing; the same geometry
                // rule as BuildConnections().
                float offset = GetArcOffset(
                    index,
                    totals[pair]);

                Vector3 start =
                    from +
                    direction * 0.48f +
                    perpendicular * offset;

                Vector3 tip =
                    to -
                    direction * 0.48f +
                    perpendicular * offset;

                start.z = 0.3f;
                tip.z = 0.3f;

                display.Shaft.SetPosition(0, start);
                display.Shaft.SetPosition(1, tip);

                Vector3 basePoint =
                    tip - direction * 0.25f;

                display.Arrow.SetPosition(
                    0,
                    basePoint +
                    perpendicular * 0.13f);

                display.Arrow.SetPosition(1, tip);

                display.Arrow.SetPosition(
                    2,
                    basePoint -
                    perpendicular * 0.13f);
            }
        }
    }
}
