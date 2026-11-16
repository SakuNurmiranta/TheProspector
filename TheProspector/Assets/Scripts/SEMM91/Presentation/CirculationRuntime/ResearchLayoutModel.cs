
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEMM91.Presentation
{
    public struct ResearchMail
    {
        public readonly string From;
        public readonly string To;
        public readonly int Day;

        public ResearchMail(string from, string to, int day)
        {
            From = from;
            To = to;
            Day = day;
        }
    }

    // Daily balance sampling, independent of reward rules.
    public sealed class RollingCapitalGains
    {
        private struct CreditDay
        {
            public int Day;
            public string Node;
            public int Gain;
        }

        private readonly Func<string, int> _read;
        private readonly Dictionary<string, int> _previous;
        private readonly Dictionary<string, int> _recent;

        private readonly Queue<CreditDay> _history =
            new Queue<CreditDay>();

        private int _lastDay;

        public RollingCapitalGains(
            IEnumerable<string> ids,
            Func<string, int> readCapital,
            int initialDay)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            _read = readCapital ??
                throw new ArgumentNullException(
                    nameof(readCapital));

            if (initialDay < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(initialDay));

            _previous = new Dictionary<string, int>(
                StringComparer.Ordinal);

            _recent = new Dictionary<string, int>(
                StringComparer.Ordinal);

            foreach (string id in ids)
            {
                _previous.Add(id, _read(id));
                _recent.Add(id, 0);
            }

            _lastDay = initialDay;
        }

        public void RecordDay(int day)
        {
            if (day <= _lastDay)
                throw new ArgumentOutOfRangeException(
                    nameof(day),
                    "Days must advance strictly.");

            foreach (string id in _previous.Keys.ToArray())
            {
                int capital = _read(id);
                int gain = Math.Max(
                    0, capital - _previous[id]);

                _previous[id] = capital;

                if (gain == 0)
                    continue;

                _recent[id] = checked(
                    _recent[id] + gain);

                _history.Enqueue(new CreditDay
                {
                    Day = day,
                    Node = id,
                    Gain = gain
                });
            }

            _lastDay = day;

            // A 30-day window includes days
            // currentDay - 29 through currentDay.
            while (_history.Count > 0 &&
                   day - _history.Peek().Day >= 30)
            {
                var old = _history.Dequeue();
                _recent[old.Node] -= old.Gain;
            }
        }

        public IReadOnlyDictionary<string, int>
            RecentGains => _recent;
    }

    public sealed class ResearchLayoutPlan
    {
        public readonly Dictionary<string, Vector2> Targets =
            new Dictionary<string, Vector2>(
                StringComparer.Ordinal);

        public readonly Dictionary<string, string> Groups =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        public readonly Dictionary<string, float> GroupRadii =
            new Dictionary<string, float>(
                StringComparer.Ordinal);
    }

    public static class ResearchLayoutModel
    {
        public static Dictionary<string, float>
            CapitalSizeFactors(
                IReadOnlyDictionary<string, int> capital)
        {
            if (capital == null)
                throw new ArgumentNullException(
                    nameof(capital));

            var values = capital.Values
                .Distinct()
                .OrderBy(v => v)
                .ToArray();

            var result = new Dictionary<string, float>(
                StringComparer.Ordinal);

            foreach (var pair in capital)
            {
                if (values.Length == 1)
                {
                    result[pair.Key] = 1f;
                    continue;
                }

                int rank = Array.BinarySearch(
                    values, pair.Value);

                result[pair.Key] = Mathf.Lerp(
                    0.82f,
                    1.18f,
                    rank / (float)(values.Length - 1));
            }

            return result;
        }

        public static ResearchLayoutPlan Build(
            IReadOnlyList<string> nodeIds,
            IReadOnlyDictionary<string, int> gains30,
            IEnumerable<ResearchMail> messages,
            int currentDay,
            float outerRadius)
        {
            if (nodeIds == null)
                throw new ArgumentNullException(
                    nameof(nodeIds));

            if (gains30 == null)
                throw new ArgumentNullException(
                    nameof(gains30));

            if (messages == null)
                throw new ArgumentNullException(
                    nameof(messages));

            if (outerRadius <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(outerRadius));

            int n = nodeIds.Count;

            var indices = new Dictionary<string, int>(
                StringComparer.Ordinal);

            for (int i = 0; i < n; ++i)
                indices.Add(nodeIds[i], i);

            var last = new Dictionary<(int, int), int>();

            // Only communications from the last
            // 90 simulation days form cluster links.
            foreach (var message in messages)
            {
                if (message.Day > currentDay ||
                    currentDay - message.Day > 90 ||
                    !indices.TryGetValue(
                        message.From, out int from) ||
                    !indices.TryGetValue(
                        message.To, out int to) ||
                    from == to)
                {
                    continue;
                }

                var key = (from, to);

                if (!last.TryGetValue(
                        key, out int old) ||
                    old < message.Day)
                {
                    last[key] = message.Day;
                }
            }

            var mutual = new bool[n, n];

            foreach (var pair in last.Keys)
            {
                if (pair.Item1 < pair.Item2 &&
                    last.ContainsKey(
                        (pair.Item2, pair.Item1)))
                {
                    mutual[pair.Item1, pair.Item2] = true;
                    mutual[pair.Item2, pair.Item1] = true;
                }
            }

            // A single mutual bridge must not merge
            // two otherwise established communities.
            var cut = new List<(int, int)>();

            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                if (!mutual[i, j])
                    continue;

                // A common neighbor proves that this
                // edge is not a bridge.
                bool triangle = false;

                for (int k = 0; k < n; k++)
                {
                    if (mutual[i, k] && mutual[k, j])
                    {
                        triangle = true;
                        break;
                    }
                }

                if (triangle)
                    continue;

                int left = ReachableCount(
                    mutual, i, i, j, n,
                    out var visited);

                if (visited[j])
                    continue;

                int right = ReachableCount(
                    mutual, j, i, j, n,
                    out _);

                if (left >= 2 && right >= 2)
                    cut.Add((i, j));
            }

            foreach (var edge in cut)
            {
                mutual[edge.Item1, edge.Item2] = false;
                mutual[edge.Item2, edge.Item1] = false;
            }

            // Deterministic connected components.
            var groups = new List<List<int>>();
            var seen = new bool[n];

            for (int i = 0; i < n; i++)
            {
                if (seen[i])
                    continue;

                var members = new List<int>();
                var queue = new Queue<int>();

                queue.Enqueue(i);
                seen[i] = true;

                while (queue.Count > 0)
                {
                    int u = queue.Dequeue();
                    members.Add(u);

                    for (int v = 0; v < n; v++)
                    {
                        if (mutual[u, v] && !seen[v])
                        {
                            seen[v] = true;
                            queue.Enqueue(v);
                        }
                    }
                }

                members.Sort();
                groups.Add(members);
            }

            // Last 30 days' combined capital gain
            // determines each group's target radius.
            var totals = new int[groups.Count];
            int maximum = 0;

            for (int g = 0; g < groups.Count; g++)
            {
                foreach (int member in groups[g])
                {
                    if (gains30.TryGetValue(
                        nodeIds[member], out int value))
                    {
                        totals[g] = checked(
                            totals[g] +
                            Math.Max(0, value));
                    }
                }

                maximum = Math.Max(
                    maximum, totals[g]);
            }

            var plan = new ResearchLayoutPlan();

            for (int g = 0; g < groups.Count; g++)
            {
                var members = groups[g];

                string groupId = string.Join(
                    "|",
                    members.Select(i => nodeIds[i]));

                float inward = maximum == 0
                    ? 0f
                    : Mathf.Sqrt(
                        totals[g] / (float)maximum);

                float radius = outerRadius *
                    (1f - 0.57f * inward);

                // Preserve the orientation of the
                // original circular arrangement.
                Vector2 heading = Vector2.zero;

                foreach (int i in members)
                {
                    float angle =
                        Mathf.PI * 0.5f +
                        i * Mathf.PI * 2f /
                        Math.Max(1, n);

                    heading += new Vector2(
                        Mathf.Cos(angle),
                        Mathf.Sin(angle));
                }

                if (heading.sqrMagnitude < 0.0001f)
                {
                    float angle =
                        Mathf.PI * 0.5f +
                        members[0] * Mathf.PI * 2f /
                        Math.Max(1, n);

                    heading = new Vector2(
                        Mathf.Cos(angle),
                        Mathf.Sin(angle));
                }

                Vector2 center =
                    heading.normalized * radius;

                plan.GroupRadii[groupId] = radius;

                // Deterministic local spread:
                // no pairwise physics simulation.
                for (int k = 0; k < members.Count; k++)
                {
                    int index = members[k];
                    Vector2 target = center;

                    if (members.Count > 1)
                    {
                        float spread = Mathf.Min(
                            outerRadius * 0.22f,
                            0.65f *
                            Mathf.Sqrt(members.Count));

                        float angle =
                            k * 2f * Mathf.PI /
                            members.Count;

                        target += new Vector2(
                            Mathf.Cos(angle),
                            Mathf.Sin(angle)) * spread;
                    }

                    if (target.magnitude > outerRadius)
                    {
                        target =
                            target.normalized * outerRadius;
                    }

                    plan.Targets[nodeIds[index]] = target;
                    plan.Groups[nodeIds[index]] = groupId;
                }
            }

            return plan;
        }

        private static int ReachableCount(
            bool[,] graph,
            int start,
            int excludedA,
            int excludedB,
            int count,
            out bool[] visited)
        {
            visited = new bool[count];

            var queue = new Queue<int>();

            visited[start] = true;
            queue.Enqueue(start);

            int reached = 0;

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                reached++;

                for (int v = 0; v < count; v++)
                {
                    if ((u == excludedA &&
                         v == excludedB) ||
                        (u == excludedB &&
                         v == excludedA) ||
                        !graph[u, v] ||
                        visited[v])
                    {
                        continue;
                    }

                    visited[v] = true;
                    queue.Enqueue(v);
                }
            }

            return reached;
        }
    }
}
