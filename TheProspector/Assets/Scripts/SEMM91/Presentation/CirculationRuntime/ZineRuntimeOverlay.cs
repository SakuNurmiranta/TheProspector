
using SEMM91.Domain;
using UnityEngine;

namespace SEMM91.Presentation
{
    public sealed class ZineRuntimeOverlay : MonoBehaviour
    {
        [SerializeField]
        private bool showOverlay = true;

        [SerializeField]
        private Vector2 screenPosition =
            new Vector2(12f, 12f);

        private DemoSession _session;
        private ZineRuntimeSnapshot _snapshot;

        private int _sampledDay = -1;
        private string _displayText = "";

        public ZineRuntimeSnapshot CurrentSnapshot =>
            _snapshot;

        public void Bind(DemoSession session)
        {
            _session = session;
            _snapshot = null;
            _sampledDay = -1;
            _displayText = "";

            if (_session != null)
                RefreshNow();
        }

        public void RefreshNow()
        {
            if (_session == null)
                return;

            int previousIssues =
                _snapshot?.PublishedIssueCount ?? 0;

            _snapshot =
                ZineRuntimeSnapshot.Capture(_session);

            _sampledDay = _snapshot.Day;

            _displayText =
                "Published issues: " +
                    _snapshot.PublishedIssueCount +
                "  (later: " +
                    _snapshot.LaterIssueCount + ")\n" +

                "Later-issue readers: " +
                    _snapshot.LaterIssueReaders +
                "  Reader/issue pairs: " +
                    _snapshot.LaterIssueReadEvents + "\n" +

                "Printed copies: " +
                    _snapshot.LaterIssuePrintedCopies +
                "  Layer 2 inserts: " +
                    _snapshot.LaterIssuePostalAttachments + "\n" +

                "Opened inserts: " +
                    _snapshot.LaterIssueOpenedInserts + "\n" +

                "Published addresses: " +
                    _snapshot.PublishedAddresses + "\n" +

                "Contact descriptions: " +
                    _snapshot.PublishedContactDescriptions +
                "  Demo advertisements: " +
                    _snapshot.PublishedDemoAdvertisements + "\n" +

                "Later-issue first contacts: " +
                    _snapshot.LaterIssueContactsSent;

            if (previousIssues > 0 &&
                _snapshot.PublishedIssueCount >
                    previousIssues)
            {
                Debug.Log(
                    "[SEMM91] Zine publication: " +
                    "day=" + _snapshot.Day +
                    " issues=" +
                    _snapshot.PublishedIssueCount +
                    " readers=" +
                    _snapshot.LaterIssueReaders);
            }
        }

        private void Update()
        {
            if (_session == null)
                return;

            int day = _session.Day;

            int currentIssues =
                1 + (_session.RecurringZines?
                    .Issues.Count ?? 0);

            bool issueChanged =
                _snapshot == null ||
                currentIssues !=
                    _snapshot.PublishedIssueCount;

            bool sampleDue =
                _sampledDay < 0 ||
                day < _sampledDay ||
                day - _sampledDay >= 10;

            if (issueChanged || sampleDue)
                RefreshNow();
        }

        private void OnGUI()
        {
            if (!showOverlay || _snapshot == null)
                return;

            float x = screenPosition.x;
            float y = screenPosition.y;

            var box = new Rect(x, y, 390f, 182f);

            GUI.Box(box, "SEMM91 — Zine circulation");

            GUI.Label(
                new Rect(
                    x + 10f,
                    y + 24f,
                    370f,
                    18f),
                "Simulation day: " + _session.Day);

            GUI.Label(
                new Rect(
                    x + 10f,
                    y + 43f,
                    370f,
                    132f),
                _displayText);
        }
    }
}
