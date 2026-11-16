
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Turns physically received and listened demos into
    /// editorially approved zine advertisements.
    ///
    /// This collector never reads sender preferences.
    /// It does not manufacture demos, mail or contacts.
    /// </summary>
    public sealed class ZineEditorialDemoReviewCollector
        : IDisposable
    {
        private sealed class PendingReview
        {
            public string SenderNodeId;
            public string DemoId;
        }

        private readonly ZineEditorialCycle _editorial;
        private readonly ConfigurableCirculationScenario _scenario;
        private readonly SixAxisProfileCatalog _profiles;

        private readonly SixAxisProfile _editorialIdeal;
        private readonly int _minimumMatchingAxes;

        private readonly List<PendingReview> _pending =
            new List<PendingReview>();

        private readonly HashSet<(string Sender, string Demo)>
            _seenPairs =
                new HashSet<(string Sender, string Demo)>();

        private readonly HashSet<string> _seenPackages =
            new HashSet<string>(StringComparer.Ordinal);

        private bool _disposed;

        public int CapturedDemoLetters { get; private set; }
        public int ReviewedDemoCount { get; private set; }
        public int AcceptedReviewCount { get; private set; }
        public int RejectedReviewCount { get; private set; }

        public int PendingReviewCount => _pending.Count;

        public ZineEditorialDemoReviewCollector(
            ZineEditorialCycle editorial,
            ConfigurableCirculationScenario scenario,
            SixAxisProfileCatalog profiles,
            int minimumMatchingAxes = 4)
        {
            _editorial = editorial ??
                throw new ArgumentNullException(
                    nameof(editorial));

            _scenario = scenario ??
                throw new ArgumentNullException(
                    nameof(scenario));

            _profiles = profiles ??
                throw new ArgumentNullException(
                    nameof(profiles));

            if (minimumMatchingAxes < 0 ||
                minimumMatchingAxes > SixAxisProfile.AxisCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumMatchingAxes));
            }

            if (!_profiles.TryGetProfile(
                    SixAxisProfileRole.EditorialPreference,
                    _editorial.EditorNodeId,
                    out _editorialIdeal))
            {
                throw new InvalidOperationException(
                    "The zine editor needs an initialized " +
                    "editorial six-axis preference.");
            }

            _minimumMatchingAxes = minimumMatchingAxes;

            _scenario.Exchange.MessageOpened +=
                OnMessageOpened;

            _scenario.World.DayCompleted +=
                OnDayCompleted;
        }

        private void OnMessageOpened(BacklogItem item)
        {
            if (_disposed || item == null)
                return;

            var package = item.Package;

            if (item.RecipientNodeId !=
                _editorial.EditorNodeId)
            {
                return;
            }

            if (package.TargetNodeId !=
                _editorial.EditorNodeId)
            {
                return;
            }

            // Only actual physical demo submissions.
            if (package.CassetteCopyId == null ||
                string.IsNullOrWhiteSpace(
                    package.PayloadProductId))
            {
                return;
            }

            if (!_seenPackages.Add(package.Id))
                return;

            CapturedDemoLetters++;

            var pair = (
                Sender: package.SourceNodeId,
                Demo: package.PayloadProductId);

            // Once this sender/demo pair has been
            // reviewed, later copies do not re-advertise it.
            if (!_seenPairs.Add(pair))
                return;

            _pending.Add(new PendingReview
            {
                SenderNodeId = pair.Sender,
                DemoId = pair.Demo
            });
        }

        private void OnDayCompleted(int day)
        {
            if (_disposed || _pending.Count == 0)
                return;

            var completed = new List<PendingReview>();

            foreach (var review in _pending)
            {
                var listening =
                    _scenario.GetListeningStatus(
                        _editorial.EditorNodeId,
                        review.DemoId);

                if (listening == null ||
                    listening.Phase !=
                        ListeningPhase.Completed)
                {
                    continue;
                }

                // The source is the observable sonic
                // profile of a demo the editor has heard.
                // Never query PersonalPreference here.
                if (!_profiles.TryGetProfile(
                        SixAxisProfileRole.DemoAtmosphere,
                        review.DemoId,
                        out var atmosphere))
                {
                    continue;
                }

                int matches =
                    _editorialIdeal.MatchingAxes(
                        atmosphere);

                ReviewedDemoCount++;

                if (matches >= _minimumMatchingAxes)
                {
                    // A published claim is associated with
                    // the actual person who sent this demo.
                    _editorial.RecordDemoAdvertisement(
                        review.SenderNodeId,
                        review.DemoId,
                        atmosphere);

                    AcceptedReviewCount++;
                }
                else
                {
                    RejectedReviewCount++;
                }

                completed.Add(review);
            }

            foreach (var review in completed)
                _pending.Remove(review);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _scenario.Exchange.MessageOpened -=
                OnMessageOpened;

            _scenario.World.DayCompleted -=
                OnDayCompleted;

            _disposed = true;
        }
    }
}
